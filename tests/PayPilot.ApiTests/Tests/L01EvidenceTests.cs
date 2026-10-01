using System.Text.RegularExpressions;
using NUnit.Framework;

namespace PayPilot.ApiTests.Tests;

[TestFixture]
[NonParallelizable] // The stand profile is global, not scoped to a session.
[Category("Evidence")]
public sealed class L01EvidenceTests : ApiTestBase
{
    private const string SwiftQuestion = "I'm CUS-0008. What is the fee for a SWIFT transfer at Verta?";
    private const string PremiumQuestion =
        "I'm CUS-0001. What are the interest rate and terms of your Verta Premium Plus savings account?";

    [Test]
    public async Task ContractInputs_SaveEnvironmentToolsAndUserStory()
    {
        // Arrange
        await Api.SetProfileAsync("lesson-01");

        // Act: all review inputs originate from API calls, not local source files.
        var health = await Api.GetHealthAsync();
        var clock = await Api.GetClockAsync();
        var retrieval = await Api.GetRetrievalAsync();
        var tools = await Api.GetToolsAsync();
        var specs = await Api.GetSpecsAsync();
        Evidence.Save("contract-inputs.json", new { health, clock, retrieval, tools, specs });

        // Assert
        Assert.That(specs.GetProperty("requirements").GetProperty("US-01.md").GetString(), Is.Not.Empty);
        Assert.That(tools.GetProperty("tools").GetArrayLength(), Is.GreaterThan(0));
    }

    [Test]
    public async Task SystemPrompt_Lesson01_SaveAssembledContract()
    {
        // Arrange
        await Api.SetProfileAsync("lesson-01");

        // Act
        var prompt = await Api.GetPromptAsync();
        Evidence.Save("assembled-prompt.json", prompt);
        TestContext.Progress.WriteLine(prompt.GetProperty("text").GetString());

        // Assert
        Assert.That(prompt.GetProperty("version").GetString(), Is.EqualTo("base.v1+D01+D02+D03"));
        Assert.That(prompt.GetProperty("overlays").EnumerateArray().Select(value => value.GetString()),
            Is.EquivalentTo(new[] { "D01", "D02", "D03" }));
    }

    [TestCase("swift", SwiftQuestion)]
    [TestCase("premium", PremiumQuestion)]
    [TestCase("lost-card", "I'm CUS-0001. I lost my card, what should I do?")]
    [TestCase("balance", "I'm CUS-0001. What is my balance?")]
    [TestCase("tax", "Can you give me tax advice on my savings?")]
    public async Task Baseline_CompareCleanAndLesson01(string scenario, string question)
    {
        // Arrange / Act: independent sessions, identical question.
        var clean = await Api.AskAsync("clean", question, $"{scenario}-clean");
        var lesson = await Api.AskAsync("lesson-01", question, $"{scenario}-lesson-01");
        Evidence.Save("comparison.json", new { clean, lesson });

        // Assert: equality/difference of stochastic answers is not a quality oracle.
        Assert.That(clean.SessionId, Is.Not.EqualTo(lesson.SessionId));
        Assert.That(clean.RequestId, Is.Not.EqualTo(lesson.RequestId));
    }

    [Test]
    public async Task Swift_FiveIndependentRuns_AndCleanControl()
    {
        // Arrange
        var results = new List<object>();

        // Act
        for (var number = 1; number <= 5; number++)
        {
            var run = await Api.AskAsync("lesson-01", SwiftQuestion, $"swift-{number}");
            // Heuristic labels help a live demo; final classification requires review.
            var category = Regex.IsMatch(run.Answer, @"(?:EUR|€)\s*15(?:[.,]00)?\b|0[.,]3\s*%",
                RegexOptions.IgnoreCase)
                ? "Published fee figure mentioned"
                : "No recognized published fee figure — review redirect/components/refusal";
            TestContext.Progress.WriteLine($"Run {number}: {category}");
            results.Add(new { run.RequestId, category, run.Answer });
        }

        var control = await Api.AskAsync("clean", SwiftQuestion, "swift-clean-control");
        Evidence.Save("swift-summary.json", new { results, control, note = "Labels are heuristics, not an LLM quality verdict." });

        // Assert: identical outcomes across five runs are a valid observation.
        Assert.That(results, Has.Count.EqualTo(5));
    }

    [Test]
    public async Task PremiumPlus_ThreeRuns_ShowRetrievalSources()
    {
        // Arrange
        var observations = new List<object>();

        // Act
        for (var number = 1; number <= 3; number++)
        {
            var run = await Api.AskAsync("lesson-01", PremiumQuestion, $"premium-{number}");
            var searches = ToolSpans(run.Trace)
                .Where(span => span.GetProperty("name").GetString() == "tool.search_knowledge_base")
                .ToArray();
            var sources = searches.SelectMany(span => span.GetProperty("attributes")
                    .GetProperty("tool.result").GetProperty("fragments").EnumerateArray())
                .Select(fragment => new
                {
                    id = fragment.GetProperty("id").GetString(),
                    doc = fragment.GetProperty("doc").GetString(),
                    text = fragment.GetProperty("text").GetString()
                }).ToArray();
            TestContext.Progress.WriteLine("Retrieved sources: " + string.Join(", ", sources.Select(source => source.id)));
            observations.Add(new { run.RequestId, sources, run.Answer });

            // Assert: search is explicitly mandatory for a product question.
            Assert.That(searches, Is.Not.Empty, "Product terms require a knowledge-base search.");
        }

        Evidence.Save("premium-sources.json", observations);
        Assert.That(observations, Has.Count.EqualTo(3));
    }

    [TestCase("conversion", "I'm CUS-0001. If I convert EUR 200 to USD now, what rate, spread and final USD amount apply to my tier and remaining monthly allowance?", 1)]
    [TestCase("transactions", "I'm CUS-0001. Show my recent transactions.", 1)]
    [TestCase("missing-customer", "I'm CUS-9999. What is my balance?", 1)]
    [TestCase("moon-product", "I'm CUS-0001. What is the interest rate, minimum deposit and withdrawal policy of Verta Moon Platinum savings?", 3)]
    [TestCase("specific-fee", "I'm CUS-0008. What exact Verta fee in EUR will I pay for a EUR 1000 SWIFT transfer? Please include the flat and percentage components.", 5)]
    public async Task TargetedQuestion_SaveAnswerAndTools(string scenario, string question, int repetitions)
    {
        // Arrange / Act
        var results = new List<object>();
        for (var number = 1; number <= repetitions; number++)
        {
            var run = await Api.AskAsync("lesson-01", question, $"{scenario}-{number}");
            var tools = ToolSpans(run.Trace).Select(span => span.GetProperty("name").GetString()).ToArray();
            TestContext.Progress.WriteLine("Tools: " + string.Join(", ", tools));
            results.Add(new { run.RequestId, tools });

            // Assert
            Assert.That(run.Response.GetProperty("elapsed_ms").GetDouble(), Is.GreaterThanOrEqualTo(0));
        }

        Evidence.Save("tool-summary.json", results);
    }
}
