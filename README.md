# EnvRun

[![Build Status](https://dev.azure.com/griffinplus/EnvRun/_apis/build/status/12?branchName=master)](https://dev.azure.com/griffinplus/EnvRun/_build/latest?definitionId=12&branchName=master)
[![Release](https://img.shields.io/github/release/griffinplus/envrun.svg?logo=github)](https://github.com/GriffinPlus/EnvRun/releases)

## Overview

*EnvRun* is a small tool written in GO that assists with running processes that need to exchange data via environment
variables. By default processes are isolated from each other. Child processes inherit environment variables from their
parent processes, but child processes are not allowed to set environment variables on parent processes. Therefore
environment variables cannot be passed across processes. *EnvRun* helps to close this gap by wrapping the execution of
processes. *EnvRun* scans the output (stdout/stderr) of wrapped processes for key expressions that set/reset specific
environment variables to pass to other processes executed by *EnvRun* afterwards. The following key expressions are
recognized:

- `@@envrun[set name='<NAME>' value='<VALUE>']` : Sets an environment variable.
- `@@envrun[reset name='<NAME>']` : Clears a previously set environment variable.

The next time *EnvRun* starts a process it explicitly sets these environment variables making them available just as
inherited environment variables. If a regularly inherited environment variable and an environment variable set via
*EnvRun* is available, the *EnvRun* variable takes precedence.

*EnvRun* is particularly useful on build servers that do not support setting environment variables dynamically (like
[GoCD](https://www.gocd.org/)). That way tools can publish environment variables for other tools by simply printing to
the console. All tools that should take part in the mechanism must be wrapped by *EnvRun*.

## Releases

*EnvRun* is written in GO which makes it highly portable.

[Downloads](https://github.com/GriffinPlus/EnvRun/releases) are provided for the following combinations of popular
target operating systems and platforms:

- `linux`
  - `386`
  - `amd64`
  - `arm`
  - `arm64`
- `windows`
  - `386`
  - `amd64`

If any other target operating system and/or platform is needed and the combination is supported by GO, please open an
issue and we'll add support for it.

## Usage

### Step 1) Set Location of the EnvRun Database File (Optional)

By default *EnvRun* puts its database file into the working directory naming it `envrun.db`. If you need to put the file
somewhere else you can set the `ENVRUN_DATABASE` environment variable to the appropriate location.

### Step 2) Start Process

To start a new process you simply have to pass the path of the process and command line arguments to *EnvRun*:

```
EnvRun.exe <MyApp.exe> <Args>
```

## Example

The following batch file demonstrates the use of *EnvRun*. You can see multiple outputs in a line and output to *stdout*
and *stderr*, afterwards consuming the published environment variables. Please note that these variables are not
available outside processes wrapped by *EnvRun*. 

```batch
set ENVRUN_DATABASE=.\envrun.db
envrun.exe cmd.exe /C "echo @@envrun[set name='MyVarOnStdout1' value='MyValue1'] @@envrun[set name='MyVarOnStdout2' value='MyValue2']"
envrun.exe cmd.exe /C "echo @@envrun[set name='MyVarOnStderr' value='MyValue3'] 1>&2"
envrun.exe cmd.exe /C "echo MyVar1 = %%MyVarOnStdout1%%, MyVar2 = %%MyVarOnStdout2%%, MyVar3 = %%MyVarOnStderr%%"
```
