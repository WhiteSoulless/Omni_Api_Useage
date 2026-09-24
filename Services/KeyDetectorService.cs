using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OmniKeyStudio.Models;

namespace OmniKeyStudio.Services;

public class KeyDetectorService
{
    // Regex Patterns for major AI providers
    private static readonly Regex GeminiPattern = new(@"^AIzaSy[a-zA-Z0-9_\-]{20,}$", RegexOptions.Compiled);
    private static readonly Regex AnthropicPattern = new(@"^sk-ant-(api\d{2}-)?[a-zA-Z0-9_\-]{20,}$", RegexOptions.Compiled);
    private static readonly Regex GroqPattern = new(@"^gsk_[a-zA-Z0-9_\-]{20,}$", RegexOptions.Compiled);
    private static readonly Regex OpenRouterPattern = new(@"^sk-or-v1-[a-zA-Z0-9_\-]{16,}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex OpenAiProjectPattern = new(@"^sk-proj-[a-zA-Z0-9_\-]{20,}$", RegexOptions.Compiled);
    private static readonly Regex OpenAiAdminPattern = new(@"^(sk-admin-|sk-svcacct-)[a-zA-Z0-9_\-]{20,}$", RegexOptions.Compiled);
    private static readonly Regex XAiPattern = new(@"^xai-[a-zA-Z0-9_\-]{20,}$", RegexOptions.Compiled);
    private static readonly Regex CerebrasPattern = new(@"^csk-[a-zA-Z0-9_\-]{20,}$", RegexOptions.Compiled);
    private static readonly Regex HuggingFacePattern = new(@"^hf_[a-zA-Z0-9]{20,}$", RegexOptions.Compiled);
    private static readonly Regex GitHubPattern = new(@"^(ghp_[a-zA-Z0-9]{20,}|github_pat_[a-zA-Z0-9_]{20,})$", RegexOptions.Compiled);
    private static readonly Regex PerplexityPattern = new(@"^pplx-[a-zA-Z0-9_\-]{20,}$", RegexOptions.Compiled);
    private static readonly Regex GenericSkPattern = new(@"^sk-[a-zA-Z0-9_\-]{15,}$", RegexOptions.Compiled);
    private static readonly Regex MistralPattern = new(@"^[a-zA-Z0-9]{32}$", RegexOptions.Compiled);

    public static string CleanKey(string? rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey)) return string.Empty;

        string clean = rawKey.Trim();

        // Remove surrounding quotes ("...", '...')
        if ((clean.StartsWith("\"") && clean.EndsWith("\"")) || (clean.StartsWith("'") && clean.EndsWith("'")))
        {
            clean = clean[1..^1].Trim();
        }

        // Remove "Bearer " prefix if user copied auth header
        if (clean.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean["Bearer ".Length..].Trim();
        }

        // Remove "export API_KEY=" or "KEY=" prefix if copied from .env or shell
        int equalsIdx = clean.IndexOf('=');
        if (equalsIdx > 0 && equalsIdx < 30 && !clean.Contains(' '))
        {
            string prefix = clean[..equalsIdx];
            if (prefix.Contains("KEY", StringComparison.OrdinalIgnoreCase) || 
                prefix.Contains("TOKEN", StringComparison.OrdinalIgnoreCase) ||
                prefix.Contains("API", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[(equalsIdx + 1)..].Trim();
                // Check quotes again
                if ((clean.StartsWith("\"") && clean.EndsWith("\"")) || (clean.StartsWith("'") && clean.EndsWith("'")))
                {
                    clean = clean[1..^1].Trim();
                }
            }
        }

        return clean;
    }

    public DetectionResult DetectProvider(string key)
    {
        string trimmed = CleanKey(key);

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new DetectionResult
            {
                CandidateProvider = "Boş Anahtar",
                Confidence = 0.0,
                Reason = "Lütfen bir API anahtarı girin veya yapıştırın."
            };
        }

        // 1. Google Gemini (Google AI Studio)
        if (GeminiPattern.IsMatch(trimmed) || trimmed.StartsWith("AIzaSy"))
        {
            return new DetectionResult
            {
                CandidateProvider = "Google Gemini",
                Confidence = 0.99,
                Reason = "Google AI Studio / Gemini 'AIzaSy...' resmi anahtar biçimi tespit edildi.",
                PatternMatch = "AIzaSy*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 2. Groq (Ultra fast LPU Cloud)
        if (GroqPattern.IsMatch(trimmed) || trimmed.StartsWith("gsk_"))
        {
            return new DetectionResult
            {
                CandidateProvider = "Groq",
                Confidence = 0.99,
                Reason = "Groq LPU Cloud 'gsk_...' ultra hızlı model anahtarı tespit edildi.",
                PatternMatch = "gsk_*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 3. OpenRouter
        if (OpenRouterPattern.IsMatch(trimmed) || trimmed.StartsWith("sk-or-v1-") || trimmed.StartsWith("sk-or-"))
        {
            return new DetectionResult
            {
                CandidateProvider = "OpenRouter",
                Confidence = 0.99,
                Reason = "OpenRouter 'sk-or-v1-...' birleşik AI sağlayıcı anahtarı tespit edildi.",
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
                Reason = "Anthropic Claude 'sk-ant-...' resmi API formatı tespit edildi.",
                PatternMatch = "sk-ant-*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 5. OpenAI Project / Service Account Key
        if (OpenAiProjectPattern.IsMatch(trimmed) || trimmed.StartsWith("sk-proj-"))
        {
            return new DetectionResult
            {
                CandidateProvider = "OpenAI",
                Confidence = 0.99,
                Reason = "OpenAI güncel proje anahtarı 'sk-proj-...' tespit edildi.",
                PatternMatch = "sk-proj-*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        if (OpenAiAdminPattern.IsMatch(trimmed) || trimmed.StartsWith("sk-admin-") || trimmed.StartsWith("sk-svcacct-"))
        {
            return new DetectionResult
            {
                CandidateProvider = "OpenAI",
                Confidence = 0.99,
                Reason = "OpenAI Yönetici/Servis anahtarı tespit edildi.",
                PatternMatch = "sk-admin-* / sk-svcacct-*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 6. xAI (Grok)
        if (XAiPattern.IsMatch(trimmed) || trimmed.StartsWith("xai-"))
        {
            return new DetectionResult
            {
                CandidateProvider = "xAI (Grok)",
                Confidence = 0.98,
                Reason = "xAI Grok 'xai-...' resmi API anahtarı tespit edildi.",
                PatternMatch = "xai-*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 7. Cerebras
        if (CerebrasPattern.IsMatch(trimmed) || trimmed.StartsWith("csk-"))
        {
            return new DetectionResult
            {
                CandidateProvider = "Cerebras",
                Confidence = 0.98,
                Reason = "Cerebras AI 'csk-...' donanım hızlandırma anahtarı tespit edildi.",
                PatternMatch = "csk-*",
                RequiresProbing = false,
                AlternativeProviders = new List<string>()
            };
        }

        // 8. Hugging Face
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

        // 9. GitHub Models PAT
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

        // 10. Perplexity
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

        // 11. Generic "sk-..." format -> DeepSeek or OpenAI legacy
        if (GenericSkPattern.IsMatch(trimmed) || trimmed.StartsWith("sk-"))
        {
            // DeepSeek keys are typically 32-35 characters hex after sk-
            bool looksLikeDeepSeek = trimmed.Length >= 32 && trimmed.Length <= 40;
            string primaryGuess = looksLikeDeepSeek ? "DeepSeek" : "OpenAI";

            return new DetectionResult
            {
                CandidateProvider = primaryGuess,
                Confidence = 0.85,
                Reason = $"'{trimmed[..Math.Min(7, trimmed.Length)]}...' ön eki tespit edildi. ({primaryGuess} veya OpenAI uyumlu servis). Aşağıdaki açılır listeden sağlayıcıyı değiştirebilirsiniz.",
                PatternMatch = "sk-*",
                RequiresProbing = true,
                AlternativeProviders = new List<string> { "DeepSeek", "OpenAI", "Mistral AI", "Özel API" }
            };
        }

        // 12. Mistral 32 hex chars
        if (MistralPattern.IsMatch(trimmed) || trimmed.StartsWith("mistral_"))
        {
            return new DetectionResult
            {
                CandidateProvider = "Mistral AI",
                Confidence = 0.75,
                Reason = "32 karakterli standart Mistral anahtar formatı.",
                PatternMatch = "32-char hex",
                RequiresProbing = true,
                AlternativeProviders = new List<string> { "Mistral AI", "Cohere", "Custom" }
            };
        }

        // Unknown / Custom
        return new DetectionResult
        {
            CandidateProvider = "Özel / Custom",
            Confidence = 0.30,
            Reason = "Format bilinen standart ön eklere uymuyor. Sağlayıcıyı açılır listeden seçebilir veya doğrudan kaydedebilirsiniz.",
            PatternMatch = "Özel Format",
            RequiresProbing = true,
            AlternativeProviders = new List<string> { "Google Gemini", "Groq", "OpenAI", "DeepSeek", "Anthropic Claude", "Özel / Custom" }
        };
    }
}
