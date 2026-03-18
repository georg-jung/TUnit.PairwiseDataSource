using System.Diagnostics;
using System.Xml.Linq;

namespace TUnit.PairwiseDataSource.Tests;

internal static class TrxTestProjectRunner
{
    private static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);

    public static Task<TestProjectRunResult> RunMainTestProject(string filter)
    {
        return RunProject(
            filter,
            Path.Combine("test", "TUnit.PairwiseDataSource.Tests", "TUnit.PairwiseDataSource.Tests.csproj"),
            "MainProject");
    }

    public static Task<TestProjectRunResult> RunFailureScenarioProject(string filter)
    {
        return RunProject(
            filter,
            Path.Combine("test", "TUnit.PairwiseDataSource.FailureScenarios", "TUnit.PairwiseDataSource.FailureScenarios.csproj"),
            "FailureScenarios");
    }

    private static async Task<TestProjectRunResult> RunProject(string filter, string relativeProjectPath, string resultsBucket)
    {
        var repositoryRoot = RepositoryRoot.Value;
        var projectPath = Path.Combine(repositoryRoot, relativeProjectPath);

        var resultsDirectory = Path.Combine(repositoryRoot,
            "TestResults",
            resultsBucket,
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(resultsDirectory);

        const string trxFileName = "results.trx";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.ArgumentList.Add("test");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--no-restore");
        startInfo.ArgumentList.Add("--results-directory");
        startInfo.ArgumentList.Add(resultsDirectory);
        startInfo.ArgumentList.Add("--report-trx");
        startInfo.ArgumentList.Add("--report-trx-filename");
        startInfo.ArgumentList.Add(trxFileName);
        startInfo.ArgumentList.Add("--treenode-filter");
        startInfo.ArgumentList.Add(filter);
        startInfo.ArgumentList.Add("--no-progress");
        startInfo.ArgumentList.Add("--disable-logo");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start dotnet test process for '{relativeProjectPath}'.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        var trxPath = Path.Combine(resultsDirectory, trxFileName);

        if (!File.Exists(trxPath))
        {
            throw new InvalidOperationException(
                $"Test project run did not produce a TRX file at '{trxPath}'.{Environment.NewLine}" +
                $"Project: {projectPath}{Environment.NewLine}" +
                $"Exit code: {process.ExitCode}{Environment.NewLine}" +
                $"Stdout:{Environment.NewLine}{standardOutput}{Environment.NewLine}" +
                $"Stderr:{Environment.NewLine}{standardError}");
        }

        return ParseRunResult(trxPath, process.ExitCode, standardOutput, standardError);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (IsRepositoryRoot(current))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate the repository root starting from '{AppContext.BaseDirectory}'.");
    }

    private static bool IsRepositoryRoot(DirectoryInfo directory)
    {
        return File.Exists(Path.Combine(directory.FullName, "TUnit.PairwiseDataSource.slnx"))
               && File.Exists(Path.Combine(directory.FullName, "Directory.Build.props"))
               && Directory.Exists(Path.Combine(directory.FullName, "src"))
               && Directory.Exists(Path.Combine(directory.FullName, "test"))
               // Git worktrees use a .git file that points at the real git dir.
               && (Directory.Exists(Path.Combine(directory.FullName, ".git"))
                   || File.Exists(Path.Combine(directory.FullName, ".git")));
    }

    private static TestProjectRunResult ParseRunResult(string trxPath, int exitCode, string standardOutput, string standardError)
    {
        var document = XDocument.Load(trxPath);
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        var counters = document
            .Descendants(ns + "ResultSummary")
            .Elements(ns + "Counters")
            .Single();

        var failedTests = document
            .Descendants(ns + "UnitTestResult")
            .Where(x => string.Equals((string?)x.Attribute("outcome"), "Failed", StringComparison.Ordinal))
            .Select(x => new FailedTestResult(
                (string?)x.Attribute("testName") ?? "Unknown",
                (string?)x.Element(ns + "Output")?.Element(ns + "ErrorInfo")?.Element(ns + "Message") ?? string.Empty))
            .ToArray();

        return new TestProjectRunResult(
            exitCode,
            int.Parse((string?)counters.Attribute("total") ?? "0"),
            int.Parse((string?)counters.Attribute("passed") ?? "0"),
            int.Parse((string?)counters.Attribute("failed") ?? "0"),
            int.Parse((string?)counters.Attribute("notExecuted") ?? "0"),
            failedTests,
            standardOutput,
            standardError);
    }
}

internal sealed record TestProjectRunResult(
    int ExitCode,
    int Total,
    int Passed,
    int Failed,
    int NotExecuted,
    IReadOnlyList<FailedTestResult> FailedTests,
    string StandardOutput,
    string StandardError);

internal sealed record FailedTestResult(string TestName, string Message);
