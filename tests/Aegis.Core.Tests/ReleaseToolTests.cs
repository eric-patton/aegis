using System.Diagnostics;
using System.Text.Json;
using Aegis.Cli;

namespace Aegis.Core.Tests;

public class ReleaseToolTests
{
    [Fact]
    public void ReleaseJourney_EmitsTheNineCardMatrix_AndFailsWhenCoverageIsAbsent()
    {
        var original = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            int exit = JourneyRunner.Run(["--release", "--cycles", "0", "--json"]);
            Assert.Equal(2, exit);
        }
        finally
        {
            Console.SetOut(original);
        }

        using var json = JsonDocument.Parse(output.ToString());
        var root = json.RootElement;
        Assert.True(root.GetProperty("release").GetBoolean());
        Assert.False(root.GetProperty("releasePassed").GetBoolean());
        Assert.Equal(1, root.GetProperty("generatorVersion").GetInt32());
        Assert.Equal(99, root.GetProperty("saveVersion").GetInt32());
        var matrix = root.GetProperty("releaseCoverage");
        Assert.Equal(9, matrix.EnumerateObject().Count());
        for (int i = 1; i <= 9; i++)
            Assert.True(matrix.TryGetProperty($"V1-{i:00}", out _));
    }

    [Fact]
    public void PackagingScript_PinsCleanNativeAotInputs_Hashes_AndExtractionSmokes()
    {
        string root = RepoRoot();
        string script = File.ReadAllText(Path.Combine(root, "scripts", "release.ps1"));

        Assert.Contains("status --porcelain", script);
        Assert.Contains("requires a clean worktree", script);
        Assert.Contains("-p:PublishAot=true", script);
        Assert.Contains("--self-contained true", script);
        Assert.Contains("aegis-1.0.0-$Runtime.zip", script);
        Assert.Contains("saveVersion=99", script);
        Assert.Contains("generatorVersion=1", script);
        Assert.Contains("Get-FileHash", script);
        Assert.Contains("$zipPath.sha256", script);
        Assert.Contains("Expand-Archive", script);
        Assert.Contains("& $exe --help", script);
        Assert.Contains("& $exe sim --seed 1", script);
        Assert.Contains("& $exe worldgen --seeds 1", script);
        Assert.Contains("$sim.final.turn -ne 4", script);
        Assert.Contains("$worldgen.digestMismatches -ne 0", script);

        var start = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-Command");
        string scriptPath = Path.Combine(root, "scripts", "release.ps1").Replace("'", "''");
        start.ArgumentList.Add($"$errors = @(); [Management.Automation.Language.Parser]::ParseFile('{scriptPath}', [ref]$null, [ref]$errors) | Out-Null; if ($errors.Count) {{ $errors | ForEach-Object {{ $_.Message }}; exit 1 }}");
        using var parser = Process.Start(start)!;
        parser.WaitForExit();
        Assert.True(parser.ExitCode == 0, parser.StandardOutput.ReadToEnd() + parser.StandardError.ReadToEnd());

        Assert.True(File.Exists(Path.Combine(root, "README.md")));
        Assert.True(File.Exists(Path.Combine(root, "RELEASE-NOTES-1.0.0.md")));
        Assert.True(File.Exists(Path.Combine(root, "THIRD-PARTY-NOTICES.md")));
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Aegis.slnx")))
            directory = directory.Parent;
        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the Aegis repository root.");
    }
}
