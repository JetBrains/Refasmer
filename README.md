# Refasmer

Simple tool to make reference assembly. Strips method bodies, private class fields etc. Also can generate xml files like FrameworkList.xml and dump some dll meta info to JSON.
Also can be used as library.

## Usage:
```
RefasmerExe.exe [options] <dll> [<dll> ...]
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

## Examples:

```RefasmerExe.exe -v -O ref -c *.dll```

will handle all DLLs in current dir continuing on errors. Output dlls will be placed to **./ref** directory

```RefasmerExe.exe -l -a Redist="Microsoft-Windows-CLRCoreComp.3.5" -a Name=".NET Framework 3.5" -a RuntimeVersion="3.5" -a ShortName="Full" *.dll > FrameworkList.xml```

will generate FrameworkList for all DLLs in current dir with root tag

```xml
<FileList Redist="Microsoft-Windows-CLRCoreComp.3.5" Name=".NET Framework 3.5" RuntimeVersion="3.5" ShortName="Full">
```

## Links

* [Reference assembly specs](https://docs.microsoft.com/en-us/dotnet/standard/assembly/reference-assemblies)
