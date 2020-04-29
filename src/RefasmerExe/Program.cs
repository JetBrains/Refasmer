using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml;
using JetBrains.Refasmer.Filters;
using Microsoft.Extensions.Logging;
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
        private static bool _stripPrivate;
        private static string _outputDir;
        private static string _outputFile;
        private static LoggerBase _logger;

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
                { "h|help", "show help", v => showHelp = v != null },
                { "c|continue", "continue on errors", v => continueOnErrors = v != null },
                
                { "O|outputdir=", "set output directory", v => _outputDir = v },
                { "o|output=", "set output file, for single file only", v => _outputFile = v },

                { "r|refasm", "make reference assembly, default action", v => {  if (v != null) operation = Operation.MakeRefasm; } },
                { "w|overwrite", "overwrite source files", v => _overwrite = v != null },
                { "s|strip", "strip private fields and methods", v => _stripPrivate = v != null },
                
                { "l|list", "make file list xml", v => {  if (v != null) operation = Operation.MakeXmlList; } },
                { "a|attr=", "add FileList tag attribute", v =>  AddFileListAttr(v, fileListAttr) },
                
                { "<>", v => inputs.Add(v) },
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
            
            if (showHelp)
            {
                var selfName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                
                Console.Out.WriteLine($"Usage: {selfName} [options] <dll> [<dll> ...]");
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

                XmlTextWriter xmlWriter = null;
                
                if (operation == Operation.MakeXmlList)
                {
                    xmlWriter = !string.IsNullOrEmpty(_outputFile) 
                        ? new XmlTextWriter(_outputFile, Encoding.UTF8) 
                        : new XmlTextWriter(Console.Out);
                    
                    xmlWriter.Formatting = Formatting.Indented;  

                    xmlWriter.WriteStartDocument();
                    xmlWriter.WriteStartElement("FileList");

                    foreach (var (key, value) in fileListAttr)
                        xmlWriter.WriteAttributeString(key, value);
                }
                
                _logger.Info?.Invoke($"Processing {inputs.Count} assemblies");
                foreach (var input in inputs)
                {
                    _logger.Info?.Invoke($"Processing {input}");
                    using (_logger.WithLogPrefix($"[{Path.GetFileName(input)}]"))
                    {
                        MetadataReader metaReader;

                        PEReader peReader;
                        try
                        {
                            _logger.Trace?.Invoke("Reading assembly");
                            peReader = new PEReader(new FileStream(input, FileMode.Open)); 
                            metaReader = peReader.GetMetadataReader();

                            if (!metaReader.IsAssembly)
                            {
                                _logger.Error?.Invoke("File format is not supported");
                                if (continueOnErrors)
                                    continue;
                                return 1;
                            }
                        }
                        catch (InvalidOperationException e)
                        {
                            _logger.Error?.Invoke(e.Message);
                            if (continueOnErrors)
                                continue;
                            return 1;
                        }

                        switch (operation)
                        {
                            case Operation.MakeRefasm:
                                MakeRefasm(metaReader, peReader, input);
                                break;
                            case Operation.MakeXmlList:
                                WriteAssemblyToXml(metaReader, xmlWriter);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
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
                return 1;
            }
        }

        private static void WriteAssemblyToXml(MetadataReader metaReader, XmlTextWriter xmlWriter)
        {
            var assembly = metaReader.GetAssemblyDefinition();
            
            xmlWriter.WriteStartElement("File");
            xmlWriter.WriteAttributeString("AssemblyName", metaReader.GetString(assembly.Name));
            xmlWriter.WriteAttributeString("Version", assembly.Version.ToString(4));

            var culture = metaReader.GetString(assembly.Culture);
            xmlWriter.WriteAttributeString("Culture", string.IsNullOrEmpty(culture) ? "neutral" : culture);

            var sb = new StringBuilder();

            foreach (var b in metaReader.GetBlobContent(assembly.PublicKey))
                sb.Append(b.ToString("x2"));
            
            xmlWriter.WriteAttributeString("PublicKeyToken", sb.ToString());

            xmlWriter.WriteAttributeString("InGac", "false");
            xmlWriter.WriteAttributeString("ProcessorArchitecture", "MSIL");
                 
            xmlWriter.WriteEndElement();
        }

        private static void MakeRefasm(MetadataReader metaReader, PEReader peReader, string input )
        {
            var filter = new AllowPublic();
            
            var result = MetadataImporter.MakeRefasm(metaReader, peReader, filter, _logger);
            
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
                output = $"{Path.GetFileName(input)}.refasm.dll";
            }
            
            _logger.Debug?.Invoke($"Writing result to {output}");
            if (File.Exists(output))
                File.Delete(output);

            File.WriteAllBytes(output, result);
        }
   }
}