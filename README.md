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
refasmer [options] <dll> [<**/*.dll> ...]
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
      --omit-non-api-members=VALUE
                             omit private members and types not participating
                               in the public API (will preserve the empty vs
                               non-empty struct semantics, but might affect
                               unmanaged struct constraint)
  -m, --mock                 make mock assembly instead of reference assembly
  -n, --noattr               omit reference assembly attribute
  -l, --list                 make file list xml
  -a, --attr=VALUE           add FileList tag attribute
  -g, --globs                expand globs internally: ?, *, **
```

(note the executable is called `RefasmerExe.exe` if built locally; `refasmer` is a name of an executable installed by `dotnet tool install`)

Mock assembly throws `System.NotImplementedException` in each imported method.

Reference assembly contains only type definition and method signatures with no method bodies.

By default, if you don't specify any of `--public`, `--internals`, or `--all`, Refasmer will try to detect the refasming mode from the input assembly. If the assembly has an `InternalsVisibleTo` attribute applied to it, then `--internals` will be implicitly applied; otherwise, `--public` will.

> [!IMPORTANT]
> Note that `--omit-non-api-members` performs a nontrivial transformation on the resulting assembly. Normally, a reference assembly should include any types, including private and internal ones, because this is up to the spec. However, in some cases, it is possible to omit private and internal types from the reference assembly, because they are not part of the public API, while preserving some of the value type semantics. In these cases, Refasmer is able to remove these types from the assembly, sometimes emitting synthetic fields in the output type. This will preserve the difference of empty and non-empty struct types, but will not preserve the type blittability (i.e. some types after refasming might obtain the ability to follow the `unmanaged` constraint, even if this wasn't possible before refasming).

If you didn't specify the `--all` option, you must pass either `--omit-non-api-members true` or `--omit-non-api-members false`, to exactly identify the required behavior of refasming.

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
- [Changelog][docs.changelog]
- [Contributor Guide][docs.contributing]
- [License (Apache-2.0)][docs.license]

[docs.changelog]: CHANGELOG.md
[docs.contributing]: CONTRIBUTING.md
[docs.license]: LICENSE
