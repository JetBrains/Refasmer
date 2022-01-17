using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml;

using JetBrains.Refasmer.Filters;

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

using Mono.Options;

namespace JetBrains.Refasmer
{
    public static class Program
    {
        enum Operation
        {
            MakeRefasm,
            MakeXmlList
        }

        
        private static bool _overwrite;
        private static string _outputDir;
        private static string _outputFile;
        private static LoggerBase _logger;
        
        private static bool _public;
        private static bool _internals;
        private static bool _all;

        private static bool _makeMock;
        private static bool _omitReferenceAssemblyAttr;

        private static bool _expandGlobs;

        class InvalidOptionException : Exception
        {
            public InvalidOptionException(string message) : base(message)
            {
            }
        }
        
        private static void AddFileListAttr(string v, Dictionary<string, string> attrs)
        {
            if (!string.IsNullOrEmpty(v))
            {
                var split = v.Split('=');
                if (split.Length == 2)
                {
                    attrs[split[0]] = split[1];
                    return;
                }
            }
            throw new InvalidOptionException("FileList attr should be like name=value");
        }

        public static int Main(string[] args)
        {
            var inputs = new List<string>();
            var operation = Operation.MakeRefasm;

            var verbosity = LogLevel.Warning;
            var showHelp = false;
            var quiet = false;
            var continueOnErrors = false;

            var fileListAttr = new Dictionary<string, string>();
            
            var options = new OptionSet
            {
                { "v", "increase verbosity", v => { if (v != null && verbosity > LogLevel.Trace) verbosity--; } },
                { "q|quiet", "be quiet", v => quiet = v != null },
                { "h|?|help", "show help", v => showHelp = v != null },
                { "c|continue", "continue on errors", v => continueOnErrors = v != null },
                
                { "O|outputdir=", "set output directory", v => _outputDir = v },
                { "o|output=", "set output file, for single file only", v => _outputFile = v },

                { "r|refasm", "make reference assembly, default action", v => {  if (v != null) operation = Operation.MakeRefasm; } },
                { "w|overwrite", "overwrite source files", v => _overwrite = v != null },

                { "p|public", "drop non-public types even with InternalsVisibleTo", v => _public = v != null },
                { "i|internals", "import public and internal types", v => _internals = v != null },
                { "all", "ignore visibility and import all", v => _all = v != null },
                
                { "m|mock", "make mock assembly instead of reference assembly", p => _makeMock = p != null },
                { "n|noattr", "omit reference assembly attribute", p => _omitReferenceAssemblyAttr = p != null },

                { "l|list", "make file list xml", v => {  if (v != null) operation = Operation.MakeXmlList; } },
                { "a|attr=", "add FileList tag attribute", v =>  AddFileListAttr(v, fileListAttr) },
                
                { "g|globs", "expand globs internally: ?, *, **", p => _expandGlobs = p != null },                
                
                { "<>", "one or more input files", v => inputs.Add(v) },
            };

            try
            {
                options.Parse(args);
            }
            catch (InvalidOptionException e)
            {
                Console.Error.WriteLine(e.Message);
                return 1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{e}");
                return 1;
            }

            if (quiet)
                verbosity = LogLevel.None;
            
            if (showHelp || args.Length == 0)
            {
                var selfName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                
                Console.Out.WriteLine($"Usage: {selfName} [options] <dll> [<**/*.dll> ...]");
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

            try
            {
                _logger.Trace?.Invoke($"Program arguments: {string.Join(" ", args)}");

                // Apply input globbing
                var dirCurrent = new DirectoryInfo(Environment.CurrentDirectory);
                var inputsExpanded = inputs.SelectMany(input => ExpandInput(input, dirCurrent, _logger)).OrderBy(t => t.Path, StringComparer.OrdinalIgnoreCase).ToImmutableArray();

                // Re-check for the second time, after expanding globs
                if (!string.IsNullOrEmpty(_outputFile) && inputs.Count > 1)
                {
                    Console.Error.WriteLine("Output file should not be specified for many inputs");
                    return 2;
                }
                
                XmlTextWriter xmlWriter = null;
                
                if (operation == Operation.MakeXmlList)
                {
                    xmlWriter = !string.IsNullOrEmpty(_outputFile) 
                        ? new XmlTextWriter(_outputFile, Encoding.UTF8) 
                        : new XmlTextWriter(Console.Out);
                    
                    xmlWriter.Formatting = Formatting.Indented;  

                    xmlWriter.WriteStartDocument();
                    xmlWriter.WriteStartElement("FileList");

                    foreach (var kv in fileListAttr)
                        xmlWriter.WriteAttributeString(kv.Key, kv.Value);
                }
                
                _logger.Info?.Invoke($"Processing {inputs.Count} assemblies");
                for (var nInput = 0; nInput < inputsExpanded.Length; nInput++)
                {
                    var input = inputsExpanded[nInput];
                    _logger.Info?.Invoke($"Processing {input.Path}");
                    using(_logger.WithLogPrefix($"[{Path.GetFileName(input.RelativeForOutput)}]"))
                    {
                        try
                        {
                            switch (operation)
                            {
                            case Operation.MakeRefasm:
                                MakeRefasm(input);
                                break;
                            case Operation.MakeXmlList:
                                WriteAssemblyToXml(input, xmlWriter);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                            }
                        }
                        catch (InvalidOperationException e)
                        {
                            _logger.Error?.Invoke(e.Message);
                            if (continueOnErrors)
                                continue;
                            if (nInput < inputsExpanded.Length - 1) // When doing multiple files, let user know some might be left undone
                                _logger.Error?.Invoke($"Aborted on first error, {inputsExpanded.Length - nInput + 1:N0} files left unprocessed; pass “--continue” to try them anyway");
                            return 1;
                        }
                    }
                }

                if (xmlWriter != null)
                {
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndDocument();
                    
                    xmlWriter.Close();
                }

                _logger.Info?.Invoke("All done");
                return 0;
            }
            catch (Exception e)
            {
                _logger.Error?.Invoke($"{e}");
                _logger.Error?.Invoke("ABORTED"); // When doing multiple files, let user know some might be left undone
                return 1;
            }
        }

        private static void WriteAssemblyToXml((string Path, string RelativeForOutput) input, XmlTextWriter xmlWriter)
        {
            using var _ = ReadAssembly(input.Path, out var metaReader);
            
            if (!metaReader.IsAssembly)
                return;
            
            var assembly = metaReader.GetAssemblyDefinition();
            
            xmlWriter.WriteStartElement("File");
            xmlWriter.WriteAttributeString("AssemblyName", metaReader.GetString(assembly.Name));
            xmlWriter.WriteAttributeString("Version", assembly.Version.ToString(4));

            var culture = metaReader.GetString(assembly.Culture);
            xmlWriter.WriteAttributeString("Culture", string.IsNullOrEmpty(culture) ? "neutral" : culture);


            var publicKey = metaReader.GetBlobBytes(assembly.PublicKey);
            var publicKeyToken = PublicKeyTokenCalculator.CalculatePublicKeyToken(publicKey); 
            
            var publicKeyTokenStr =  BitConverter.ToString(publicKeyToken).Replace("-", string.Empty).ToLowerInvariant();
            
            xmlWriter.WriteAttributeString("PublicKeyToken", publicKeyTokenStr);

            xmlWriter.WriteAttributeString("InGac", "false");
            xmlWriter.WriteAttributeString("ProcessorArchitecture", "MSIL");
                 
            xmlWriter.WriteEndElement();
        }

        private static void MakeRefasm((string Path, string RelativeForOutput) input)
        {
            IImportFilter filter = null;

            if (_public)
                filter = new AllowPublic();
            else if (_internals)
                filter = new AllowPublicAndInternals();
            else if (_all)
                filter = new AllowAll();
            
            byte[] result;
            using (var peReader = ReadAssembly(input.Path, out var metaReader))
                result = MetadataImporter.MakeRefasm(metaReader, peReader, _logger, filter, _makeMock, _omitReferenceAssemblyAttr);

            string output;

            if (!string.IsNullOrEmpty(_outputFile))
            {
                output = _outputFile;
            }
            else if (!string.IsNullOrEmpty(_outputDir))
            {
                output = Path.Combine(_outputDir, input.RelativeForOutput);
            }
            else if (_overwrite)
            {
                output = input.Path;
            }
            else
            {
                output = $"{input.Path}.{(_makeMock ? "mock" : "refasm")}.dll";
            }
            
            _logger.Debug?.Invoke($"Writing result to {output}");
            
            if (File.Exists(output))
                File.Delete(output);

            var outdir = Path.GetDirectoryName(output);
            
            if (!string.IsNullOrEmpty(outdir))
                Directory.CreateDirectory(outdir);

            File.WriteAllBytes(output, result);
        }
        
        private static PEReader ReadAssembly(string input, out MetadataReader metaReader)
        {
            if(input == null)
                throw new ArgumentNullException(nameof(input));
            _logger.Debug?.Invoke($"Reading assembly {input}");
            
            // stream closed by memory block provider within PEReader when the latter is disposed of 
            var peReader = new PEReader(new FileStream(input, FileMode.Open) /* stream closed by memory block provider within PEReader when the latter is disposed of */); 
            metaReader = peReader.GetMetadataReader();

            if (!metaReader.IsAssembly)
                _logger.Warning?.Invoke($"Dll has no assembly: {input}");
            return peReader;
        }

        private static ImmutableArray<(string Path, string RelativeForOutput)> ExpandInput(string input, DirectoryInfo baseForRelativeInput, LoggerBase logger)
        {
            if (!_expandGlobs)
                return ImmutableArray.Create((input, Path.GetFileName(input)));

            // Is this item globbing?
            var indexOfGlob = input.IndexOfAny(new[] {'*', '?'});
            if (indexOfGlob < 0)
                return ImmutableArray.Create((input, Path.GetFileName(input)));

            // Cut into non-globbing base dir and globbing mask (if input is an abs path, otherwise we use currentdir)
            DirectoryInfo basedir;
            var indexOfSepBeforeGlob = input.LastIndexOfAny(new[] {'\\', '/'}, indexOfGlob, indexOfGlob);
            string mask;
            if(indexOfSepBeforeGlob >= 0)
            {
                var beforemask = input.Substring(0, indexOfSepBeforeGlob + 1);
                basedir = Path.IsPathRooted(beforemask) ? new DirectoryInfo(beforemask) : new DirectoryInfo(Path.Combine(baseForRelativeInput.FullName, beforemask));
                mask = input.Substring(indexOfSepBeforeGlob + 1);
            }
            else
            {
                basedir = baseForRelativeInput;
                mask = input;
            }

            var result = new Matcher(StringComparison.OrdinalIgnoreCase).AddInclude(mask).Execute(new DirectoryInfoWrapper(basedir));
            if(!result.HasMatches)
                throw new InvalidOperationException($"The pattern “{input}” didn't match any files at all. Were looking for “{mask}” under the “{basedir.FullName}” directory.");

            var expanded = result.Files.Select(match => (Path.Combine(basedir.FullName, match.Path), match.Path)).ToImmutableArray();
            
            logger.Info?.Invoke($"Expanded “{input}” into {expanded.Length:N0} files.");
            return expanded;
        }
   }
}