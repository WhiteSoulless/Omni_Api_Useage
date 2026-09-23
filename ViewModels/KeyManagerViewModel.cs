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
        if (string.IsNullOrWhiteSpace(value))
        {
            DetectedProvider = "Henüz anahtar girilmedi";
            DetectionConfidence = "-";
            DetectionReason = "Bir API anahtarı yapıştırın, formatı anında analiz edilsin.";
            PatternMatch = "-";
            return;
        }

        var result = _detectorService.DetectProvider(value);
        DetectedProvider = result.CandidateProvider;
        DetectionConfidence = result.ConfidencePercentage;
        DetectionReason = result.Reason;
        PatternMatch = result.PatternMatch;

        if (result.CandidateProvider.Contains("Ollama") || result.CandidateProvider.Contains("Özel"))
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
        if (string.IsNullOrWhiteSpace(InputKey))
        {
            ProbeStatus = "Lütfen bir API anahtarı girin.";
            return;
        }

        IsProbing = true;
        ProbeStatus = "Anahtar test ediliyor ve modeller taranıyor...";

        try
        {
            var result = await _validatorService.ValidateAndProbeKeyAsync(InputKey, DetectedProvider, CustomBaseUrl);
            IsProbing = false;

            if (result.IsSuccess)
            {
                ProbeStatus = $"Başarılı! {result.Message}";

                // Check if existing
                var existing = SavedKeys.FirstOrDefault(k => k.Key == InputKey.Trim());
                if (existing != null)
                {
                    existing.Provider = result.DetectedProvider;
                    existing.IsValid = true;
                    existing.LastValidated = DateTime.Now;
                    existing.StatusMessage = result.Message;
                    existing.DiscoveredModels = result.DiscoveredModels;
                    existing.CustomBaseUrl = CustomBaseUrl;
                }
                else
                {
                    var newEntry = new ApiKeyEntry
                    {
                        Key = InputKey.Trim(),
                        Provider = result.DetectedProvider,
                        IsValid = true,
                        LastValidated = DateTime.Now,
                        StatusMessage = result.Message,
                        DiscoveredModels = result.DiscoveredModels,
                        CustomBaseUrl = CustomBaseUrl
                    };
                    SavedKeys.Insert(0, newEntry);
                }

                _storageService.SaveKeys(SavedKeys.ToList());
                OnKeysChanged?.Invoke();

                InputKey = string.Empty;
                CustomBaseUrl = string.Empty;
            }
            else
            {
                ProbeStatus = $"Doğrulama Başarısız: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            IsProbing = false;
            ProbeStatus = $"Hata oluştu: {ex.Message}";
        }
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
