using OmniKeyStudio.Models;
using OmniKeyStudio.Services;
using Xunit;

namespace OmniKeyStudio.Tests;

public class KeyDetectorTests
{
    private readonly KeyDetectorService _detector = new();

    [Theory]
    [InlineData("AIzaSy_MOCK_TEST_SAMPLE_KEY_FOR_UNIT_TESTING_12345", "Google Gemini")]
    [InlineData("gsk_mock_test_sample_groq_key_for_testing_12345", "Groq")]
    [InlineData("sk-or-v1-mock-test-sample-openrouter-key-12345", "OpenRouter")]
    [InlineData("sk-ant-api03-mock-test-sample-anthropic-key-12345", "Anthropic Claude")]
    [InlineData("sk-proj-mock-test-sample-openai-project-key-12345", "OpenAI")]
    [InlineData("hf_mock_test_sample_huggingface_token_12345", "Hugging Face")]
    [InlineData("ghp_mock_test_sample_github_token_12345", "GitHub Models")]
    [InlineData("pplx-mock-test-sample-perplexity-token-12345", "Perplexity AI")]
    public void DetectProvider_ShouldIdentifyExpectedProvider(string key, string expectedProvider)
    {
        var result = _detector.DetectProvider(key);
        Assert.Equal(expectedProvider, result.CandidateProvider);
        Assert.True(result.Confidence >= 0.90);
    }

    [Fact]
    public void DetectProvider_GenericSk_ShouldRequireProbing()
    {
        var result = _detector.DetectProvider("sk-mock-generic-key-sample-12345");
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
