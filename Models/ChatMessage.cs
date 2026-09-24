using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OmniKeyStudio.Models;

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    private string _role = "user"; // "user", "assistant", "system"

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private DateTime _timestamp = DateTime.Now;

    [ObservableProperty]
    private string _provider = string.Empty;

    [ObservableProperty]
    private string _model = string.Empty;

    [ObservableProperty]
    private long _responseTimeMs;

    [ObservableProperty]
    private double? _tokensPerSec;

    public bool IsUser => Role.Equals("user", StringComparison.OrdinalIgnoreCase);
    public bool IsAssistant => Role.Equals("assistant", StringComparison.OrdinalIgnoreCase);
    public bool IsSystem => Role.Equals("system", StringComparison.OrdinalIgnoreCase);

    public string FormattedTime => Timestamp.ToString("HH:mm:ss");

    public string StatsText => ResponseTimeMs > 0 
        ? $"⚡ {ResponseTimeMs} ms {(TokensPerSec.HasValue ? $"• {TokensPerSec.Value:F1} tok/s" : "")}" 
        : string.Empty;

    partial void OnResponseTimeMsChanged(long value) => OnPropertyChanged(nameof(StatsText));
    partial void OnTokensPerSecChanged(double? value) => OnPropertyChanged(nameof(StatsText));
}
