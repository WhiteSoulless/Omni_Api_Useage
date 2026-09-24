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

    public void RefreshModels(string? preferredProvider = null)
    {
        AvailableModels.Clear();
        var keys = _storageService.LoadKeys();

        // 1. Gather keys map
        var groqKey = keys.FirstOrDefault(k => k.Provider == "Groq")?.Key ?? "";
        var geminiKey = keys.FirstOrDefault(k => k.Provider == "Google Gemini")?.Key ?? "";
        var openRouterKey = keys.FirstOrDefault(k => k.Provider == "OpenRouter")?.Key ?? "";
        var openAiKey = keys.FirstOrDefault(k => k.Provider == "OpenAI")?.Key ?? "";
        var deepSeekKey = keys.FirstOrDefault(k => k.Provider == "DeepSeek")?.Key ?? "";
        var claudeKey = keys.FirstOrDefault(k => k.Provider == "Anthropic Claude")?.Key ?? "";
        var xAiKey = keys.FirstOrDefault(k => k.Provider == "xAI (Grok)")?.Key ?? "";
        var mistralKey = keys.FirstOrDefault(k => k.Provider == "Mistral AI")?.Key ?? "";
        var perplexityKey = keys.FirstOrDefault(k => k.Provider == "Perplexity AI")?.Key ?? "";

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
        allList.Add(new ModelOption { ModelId = "meta-llama/llama-3.3-70b-instruct:free", DisplayName = "Llama 3.3 70B (Free)", Provider = "OpenRouter", ApiKey = openRouterKey, SpeedTag = "★ Tamamen Ücretsiz", IsFree = true });
        allList.Add(new ModelOption { ModelId = "deepseek/deepseek-r1:free", DisplayName = "DeepSeek R1 (Free)", Provider = "OpenRouter", ApiKey = openRouterKey, SpeedTag = "★ Tamamen Ücretsiz", IsFree = true });
        allList.Add(new ModelOption { ModelId = "google/gemini-2.0-flash-exp:free", DisplayName = "Gemini 2.0 Flash (Free)", Provider = "OpenRouter", ApiKey = openRouterKey, SpeedTag = "★ Tamamen Ücretsiz", IsFree = true });

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
            SelectedModel = AvailableModels.FirstOrDefault(m => m.Provider == preferredProvider) ?? AvailableModels.FirstOrDefault();
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

        if (SelectedModel == null)
        {
            StreamingStatus = "Lütfen önce bir model seçin.";
            return;
        }

        // Dynamically resolve key if empty
        if (string.IsNullOrWhiteSpace(SelectedModel.ApiKey))
        {
            var keys = _storageService.LoadKeys();
            var matchingKey = keys.FirstOrDefault(k => k.Provider == SelectedModel.Provider && !string.IsNullOrWhiteSpace(k.Key));
            if (matchingKey != null)
            {
                SelectedModel.ApiKey = matchingKey.Key;
            }
            else
            {
                // Find ANY valid key the user has
                var anyAvailableKey = keys.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.Key));
                if (anyAvailableKey != null)
                {
                    var switchModel = AvailableModels.FirstOrDefault(m => m.Provider == anyAvailableKey.Provider && m.HasKey);
                    if (switchModel != null)
                    {
                        SelectedModel = switchModel;
                        StreamingStatus = $"'{SelectedModel.Provider}' için kayıtlı anahtarınız ({anyAvailableKey.MaskedKey}) bulundu ve otomatik seçildi. Yanıt alınıyor...";
                    }
                }
                else
                {
                    StreamingStatus = $"⚠️ Uyarı: Henüz hiçbir API anahtarı kaydedilmedi. Lütfen 'Anahtar Yöneticisi' sekmesinden bir anahtar ekleyin veya yukarıdaki hızlı anahtar kutusuna yapıştırın.";
                    return;
                }
            }
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
