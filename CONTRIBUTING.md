Contributor Guide
=================

Prerequisites
-------------
.NET SDK 7.0 is required to build and run the project.

Building
--------
To build the project, execute the following shell command:
```console
$ cd src && dotnet build
```

Tests
-----
To run the tests, execute the following shell command:
```console
$ cd src && dotnet test
```

If you made changes and want to update the test data, run the following shell command (PowerShell Core is required):
```console
$ pwsh ./scripts/Approve-TestResults.ps1
```
