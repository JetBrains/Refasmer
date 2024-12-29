using System.Globalization;
using System.Text;
using Medallion.Shell;
using Mono.Cecil;

namespace JetBrains.Refasmer.Tests;

public abstract class IntegrationTestBase
{
    protected static async Task<string> BuildTestAssembly()
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

    protected static async Task<string> BuildTestAssemblyWithInternalTypeInPublicApi()
    {
        var assemblyPath = await BuildTestAssembly();
        var internalizedAssemblyPath = Path.ChangeExtension(assemblyPath, ".TypesMarkedInternal.dll");
        MarkTypesInternal(assemblyPath, internalizedAssemblyPath, "ToBeMarkedInternal");
        return internalizedAssemblyPath;
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

    protected static int ExecuteRefasmAndGetExitCode(
        string assemblyPath,
        string outputPath,
        params string[] additionalOptions)
    {
        var args = new List<string>
        {
            "-vvv",
            $"--output={outputPath}"
        };
        args.AddRange(additionalOptions);
        args.Add(assemblyPath);
        return Program.Main(args.ToArray());
    }

    protected static string RefasmTestAssembly(string assemblyPath, bool omitNonApiMembers = false)
    {
        var outputPath = Path.GetTempFileName();
        var options = new[]{ "--omit-non-api-members", omitNonApiMembers.ToString(CultureInfo.InvariantCulture) };
        using var collector = CollectConsoleOutput();
        var exitCode = ExecuteRefasmAndGetExitCode(assemblyPath, outputPath, options);
        Assert.That(
            exitCode,
            Is.EqualTo(0),
            $"Refasmer returned exit code {exitCode}. StdOut:\n{collector.StdOut}\nStdErr: {collector.StdErr}");

        return outputPath;
    }

    protected static Task VerifyTypeContent(string assemblyPath, string typeName) =>
        VerifyTypeContents(assemblyPath, [typeName], parameters: [typeName]);

    protected static Task VerifyTypeContents(string assemblyPath, string[] typeNames, bool assertTypeExists = true, object[]? parameters = null)
    {
        var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        var printout = new StringBuilder();
        foreach (var typeName in typeNames)
        {
            var type = assembly.MainModule.GetType(typeName);
            if (assertTypeExists)
            {
                Assert.That(
                    type,
                    Is.Not.Null,
                    $"Type \"{typeName}\" is not found in assembly \"{assemblyPath}\".");
            }

            if (type != null)
                Printer.PrintType(type, printout);
        }

        var verifySettings = new VerifySettings();
        verifySettings.DisableDiff();
        verifySettings.UseDirectory("data");
        if (parameters != null)
            verifySettings.UseParameters(parameters);
        return Verify(printout, verifySettings);
    }

    private static void MarkTypesInternal(string inputAssemblyPath, string outputAssemblyPath, string typeNameSuffix)
    {
        var anyTypeMathed = false;
        var assemblyDefinition = AssemblyDefinition.ReadAssembly(inputAssemblyPath);
        foreach (var type in assemblyDefinition.MainModule.Types)
        {
            if (type.IsPublic && type.Name.EndsWith(typeNameSuffix))
            {
                type.IsPublic = false;
                type.IsNotPublic = true;
                anyTypeMathed = true;
            }
        }

        if (!anyTypeMathed)
        {
            Assert.Fail(
                $"Unable to find any types with names ending on \"{typeNameSuffix}\" " +
                $"in the assembly \"{inputAssemblyPath}\".");
        }

        assemblyDefinition.Write(outputAssemblyPath);
        assemblyDefinition.Dispose();
    }

    protected class Outputs : IDisposable
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

    protected static Outputs CollectConsoleOutput() => new();

    [TearDown]
    public void TearDown()
    {
        Program.ResetArguments();
    }
}
