# EnvRun

[![Build (master)](https://img.shields.io/appveyor/ci/ravenpride/envrun/master.svg?logo=appveyor)](https://ci.appveyor.com/project/ravenpride/envrun/branch/master)


## Overview

*EnvRun* is a small tool that assists with running processes that need to exchange environment variables. By default processes are isolated from each other. Child processes inherit environment variables from their parent processes, but child processes are not allowed to set environment variables on parent processes. Therefore environment variables cannot be passed across processes. *EnvRun* helps to close this gap by wrapping the execution of processes. *EnvRun* scans the output of wrapped processes for key expressions that set/reset specific environment variables to pass to other processes executed by *EnvRun*. The following key expressions are recognized:

- `@@envrun[set name='<NAME>' value='<VALUE>']`
- `@@envrun[reset name='<NAME>']`

The next time *EnvRun* starts a process it explicitly sets these environment variables making them available just as inherited environment variables.

*EnvRun* is particularly useful on build servers that do not support setting environment variables dynamically (like GoCD).


## Usage

### Step 1) Set Location of the EnvRun Database File (Optional)

By default *EnvRun* puts its database file into the working directory naming it `envrun.db`. If you need to put the file somewhere else you can set the `ENVRUN_DATABASE` environment variable to the appropriate location.

### Step 2) Start Process

To start a new process you only need to pass the path of the process and command line arguments to *EnvRun*:
```
EnvRun.exe <MyApp.exe> <Args>
```
