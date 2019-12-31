using System;
using Microsoft.Extensions.Logging;
using Mono.Cecil;

namespace JetBrains.Refasmer
{
    public static class Program
    {
        private static readonly LoggerBase Logger = new LoggerBase(new VerySimpleLogger(Console.Error, LogLevel.Information));
        
        public static void Main(string[] args)
        {
            var resolver = new AssemblyResolver(new[] { "/tmp/2" }, false);
            var assemblyPath = "/tmp/2/System.Core.dll";

            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters() { AssemblyResolver = resolver});

            var stripper = new AssemblyStripper(Logger);
            
            stripper.MakeRefAssembly(assembly);
            
            assembly.Write("/tmp/2/System.Core.stripped.dll");
            Logger.Debug("All done");
        }
    }
}