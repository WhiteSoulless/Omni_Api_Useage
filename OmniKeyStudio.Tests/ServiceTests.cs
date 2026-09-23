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
        var storage = new SecureStorageService();
        var testKeys = new List<ApiKeyEntry>
        {
            new ApiKeyEntry
            {
                Provider = "Groq",
                Key = "gsk_mock_test_key_sample_12345",
                IsValid = true,
                StatusMessage = "Doğrulandı"
            },
            new ApiKeyEntry
            {
                Provider = "Google Gemini",
                Key = "AIzaSyMockTestKeySample1234567890",
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

    [Fact]
    public void CodeGenerator_ShouldProduceValidSnippets()
    {
        var generator = new CodeGeneratorService();

        string csharp = generator.GenerateCSharpCode("Groq", "llama-3.3-70b-versatile", "gsk_mock_dummy", "");
        Assert.Contains("HttpClient", csharp);
        Assert.Contains("llama-3.3-70b-versatile", csharp);
        Assert.Contains("api.groq.com", csharp);

        string python = generator.GeneratePythonCode("Google Gemini", "gemini-2.0-flash", "AIzaSyMockDummy", "");
        Assert.Contains("from google import genai", python);
        Assert.Contains("gemini-2.0-flash", python);

        string curl = generator.GenerateCurlCode("OpenRouter", "meta-llama/llama-3.3-70b-instruct:free", "sk-or-v1-mock-dummy", "");
        Assert.Contains("openrouter.ai", curl);
        Assert.Contains("curl", curl);
    }
}
