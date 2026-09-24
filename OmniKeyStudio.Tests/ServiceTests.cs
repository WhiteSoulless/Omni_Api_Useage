using System;
using System.Collections.Generic;
using System.Linq;
using OmniKeyStudio.Models;
using OmniKeyStudio.Services;
using Xunit;

namespace OmniKeyStudio.Tests;

public class ServiceTests
{
    [Fact]
    public void SecureStorage_SaveAndLoad_ShouldPreserveKeysWithDpapi()
    {
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "OmniKeyStudioTests_" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new SecureStorageService(tempDir);
            var testKeys = new List<ApiKeyEntry>
            {
                new ApiKeyEntry
                {
                    Provider = "Groq",
                    Key = "gsk_" + "sample_test_placeholder_key",
                    IsValid = true,
                    StatusMessage = "Doğrulandı"
                },
                new ApiKeyEntry
                {
                    Provider = "Google Gemini",
                    Key = "AIzaSy" + "sample_test_placeholder_key",
                    IsValid = true,
                    StatusMessage = "Aktif"
                }
            };

            storage.SaveKeys(testKeys);
            var loaded = storage.LoadKeys();

            Assert.NotEmpty(loaded);
            Assert.Contains(loaded, k => k.Provider == "Groq" && k.Key.StartsWith("gsk_"));
            Assert.Contains(loaded, k => k.Provider == "Google Gemini" && k.Key.StartsWith("AIzaSy"));
        }
        finally
        {
            if (System.IO.Directory.Exists(tempDir))
            {
                System.IO.Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void CodeGenerator_ShouldProduceValidSnippets()
    {
        var generator = new CodeGeneratorService();

        string csharp = generator.GenerateCSharpCode("Groq", "llama-3.3-70b-versatile", "TEST_KEY_PLACEHOLDER", "");
        Assert.Contains("HttpClient", csharp);
        Assert.Contains("llama-3.3-70b-versatile", csharp);
        Assert.Contains("api.groq.com", csharp);

        string python = generator.GeneratePythonCode("Google Gemini", "gemini-3.6-flash", "TEST_KEY_PLACEHOLDER", "");
        Assert.Contains("from google import genai", python);
        Assert.Contains("gemini-3.6-flash", python);

        string curl = generator.GenerateCurlCode("OpenRouter", "meta-llama/llama-3.3-70b-instruct:free", "TEST_KEY_PLACEHOLDER", "");
        Assert.Contains("openrouter.ai", curl);
        Assert.Contains("curl", curl);
    }

    [Fact]
    public void ChatPlayground_FindKeyForProvider_ShouldMatchAccurately()
    {
        var keys = new List<ApiKeyEntry>
        {
            new ApiKeyEntry { Provider = "Google Gemini", Key = "AQ.Ab" + "test_placeholder_key" },
            new ApiKeyEntry { Provider = "Groq", Key = "gsk_" + "test_placeholder_key" }
        };

        string geminiKey = ViewModels.ChatPlaygroundViewModel.FindKeyForProvider(keys, "Google Gemini");
        Assert.StartsWith("AQ.Ab", geminiKey);

        string groqKey = ViewModels.ChatPlaygroundViewModel.FindKeyForProvider(keys, "Groq");
        Assert.StartsWith("gsk_", groqKey);

        string missingKey = ViewModels.ChatPlaygroundViewModel.FindKeyForProvider(keys, "OpenRouter");
        Assert.Empty(missingKey);
    }
}
