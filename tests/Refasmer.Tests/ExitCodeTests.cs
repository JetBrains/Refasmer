namespace JetBrains.Refasmer.Tests;

public class ExitCodeTests : IntegrationTestBase
{
    [TestCase]
    public Task OmitNonApiTypesTrue() =>
        DoTest(0, "--omit-non-api-members", "true");

    [TestCase]
    public Task OmitNonApiTypesFalse() =>
        DoTest(0, "--omit-non-api-members", "false");

    [TestCase]
    public Task OmitNonApiTypesMissing() =>
        DoTest(2);

    private async Task DoTest(int expectedCode, params string[] additionalArgs)
    {
        var assemblyPath = await BuildTestAssembly();
        var outputPath = Path.GetTempFileName();
        using var collector = CollectConsoleOutput();
        var exitCode = ExecuteRefasmAndGetExitCode(assemblyPath, outputPath, additionalArgs);
        if (exitCode != expectedCode)
        {
            await TestContext.Out.WriteLineAsync($"StdOut:\n{collector.StdOut}\nStdErr: {collector.StdErr}");
        }
        Assert.That(
            exitCode,
            Is.EqualTo(expectedCode),
            $"Refasmer returned exit code {exitCode}, while {expectedCode} was expected.");
    }
}
