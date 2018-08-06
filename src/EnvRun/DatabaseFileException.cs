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

namespace GriffinPlus.EnvRun
{
	/// <summary>
	/// Exception that is thrown, if there is something wrong with the environment database file.
	/// </summary>
	public class DatabaseFileException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseFileException"/> class.
		/// </summary>
		public DatabaseFileException()
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseFileException"/> class.
		/// </summary>
		/// <param name="message">Message describing the reason why the exception is thrown.</param>
		public DatabaseFileException(string message) : base(message)
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseFileException"/> class.
		/// </summary>
		/// <param name="format">String that is used to format the final message describing the reason why the exception is thrown.</param>
		/// <param name="args">Arguments used to format the final exception message.</param>
		public DatabaseFileException(string format, params object[] args) :
			base(string.Format(format, args))
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseFileException"/> class.
		/// </summary>
		/// <param name="message">Message describing the reason why the exception is thrown.</param>
		/// <param name="ex">Some other exception that caused the exception to be thrown.</param>
		public DatabaseFileException(string message, Exception ex) : base(message, ex)
		{

		}
	}
}
