# Refasmer

Simple tool to make reference assembly. Strips method bodies, private class fields etc. Also can generate xml files like FrameworkList.xml.
Can be used as library (Refasmer.dll).

## Installation
You could download refasmer from GitHub: https://github.com/JetBrains/Refasmer/releases or install as .NET Tool:
```dotnet tool install -g JetBrains.Refasmer.CliTool```

## Usage:
```
refasmer [options] <dll> [<dll> ...]
Options:
  -v                         increase verbosity
  -q, --quiet                be quiet
  -h, --help                 show help
  -c, --continue             continue on errors
  -O, --outputdir=VALUE      set output directory
  -o, --output=VALUE         set output file, for single file only
  -r, --refasm               make reference assembly, default action
  -w, --overwrite            overwrite source files
  -l, --list                 make file list xml
  -a, --attr=VALUE           add FileList tag attribute
```

(note the executable is called `RefasmerExe` if built locally; `refasmer` is a name of an executable installed by `dotnet tool install`)

## Examples:

```refasmer -v -O ref -c a.dll b.dll c.dll```

will handle all passed DLL files continuing on errors. Output dlls will be placed to **./ref** directory

```refasmer -l -a Redist="Microsoft-Windows-CLRCoreComp.3.5" -a Name=".NET Framework 3.5" -a RuntimeVersion="3.5" -a ShortName="Full" a.dll b.dll c.dll > FrameworkList.xml```

will generate FrameworkList for all passed DLL files with root tag

```xml
<FileList Redist="Microsoft-Windows-CLRCoreComp.3.5" Name=".NET Framework 3.5" RuntimeVersion="3.5" ShortName="Full">
```

## Links

* [Reference assembly specs](https://docs.microsoft.com/en-us/dotnet/standard/assembly/reference-assemblies)
