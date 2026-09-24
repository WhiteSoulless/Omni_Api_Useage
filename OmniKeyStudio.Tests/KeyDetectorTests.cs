using OmniKeyStudio.Models;
using OmniKeyStudio.Services;
using Xunit;

namespace OmniKeyStudio.Tests;

public class KeyDetectorTests
{
    private readonly KeyDetectorService _detector = new();

    [Theory]
    [InlineData("AIzaSy" + "_TEST_PREFIX_SAMPLE_NO_REAL_KEY", "Google Gemini")]
    [InlineData("gsk_" + "test_prefix_sample_no_real_key", "Groq")]
    [InlineData("sk-or-v1-" + "test_prefix_sample_no_real_key", "OpenRouter")]
    [InlineData("sk-ant-" + "test_prefix_sample_no_real_key", "Anthropic Claude")]
    [InlineData("sk-proj-" + "test_prefix_sample_no_real_key", "OpenAI")]
    [InlineData("hf_" + "test_prefix_sample_no_real_key", "Hugging Face")]
    [InlineData("ghp_" + "test_prefix_sample_no_real_key", "GitHub Models")]
    [InlineData("pplx-" + "test_prefix_sample_no_real_key", "Perplexity AI")]
    public void DetectProvider_ShouldIdentifyExpectedProvider(string key, string expectedProvider)
    {
        var result = _detector.DetectProvider(key);
        Assert.Equal(expectedProvider, result.CandidateProvider);
        Assert.True(result.Confidence >= 0.90);
    }

    [Fact]
    public void DetectProvider_GenericSk_ShouldRequireProbing()
    {
        var result = _detector.DetectProvider("sk-" + "test_generic_key_placeholder");
        Assert.True(result.RequiresProbing);
        Assert.Contains("OpenAI", result.AlternativeProviders);
        Assert.Contains("DeepSeek", result.AlternativeProviders);
    }

    [Fact]
    public void DetectProvider_EmptyKey_ShouldReturnConfidenceZero()
    {
        var result = _detector.DetectProvider("");
        Assert.Equal(0.0, result.Confidence);
    }
}
