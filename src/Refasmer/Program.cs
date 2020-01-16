using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Xml;
using JetBrains.Refasmer.PEHandler;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using Mono.Options;
using PEHandler;

namespace JetBrains.Refasmer
{
    public static class Program
    {
        enum Operation
        {
            MakeRefasm,
            DumpMetainfo,
            MakeXmlList
        }

        
        static bool _overwrite;
        static string _outputDir;
        static string _outputFile;
        static LoggerBase _logger;

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
            var referencePaths = new List<string> {"."};
            var useSystemReferencePath = false;
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
                { "e|refpath=", "add reference path", v => referencePaths.Add(v) },
                { "s|sysrefpath", "use system reference path", v => useSystemReferencePath = v != null },
                { "d|dump", "dump assembly meta info", v => {  if (v != null) operation = Operation.DumpMetainfo; } },

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
                _logger.Trace($"Program arguments: {string.Join(" ", args)}");

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
                
                _logger.Info($"Processing {inputs.Count} assemblies");
                foreach (var input in inputs)
                {
                    _logger.Info($"Processing {input}");
                    using (_logger.WithLogPrefix($"[{Path.GetFileName(input)}]"))
                    {
                        AssemblyDefinition assembly;

                        try
                        {
                            _logger.Trace("Reading assembly");
                            var module = ModuleDefinition.ReadModule(input, new ReaderParameters {AssemblyResolver = resolver});

                            if ((module.Attributes & ModuleAttributes.ILOnly) == 0)
                            {
                                _logger.Error("Mixed-mode assemblies is not supported");
                                
                                if (continueOnErrors)
                                    continue;
                                return 1;
                            }

                            assembly = module.Assembly;

                            if (assembly == null)
                            {
                                _logger.Error("Module format is not supported");
                                if (continueOnErrors)
                                    continue;
                                return 1;
                            }

                        }
                        catch (BadImageFormatException e)
                        {
                            _logger.Error(e.Message);
                            if (continueOnErrors)
                                continue;
                            return 1;
                        }

                        switch (operation)
                        {
                            case Operation.MakeRefasm:
                                MakeRefasm(assembly, input);
                                break;
                            case Operation.DumpMetainfo:
                                DumpAssembly(assembly, input);
                                break;
                            case Operation.MakeXmlList:
                                WriteAssemblyToXml(assembly, xmlWriter);
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

                _logger.Info("All done");
                return 0;
            }
            catch (Exception e)
            {
                _logger.Error($"{e}");
                return 1;
            }
        }

        private static void WriteAssemblyToXml(AssemblyDefinition assembly, XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("File");
            xmlWriter.WriteAttributeString("AssemblyName", assembly.Name.Name);
            xmlWriter.WriteAttributeString("Version", assembly.Name.Version.ToString(4));
            xmlWriter.WriteAttributeString("Culture", string.IsNullOrEmpty(assembly.Name.Culture) ? "neutral" : assembly.Name.Culture);

            
            var sb = new StringBuilder();

            foreach (var b in assembly.Name.PublicKeyToken)
                sb.Append(b.ToString("x2"));
            
            xmlWriter.WriteAttributeString("PublicKeyToken", sb.ToString());

            xmlWriter.WriteAttributeString("InGac", "false");
            xmlWriter.WriteAttributeString("ProcessorArchitecture", "MSIL");
                 
            xmlWriter.WriteEndElement();
            
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
                output = $"{Path.GetFileName(input)}.refasm";
            }

            using var ms = new MemoryStream();
            assembly.Write(ms);
            ms.Seek(0, SeekOrigin.Begin);

            var peFile = new PEFile(ms, 512);

            peFile.RsrcHandler.Root.Entries.Clear();
            peFile.RsrcHandler.Write();
            var bytes = peFile.Write();
            
            ms.Seek(0, SeekOrigin.Begin);
            ms.Write(bytes);
            
            _logger.Trace($"Writing result to {output}");
            if (File.Exists(output))
                File.Delete(output);

            File.WriteAllBytes(output, bytes);
        }
        

        private static void DumpAssembly(AssemblyDefinition assembly, string input)
        {
            var dumper = new AssemblyDumper(_logger);

            var writer = Console.Out;
            
            if (!string.IsNullOrEmpty(_outputFile))
            {
                writer = new StreamWriter(_outputFile);
            }
            else if (!string.IsNullOrEmpty(_outputDir))
            {
                writer = new StreamWriter(Path.Combine(_outputDir, $"{Path.GetFileName(input)}.dump"));
            }

            dumper.DumpAssembly(assembly, writer);
            writer.Flush();
        }
    }
}