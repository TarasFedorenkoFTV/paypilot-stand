using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;

namespace PayPilot.ApiTests.Api;

public sealed class PayPilotClient : IDisposable
{
    private readonly RestClient _client;
    private readonly EvidenceWriter _evidence;
    private int _requestNumber;

    public PayPilotClient(string baseUrl, EvidenceWriter evidence)
    {
        _client = new RestClient(new RestClientOptions(baseUrl));
        _evidence = evidence;
    }

    public Task<JsonElement> GetHealthAsync() => SendAsync("health", Method.Get);
    public Task<JsonElement> GetProfileAsync() => SendAsync("api/_test/defects", Method.Get);
    public Task<JsonElement> SetProfileAsync(string profile) =>
        SendAsync("api/_test/profile", Method.Put, new { profile });
    public Task<JsonElement> SetExtraDefectsAsync(string defects) =>
        SendAsync("api/_test/defects", Method.Put, new { defects });
    public Task<JsonElement> GetPromptAsync() => SendAsync("api/_test/prompt", Method.Get);
    public Task<JsonElement> GetSpecsAsync() => SendAsync("api/_test/specs", Method.Get);
    public Task<JsonElement> GetToolsAsync() => SendAsync("api/_test/tools", Method.Get);
    public Task<JsonElement> GetClockAsync() => SendAsync("api/_test/clock", Method.Get);
    public Task<JsonElement> GetRetrievalAsync() => SendAsync("api/_test/retrieval", Method.Get);

    public async Task<ChatEvidence> AskAsync(string profile, string question, string label)
    {
        await SetProfileAsync(profile);
        var health = await GetHealthAsync();
        Assert.That(health.GetProperty("profile").GetString(), Is.EqualTo(profile));
        var expectedDefects = profile == "lesson-01"
            ? new[] { "D01", "D02", "D03" }
            : Array.Empty<string>();
        Assert.That(health.GetProperty("active_defects").EnumerateArray()
            .Select(value => value.GetString()).ToArray(), Is.EquivalentTo(expectedDefects));

        var sessionId = $"csharp-l01-{Guid.NewGuid():N}";
        var startedUtc = DateTimeOffset.UtcNow;
        TestContext.Progress.WriteLine($"\n>>> {label} | profile={profile} | session={sessionId}\n{question}");

        var response = await SendAsync("chat", Method.Post,
            new { session_id = sessionId, message = question });
        var answer = response.GetProperty("answer").GetString() ?? string.Empty;
        var requestId = response.GetProperty("request_id").GetString() ?? string.Empty;
        TestContext.Progress.WriteLine($"\n<<< request_id={requestId}\n{answer}\n");

        // Save the answer before fetching the trace: failures must not lose evidence.
        _evidence.Save(label + ".response.json", new
        {
            profile,
            question,
            session_id = sessionId,
            started_utc = startedUtc,
            response
        });

        var trace = await SendAsync($"api/_test/traces/{requestId}", Method.Get);
        _evidence.Save(label + ".trace.json", trace);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Is.Not.Empty, "The agent must return an answer.");
            Assert.That(requestId, Is.Not.Empty);
            Assert.That(response.GetProperty("session_id").GetString(), Is.EqualTo(sessionId));
            Assert.That(response.GetProperty("step_number").GetInt32(), Is.EqualTo(1));
            Assert.That(trace.GetProperty("request_id").GetString(), Is.EqualTo(requestId));
            Assert.That(trace.GetProperty("attributes").GetProperty("session.id").GetString(),
                Is.EqualTo(sessionId));
            Assert.That(trace.GetProperty("attributes").GetProperty("run.profile").GetString(),
                Is.EqualTo(profile));
        });

        return new ChatEvidence(profile, question, sessionId, requestId, answer, response, trace);
    }

    private async Task<JsonElement> SendAsync(string resource, Method method, object? body = null)
    {
        var request = new RestRequest(resource, method);
        if (body is not null)
        {
            request.AddJsonBody(body);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(240));
        var response = await _client.ExecuteAsync(request, timeout.Token);
        _evidence.Save($"http-{++_requestNumber:000}.json", new
        {
            method = method.ToString(),
            resource,
            request_body = body,
            status_code = (int)response.StatusCode,
            raw_response = response.Content,
            error = response.ErrorMessage
        });

        if (response.StatusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(response.Content))
        {
            throw new InvalidOperationException(
                $"{method} /{resource}: HTTP {(int)response.StatusCode}; " +
                $"{response.ErrorMessage}; response: {response.Content}");
        }

        using var document = JsonDocument.Parse(response.Content);
        return document.RootElement.Clone();
    }

    public void Dispose() => _client.Dispose();
}

public sealed record ChatEvidence(
    string Profile,
    string Question,
    string SessionId,
    string RequestId,
    string Answer,
    JsonElement Response,
    JsonElement Trace);
