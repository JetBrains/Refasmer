using System.Text;
using Medallion.Shell;
using Mono.Cecil;

namespace JetBrains.Refasmer.Tests;

public class IntegrationTests
{
    [TestCase("RefasmerTestAssembly.PublicClassWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.PublicStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.UnsafeClassWithFunctionPointer")]
    [TestCase("RefasmerTestAssembly.StructWithNestedPrivateTypes")]
    [TestCase("RefasmerTestAssembly.BlittableGraph")]
    [TestCase("RefasmerTestAssembly.BlittableStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.NonBlittableStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.NonBlittableGraph")]
    public async Task CheckRefasmedType(string typeName)
    {
        var assemblyPath = await BuildTestAssembly();
        var resultAssembly = RefasmTestAssembly(assemblyPath);
        await VerifyTypeContent(resultAssembly, typeName);
    }
    
    [TestCase("RefasmerTestAssembly.PublicClassWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.PublicStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.UnsafeClassWithFunctionPointer")]
    [TestCase("RefasmerTestAssembly.StructWithNestedPrivateTypes")]
    [TestCase("RefasmerTestAssembly.BlittableGraph")]
    [TestCase("RefasmerTestAssembly.BlittableStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.NonBlittableStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.NonBlittableGraph")]

    public async Task CheckRefasmedTypeOmitNonApi(string typeName)
    {
        var assemblyPath = await BuildTestAssembly();
        var resultAssembly = RefasmTestAssembly(assemblyPath, omitNonApiMembers: true);
        await VerifyTypeContent(resultAssembly, typeName);
    }
    
    private static async Task<string> BuildTestAssembly()
    {
        var root = FindSourceRoot();
        var testProject = Path.Combine(root, "tests/RefasmerTestAssembly/RefasmerTestAssembly.csproj");
        Console.WriteLine($"Building project {testProject}…");
        var result = await Command.Run("dotnet", "build", testProject, "--configuration", "Release").Task;

        Assert.That(
            result.ExitCode,
            Is.EqualTo(0),
            $"Failed to build test assembly, exit code {result.ExitCode}. StdOut:\n{result.StandardOutput}\nStdErr: {result.StandardError}");
        
        return Path.Combine(root, "tests/RefasmerTestAssembly/bin/Release/net6.0/RefasmerTestAssembly.dll");
    }
    
    private static string FindSourceRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "README.md")))
                return current;
            current = Path.GetDirectoryName(current);
        }
        throw new Exception("Cannot find source root.");
    }

    private static string RefasmTestAssembly(string assemblyPath, bool omitNonApiMembers = false)
    {
        var tempLocation = Path.GetTempFileName();
        var args = new List<string>
        {
            "-v",
            $"--output={tempLocation}"
        };
        if (omitNonApiMembers)
        {
            args.Add("--omit-non-api-members");
        }
        args.Add(assemblyPath);
        using var collector = CollectConsoleOutput();
        var exitCode = Program.Main(args.ToArray());
        Assert.That(
            exitCode,
            Is.EqualTo(0), 
            $"Refasmer returned exit code {exitCode}. StdOut:\n{collector.StdOut}\nStdErr: {collector.StdErr}");

        return tempLocation;
    }

    private static Task VerifyTypeContent(string assemblyPath, string typeName)
    {
        var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        var type = assembly.MainModule.GetType(typeName);
        Assert.That(assembly.MainModule.GetType(typeName), Is.Not.Null);
        
        var printout = new StringBuilder();
        Printer.PrintType(type, printout);

        var verifySettings = new VerifySettings();
        verifySettings.DisableDiff();
        verifySettings.UseDirectory("data");
        verifySettings.UseParameters(typeName);
        return Verify(printout, verifySettings);
    }

    private class Outputs : IDisposable
    {
        public StringBuilder StdOut { get; } = new();
        public StringBuilder StdErr { get; } = new();
        
        private readonly TextWriter _oldStdOut;
        private readonly TextWriter _oldStdErr;
        
        public Outputs()
        {
            _oldStdOut = Console.Out;
            _oldStdErr = Console.Error;
            Console.SetOut(new StringWriter(StdOut));
            Console.SetError(new StringWriter(StdErr));
        }
        
        public void Dispose()
        {
            Console.SetError(_oldStdErr);
            Console.SetOut(_oldStdOut);
        }
    }

    private static Outputs CollectConsoleOutput() => new();
}