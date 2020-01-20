# Refasmer

Simple tool to make reference assembly. Strips method bodies, private class fields etc. Also can generate xml files like FrameworkList.xml and dump some dll meta info to JSON.

## Usage:
```
dotnet Refasmer.dll [options] <dll to strip> [<dll to strip> ...]
Options:
  -v                         increase verbosity
  -q, --quiet                be quiet
  -h, --help                 show help
  -c, --continue             continue on errors
  -O, --outputdir=VALUE      set output directory
  -o, --output=VALUE         set output file, for single file only
  -r, --refasm               make reference assembly, default action
  -p, --pestrip              strip native PE resources
  -w, --overwrite            overwrite source files
  -e, --refpath=VALUE        add reference path
  -s, --sysrefpath           use system reference path
  -d, --dump                 dump assembly meta info
  -l, --list                 make file list xml
  -a, --attr=VALUE           add FileList tag attribute
  ```

## Examples:

```dotnet Refasmer.dll -v -O ref -c -p *.dll```

will handle all DLLs in current dir continuing on errors and will try to strip native resources. Output dlls will be placed to **./ref** directory

```dotnet Refasmer.dll -l -a Redist="Microsoft-Windows-CLRCoreComp.3.5" -a Name=".NET Framework 3.5" -a RuntimeVersion="3.5" -a ShortName="Full" *.dll > FrameworkList.xml```

will generate FrameworkList for all DLLs in current dir with root tag

```xml
<FileList Redist="Microsoft-Windows-CLRCoreComp.3.5" Name=".NET Framework 3.5" RuntimeVersion="3.5" ShortName="Full">
```


```dotnet Refasmer.dll -d TestDll.dll```

will dump dll metainfo to stdout

## Links 

* [Reference assembly specs](https://docs.microsoft.com/en-us/dotnet/standard/assembly/reference-assemblies)
