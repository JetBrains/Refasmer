using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Refasmer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace RefasmerTests
{
    public class Tests
    {
        private const string CsFile = "TestDll.cs";
        private const string TestAssemblyName = "TestDll";
        private const string TestAssemblyPath = "TestDll.dll";
        private const string TestRefAssemblyPath = "TestDll.refasm.dll";
        private const string TestRefAssemblyDumpPath = "TestDll.refasm.dll.dump";
        private const string TestRefAssemblyDumpGoldPath = "TestDll.refasm.dll.dump.gold";

        private static bool CheckManagedReference( string dllPath )
        {
            try
            {
                return AssemblyName.GetAssemblyName(dllPath) != null;
            }
            catch (BadImageFormatException)
            {
                // Got unmanaged dll (windows-only case)
                return false;
            }
        }
        
        [SetUp]
        public void Setup()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(CsFile));

            var sdkPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            var sdkDlls = Directory.GetFiles(sdkPath, "*.dll")
                .Where(CheckManagedReference)
                .Select(x => MetadataReference.CreateFromFile(x))
                .ToList();
            
            var compilation = CSharpCompilation.Create(TestAssemblyName, new [] {syntaxTree}, sdkDlls,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var fs = new FileStream(TestAssemblyPath, FileMode.Create, FileAccess.Write);
            var compilationResult = compilation.Emit(fs);

            if (!compilationResult.Success)
            {
                var failures = compilationResult.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                var message = string.Join("\n", failures.Select(f => $"{f.Id}: {f.GetMessage()}"));
                Console.Out.WriteLine(message);
            }
            
            Assert.True(compilationResult.Success);
            if (File.Exists(TestRefAssemblyPath))
                File.Delete(TestRefAssemblyPath);
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void CheckRefAssembly()
        {
            var res = Program.Main(new[] {TestAssemblyPath, "-o", TestRefAssemblyPath});
            Assert.AreEqual(0, res);
            
            res = Program.Main(new[] {"-d", TestRefAssemblyPath, "-o", TestRefAssemblyDumpPath});
            Assert.AreEqual(0, res);

            var gold = File.ReadAllText(TestRefAssemblyDumpGoldPath);
            var dump = File.ReadAllText(TestRefAssemblyDumpPath);
            
            Assert.AreEqual(gold, dump);
        }

    }
}