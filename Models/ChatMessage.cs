using System;

namespace OmniKeyStudio.Models;

public class ChatMessage
{
    public string Role { get; set; } = "user"; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long ResponseTimeMs { get; set; }
    public double? TokensPerSec { get; set; }

    public bool IsUser => Role.Equals("user", StringComparison.OrdinalIgnoreCase);
    public bool IsAssistant => Role.Equals("assistant", StringComparison.OrdinalIgnoreCase);
    public bool IsSystem => Role.Equals("system", StringComparison.OrdinalIgnoreCase);

    public string FormattedTime => Timestamp.ToString("HH:mm:ss");
    public string StatsText => ResponseTimeMs > 0 
        ? $"⚡ {ResponseTimeMs} ms {(TokensPerSec.HasValue ? $"• {TokensPerSec.Value:F1} tok/s" : "")}" 
        : string.Empty;
}
