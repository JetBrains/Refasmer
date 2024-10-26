using System.Text;
using Medallion.Shell;
using Mono.Cecil;

namespace JetBrains.Refasmer.Tests;

public class IntegrationTests
{
    [TestCase("RefasmerTestAssembly.PublicClassWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.PublicStructWithPrivateFields")]
    [TestCase("RefasmerTestAssembly.UnsafeClassWithFunctionPointer")]
    public async Task CheckRefasmedType(string typeName)
    {
        var assemblyPath = await BuildTestAssembly();
        var resultAssembly = RefasmTestAssembly(assemblyPath);
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

    private static string RefasmTestAssembly(string assemblyPath)
    {
        var tempLocation = Path.GetTempFileName();
        var exitCode = Program.Main(new[] { $"-v", $"--output={tempLocation}", assemblyPath });
        Assert.That(exitCode, Is.EqualTo(0));

        return tempLocation;
    }

    private static Task VerifyTypeContent(string assemblyPath, string typeName)
    {
        var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        var type = assembly.MainModule.GetType(typeName);
        var printout = new StringBuilder();
        Printer.PrintType(type, printout);

        var verifySettings = new VerifySettings();
        verifySettings.DisableDiff();
        verifySettings.UseDirectory("data");
        verifySettings.UseParameters(typeName);
        return Verify(printout, verifySettings);
    }
}