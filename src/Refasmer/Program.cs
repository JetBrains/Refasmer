using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using Mono.Options;

namespace JetBrains.Refasmer
{
    public static class Program
    {
        enum Operation
        {
            MakeRefasm,
            DumpMetainfo,
        }


        static bool _overwrite;
        static string _outputDir;
        static string _outputFile;
        static LoggerBase _logger;

        public static int Main(string[] args)
        {
            var inputs = new List<string>();
            var referencePaths = new List<string> {"."};
            var useSystemReferencePath = false;
            var operation = Operation.MakeRefasm;

            var verbosity = LogLevel.Warning;
            var showHelp = false;
            var quiet = false;

            var options = new OptionSet
            {
                { "v", "increase verbosity", v => { if (v != null && verbosity > LogLevel.Trace) verbosity--; } },
                { "q|quiet", "be quiet", v => quiet = v != null },
                { "h|help", "show help", v => { showHelp = v != null; } },
                { "w|overwrite", "overwrite source files", v => _overwrite = v != null },
                { "o|output=", "set output file, for single file only", v => _outputFile = v },
                { "O|outputdir=", "set output directory", v => _outputDir = v },
                { "r|refasm", "make reference assembly, default action", v => {  if (v != null) operation = Operation.MakeRefasm; } },
                { "d|dump", "dump assembly meta info", v => {  if (v != null) operation = Operation.DumpMetainfo; } },
                { "refpath=", "add reference path", v => referencePaths.Add(v) },
                { "sysrefpath", "use system reference path", v => useSystemReferencePath = v != null },
                { "<>", v => inputs.Add(v) },
            };

            options.Parse(args);

            if (quiet)
                verbosity = LogLevel.None;
            
            if (showHelp)
            {
                var selfName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                
                Console.Out.WriteLine($"Usage: {selfName} [options] <dll to strip> [<dll to strip> ...]");
                Console.Out.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            if (!string.IsNullOrEmpty(_outputFile) && inputs.Count > 1)
            {
                Console.Error.WriteLine("Output file should not be specified for many inputs");
                return 2;
            }

            _logger = new LoggerBase(new VerySimpleLogger(Console.Error, verbosity));
            var resolver = new AssemblyResolver(referencePaths, useSystemReferencePath);

            try
            {
                _logger.Info($"Processing {inputs.Count} assemblies");
                foreach (var input in inputs)
                {
                    _logger.Info($"Processing {input}");
                    using (_logger.WithLogPrefix($"[{Path.GetFileName(input)}]"))
                    {
                        _logger.Trace("Reading assembly");
                        var assembly = AssemblyDefinition.ReadAssembly(input, new ReaderParameters {AssemblyResolver = resolver});

                        switch (operation)
                        {
                            case Operation.MakeRefasm:
                                MakeRefasm(assembly, input);
                                break;
                            case Operation.DumpMetainfo:
                                DumpAssembly(assembly, input);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        
                    }
                }
                _logger.Info("All done");
                return 0;
            }
            catch (Exception e)
            {
                _logger.Error($"{e}");
                return 1;
            }
        }

        private static void MakeRefasm(AssemblyDefinition assembly, string input)
        {
            var stripper = new AssemblyStripper(_logger);
            stripper.MakeRefAssembly(assembly);

            string output;

            if (!string.IsNullOrEmpty(_outputFile))
            {
                output = _outputFile;
            }
            else if (!string.IsNullOrEmpty(_outputDir))
            {
                output = Path.Combine(_outputDir, Path.GetFileName(input));
            }
            else if (_overwrite)
            {
                output = input;
            }
            else
            {
                output = $"{Path.GetFileName(input)}.stripped";
            }

            _logger.Trace($"Writing result to {output}");
            assembly.Write(output);
        }

        private static void DumpAssembly(AssemblyDefinition assembly, string input)
        {
            var dumper = new AssemblyDumper(_logger);

            var writer = Console.Out;
            
            if (!string.IsNullOrEmpty(_outputDir))
            {
                writer = new StreamWriter(Path.Combine(_outputDir, $"{Path.GetFileName(input)}.dump"));
            }

            dumper.DumpAssembly(assembly, writer);
            writer.Flush();
        }
    }
}