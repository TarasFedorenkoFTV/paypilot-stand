using System.Text.Json;
using NUnit.Framework;

namespace PayPilot.ApiTests.Api;

public sealed class EvidenceWriter
{
    private static readonly string RunId = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string DirectoryPath { get; }

    public EvidenceWriter()
    {
        var evidenceRoot = Environment.GetEnvironmentVariable("PAYPILOT_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(evidenceRoot))
        {
            var projectDirectory = new DirectoryInfo(AppContext.BaseDirectory);
            while (projectDirectory.Parent is not null &&
                   !File.Exists(Path.Combine(projectDirectory.FullName, "PayPilot.ApiTests.csproj")))
            {
                projectDirectory = projectDirectory.Parent;
            }

            evidenceRoot = File.Exists(Path.Combine(projectDirectory.FullName, "PayPilot.ApiTests.csproj"))
                ? Path.Combine(projectDirectory.FullName, "Artifacts")
                : Path.Combine(TestContext.CurrentContext.WorkDirectory, "Artifacts");
        }

        var testName = string.Concat(TestContext.CurrentContext.Test.Name
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_'
                ? character : '_'));
        DirectoryPath = Path.GetFullPath(Path.Combine(evidenceRoot, RunId, testName));
        Directory.CreateDirectory(DirectoryPath);
        TestContext.Progress.WriteLine($"Evidence: {DirectoryPath}");
    }

    public void Save(string fileName, object value)
    {
        var path = Path.Combine(DirectoryPath, fileName);
        File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions));
        TestContext.AddTestAttachment(path);
    }
}
