///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://griffin.plus)
//
// Copyright 2018 Sascha Falk <sascha@falk-online.eu>
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed
// on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for
// the specific language governing permissions and limitations under the License.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GriffinPlus.EnvRun
{
	/// <summary>
	/// The application's core logic.
	/// </summary>
	internal class AppCore : IDisposable
	{
		private static Regex sDatabaseLineRegex = new Regex(@"^\s*(.+?)\s*=\s*'(.*?)'\s*$", RegexOptions.Compiled);
		private static Regex sEnvRunCommandRegex = new Regex(@"@@envrun\[\s*(.+?)\s*]", RegexOptions.Compiled);
		private static Regex sSetVariableCommandRegex = new Regex(@"^set\s*name\s*=\s*'(.+?)'\s*value\s*=\s*'(.*?)'$", RegexOptions.Compiled);
		private static Regex sResetVariableCommandRegex = new Regex(@"^reset\s*name\s*=\s*'(.*?)'$", RegexOptions.Compiled);
		private static Regex sExpandedVariableRegex = new Regex(@"{{\s*(.+?)\s*}}", RegexOptions.Compiled);

		private string mDatabaseFilePath;
		private FileStream mDatabaseFile;
		private Dictionary<string, string> mVariables = new Dictionary<string, string>();

		/// <summary>
		/// Initializes a new instance of the <see cref="AppCore"/> class.
		/// </summary>
		/// <param name="databaseFilePath">Path of the file that stores environment variables.</param>
		public AppCore(string databaseFilePath)
		{
			mDatabaseFilePath = databaseFilePath;

			// create directory the database file resides in, if necessary
			string directory = Path.GetDirectoryName(mDatabaseFilePath);
			if (!string.IsNullOrWhiteSpace(directory))
			{
				Directory.CreateDirectory(directory);
			}

			// read initial set of environment variables
			int lineNumber = 0;
			mDatabaseFile = File.Open(mDatabaseFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
			try
			{
				using (StreamReader reader = new StreamReader(mDatabaseFile, Encoding.UTF8, true, 4096, true))
				{
					while (true)
					{
						// read line
						lineNumber++;
						string line = reader.ReadLine();
						if (line == null) break;

						// parse line
						var match = sDatabaseLineRegex.Match(line);
						if (!match.Success)
						{
							throw new DatabaseFileException("Format error in environment database file, line {0} ({1}).", lineNumber, line);
						}

						// store parsed environment variable value
						string name = match.Groups[1].Value;
						string value = match.Groups[2].Value;
						mVariables[name] = value;
					}
				}
			}
			catch (Exception)
			{
				mDatabaseFile.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Disposes the object releasing any resources.
		/// </summary>
		public void Dispose()
		{
			if (mDatabaseFile != null)
			{
				mDatabaseFile.SetLength(0);
				using (StreamWriter writer = new StreamWriter(mDatabaseFile, Encoding.UTF8))
				{
					foreach (var kvp in mVariables.OrderBy(x => x.Key))
					{
						writer.WriteLine("{0} = '{1}'", kvp.Key, kvp.Value);
					}
				}

				mDatabaseFile.Dispose();
				mDatabaseFile = null;
			}
		}

		/// <summary>
		/// Starts the specified process and scans its output for EnvRun commands.
		/// </summary>
		/// <param name="processPath">Path of the process to start</param>
		/// <param name="processArguments">Arguments to pass to the process.</param>
		public async Task<int> RunProcess(string processPath, params string[] processArguments)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo(processPath);

			List<string> arguments = new List<string>();
			foreach (var argument in processArguments)
			{
				string replaced = argument;

				foreach (Match match in sExpandedVariableRegex.Matches(argument))
				{
					string replacement;
					mVariables.TryGetValue(match.Groups[1].Value, out replacement);
					if (replacement == null)
					{
						replacement = Environment.GetEnvironmentVariable(match.Groups[1].Value);
						if (replacement == null)
						{
							Console.Error.WriteLine("ERROR: Environment variable ({0}) is unknown.", argument);
						}
					}

					if (replacement != null)
					{
						replaced = sExpandedVariableRegex.Replace(replaced, replacement);
					}
				}

				if (replaced.Contains(' '))
				{
					replaced = '"' + replaced + '"';
				}

				arguments.Add(replaced);
			}

			startInfo.Arguments = string.Join(" ", arguments);
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			foreach (var kvp in mVariables) startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
			Process process = new Process();
			process.StartInfo = startInfo;
			process.Start();

			char[] stdoutBuffer = new char[4096];
			StringBuilder stdoutBuilder = new StringBuilder();
			Task<int> stdoutTask = process.StandardOutput.ReadAsync(stdoutBuffer, 0, stdoutBuffer.Length);

			char[] stderrBuffer = new char[4096];
			StringBuilder stderrBuilder = new StringBuilder();
			Task<int> stderrTask = process.StandardError.ReadAsync(stderrBuffer, 0, stderrBuffer.Length);

			while (!process.HasExited)
			{
				await Task.WhenAny(stdoutTask, stderrTask);

				// stdout: forward output of child process to calling process and process lines
				if (stdoutTask.IsCompleted)
				{
					int charCount = stdoutTask.Result;
					for (int i = 0; i < charCount; i++)
					{
						char c = stdoutBuffer[i];
						stdoutBuilder.Append(c);
						if (c == '\n')
						{
							ProcessLine(stdoutBuilder.ToString());
							stdoutBuilder.Clear();
						}
					}

					Console.Out.Write(stdoutBuffer, 0, charCount);
					stdoutTask = process.StandardOutput.ReadAsync(stdoutBuffer, 0, stdoutBuffer.Length);
				}

				// stderr: forward output of child process to calling process and process lines
				if (stderrTask.IsCompleted)
				{
					int charCount = stderrTask.Result;
					for (int i = 0; i < charCount; i++)
					{
						char c = stderrBuffer[i];
						stderrBuilder.Append(c);
						if (c == '\n')
						{
							ProcessLine(stderrBuilder.ToString());
							stderrBuilder.Clear();
						}
					}

					Console.Error.Write(stderrBuffer, 0, charCount);
					stderrTask = process.StandardError.ReadAsync(stderrBuffer, 0, stderrBuffer.Length);
				}
			}

			// process remaining data in stdout
			int count = await stdoutTask;
			stdoutBuilder.Append(stdoutBuffer, 0, count);
			Console.Out.Write(stdoutBuffer, 0, count);
			foreach (string line in stdoutBuilder.ToString().Split('\n'))
			{
				ProcessLine(line + '\n');
			}

			// process remaining data in stderr
			count = await stderrTask;
			stderrBuilder.Append(stderrBuffer, 0, count);
			Console.Error.Write(stderrBuffer, 0, count);
			foreach (string line in stderrBuilder.ToString().Split('\n'))
			{
				ProcessLine(line + '\n');
			}

			return process.ExitCode;
		}

		/// <summary>
		/// Scans the specified line for EnvRun specific key expressions.
		/// </summary>
		/// <param name="line"></param>
		private void ProcessLine(string line)
		{
			var match = sEnvRunCommandRegex.Match(line);
			if (match.Success)
			{
				string command = match.Groups[1].Value;

				match = sSetVariableCommandRegex.Match(command);
				if (match.Success)
				{
					string name = match.Groups[1].Value;
					string value = match.Groups[2].Value;
					mVariables[name] = value;
					return;
				}

				match = sResetVariableCommandRegex.Match(command);
				if (match.Success)
				{
					string name = match.Groups[1].Value;
					mVariables.Remove(name);
					return;
				}

				Console.Error.WriteLine("ERROR: Unknown EnvRun command ({0})", command);
			}
		}

	}
}
