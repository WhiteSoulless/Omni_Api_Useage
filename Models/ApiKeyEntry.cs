using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniKeyStudio.Models;

public class ApiKeyEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Provider { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty; // Protected when saved
    public string CustomBaseUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastValidated { get; set; }
    public bool IsValid { get; set; }
    public string StatusMessage { get; set; } = "Henüz test edilmedi";
    public List<string> DiscoveredModels { get; set; } = new();
    public bool IsActive { get; set; } = true;

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
}
