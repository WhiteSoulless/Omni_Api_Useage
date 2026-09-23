using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OmniKeyStudio.Models;

namespace OmniKeyStudio.Services;

public class KeyDetectorService
{
    // Regex Patterns for major AI providers
    private static readonly Regex GeminiPattern = new(@"^AIzaSy[a-zA-Z0-9_\-]{33}$", RegexOptions.Compiled);
    private static readonly Regex AnthropicPattern = new(@"^sk-ant-(api\d{2}-)?[a-zA-Z0-9_\-]{80,}$", RegexOptions.Compiled);
    private static readonly Regex GroqPattern = new(@"^gsk_[a-zA-Z0-9]{52,}$", RegexOptions.Compiled);
    private static readonly Regex OpenRouterPattern = new(@"^sk-or-v1-[a-f0-9]{64}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex OpenAiProjectPattern = new(@"^sk-proj-[a-zA-Z0-9_\-]{50,}$", RegexOptions.Compiled);
    private static readonly Regex HuggingFacePattern = new(@"^hf_[a-zA-Z0-9]{34,}$", RegexOptions.Compiled);
    private static readonly Regex GitHubPattern = new(@"^(ghp_[a-zA-Z0-9]{36}|github_pat_[a-zA-Z0-9_]{82})$", RegexOptions.Compiled);
    private static readonly Regex PerplexityPattern = new(@"^pplx-[a-f0-9]{48,}$", RegexOptions.Compiled);
    private static readonly Regex GenericSkPattern = new(@"^sk-[a-zA-Z0-9_\-]{20,}$", RegexOptions.Compiled);
    private static readonly Regex MistralPattern = new(@"^[a-zA-Z0-9]{32}$", RegexOptions.Compiled);

    public DetectionResult DetectProvider(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return new DetectionResult
            {
                CandidateProvider = "Boş Anahtar",
                Confidence = 0.0,
                Reason = "Lütfen bir API anahtarı girin veya yapıştırın."
            };
        }

        string trimmed = key.Trim();

        // 1. Google Gemini
        if (GeminiPattern.IsMatch(trimmed) || (trimmed.StartsWith("AIzaSy") && trimmed.Length >= 35))
        {
            return new DetectionResult
            {
                CandidateProvider = "Google Gemini",
                Confidence = 0.99,
                Reason = "Google AI Studio / Gemini 'AIzaSy...' imzası tespit edildi.",
                PatternMatch = "AIzaSy*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 2. Groq (Ultra fast Llama/Mixtral)
        if (GroqPattern.IsMatch(trimmed) || trimmed.StartsWith("gsk_"))
        {
            return new DetectionResult
            {
                CandidateProvider = "Groq",
                Confidence = 0.99,
                Reason = "Groq Ultra-Fast LPU Cloud 'gsk_...' imzası tespit edildi.",
                PatternMatch = "gsk_*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 3. OpenRouter
        if (OpenRouterPattern.IsMatch(trimmed) || trimmed.StartsWith("sk-or-v1-"))
        {
            return new DetectionResult
            {
                CandidateProvider = "OpenRouter",
                Confidence = 0.99,
                Reason = "OpenRouter 'sk-or-v1-...' birleşik AI yönlendirici anahtarı tespit edildi.",
                PatternMatch = "sk-or-v1-*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 4. Anthropic Claude
        if (AnthropicPattern.IsMatch(trimmed) || trimmed.StartsWith("sk-ant-"))
        {
            return new DetectionResult
            {
                CandidateProvider = "Anthropic Claude",
                Confidence = 0.99,
                Reason = "Anthropic Claude 'sk-ant-...' resmi formatı tespit edildi.",
                PatternMatch = "sk-ant-*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 5. OpenAI Project Key
        if (OpenAiProjectPattern.IsMatch(trimmed) || trimmed.StartsWith("sk-proj-"))
        {
            return new DetectionResult
            {
                CandidateProvider = "OpenAI",
                Confidence = 0.98,
                Reason = "OpenAI güncel proje anahtarı 'sk-proj-...' tespit edildi.",
                PatternMatch = "sk-proj-*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 6. Hugging Face
        if (HuggingFacePattern.IsMatch(trimmed) || trimmed.StartsWith("hf_"))
        {
            return new DetectionResult
            {
                CandidateProvider = "Hugging Face",
                Confidence = 0.98,
                Reason = "Hugging Face Kullanıcı Erişim Token'ı 'hf_...' tespit edildi.",
                PatternMatch = "hf_*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 7. GitHub Models PAT
        if (GitHubPattern.IsMatch(trimmed) || trimmed.StartsWith("ghp_") || trimmed.StartsWith("github_pat_"))
        {
            return new DetectionResult
            {
                CandidateProvider = "GitHub Models",
                Confidence = 0.98,
                Reason = "GitHub Personal Access Token (Models/Marketplace) tespit edildi.",
                PatternMatch = "ghp_* / github_pat_*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 8. Perplexity
        if (PerplexityPattern.IsMatch(trimmed) || trimmed.StartsWith("pplx-"))
        {
            return new DetectionResult
            {
                CandidateProvider = "Perplexity AI",
                Confidence = 0.98,
                Reason = "Perplexity sonar API anahtarı 'pplx-...' tespit edildi.",
                PatternMatch = "pplx-*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 9. Generic "sk-..." format -> Could be OpenAI, DeepSeek, Mistral, or custom OpenAI-compatible server
        if (GenericSkPattern.IsMatch(trimmed) || trimmed.StartsWith("sk-"))
        {
            return new DetectionResult
            {
                CandidateProvider = "OpenAI / DeepSeek",
                Confidence = 0.70,
                Reason = "Standart 'sk-...' ön eki tespit edildi. Bu anahtar OpenAI, DeepSeek veya yerel bir proxy olabilir.",
                PatternMatch = "sk-*",
                RequiresProbing = true,
                AlternativeProviders = new List<string> { "OpenAI", "DeepSeek", "OpenRouter", "Mistral" }
            };
        }

        // 10. Mistral 32 hex chars
        if (MistralPattern.IsMatch(trimmed))
        {
            return new DetectionResult
            {
                CandidateProvider = "Mistral AI",
                Confidence = 0.65,
                Reason = "32 karakterli standart alfanümerik Mistral anahtar formatı.",
                PatternMatch = "32-char hex",
                RequiresProbing = true,
                AlternativeProviders = new List<string> { "Mistral AI", "Cohere", "Custom" }
            };
        }

        // Unknown
        return new DetectionResult
        {
            CandidateProvider = "Özel / Tanımlanamadı",
            Confidence = 0.20,
            Reason = "Biçim bilinen ana sağlayıcı kalıplarına tam uymuyor. Özel bir OpenAI-uyumlu sunucu veya yerel endpoint olabilir.",
            PatternMatch = "Bilinmeyen",
            RequiresProbing = true,
            AlternativeProviders = new List<string> { "Özel OpenAI-Uyumlu Sunucu", "Ollama", "LM Studio" }
        };
    }
}
