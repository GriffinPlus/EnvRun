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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GriffinPlus.EnvRun
{
	/// <summary>
	/// The application (entry point, command line processing, usage information).
	/// </summary>
	class Program
	{
		/// <summary>
		/// Exit codes returned by the the application.
		/// </summary>
		internal enum ExitCode
		{
			Success = 0,
			ArgumentError = 1,
			GeneralError = 2,
			FileNotFound = 3,
			EnvRunDatabaseVariableNotSet = 4,
		}

		static async Task<int> Main(string[] args)
		{
			// show usage information, if no arguments are specified
			if (args.Length < 1)
			{
				PrintUsage(Console.Out);
				return (int)ExitCode.Success;
			}

			// split up arguments
			string processPath = args[0];
			string[] processArguments = args.Length > 1 ? args.Skip(1).ToArray() : new string[0];

			// get the path of the environment database file
			string databasePath = Environment.GetEnvironmentVariable("ENVRUN_DATABASE");
			if (string.IsNullOrWhiteSpace(databasePath))
			{
				Console.Error.WriteLine("ERROR: The ENVRUN_DATABASE environment variable is not set.");
				return (int)ExitCode.EnvRunDatabaseVariableNotSet;
			}

			// expand environment variables in the database file path
			databasePath = Environment.ExpandEnvironmentVariables(databasePath);

			// run application
			try
			{
				using (AppCore app = new AppCore(databasePath)) // locks the database file
				{
					int exitCode = await app.RunProcess(processPath, processArguments);
					return exitCode;
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("ERROR: {0}", ex.Message);
				return (int)ExitCode.GeneralError;
			}
		}

		/// <summary>
		/// Writes usage text.
		/// </summary>
		/// <param name="writer">Text writer to use.</param>
		static void PrintUsage(TextWriter writer)
		{
			Version version = Assembly.GetExecutingAssembly().GetName().Version;
			writer.WriteLine("  Griffin+ EnvRun v{0}", version);
			writer.WriteLine("--------------------------------------------------------------------------------");
			writer.WriteLine("  Executes another application and scans its output (stdout/stderr) for certain");
			writer.WriteLine("  key expressions instructing EnvRun to maintain a set of environment");
			writer.WriteLine("  variables for following runs.");
			writer.WriteLine("--------------------------------------------------------------------------------");
			writer.WriteLine();
			writer.WriteLine("  USAGE:");
			writer.WriteLine();
			writer.WriteLine("  Step 1) Set ENVRUN_DATABASE environment variable to the path of the database file.");
			writer.WriteLine("  Step 2) Start application: EnvRun.exe <path> <arguments>");
			writer.WriteLine();
			writer.WriteLine("  The following expressions are recognized in the output:");
			writer.WriteLine("  - @@envrun[set name='<name>' value='<value>']");
			writer.WriteLine("  - @@envrun[reset name='<name>']");
			writer.WriteLine();
			writer.WriteLine("--------------------------------------------------------------------------------");
		}

		/// <summary>
		/// Writes version information.
		/// </summary>
		/// <param name="writer">Text writer to use.</param>
		static void PrintVersion(TextWriter writer)
		{
			Version version = Assembly.GetExecutingAssembly().GetName().Version;
			writer.WriteLine("EnvRun v{0}", version);
		}

	}
}
