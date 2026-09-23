using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace OmniKeyStudio.Services;

public class ValidationResult
{
    public bool IsSuccess { get; set; }
    public string DetectedProvider { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> DiscoveredModels { get; set; } = new();
}

public class KeyValidatorService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(12) };

    public async Task<ValidationResult> ValidateAndProbeKeyAsync(string key, string? suggestedProvider = null, string? customBaseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return new ValidationResult { IsSuccess = false, Message = "API anahtarı boş olamaz." };
        }

        string trimmedKey = key.Trim();

        // If user specified custom base URL
        if (!string.IsNullOrWhiteSpace(customBaseUrl))
        {
            return await ProbeOpenAiCompatibleAsync(trimmedKey, customBaseUrl.TrimEnd('/'), "Özel API");
        }

        // If provider is already detected with high confidence, test that provider first
        if (!string.IsNullOrWhiteSpace(suggestedProvider) && suggestedProvider != "OpenAI / DeepSeek" && suggestedProvider != "Özel / Tanımlanamadı")
        {
            var directResult = await ProbeSpecificProviderAsync(suggestedProvider, trimmedKey);
            if (directResult.IsSuccess)
            {
                return directResult;
            }
        }

        // If it's a generic "sk-..." or direct validation failed, probe candidate providers in parallel
        var probeTasks = new List<Task<ValidationResult>>
        {
            ProbeOpenAiAsync(trimmedKey),
            ProbeGroqAsync(trimmedKey),
            ProbeDeepSeekAsync(trimmedKey),
            ProbeOpenRouterAsync(trimmedKey),
            ProbeGeminiAsync(trimmedKey),
            ProbeAnthropicAsync(trimmedKey),
            ProbeMistralAsync(trimmedKey)
        };

        while (probeTasks.Count > 0)
        {
            var finishedTask = await Task.WhenAny(probeTasks);
            probeTasks.Remove(finishedTask);

            try
            {
                var res = await finishedTask;
                if (res.IsSuccess)
                {
                    return res;
                }
            }
            catch
            {
                // Continue probing other candidates
            }
        }

        return new ValidationResult
        {
            IsSuccess = false,
            DetectedProvider = suggestedProvider ?? "Bilinmeyen",
            Message = "Anahtar test edildi ancak hiçbir servis tarafından yetkilendirilmedi (Yetkisiz / Hatalı Anahtar)."
        };
    }

    private async Task<ValidationResult> ProbeSpecificProviderAsync(string provider, string key)
    {
        return provider switch
        {
            "Google Gemini" => await ProbeGeminiAsync(key),
            "Groq" => await ProbeGroqAsync(key),
            "OpenRouter" => await ProbeOpenRouterAsync(key),
            "Anthropic Claude" => await ProbeAnthropicAsync(key),
            "OpenAI" => await ProbeOpenAiAsync(key),
            "DeepSeek" => await ProbeDeepSeekAsync(key),
            "Mistral AI" => await ProbeMistralAsync(key),
            _ => await ProbeOpenAiCompatibleAsync(key, "https://api.openai.com/v1", provider)
        };
    }

    public async Task<ValidationResult> ProbeGeminiAsync(string key)
    {
        try
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models?key={Uri.EscapeDataString(key)}";
            using var response = await HttpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var models = ExtractGeminiModels(content);
                return new ValidationResult
                {
                    IsSuccess = true,
                    DetectedProvider = "Google Gemini",
                    Message = $"Doğrulandı! Google Gemini AI Studio erişimi aktif. {models.Count} model listelendi.",
                    DiscoveredModels = models
                };
            }
            return new ValidationResult { IsSuccess = false, DetectedProvider = "Google Gemini", Message = $"Gemini Hatası: HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ValidationResult { IsSuccess = false, DetectedProvider = "Google Gemini", Message = ex.Message };
        }
    }

    public async Task<ValidationResult> ProbeGroqAsync(string key)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.groq.com/openai/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

            using var response = await HttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var models = ExtractOpenAiModels(content);
                return new ValidationResult
                {
                    IsSuccess = true,
                    DetectedProvider = "Groq",
                    Message = $"Doğrulandı! Groq LPU Cloud aktif (Ultra Hızlı Llama 3 / Mixtral). {models.Count} model bulundu.",
                    DiscoveredModels = models
                };
            }
            return new ValidationResult { IsSuccess = false, DetectedProvider = "Groq", Message = $"Groq HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ValidationResult { IsSuccess = false, DetectedProvider = "Groq", Message = ex.Message };
        }
    }

    public async Task<ValidationResult> ProbeOpenRouterAsync(string key)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://openrouter.ai/api/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

            using var response = await HttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var models = ExtractOpenAiModels(content);
                return new ValidationResult
                {
                    IsSuccess = true,
                    DetectedProvider = "OpenRouter",
                    Message = $"Doğrulandı! OpenRouter birleşik AI yönlendirici aktif. {models.Count} model listelendi.",
                    DiscoveredModels = models
                };
            }
            return new ValidationResult { IsSuccess = false, DetectedProvider = "OpenRouter", Message = $"OpenRouter HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ValidationResult { IsSuccess = false, DetectedProvider = "OpenRouter", Message = ex.Message };
        }
    }

    public async Task<ValidationResult> ProbeOpenAiAsync(string key)
    {
        return await ProbeOpenAiCompatibleAsync(key, "https://api.openai.com/v1", "OpenAI");
    }

    public async Task<ValidationResult> ProbeDeepSeekAsync(string key)
    {
        return await ProbeOpenAiCompatibleAsync(key, "https://api.deepseek.com", "DeepSeek");
    }

    public async Task<ValidationResult> ProbeMistralAsync(string key)
    {
        return await ProbeOpenAiCompatibleAsync(key, "https://api.mistral.ai/v1", "Mistral AI");
    }

    public async Task<ValidationResult> ProbeAnthropicAsync(string key)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/v1/models");
            request.Headers.Add("x-api-key", key);
            request.Headers.Add("anthropic-version", "2023-06-01");

            using var response = await HttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var models = ExtractAnthropicModels(content);
                return new ValidationResult
                {
                    IsSuccess = true,
                    DetectedProvider = "Anthropic Claude",
                    Message = $"Doğrulandı! Anthropic Claude API erişimi aktif. {models.Count} model bulundu.",
                    DiscoveredModels = models
                };
            }
            return new ValidationResult { IsSuccess = false, DetectedProvider = "Anthropic Claude", Message = $"Anthropic HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ValidationResult { IsSuccess = false, DetectedProvider = "Anthropic Claude", Message = ex.Message };
        }
    }

    public async Task<ValidationResult> ProbeOpenAiCompatibleAsync(string key, string baseUrl, string providerName)
    {
        try
        {
            string url = $"{baseUrl.TrimEnd('/')}/models";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

            using var response = await HttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var models = ExtractOpenAiModels(content);
                return new ValidationResult
                {
                    IsSuccess = true,
                    DetectedProvider = providerName,
                    Message = $"Doğrulandı! {providerName} API aktif. {models.Count} model tespit edildi.",
                    DiscoveredModels = models
                };
            }
            return new ValidationResult { IsSuccess = false, DetectedProvider = providerName, Message = $"{providerName} HTTP {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ValidationResult { IsSuccess = false, DetectedProvider = providerName, Message = ex.Message };
        }
    }

    private static List<string> ExtractOpenAiModels(string json)
    {
        var list = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in data.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var id))
                    {
                        list.Add(id.GetString() ?? string.Empty);
                    }
                }
            }
        }
        catch { }
        return list;
    }

    private static List<string> ExtractGeminiModels(string json)
    {
        var list = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in models.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var name))
                    {
                        string str = name.GetString() ?? "";
                        if (str.StartsWith("models/")) str = str["models/".Length..];
                        if (!string.IsNullOrWhiteSpace(str)) list.Add(str);
                    }
                }
            }
        }
        catch { }
        return list;
    }

    private static List<string> ExtractAnthropicModels(string json)
    {
        var list = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in data.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var id))
                    {
                        list.Add(id.GetString() ?? string.Empty);
                    }
                }
            }
        }
        catch { }
        return list;
    }
}
