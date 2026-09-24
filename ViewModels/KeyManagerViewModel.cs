using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniKeyStudio.Models;
using OmniKeyStudio.Services;

namespace OmniKeyStudio.ViewModels;

public partial class KeyManagerViewModel : ObservableObject
{
    private readonly KeyDetectorService _detectorService;
    private readonly KeyValidatorService _validatorService;
    private readonly SecureStorageService _storageService;

    [ObservableProperty]
    private string _inputKey = string.Empty;

    [ObservableProperty]
    private string _customBaseUrl = string.Empty;

    [ObservableProperty]
    private string _detectedProvider = "Henüz anahtar girilmedi";

    [ObservableProperty]
    private string _selectedProvider = "Google Gemini";

    [ObservableProperty]
    private string _detectionConfidence = "-";

    [ObservableProperty]
    private string _detectionReason = "Bir API anahtarı yapıştırın, formatı anında analiz edilsin.";

    [ObservableProperty]
    private string _patternMatch = "-";

    [ObservableProperty]
    private bool _isProbing;

    [ObservableProperty]
    private string _probeStatus = string.Empty;

    [ObservableProperty]
    private ApiKeyEntry? _selectedKey;

    public ObservableCollection<string> AllProviders { get; } = new()
    {
        "Google Gemini",
        "Groq",
        "OpenAI",
        "DeepSeek",
        "OpenRouter",
        "Anthropic Claude",
        "xAI (Grok)",
        "Mistral AI",
        "Perplexity AI",
        "Cerebras",
        "Hugging Face",
        "GitHub Models",
        "Yerel Ollama",
        "Özel / Custom"
    };

    public ObservableCollection<ApiKeyEntry> SavedKeys { get; } = new();

    public event Action? OnKeysChanged;

    public KeyManagerViewModel(
        KeyDetectorService detectorService,
        KeyValidatorService validatorService,
        SecureStorageService storageService)
    {
        _detectorService = detectorService;
        _validatorService = validatorService;
        _storageService = storageService;

        LoadExistingKeys();
    }

    private void LoadExistingKeys()
    {
        SavedKeys.Clear();
        var keys = _storageService.LoadKeys();
        foreach (var key in keys)
        {
            SavedKeys.Add(key);
        }
    }

    partial void OnInputKeyChanged(string value)
    {
        string clean = KeyDetectorService.CleanKey(value);

        if (string.IsNullOrWhiteSpace(clean))
        {
            DetectedProvider = "Henüz anahtar girilmedi";
            DetectionConfidence = "-";
            DetectionReason = "Bir API anahtarı yapıştırın, formatı anında analiz edilsin.";
            PatternMatch = "-";
            return;
        }

        var result = _detectorService.DetectProvider(clean);
        DetectedProvider = result.CandidateProvider;
        DetectionConfidence = result.ConfidencePercentage;
        DetectionReason = result.Reason;
        PatternMatch = result.PatternMatch;

        // Auto-select in dropdown if known
        if (AllProviders.Contains(result.CandidateProvider))
        {
            SelectedProvider = result.CandidateProvider;
        }
        else if (result.CandidateProvider.Contains("Gemini"))
        {
            SelectedProvider = "Google Gemini";
        }
        else if (result.CandidateProvider.Contains("Groq"))
        {
            SelectedProvider = "Groq";
        }
        else if (result.CandidateProvider.Contains("DeepSeek"))
        {
            SelectedProvider = "DeepSeek";
        }
        else if (result.CandidateProvider.Contains("OpenAI"))
        {
            SelectedProvider = "OpenAI";
        }

        if (SelectedProvider.Contains("Ollama") || SelectedProvider.Contains("Özel"))
        {
            if (string.IsNullOrWhiteSpace(CustomBaseUrl))
            {
                CustomBaseUrl = "http://localhost:11434/v1";
            }
        }
    }

    [RelayCommand]
    private async Task ValidateAndSaveAsync()
    {
        string cleanKey = KeyDetectorService.CleanKey(InputKey);
        if (string.IsNullOrWhiteSpace(cleanKey))
        {
            ProbeStatus = "Lütfen bir API anahtarı girin.";
            return;
        }

        IsProbing = true;
        ProbeStatus = $"{SelectedProvider} anahtarı canlı test ediliyor ve modeller taranıyor...";

        try
        {
            var result = await _validatorService.ValidateAndProbeKeyAsync(cleanKey, SelectedProvider, CustomBaseUrl);
            IsProbing = false;

            if (result.IsSuccess)
            {
                ProbeStatus = $"Başarılı! {result.Message}";
                SaveKeyEntry(cleanKey, result.DetectedProvider, true, result.Message, result.DiscoveredModels);
                InputKey = string.Empty;
                CustomBaseUrl = string.Empty;
            }
            else
            {
                ProbeStatus = $"⚠️ Doğrulama Uyarısı: {result.Message}\n💡 Not: Anahtarınızın doğru olduğundan eminseniz aşağıdaki '💾 Doğrulamadan Kaydet' butonu ile doğrudan ekleyebilirsiniz.";
            }
        }
        catch (Exception ex)
        {
            IsProbing = false;
            ProbeStatus = $"Bağlantı hatası: {ex.Message}. '💾 Doğrulamadan Kaydet' butonu ile anahtarı yine de ekleyebilirsiniz.";
        }
    }

    [RelayCommand]
    private void SaveDirectly()
    {
        string cleanKey = KeyDetectorService.CleanKey(InputKey);
        if (string.IsNullOrWhiteSpace(cleanKey))
        {
            ProbeStatus = "Lütfen bir API anahtarı girin.";
            return;
        }

        string provider = !string.IsNullOrWhiteSpace(SelectedProvider) ? SelectedProvider : "Özel / Custom";
        var defaultModels = KeyValidatorService.GetDefaultModelsForProvider(provider);

        SaveKeyEntry(cleanKey, provider, true, "Kullanıcı tarafından doğrudan eklendi", defaultModels);
        ProbeStatus = $"✅ {provider} anahtarı başarıyla kasaya eklendi ({defaultModels.Count} model hazır).";

        InputKey = string.Empty;
        CustomBaseUrl = string.Empty;
    }

    private void SaveKeyEntry(string key, string provider, bool isValid, string message, System.Collections.Generic.List<string> models)
    {
        var existing = SavedKeys.FirstOrDefault(k => k.Key == key);
        if (existing != null)
        {
            existing.Provider = provider;
            existing.IsValid = isValid;
            existing.LastValidated = DateTime.Now;
            existing.StatusMessage = message;
            existing.DiscoveredModels = models;
            existing.CustomBaseUrl = CustomBaseUrl;
        }
        else
        {
            var newEntry = new ApiKeyEntry
            {
                Key = key,
                Provider = provider,
                IsValid = isValid,
                LastValidated = DateTime.Now,
                StatusMessage = message,
                DiscoveredModels = models,
                CustomBaseUrl = CustomBaseUrl
            };
            SavedKeys.Insert(0, newEntry);
        }

        _storageService.SaveKeys(SavedKeys.ToList());
        OnKeysChanged?.Invoke();
    }

    [RelayCommand]
    private void DeleteKey(ApiKeyEntry? entry)
    {
        var target = entry ?? SelectedKey;
        if (target != null && SavedKeys.Contains(target))
        {
            SavedKeys.Remove(target);
            _storageService.SaveKeys(SavedKeys.ToList());
            OnKeysChanged?.Invoke();
            ProbeStatus = $"{target.Provider} anahtarı güvenli kasadan silindi.";
        }
    }

    [RelayCommand]
    private async Task TestSelectedKeyAsync(ApiKeyEntry? entry)
    {
        var target = entry ?? SelectedKey;
        if (target == null) return;

        IsProbing = true;
        ProbeStatus = $"{target.Provider} anahtarı yeniden test ediliyor...";

        var result = await _validatorService.ValidateAndProbeKeyAsync(target.Key, target.Provider, target.CustomBaseUrl);
        IsProbing = false;

        target.IsValid = result.IsSuccess;
        target.LastValidated = DateTime.Now;
        target.StatusMessage = result.Message;
        if (result.DiscoveredModels.Count > 0)
        {
            target.DiscoveredModels = result.DiscoveredModels;
        }

        _storageService.SaveKeys(SavedKeys.ToList());
        OnKeysChanged?.Invoke();
        ProbeStatus = result.Message;
    }

    [RelayCommand]
    private void ClearInput()
    {
        InputKey = string.Empty;
        CustomBaseUrl = string.Empty;
        ProbeStatus = string.Empty;
    }
}
