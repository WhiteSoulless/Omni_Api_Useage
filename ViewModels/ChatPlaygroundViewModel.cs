using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniKeyStudio.Models;
using OmniKeyStudio.Services;

namespace OmniKeyStudio.ViewModels;

public class ModelOption
{
    public string ModelId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string CustomBaseUrl { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public string SpeedTag { get; set; } = string.Empty;

    public bool HasKey => !string.IsNullOrWhiteSpace(ApiKey);

    public string FullTitle => HasKey 
        ? $"🟢 {DisplayName} [{Provider}] - {SpeedTag} (Aktif)" 
        : $"⚪ {DisplayName} [{Provider}] - {SpeedTag} (Anahtar Yok)";
}

public partial class ChatPlaygroundViewModel : ObservableObject
{
    private readonly LlmClientService _llmService;
    private readonly SecureStorageService _storageService;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _systemPrompt = "Sen yardımsever, hızlı ve zeki bir yapay zeka asistanısın.";

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private string _streamingStatus = "Hazır";

    [ObservableProperty]
    private ModelOption? _selectedModel;

    [ObservableProperty]
    private string _quickKeyInput = string.Empty;

    [ObservableProperty]
    private string _activeKeyStatusText = "Kayıtlı anahtar aranıyor...";

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<ModelOption> AvailableModels { get; } = new();

    public event Action? OnNavigateToKeyManager;

    public ChatPlaygroundViewModel(LlmClientService llmService, SecureStorageService storageService)
    {
        _llmService = llmService;
        _storageService = storageService;

        RefreshModels();
    }

    public static string FindKeyForProvider(IEnumerable<ApiKeyEntry> keys, string provider)
    {
        if (keys == null || string.IsNullOrWhiteSpace(provider)) return string.Empty;

        var validKeys = keys.Where(k => !string.IsNullOrWhiteSpace(k.Key)).ToList();
        if (validKeys.Count == 0) return string.Empty;

        // 1. Direct provider match
        var match = validKeys.FirstOrDefault(k => string.Equals(k.Provider, provider, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match.Key;

        // 2. Specific key prefix matching
        if (provider.Contains("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            var gemKey = validKeys.FirstOrDefault(k => k.Key.StartsWith("AQ.") || k.Key.StartsWith("AIzaSy"));
            if (gemKey != null) return gemKey.Key;
        }
        if (provider.Contains("Groq", StringComparison.OrdinalIgnoreCase))
        {
            var groqKey = validKeys.FirstOrDefault(k => k.Key.StartsWith("gsk_"));
            if (groqKey != null) return groqKey.Key;
        }
        if (provider.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase))
        {
            var orKey = validKeys.FirstOrDefault(k => k.Key.StartsWith("sk-or-"));
            if (orKey != null) return orKey.Key;
        }
        if (provider.Contains("Anthropic", StringComparison.OrdinalIgnoreCase) || provider.Contains("Claude", StringComparison.OrdinalIgnoreCase))
        {
            var antKey = validKeys.FirstOrDefault(k => k.Key.StartsWith("sk-ant-"));
            if (antKey != null) return antKey.Key;
        }

        // 3. Partial provider name match
        match = validKeys.FirstOrDefault(k => 
            k.Provider.Contains(provider, StringComparison.OrdinalIgnoreCase) || 
            provider.Contains(k.Provider, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match.Key;

        return string.Empty;
    }

    public void RefreshModels(string? preferredProvider = null)
    {
        AvailableModels.Clear();
        var keys = _storageService.LoadKeys();

        // 1. Gather keys map with robust matching
        var groqKey = FindKeyForProvider(keys, "Groq");
        var geminiKey = FindKeyForProvider(keys, "Google Gemini");
        var openRouterKey = FindKeyForProvider(keys, "OpenRouter");
        var openAiKey = FindKeyForProvider(keys, "OpenAI");
        var deepSeekKey = FindKeyForProvider(keys, "DeepSeek");
        var claudeKey = FindKeyForProvider(keys, "Anthropic Claude");
        var xAiKey = FindKeyForProvider(keys, "xAI (Grok)");
        var mistralKey = FindKeyForProvider(keys, "Mistral AI");
        var perplexityKey = FindKeyForProvider(keys, "Perplexity AI");

        var allList = new List<ModelOption>();

        // Google Gemini
        allList.Add(new ModelOption { ModelId = "gemini-2.0-flash", DisplayName = "Gemini 2.0 Flash", Provider = "Google Gemini", ApiKey = geminiKey, SpeedTag = "🚀 Yeni Nesil Hızlı", IsFree = true });
        allList.Add(new ModelOption { ModelId = "gemini-1.5-flash", DisplayName = "Gemini 1.5 Flash", Provider = "Google Gemini", ApiKey = geminiKey, SpeedTag = "⚡ 1M Bağlam Hızlı", IsFree = true });
        allList.Add(new ModelOption { ModelId = "gemini-1.5-pro", DisplayName = "Gemini 1.5 Pro", Provider = "Google Gemini", ApiKey = geminiKey, SpeedTag = "🧠 Derin Düşünme", IsFree = true });

        // Groq
        allList.Add(new ModelOption { ModelId = "llama-3.3-70b-versatile", DisplayName = "Llama 3.3 70B Versatile", Provider = "Groq", ApiKey = groqKey, SpeedTag = "⚡ 500+ tok/s Ultra Hızlı", IsFree = true });
        allList.Add(new ModelOption { ModelId = "llama-3.1-8b-instant", DisplayName = "Llama 3.1 8B Instant", Provider = "Groq", ApiKey = groqKey, SpeedTag = "⚡ 750+ tok/s Işık Hızında", IsFree = true });
        allList.Add(new ModelOption { ModelId = "mixtral-8x7b-32768", DisplayName = "Mixtral 8x7B (32k)", Provider = "Groq", ApiKey = groqKey, SpeedTag = "⚡ 450+ tok/s", IsFree = true });

        // DeepSeek
        allList.Add(new ModelOption { ModelId = "deepseek-chat", DisplayName = "DeepSeek V3 (Chat)", Provider = "DeepSeek", ApiKey = deepSeekKey, SpeedTag = "⚡ Hızlı & Zeki", IsFree = false });
        allList.Add(new ModelOption { ModelId = "deepseek-reasoner", DisplayName = "DeepSeek R1 (Reasoner)", Provider = "DeepSeek", ApiKey = deepSeekKey, SpeedTag = "🧠 Akıl Yürütme", IsFree = false });

        // OpenAI
        allList.Add(new ModelOption { ModelId = "gpt-4o", DisplayName = "GPT-4o (Amiral)", Provider = "OpenAI", ApiKey = openAiKey, SpeedTag = "🚀 Hızlı", IsFree = false });
        allList.Add(new ModelOption { ModelId = "gpt-4o-mini", DisplayName = "GPT-4o Mini", Provider = "OpenAI", ApiKey = openAiKey, SpeedTag = "⚡ Çok Hızlı", IsFree = false });

        // OpenRouter Free
        allList.Add(new ModelOption { ModelId = "meta-llama/llama-3.3-70b-instruct:free", DisplayName = "Llama 3.3 70B (Free)", Provider = "OpenRouter", ApiKey = openRouterKey, SpeedTag = "★ 0$ Model (OpenRouter Anahtarı Gerekir)", IsFree = true });
        allList.Add(new ModelOption { ModelId = "deepseek/deepseek-r1:free", DisplayName = "DeepSeek R1 (Free)", Provider = "OpenRouter", ApiKey = openRouterKey, SpeedTag = "★ 0$ Model (OpenRouter Anahtarı Gerekir)", IsFree = true });
        allList.Add(new ModelOption { ModelId = "google/gemini-2.0-flash-exp:free", DisplayName = "Gemini 2.0 Flash (Free)", Provider = "OpenRouter", ApiKey = openRouterKey, SpeedTag = "★ 0$ Model (OpenRouter Anahtarı Gerekir)", IsFree = true });

        // Anthropic Claude
        allList.Add(new ModelOption { ModelId = "claude-3-5-sonnet-20241022", DisplayName = "Claude 3.5 Sonnet", Provider = "Anthropic Claude", ApiKey = claudeKey, SpeedTag = "🧠 Çok Zeki", IsFree = false });
        allList.Add(new ModelOption { ModelId = "claude-3-5-haiku-20241022", DisplayName = "Claude 3.5 Haiku", Provider = "Anthropic Claude", ApiKey = claudeKey, SpeedTag = "⚡ Ultra Hızlı", IsFree = false });

        // xAI Grok
        allList.Add(new ModelOption { ModelId = "grok-beta", DisplayName = "Grok Beta", Provider = "xAI (Grok)", ApiKey = xAiKey, SpeedTag = "🚀 Hızlı", IsFree = false });
        allList.Add(new ModelOption { ModelId = "grok-2-latest", DisplayName = "Grok 2", Provider = "xAI (Grok)", ApiKey = xAiKey, SpeedTag = "🧠 Akıl Yürütme", IsFree = false });

        // Mistral
        allList.Add(new ModelOption { ModelId = "mistral-large-latest", DisplayName = "Mistral Large", Provider = "Mistral AI", ApiKey = mistralKey, SpeedTag = "🚀 Güçlü", IsFree = false });
        allList.Add(new ModelOption { ModelId = "codestral-latest", DisplayName = "Codestral (Kod Uzmanı)", Provider = "Mistral AI", ApiKey = mistralKey, SpeedTag = "⚡ Hızlı Kodlama", IsFree = false });

        // Perplexity
        allList.Add(new ModelOption { ModelId = "sonar", DisplayName = "Sonar (Web Arama)", Provider = "Perplexity AI", ApiKey = perplexityKey, SpeedTag = "🌐 Arama Destekli", IsFree = false });

        // Any custom discovered models
        foreach (var keyEntry in keys)
        {
            foreach (var discovered in keyEntry.DiscoveredModels.Take(10))
            {
                if (!allList.Any(m => m.ModelId == discovered && m.Provider == keyEntry.Provider))
                {
                    allList.Add(new ModelOption
                    {
                        ModelId = discovered,
                        DisplayName = discovered,
                        Provider = keyEntry.Provider,
                        ApiKey = keyEntry.Key,
                        CustomBaseUrl = keyEntry.CustomBaseUrl,
                        SpeedTag = "Keşfedilen Model",
                        IsFree = false
                    });
                }
            }
        }

        // SORT: Models WITH keys come FIRST!
        var sorted = allList.OrderByDescending(m => m.HasKey).ThenBy(m => m.Provider).ToList();
        foreach (var m in sorted)
        {
            AvailableModels.Add(m);
        }

        // Auto Select target:
        if (!string.IsNullOrWhiteSpace(preferredProvider))
        {
            SelectedModel = AvailableModels.FirstOrDefault(m => m.Provider == preferredProvider && m.HasKey)
                         ?? AvailableModels.FirstOrDefault(m => m.Provider == preferredProvider)
                         ?? AvailableModels.FirstOrDefault(m => m.HasKey)
                         ?? AvailableModels.FirstOrDefault();
        }
        else
        {
            // Pick first model that HAS a key
            SelectedModel = AvailableModels.FirstOrDefault(m => m.HasKey) ?? AvailableModels.FirstOrDefault();
        }

        UpdateActiveKeyStatus();
    }

    partial void OnSelectedModelChanged(ModelOption? value)
    {
        if (value != null && string.IsNullOrWhiteSpace(value.ApiKey))
        {
            var keys = _storageService.LoadKeys();
            string key = FindKeyForProvider(keys, value.Provider);
            if (!string.IsNullOrWhiteSpace(key))
            {
                value.ApiKey = key;
            }
        }
        UpdateActiveKeyStatus();
    }

    private void UpdateActiveKeyStatus()
    {
        if (SelectedModel == null)
        {
            ActiveKeyStatusText = "Model seçilmedi.";
            return;
        }

        if (SelectedModel.HasKey)
        {
            string masked = SelectedModel.ApiKey.Length > 8 
                ? $"{SelectedModel.ApiKey[..4]}...{SelectedModel.ApiKey[^4..]}" 
                : "****";
            ActiveKeyStatusText = $"✅ {SelectedModel.Provider} API Anahtarı Aktif ({masked})";
        }
        else
        {
            var keys = _storageService.LoadKeys();
            var anyKey = keys.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.Key));
            if (anyKey != null)
            {
                ActiveKeyStatusText = $"⚠️ Bu model ({SelectedModel.Provider}) için anahtar yok. (Kayıtlı: {anyKey.Provider})";
            }
            else
            {
                ActiveKeyStatusText = "⚠️ Henüz hiçbir API anahtarı eklenmedi.";
            }
        }
    }

    [RelayCommand]
    private void QuickSaveKey()
    {
        string clean = KeyDetectorService.CleanKey(QuickKeyInput);
        if (string.IsNullOrWhiteSpace(clean))
        {
            StreamingStatus = "Lütfen bir API anahtarı yapıştırın.";
            return;
        }

        var detector = new KeyDetectorService();
        var det = detector.DetectProvider(clean);
        string provider = det.CandidateProvider != "Boş Anahtar" && det.CandidateProvider != "Özel / Custom"
            ? det.CandidateProvider
            : (SelectedModel?.Provider ?? "Google Gemini");

        var keys = _storageService.LoadKeys();
        var existing = keys.FirstOrDefault(k => k.Key == clean);
        if (existing != null)
        {
            existing.Provider = provider;
            existing.IsValid = true;
        }
        else
        {
            keys.Insert(0, new ApiKeyEntry
            {
                Key = clean,
                Provider = provider,
                IsValid = true,
                StatusMessage = "Hızlı eklendi",
                DiscoveredModels = KeyValidatorService.GetDefaultModelsForProvider(provider)
            });
        }

        _storageService.SaveKeys(keys);
        QuickKeyInput = string.Empty;
        RefreshModels(provider);
        StreamingStatus = $"✅ {provider} anahtarı kaydedildi ve model aktif edildi! Mesajınızı yazabilirsiniz.";
    }

    [RelayCommand]
    private void GoToKeyManager()
    {
        OnNavigateToKeyManager?.Invoke();
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || IsGenerating) return;

        // Auto-save key if user pasted into quick key box
        if (!string.IsNullOrWhiteSpace(QuickKeyInput))
        {
            QuickSaveKey();
        }

        if (SelectedModel == null)
        {
            StreamingStatus = "Lütfen önce bir model seçin.";
            return;
        }

        // 1. Resolve key for SelectedModel if empty
        if (string.IsNullOrWhiteSpace(SelectedModel.ApiKey))
        {
            var keys = _storageService.LoadKeys();
            string matchingKey = FindKeyForProvider(keys, SelectedModel.Provider);
            if (!string.IsNullOrWhiteSpace(matchingKey))
            {
                SelectedModel.ApiKey = matchingKey;
            }
        }

        // 2. If STILL empty, check if we can switch to ANY model that HAS an active key
        if (string.IsNullOrWhiteSpace(SelectedModel.ApiKey))
        {
            var modelWithKey = AvailableModels.FirstOrDefault(m => m.HasKey && !string.IsNullOrWhiteSpace(m.ApiKey));
            if (modelWithKey != null)
            {
                SelectedModel = modelWithKey;
                StreamingStatus = $"'{SelectedModel.DisplayName}' [{SelectedModel.Provider}] için kayıtlı aktif anahtarınız bulundu ve otomatik seçildi.";
            }
        }

        // 3. If STILL empty: STOP! DO NOT SEND!
        if (string.IsNullOrWhiteSpace(SelectedModel.ApiKey))
        {
            StreamingStatus = $"⚠️ '{SelectedModel.Provider}' ({SelectedModel.DisplayName}) için API anahtarı girilmedi. Lütfen üstteki kutudan anahtarınızı yapıştırıp '⚡ Aktif Et'e basın veya 'Anahtar Kasası'ndan ekleyin.";
            return;
        }

        string userPrompt = InputText.Trim();
        InputText = string.Empty;

        var userMsg = new ChatMessage
        {
            Role = "user",
            Content = userPrompt,
            Model = SelectedModel.ModelId,
            Provider = SelectedModel.Provider
        };
        Messages.Add(userMsg);

        var assistantMsg = new ChatMessage
        {
            Role = "assistant",
            Content = "",
            Model = SelectedModel.ModelId,
            Provider = SelectedModel.Provider
        };
        Messages.Add(assistantMsg);

        IsGenerating = true;
        StreamingStatus = $"{SelectedModel.DisplayName} yanıt veriyor...";

        _cts = new CancellationTokenSource();
        var sw = Stopwatch.StartNew();
        int tokenCount = 0;

        try
        {
            var historyList = Messages.Take(Messages.Count - 1).ToList();

            await foreach (var chunk in _llmService.StreamChatAsync(
                SelectedModel.Provider,
                SelectedModel.ApiKey,
                SelectedModel.ModelId,
                historyList,
                SystemPrompt,
                SelectedModel.CustomBaseUrl,
                _cts.Token))
            {
                assistantMsg.Content += chunk;
                tokenCount += Math.Max(1, chunk.Length / 4);
            }

            sw.Stop();
            assistantMsg.ResponseTimeMs = sw.ElapsedMilliseconds;
            if (sw.ElapsedMilliseconds > 0 && tokenCount > 0)
            {
                assistantMsg.TokensPerSec = (double)tokenCount / (sw.ElapsedMilliseconds / 1000.0);
            }

            StreamingStatus = $"Tamamlandı! {sw.ElapsedMilliseconds} ms • ~{tokenCount} token";
        }
        catch (OperationCanceledException)
        {
            StreamingStatus = "Kullanıcı tarafından durduruldu.";
        }
        catch (Exception ex)
        {
            assistantMsg.Content += $"\n[Bağlantı Hatası]: {ex.Message}";
            StreamingStatus = $"Hata: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void StopGeneration()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void ClearChat()
    {
        Messages.Clear();
        StreamingStatus = "Sohbet temizlendi.";
    }

    [RelayCommand]
    private void CopyMessage(ChatMessage? msg)
    {
        if (msg != null && !string.IsNullOrEmpty(msg.Content))
        {
            Clipboard.SetText(msg.Content);
            StreamingStatus = "Mesaj panoya kopyalandı!";
        }
    }
}
