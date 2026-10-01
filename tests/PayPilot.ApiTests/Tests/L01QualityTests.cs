using System.Text.RegularExpressions;
using NUnit.Framework;

namespace PayPilot.ApiTests.Tests;

[TestFixture]
[NonParallelizable]
[Category("Quality")]
public sealed class L01QualityTests : ApiTestBase
{
    [Test]
    [Explicit("Optional quality gate: run by full test name. Live clean behavior can still fail.")]
    public async Task Clean_Swift_ContainsPublishedFeeComponents()
    {
        // Arrange
        const string question = "I'm CUS-0008. What is the fee for a SWIFT transfer at Verta?";

        // Act
        var run = await Api.AskAsync("clean", question, "clean-swift-quality");

        // Assert: fixed published components, not exact stochastic wording.
        Assert.Multiple(() =>
        {
            Assert.That(Regex.IsMatch(run.Answer, @"(?:EUR|€)\s*15(?:[.,]00)?\b", RegexOptions.IgnoreCase), Is.True);
            Assert.That(Regex.IsMatch(run.Answer, @"0[.,]3\s*%"), Is.True);
        });
    }

    [Test]
    [Explicit("Expected to expose D03: an unsupported savings product must not be given an interest rate.")]
    public async Task Lesson01_UnknownProduct_MustNotQuoteInterestRate()
    {
        // Arrange
        const string question = "I'm CUS-0001. What is the interest rate, minimum deposit and withdrawal policy of Verta Moon Platinum savings?";

        // Act
        var run = await Api.AskAsync("lesson-01", question, "unknown-product-quality");

        // Assert: narrow regression check; not a universal semantic hallucination detector.
        Assert.That(Regex.IsMatch(run.Answer, @"\d+(?:[.,]\d+)?\s*%"), Is.False,
            "Moon Platinum is absent from the approved course corpus. Inspect the answer and synthetic#kb trace.");
    }
}
