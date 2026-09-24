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
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    public static List<string> GetDefaultModelsForProvider(string provider)
    {
        return provider switch
        {
            "Google Gemini" => new List<string> { "gemini-3.6-flash", "gemini-flash-latest", "gemini-2.5-flash", "gemini-2.5-pro", "gemini-2.5-flash-lite" },
            "Groq" => new List<string> { "llama-3.3-70b-versatile", "llama-3.1-8b-instant", "mixtral-8x7b-32768", "gemma2-9b-it" },
            "OpenAI" => new List<string> { "gpt-4o", "gpt-4o-mini", "o1-mini", "gpt-4-turbo" },
            "DeepSeek" => new List<string> { "deepseek-chat", "deepseek-reasoner" },
            "OpenRouter" => new List<string> { "meta-llama/llama-3.3-70b-instruct:free", "google/gemini-2.0-flash-exp:free", "deepseek/deepseek-r1:free" },
            "Anthropic Claude" => new List<string> { "claude-3-5-sonnet-20241022", "claude-3-5-haiku-20241022" },
            "xAI (Grok)" => new List<string> { "grok-beta", "grok-2-latest" },
            "Mistral AI" => new List<string> { "mistral-large-latest", "mistral-small-latest", "codestral-latest" },
            "Perplexity AI" => new List<string> { "sonar", "sonar-pro", "sonar-reasoning" },
            "Cerebras" => new List<string> { "llama3.1-70b", "llama3.1-8b" },
            _ => new List<string> { "default-model" }
        };
    }

    public async Task<ValidationResult> ValidateAndProbeKeyAsync(string rawKey, string? selectedProvider = null, string? customBaseUrl = null)
    {
        string key = KeyDetectorService.CleanKey(rawKey);

        if (string.IsNullOrWhiteSpace(key))
        {
            return new ValidationResult { IsSuccess = false, Message = "API anahtarı boş olamaz." };
        }

        // If user specified custom base URL
        if (!string.IsNullOrWhiteSpace(customBaseUrl))
        {
            return await ProbeOpenAiCompatibleAsync(key, customBaseUrl.TrimEnd('/'), selectedProvider ?? "Özel API");
        }

        // If provider is specified or selected by user, test that specific provider first!
        if (!string.IsNullOrWhiteSpace(selectedProvider) && selectedProvider != "Özel / Custom" && selectedProvider != "Henüz anahtar girilmedi")
        {
            var directResult = await ProbeSpecificProviderAsync(selectedProvider, key);
            if (directResult.IsSuccess)
            {
                return directResult;
            }

            // If direct result gave specific quota error (429), it means the key is valid but out of quota!
            if (directResult.Message.Contains("429") || directResult.Message.Contains("kota") || directResult.Message.Contains("quota"))
            {
                directResult.IsSuccess = true;
                directResult.DiscoveredModels = GetDefaultModelsForProvider(selectedProvider);
                directResult.Message = $"{selectedProvider} anahtarı geçerli ancak geçici olarak kota limitine ulaştı (HTTP 429).";
                return directResult;
            }
        }

        // Multi-candidate fallback probing
        var candidates = new List<string> { "Google Gemini", "Groq", "DeepSeek", "OpenAI", "OpenRouter", "Anthropic Claude", "xAI (Grok)", "Mistral AI" };

        foreach (var candidate in candidates)
        {
            try
            {
                var res = await ProbeSpecificProviderAsync(candidate, key);
                if (res.IsSuccess)
                {
                    return res;
                }
            }
            catch { }
        }

        string fallbackProvider = !string.IsNullOrWhiteSpace(selectedProvider) ? selectedProvider : "Özel / Custom";
        return new ValidationResult
        {
            IsSuccess = false,
            DetectedProvider = fallbackProvider,
            DiscoveredModels = GetDefaultModelsForProvider(fallbackProvider),
            Message = "Canlı sunucu doğrulaması tamamlanamadı (Ağ engeli, yanlış anahtar veya geçici kota sorunu). Sağlayıcıyı açılır listeden seçip 'Doğrulamadan Kaydet' butonuna basarak doğrudan ekleyebilirsiniz."
        };
    }

    public async Task<ValidationResult> ProbeSpecificProviderAsync(string provider, string key)
    {
        return provider switch
        {
            "Google Gemini" => await ProbeGeminiAsync(key),
            "Groq" => await ProbeGroqAsync(key),
            "OpenRouter" => await ProbeOpenRouterAsync(key),
            "Anthropic Claude" => await ProbeAnthropicAsync(key),
            "OpenAI" => await ProbeOpenAiAsync(key),
            "DeepSeek" => await ProbeDeepSeekAsync(key),
            "xAI (Grok)" => await ProbeXAiAsync(key),
            "Mistral AI" => await ProbeMistralAsync(key),
            "Perplexity AI" => await ProbePerplexityAsync(key),
            _ => await ProbeOpenAiCompatibleAsync(key, "https://api.openai.com/v1", provider)
        };
    }

    public async Task<ValidationResult> ProbeGeminiAsync(string key)
    {
        try
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models?key={Uri.EscapeDataString(key)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-goog-api-key", key);

            using var response = await HttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var models = ExtractGeminiModels(content);
                if (models.Count == 0) models = GetDefaultModelsForProvider("Google Gemini");

                return new ValidationResult
                {
                    IsSuccess = true,
                    DetectedProvider = "Google Gemini",
                    Message = $"Doğrulandı! Google AI Studio erişimi aktif. {models.Count} model listelendi.",
                    DiscoveredModels = models
                };
            }

            int code = (int)response.StatusCode;
            string err = await response.Content.ReadAsStringAsync();
            return new ValidationResult 
            { 
                IsSuccess = false, 
                DetectedProvider = "Google Gemini", 
                Message = $"Google Gemini yanıtı: HTTP {code} ({GetHttpReason(code, err)})",
                DiscoveredModels = GetDefaultModelsForProvider("Google Gemini")
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult 
            { 
                IsSuccess = false, 
                DetectedProvider = "Google Gemini", 
                Message = $"Gemini bağlantı hatası: {ex.Message}",
                DiscoveredModels = GetDefaultModelsForProvider("Google Gemini")
            };
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
                if (models.Count == 0) models = GetDefaultModelsForProvider("Groq");

                return new ValidationResult
                {
                    IsSuccess = true,
                    DetectedProvider = "Groq",
                    Message = $"Doğrulandı! Groq LPU Cloud aktif (Ultra Hızlı). {models.Count} model bulundu.",
                    DiscoveredModels = models
                };
            }
            int code = (int)response.StatusCode;
            return new ValidationResult { IsSuccess = false, DetectedProvider = "Groq", Message = $"Groq HTTP {code}", DiscoveredModels = GetDefaultModelsForProvider("Groq") };
        }
        catch (Exception ex)
        {
            return new ValidationResult { IsSuccess = false, DetectedProvider = "Groq", Message = ex.Message, DiscoveredModels = GetDefaultModelsForProvider("Groq") };
        }
    }

    public async Task<ValidationResult> ProbeOpenRouterAsync(string key)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://openrouter.ai/api/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            request.Headers.Add("HTTP-Referer", "https://github.com/OmniKeyStudio");
            request.Headers.Add("X-Title", "OmniKey AI Studio");

            using var response = await HttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var models = ExtractOpenAiModels(content);
                if (models.Count == 0) models = GetDefaultModelsForProvider("OpenRouter");

                return new ValidationResult
                {
                    IsSuccess = true,
                    DetectedProvider = "OpenRouter",
                    Message = $"Doğrulandı! OpenRouter birleşik AI yönlendirici aktif. {models.Count} model listelendi.",
                    DiscoveredModels = models
                };
            }
            int code = (int)response.StatusCode;
            return new ValidationResult { IsSuccess = false, DetectedProvider = "OpenRouter", Message = $"OpenRouter HTTP {code}", DiscoveredModels = GetDefaultModelsForProvider("OpenRouter") };
        }
        catch (Exception ex)
        {
            return new ValidationResult { IsSuccess = false, DetectedProvider = "OpenRouter", Message = ex.Message, DiscoveredModels = GetDefaultModelsForProvider("OpenRouter") };
        }
    }

    public async Task<ValidationResult> ProbeOpenAiAsync(string key)
    {
        return await ProbeOpenAiCompatibleAsync(key, "https://api.openai.com/v1", "OpenAI");
    }

    public async Task<ValidationResult> ProbeDeepSeekAsync(string key)
    {
        // Try v1/models first, then models
        var res1 = await ProbeOpenAiCompatibleAsync(key, "https://api.deepseek.com/v1", "DeepSeek");
        if (res1.IsSuccess) return res1;
        return await ProbeOpenAiCompatibleAsync(key, "https://api.deepseek.com", "DeepSeek");
    }

    public async Task<ValidationResult> ProbeXAiAsync(string key)
    {
        return await ProbeOpenAiCompatibleAsync(key, "https://api.x.ai/v1", "xAI (Grok)");
    }

    public async Task<ValidationResult> ProbeMistralAsync(string key)
    {
        return await ProbeOpenAiCompatibleAsync(key, "https://api.mistral.ai/v1", "Mistral AI");
    }

    public async Task<ValidationResult> ProbePerplexityAsync(string key)
    {
        return await ProbeOpenAiCompatibleAsync(key, "https://api.perplexity.ai", "Perplexity AI");
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
                if (models.Count == 0) models = GetDefaultModelsForProvider("Anthropic Claude");

                return new ValidationResult
                {
                    IsSuccess = true,
                    DetectedProvider = "Anthropic Claude",
                    Message = $"Doğrulandı! Anthropic Claude API erişimi aktif. {models.Count} model bulundu.",
                    DiscoveredModels = models
                };
            }
            int code = (int)response.StatusCode;
            return new ValidationResult { IsSuccess = false, DetectedProvider = "Anthropic Claude", Message = $"Anthropic HTTP {code}", DiscoveredModels = GetDefaultModelsForProvider("Anthropic Claude") };
        }
        catch (Exception ex)
        {
            return new ValidationResult { IsSuccess = false, DetectedProvider = "Anthropic Claude", Message = ex.Message, DiscoveredModels = GetDefaultModelsForProvider("Anthropic Claude") };
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
                if (models.Count == 0) models = GetDefaultModelsForProvider(providerName);

                return new ValidationResult
                {
                    IsSuccess = true,
                    DetectedProvider = providerName,
                    Message = $"Doğrulandı! {providerName} API aktif. {models.Count} model tespit edildi.",
                    DiscoveredModels = models
                };
            }

            int code = (int)response.StatusCode;
            string err = await response.Content.ReadAsStringAsync();

            // If 429 quota reached, key is still valid!
            if (code == 429)
            {
                return new ValidationResult
                {
                    IsSuccess = true,
                    DetectedProvider = providerName,
                    Message = $"{providerName} anahtarı geçerli (Kota sınırı / HTTP 429).",
                    DiscoveredModels = GetDefaultModelsForProvider(providerName)
                };
            }

            return new ValidationResult 
            { 
                IsSuccess = false, 
                DetectedProvider = providerName, 
                Message = $"{providerName} HTTP {code}: {GetHttpReason(code, err)}",
                DiscoveredModels = GetDefaultModelsForProvider(providerName)
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult 
            { 
                IsSuccess = false, 
                DetectedProvider = providerName, 
                Message = $"{providerName} bağlantı hatası: {ex.Message}",
                DiscoveredModels = GetDefaultModelsForProvider(providerName)
            };
        }
    }

    private static string GetHttpReason(int statusCode, string body)
    {
        if (statusCode == 401) return "Yetkisiz Erişim / Geçersiz Anahtar";
        if (statusCode == 403) return "Erişim Reddedildi / İzin Yetersiz";
        if (statusCode == 429) return "Kota veya Hız Sınırı Aşıldı";
        if (statusCode == 404) return "Endpoint Bulunamadı";
        return body.Length > 80 ? body[..80] + "..." : body;
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
                        string? modelId = id.GetString();
                        if (!string.IsNullOrWhiteSpace(modelId)) list.Add(modelId);
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
                        string? modelId = id.GetString();
                        if (!string.IsNullOrWhiteSpace(modelId)) list.Add(modelId);
                    }
                }
            }
        }
        catch { }
        return list;
    }
}
