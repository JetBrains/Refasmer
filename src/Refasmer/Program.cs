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
        public static int Main(string[] args)
        {
            var verbosity = LogLevel.Warning;
            var showHelp = false;
            var quiet = false;

            var inputs = new List<string>();
            var referencePaths = new List<string> {"."};
            var useSystemReferencePath = false;
            var overwrite = false;
            string outputDir = null;
            
            var options = new OptionSet
            {
                { "v", "increase verbosity", v => { if (v != null && verbosity > LogLevel.Trace) verbosity--; } },
                { "q|quiet", "be quiet", v => quiet = v != null },
                { "h|help", "show help", v => { showHelp = v != null; } },
                { "w|overwrite", "overwrite source files", v => overwrite = v != null },
                { "o|output=", "set output directory", v => outputDir = v },
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

            var logger = new LoggerBase(new VerySimpleLogger(Console.Error, verbosity));
            var resolver = new AssemblyResolver(referencePaths, useSystemReferencePath);

            try
            {
                logger.Info($"Stripping {inputs.Count} assemblies");
                foreach (var input in inputs)
                {
                    logger.Info($"Processing {input}");
                    using (logger.WithLogPrefix($"[{Path.GetFileName(input)}]"))
                    {
                        logger.Trace("Reading assembly");
                        var assembly = AssemblyDefinition.ReadAssembly(input, new ReaderParameters {AssemblyResolver = resolver});
                        var stripper = new AssemblyStripper(logger);
                        
                        stripper.MakeRefAssembly(assembly);

                        string output;

                        if (overwrite)
                        {
                            output = input;
                        }
                        else if (!string.IsNullOrEmpty(outputDir))
                        {
                            output = Path.Combine(outputDir, Path.GetFileName(input));
                        }
                        else
                        {
                            output = $"{Path.GetFileName(input)}.stripped";
                        }

                        logger.Trace($"Writing result to {output}");
                        assembly.Write(output);
                    }
                }
                logger.Info("All done");
                return 0;
            }
            catch (Exception e)
            {
                logger.Error($"{e}");
                return 1;
            }
        }
    }
}