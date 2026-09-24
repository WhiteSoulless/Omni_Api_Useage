using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OmniKeyStudio.Models;

namespace OmniKeyStudio.Services;

public class StreamChunkResult
{
    public string Text { get; set; } = string.Empty;
    public long LatencyMs { get; set; }
}

public class LlmClientService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromMinutes(2) };

    public async IAsyncEnumerable<string> StreamChatAsync(
        string provider,
        string apiKey,
        string model,
        List<ChatMessage> conversationHistory,
        string systemPrompt = "",
        string? customBaseUrl = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            yield return $"[Hata]: '{provider}' ({model}) için bir API anahtarı girilmedi. Lütfen üstteki kutudan veya 'Anahtar Kasası'ndan bir API anahtarı ekleyin.";
            yield break;
        }

        if (string.Equals(provider, "Google Gemini", StringComparison.OrdinalIgnoreCase))
        {
            await foreach (var chunk in StreamGeminiAsync(apiKey, model, conversationHistory, systemPrompt, cancellationToken))
            {
                yield return chunk;
            }
        }
        else if (string.Equals(provider, "Anthropic Claude", StringComparison.OrdinalIgnoreCase))
        {
            await foreach (var chunk in StreamAnthropicAsync(apiKey, model, conversationHistory, systemPrompt, cancellationToken))
            {
                yield return chunk;
            }
        }
        else
        {
            // OpenAI, Groq, OpenRouter, DeepSeek, Mistral, Ollama, Custom
            string baseUrl = ResolveBaseUrl(provider, customBaseUrl);
            await foreach (var chunk in StreamOpenAiCompatibleAsync(baseUrl, apiKey, model, conversationHistory, systemPrompt, cancellationToken))
            {
                yield return chunk;
            }
        }
    }

    private static string ResolveBaseUrl(string provider, string? customBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(customBaseUrl)) return customBaseUrl.TrimEnd('/');

        return provider switch
        {
            "Groq" => "https://api.groq.com/openai/v1",
            "OpenRouter" => "https://openrouter.ai/api/v1",
            "DeepSeek" => "https://api.deepseek.com",
            "xAI (Grok)" => "https://api.x.ai/v1",
            "Cerebras" => "https://api.cerebras.ai/v1",
            "Perplexity AI" => "https://api.perplexity.ai",
            "Mistral AI" => "https://api.mistral.ai/v1",
            _ => "https://api.openai.com/v1"
        };
    }

    private async IAsyncEnumerable<string> StreamOpenAiCompatibleAsync(
        string baseUrl,
        string apiKey,
        string model,
        List<ChatMessage> history,
        string systemPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            yield return $"[Hata]: '{model}' modeli için API anahtarı boş. Lütfen anahtarınızı ekleyin.";
            yield break;
        }

        string endpoint = $"{baseUrl}/chat/completions";

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new { role = "system", content = systemPrompt });
        }

        foreach (var msg in history)
        {
            messages.Add(new { role = msg.Role.ToLowerInvariant(), content = msg.Content });
        }

        var payload = new
        {
            model = model,
            messages = messages,
            stream = true,
            temperature = 0.7
        };

        var requestJson = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        if (baseUrl.Contains("openrouter.ai"))
        {
            request.Headers.Add("HTTP-Referer", "https://github.com/OmniKeyStudio");
            request.Headers.Add("X-Title", "OmniKey AI Studio");
        }

        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string err = await response.Content.ReadAsStringAsync(cancellationToken);
            string friendlyMsg = response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => $"[Hata HTTP 401 - Yetkilendirme Hatası]: API anahtarı eksik, geçersiz veya bu modele yetkisiz. Sağlayıcı: {baseUrl}\n{err}",
                System.Net.HttpStatusCode.Forbidden => $"[Hata HTTP 403 - Erişim Engeli]: Bu modele erişim izniniz bulunmuyor.\n{err}",
                System.Net.HttpStatusCode.TooManyRequests => $"[Hata HTTP 429 - Kota / Hız Sınırı]: İstek limiti veya bakiye kotası doldu.\n{err}",
                _ => $"[Hata HTTP {(int)response.StatusCode}]: {err}"
            };
            yield return friendlyMsg;
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) continue;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("data: "))
            {
                string data = line["data: ".Length..].Trim();
                if (data == "[DONE]") break;

                string? deltaText = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("delta", out var delta) && delta.TryGetProperty("content", out var content))
                        {
                            deltaText = content.GetString();
                        }
                    }
                }
                catch { }

                if (!string.IsNullOrEmpty(deltaText))
                {
                    yield return deltaText;
                }
            }
        }
    }

    private async IAsyncEnumerable<string> StreamGeminiAsync(
        string apiKey,
        string model,
        List<ChatMessage> history,
        string systemPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            yield return "[Hata]: Google Gemini için API anahtarı boş. Lütfen geçerli bir Gemini anahtarı ekleyin.";
            yield break;
        }

        string cleanModel = model.StartsWith("models/") ? model["models/".Length..] : model;
        // Automatic migration for deprecated Gemini models (e.g. gemini-2.0-flash -> gemini-3.6-flash)
        if (cleanModel == "gemini-2.0-flash" || cleanModel == "gemini-1.5-flash" || cleanModel == "gemini-1.0-pro")
        {
            cleanModel = "gemini-3.6-flash";
        }
        else if (cleanModel == "gemini-1.5-pro")
        {
            cleanModel = "gemini-2.5-pro";
        }

        string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{cleanModel}:streamGenerateContent?key={Uri.EscapeDataString(apiKey)}&alt=sse";

        var contents = new List<object>();

        foreach (var msg in history)
        {
            string geminiRole = msg.IsUser ? "user" : "model";
            contents.Add(new
            {
                role = geminiRole,
                parts = new[] { new { text = msg.Content } }
            });
        }

        object payloadObj;
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            payloadObj = new
            {
                system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = contents
            };
        }
        else
        {
            payloadObj = new { contents = contents };
        }

        var requestJson = JsonSerializer.Serialize(payloadObj);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", apiKey);

        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string err = await response.Content.ReadAsStringAsync(cancellationToken);
            string friendlyMsg = response.StatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => $"[Gemini Hatası HTTP 400 - Geçersiz İstek]: {err}",
                System.Net.HttpStatusCode.Unauthorized => $"[Gemini Hatası HTTP 401 - Yetkisiz]: API anahtarı geçersiz veya yetkisiz.\n{err}",
                System.Net.HttpStatusCode.Forbidden => $"[Gemini Hatası HTTP 403 - İzin Yok]: Bu model için erişim yetkiniz bulunmuyor.\n{err}",
                System.Net.HttpStatusCode.TooManyRequests => $"[Gemini Hatası HTTP 429 - Kota Limiti]: Gemini ücretsiz kota sınırına ulaştınız. Lütfen kısa süre sonra tekrar deneyin.\n{err}",
                _ => $"[Gemini Hatası HTTP {(int)response.StatusCode}]: {err}"
            };
            yield return friendlyMsg;
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) continue;
            if (line.StartsWith("data: "))
            {
                string data = line["data: ".Length..].Trim();
                string? textChunk = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                    {
                        var candidate = candidates[0];
                        if (candidate.TryGetProperty("content", out var content) &&
                            content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                        {
                            textChunk = parts[0].GetProperty("text").GetString();
                        }
                    }
                }
                catch { }

                if (!string.IsNullOrEmpty(textChunk))
                {
                    yield return textChunk;
                }
            }
        }
    }

    private async IAsyncEnumerable<string> StreamAnthropicAsync(
        string apiKey,
        string model,
        List<ChatMessage> history,
        string systemPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            yield return "[Hata]: Anthropic Claude için API anahtarı boş. Lütfen geçerli bir Claude anahtarı ekleyin.";
            yield break;
        }

        string endpoint = "https://api.anthropic.com/v1/messages";

        var messages = new List<object>();
        foreach (var msg in history)
        {
            if (msg.IsSystem) continue;
            messages.Add(new { role = msg.IsUser ? "user" : "assistant", content = msg.Content });
        }

        var payload = new
        {
            model = model,
            messages = messages,
            max_tokens = 4096,
            system = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt,
            stream = true
        };

        var requestJson = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string err = await response.Content.ReadAsStringAsync(cancellationToken);
            string friendlyMsg = response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => $"[Claude Hatası HTTP 401 - Yetkisiz]: API anahtarı geçersiz veya yetkisiz.\n{err}",
                System.Net.HttpStatusCode.Forbidden => $"[Claude Hatası HTTP 403 - Erişim Reddedildi]: İzin yetersiz.\n{err}",
                System.Net.HttpStatusCode.TooManyRequests => $"[Claude Hatası HTTP 429 - Hız Sınırı]: İstek kotası doldu.\n{err}",
                _ => $"[Claude Hatası HTTP {(int)response.StatusCode}]: {err}"
            };
            yield return friendlyMsg;
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) continue;
            if (line.StartsWith("data: "))
            {
                string data = line["data: ".Length..].Trim();
                string? textChunk = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "content_block_delta")
                    {
                        if (doc.RootElement.TryGetProperty("delta", out var delta) && delta.TryGetProperty("text", out var text))
                        {
                            textChunk = text.GetString();
                        }
                    }
                }
                catch { }

                if (!string.IsNullOrEmpty(textChunk))
                {
                    yield return textChunk;
                }
            }
        }
    }
}
