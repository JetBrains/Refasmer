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
Usage: refasmer [options] <dll> [<**/*.dll> ...]
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
      --omit-non-api-types   omit private types not participating in the public
                               API (will transform the private fields of value
                               types to preserve semantics but omit types when
                               possible)
  -m, --mock                 make mock assembly instead of reference assembly
  -n, --noattr               omit reference assembly attribute
  -l, --list                 make file list xml
  -a, --attr=VALUE           add FileList tag attribute
  -g, --globs                expand globs internally: ?, *, **

```

(note the executable is called `RefasmerExe.exe` if built locally; `refasmer` is a name of an executable installed by `dotnet tool install`)

Mock assembly throws `System.NotImplementedException` in each imported method.

Reference assembly contains only type definition and method signatures with no method bodies.

Note that `--omit-non-api-types` performs a nontrivial transformation on the resulting assembly. Normally, a reference assembly should include any types participating as private members of any value type, because this is up to the spec. However, in some cases, it is possible to omit these types from the reference assembly, because they are not part of the public API, while preserving the value type semantics. In these cases, Refasmer is able to remove these types from the assembly, sometimes emitting synthetic fields in the output type, to preserve semantics, such as:
- a value type with non-empty field list should always have non-empty field list, even if all the fields are private,
- a value type's fields, even private ones, control whether the type can be considered as `unmanaged` or not (and thus whether it can fulfill in the corresponding generic constraint).

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

## Documentation
- [Contributor Guide][docs.contributing]
- [License (Apache-2.0)][docs.license]

[docs.contributing]: CONTRIBUTING.md
[docs.license]: LICENSE
