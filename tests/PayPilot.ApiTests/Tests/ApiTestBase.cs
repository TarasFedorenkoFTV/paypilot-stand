using System.Text.Json;
using NUnit.Framework;
using PayPilot.ApiTests.Api;

namespace PayPilot.ApiTests.Tests;

public abstract class ApiTestBase
{
    protected PayPilotClient Api = null!;
    protected EvidenceWriter Evidence = null!;
    private JsonElement? _originalProfile;

    [SetUp]
    public async Task SetUpAsync()
    {
        Evidence = new EvidenceWriter();
        Api = new PayPilotClient(
            Environment.GetEnvironmentVariable("PAYPILOT_BASE_URL") ?? "http://localhost:8000", Evidence);
        _originalProfile = await Api.GetProfileAsync();
        await Api.SetExtraDefectsAsync(string.Empty);

        var health = await Api.GetHealthAsync();
        var provider = health.GetProperty("provider").GetString();
        Assert.That(provider, Is.AnyOf("anthropic", "openai"),
            "L01 requires a live provider. The mock provider cannot demonstrate variation.");
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        // Restoration belongs in fixture infrastructure, not in test methods.
        try
        {
            if (_originalProfile.HasValue)
            {
                await Api.SetProfileAsync(_originalProfile.Value.GetProperty("profile").GetString()!);
            }
        }
        finally
        {
            try
            {
                if (_originalProfile.HasValue)
                {
                    var extra = _originalProfile.Value.GetProperty("extra_defects").EnumerateArray()
                        .Select(value => value.GetString());
                    await Api.SetExtraDefectsAsync(string.Join(",", extra));
                    var restored = await Api.GetProfileAsync();
                    Evidence.Save("restored-profile.json", restored);
                    Assert.Multiple(() =>
                    {
                        Assert.That(restored.GetProperty("profile").GetString(),
                            Is.EqualTo(_originalProfile.Value.GetProperty("profile").GetString()));
                        Assert.That(restored.GetProperty("extra_defects").GetRawText(),
                            Is.EqualTo(_originalProfile.Value.GetProperty("extra_defects").GetRawText()));
                    });
                }
            }
            finally
            {
                Api?.Dispose();
                _originalProfile = null;
            }
        }
    }

    protected static IEnumerable<JsonElement> ToolSpans(JsonElement trace)
    {
        if (trace.GetProperty("name").GetString()?.StartsWith("tool.", StringComparison.Ordinal) == true)
        {
            yield return trace;
        }

        if (trace.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                foreach (var span in ToolSpans(child))
                {
                    yield return span;
                }
            }
        }
    }
}
