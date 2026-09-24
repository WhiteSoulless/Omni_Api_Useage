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

    public string FullTitle => $"{DisplayName} [{Provider}] - {SpeedTag} {(IsFree ? "★ Ücretsiz" : "")}";
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

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<ModelOption> AvailableModels { get; } = new();

    public ChatPlaygroundViewModel(LlmClientService llmService, SecureStorageService storageService)
    {
        _llmService = llmService;
        _storageService = storageService;

        RefreshModels();
    }

    public void RefreshModels()
    {
        AvailableModels.Clear();
        var keys = _storageService.LoadKeys();

        // 1. Add Groq Free & Ultra-Fast Models if key exists, or as template
        var groqKey = keys.FirstOrDefault(k => k.Provider == "Groq")?.Key ?? "";
        AddGroqModels(groqKey);

        // 2. Add Gemini Models
        var geminiKey = keys.FirstOrDefault(k => k.Provider == "Google Gemini")?.Key ?? "";
        AddGeminiModels(geminiKey);

        // 3. Add OpenRouter Models
        var openRouterKey = keys.FirstOrDefault(k => k.Provider == "OpenRouter")?.Key ?? "";
        AddOpenRouterModels(openRouterKey);

        // 4. Add OpenAI / DeepSeek / Mistral / Anthropic
        foreach (var keyEntry in keys.Where(k => k.IsValid))
        {
            if (keyEntry.Provider == "OpenAI")
            {
                AvailableModels.Add(new ModelOption { ModelId = "gpt-4o", DisplayName = "GPT-4o (Amiral)", Provider = "OpenAI", ApiKey = keyEntry.Key, SpeedTag = "🚀 Hızlı", IsFree = false });
                AvailableModels.Add(new ModelOption { ModelId = "gpt-4o-mini", DisplayName = "GPT-4o Mini", Provider = "OpenAI", ApiKey = keyEntry.Key, SpeedTag = "⚡ Çok Hızlı", IsFree = false });
            }
            else if (keyEntry.Provider == "DeepSeek")
            {
                AvailableModels.Add(new ModelOption { ModelId = "deepseek-chat", DisplayName = "DeepSeek V3 (Chat)", Provider = "DeepSeek", ApiKey = keyEntry.Key, SpeedTag = "⚡ Hızlı", IsFree = false });
                AvailableModels.Add(new ModelOption { ModelId = "deepseek-reasoner", DisplayName = "DeepSeek R1 (Reasoner)", Provider = "DeepSeek", ApiKey = keyEntry.Key, SpeedTag = "🧠 Akıl Yürütme", IsFree = false });
            }
            else if (keyEntry.Provider == "Anthropic Claude")
            {
                AvailableModels.Add(new ModelOption { ModelId = "claude-3-5-sonnet-20241022", DisplayName = "Claude 3.5 Sonnet", Provider = "Anthropic Claude", ApiKey = keyEntry.Key, SpeedTag = "🧠 Çok Zeki", IsFree = false });
                AvailableModels.Add(new ModelOption { ModelId = "claude-3-5-haiku-20241022", DisplayName = "Claude 3.5 Haiku", Provider = "Anthropic Claude", ApiKey = keyEntry.Key, SpeedTag = "⚡ Ultra Hızlı", IsFree = false });
            }
            else if (keyEntry.Provider == "xAI (Grok)")
            {
                AvailableModels.Add(new ModelOption { ModelId = "grok-beta", DisplayName = "Grok Beta", Provider = "xAI (Grok)", ApiKey = keyEntry.Key, SpeedTag = "🚀 Hızlı", IsFree = false });
                AvailableModels.Add(new ModelOption { ModelId = "grok-2-latest", DisplayName = "Grok 2", Provider = "xAI (Grok)", ApiKey = keyEntry.Key, SpeedTag = "🧠 Akıl Yürütme", IsFree = false });
            }
            else if (keyEntry.Provider == "Mistral AI")
            {
                AvailableModels.Add(new ModelOption { ModelId = "mistral-large-latest", DisplayName = "Mistral Large", Provider = "Mistral AI", ApiKey = keyEntry.Key, SpeedTag = "🚀 Güçlü", IsFree = false });
                AvailableModels.Add(new ModelOption { ModelId = "codestral-latest", DisplayName = "Codestral (Kod Uzmanı)", Provider = "Mistral AI", ApiKey = keyEntry.Key, SpeedTag = "⚡ Hızlı Kodlama", IsFree = false });
            }
            else if (keyEntry.Provider == "Perplexity AI")
            {
                AvailableModels.Add(new ModelOption { ModelId = "sonar", DisplayName = "Sonar (Web Arama)", Provider = "Perplexity AI", ApiKey = keyEntry.Key, SpeedTag = "🌐 Arama Destekli", IsFree = false });
            }
            else if (!string.IsNullOrWhiteSpace(keyEntry.CustomBaseUrl))
            {
                AvailableModels.Add(new ModelOption { ModelId = "default", DisplayName = $"{keyEntry.Provider} Model", Provider = keyEntry.Provider, ApiKey = keyEntry.Key, CustomBaseUrl = keyEntry.CustomBaseUrl, SpeedTag = "Özel", IsFree = false });
            }

            // Also add any discovered models from this key if not already present
            foreach (var discovered in keyEntry.DiscoveredModels.Take(10))
            {
                if (!AvailableModels.Any(m => m.ModelId == discovered && m.Provider == keyEntry.Provider))
                {
                    AvailableModels.Add(new ModelOption
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

        if (SelectedModel == null && AvailableModels.Count > 0)
        {
            SelectedModel = AvailableModels.FirstOrDefault(m => !string.IsNullOrEmpty(m.ApiKey)) ?? AvailableModels[0];
        }
    }

    private void AddGroqModels(string apiKey)
    {
        AvailableModels.Add(new ModelOption
        {
            ModelId = "llama-3.3-70b-versatile",
            DisplayName = "Llama 3.3 70B Versatile",
            Provider = "Groq",
            ApiKey = apiKey,
            SpeedTag = "⚡ 500+ tok/s Ultra Hızlı",
            IsFree = true
        });
        AvailableModels.Add(new ModelOption
        {
            ModelId = "llama-3.1-8b-instant",
            DisplayName = "Llama 3.1 8B Instant",
            Provider = "Groq",
            ApiKey = apiKey,
            SpeedTag = "⚡ 750+ tok/s Işık Hızında",
            IsFree = true
        });
        AvailableModels.Add(new ModelOption
        {
            ModelId = "mixtral-8x7b-32768",
            DisplayName = "Mixtral 8x7B (32k)",
            Provider = "Groq",
            ApiKey = apiKey,
            SpeedTag = "⚡ 450+ tok/s",
            IsFree = true
        });
    }

    private void AddGeminiModels(string apiKey)
    {
        AvailableModels.Add(new ModelOption
        {
            ModelId = "gemini-2.0-flash",
            DisplayName = "Gemini 2.0 Flash",
            Provider = "Google Gemini",
            ApiKey = apiKey,
            SpeedTag = "🚀 Son Nesil Hızlı",
            IsFree = true
        });
        AvailableModels.Add(new ModelOption
        {
            ModelId = "gemini-1.5-flash",
            DisplayName = "Gemini 1.5 Flash",
            Provider = "Google Gemini",
            ApiKey = apiKey,
            SpeedTag = "⚡ 1M Bağlam Hızlı",
            IsFree = true
        });
    }

    private void AddOpenRouterModels(string apiKey)
    {
        AvailableModels.Add(new ModelOption
        {
            ModelId = "meta-llama/llama-3.3-70b-instruct:free",
            DisplayName = "Llama 3.3 70B (Free)",
            Provider = "OpenRouter",
            ApiKey = apiKey,
            SpeedTag = "★ Tamamen Ücretsiz",
            IsFree = true
        });
        AvailableModels.Add(new ModelOption
        {
            ModelId = "google/gemini-2.0-flash-exp:free",
            DisplayName = "Gemini 2.0 Flash (Free)",
            Provider = "OpenRouter",
            ApiKey = apiKey,
            SpeedTag = "★ Tamamen Ücretsiz",
            IsFree = true
        });
        AvailableModels.Add(new ModelOption
        {
            ModelId = "deepseek/deepseek-r1:free",
            DisplayName = "DeepSeek R1 (Free)",
            Provider = "OpenRouter",
            ApiKey = apiKey,
            SpeedTag = "★ Tamamen Ücretsiz",
            IsFree = true
        });
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

        if (string.IsNullOrWhiteSpace(SelectedModel.ApiKey))
        {
            StreamingStatus = $"Uyarı: {SelectedModel.Provider} için kaydedilmiş bir API anahtarı bulunamadı. Lütfen 'Anahtar Yöneticisi' sekmesinden bir anahtar ekleyin.";
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

                // Force WPF update on content change
                OnPropertyChanged(nameof(Messages));
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
            assistantMsg.Content += $"\n[Hata]: {ex.Message}";
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
