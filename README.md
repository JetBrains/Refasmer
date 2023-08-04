# Refasmer [![official JetBrains project](https://jb.gg/badges/official.svg)](https://confluence.jetbrains.com/display/ALL/JetBrains+on+GitHub)

Simple tool to make reference assembly. Strips method bodies, private class fields etc. Also can generate xml files like FrameworkList.xml.
Can be used as library (Refasmer.dll).

## Installation
You could download refasmer from GitHub: https://github.com/JetBrains/Refasmer/releases or install as .NET Tool:
```dotnet tool install -g JetBrains.Refasmer.CliTool```

### NuGet packages

| Package                        | Version                                                                                                                          |
|--------------------------------|----------------------------------------------------------------------------------------------------------------------------------|
| **JetBrains.Refasmer**         | [![Nuget](https://img.shields.io/nuget/v/JetBrains.Refasmer)](https://www.nuget.org/packages/JetBrains.Refasmer)                 |
| **JetBrains.Refasmer.CliTool** | [![Nuget](https://img.shields.io/nuget/v/JetBrains.Refasmer.CliTool)](https://www.nuget.org/packages/JetBrains.Refasmer.CliTool) |

## Usage:
```
refasmer [options] <dll> [<dll> ...]
Options:
  -v                         increase verbosity
  -q, --quiet                be quiet
  -h, -?, --help             show help
  -c, --continue             continue on errors
  -O, --outputdir=VALUE      set output directory
  -o, --output=VALUE         set output file, for single file only
  -r, --refasm               make reference assembly, default action
  -w, --overwrite            overwrite source files
  -p, --public               drop non-public types even with InternalsVisibleTo
  -i, --internals            import public and internal types
      --all                  ignore visibility and import all
  -m, --mock                 make mock assembly instead of reference assembly
  -n, --noattr               omit reference assembly attribute
  -l, --list                 make file list xml
  -a, --attr=VALUE           add FileList tag attribute
```

(note the executable is called `RefasmerExe.exe` if built locally; `refasmer` is a name of an executable installed by `dotnet tool install`)

Mock assembly throws System.NotImplementedException in each imported method.
Reference assembly contains only type definition and method signatures with no method bodies.

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
