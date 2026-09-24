using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OmniKeyStudio.Models;

public partial class ApiKeyEntry : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string _provider = string.Empty;

    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _customBaseUrl = string.Empty;

    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;

    [ObservableProperty]
    private DateTime? _lastValidated;

    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private string _statusMessage = "Henüz test edilmedi";

    [ObservableProperty]
    private List<string> _discoveredModels = new();

    [ObservableProperty]
    private bool _isActive = true;

    [JsonIgnore]
    public string MaskedKey
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Key)) return string.Empty;
            if (Key.Length <= 8) return new string('*', Key.Length);
            return $"{Key[..4]}...{Key[^4..]}";
        }
    }

    [JsonIgnore]
    public string DisplaySubtitle => $"{DiscoveredModels.Count} model bulundu • {(IsValid ? "✅ Doğrulandı" : "⚠️ Doğrulanmadı")}";

    partial void OnKeyChanged(string value) => OnPropertyChanged(nameof(MaskedKey));
    partial void OnIsValidChanged(bool value) => OnPropertyChanged(nameof(DisplaySubtitle));
    partial void OnDiscoveredModelsChanged(List<string> value) => OnPropertyChanged(nameof(DisplaySubtitle));
}
