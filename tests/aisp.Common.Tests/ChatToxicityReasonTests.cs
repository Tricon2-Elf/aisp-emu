using aisp.Common.Services.Toxicity;

namespace aisp.Common.Tests;

public sealed class ChatToxicityReasonTests
{
    [Fact]
    public void Format_WritesAllLabelPercentsInOrder_NoModelName()
    {
        var result = new ToxicityResult(
            "RoBERTa (en)",
            new ScoreResult(
                "hello",
                IsToxic: true,
                [
                    new LabelScore("toxicity", 0.12, false),
                    new LabelScore("severe_toxicity", 0.01, true),
                    new LabelScore("obscene", 0.03, false),
                    new LabelScore("identity_attack", 0.02, true),
                    new LabelScore("insult", 0.81, true),
                    new LabelScore("threat", 0.08, true),
                    new LabelScore("sexual_explicit", 0.01, false),
                ]
            ),
            Normalized: null
        );

        var reason = ChatToxicityReason.Format(result);
        Assert.Equal(
            "toxicity 12%; severe_toxicity 1%; obscene 3%; identity_attack 2%; insult 81%; threat 8%; sexual_explicit 1%",
            reason
        );
        Assert.DoesNotContain("RoBERTa", reason);
        Assert.DoesNotContain("NOT TOXIC", reason);
    }

    [Fact]
    public void Format_UsesVerdictNormalizedScores()
    {
        var result = new ToxicityResult(
            "DistilBERT",
            new ScoreResult(
                "SHOUT",
                true,
                [new LabelScore("toxic", 0.99, true), new LabelScore("not-toxic", 0.01, false)]
            ),
            new ScoreResult(
                "shout",
                false,
                [new LabelScore("toxic", 0.10, true), new LabelScore("not-toxic", 0.90, false)]
            )
        );

        Assert.Equal("toxic 10%; not-toxic 90%", ChatToxicityReason.Format(result));
    }

    [Fact]
    public void Format_TruncatesToMaxLength()
    {
        var scores = Enumerable
            .Range(0, 80)
            .Select(i => new LabelScore($"label_{i}_quite_long", 0.5, false))
            .ToArray();
        var result = new ToxicityResult(
            "RoBERTa (en)",
            new ScoreResult("x", false, scores),
            Normalized: null
        );

        var reason = ChatToxicityReason.Format(result);
        Assert.Equal(ChatToxicityReason.MaxLength, reason.Length);
    }
}
