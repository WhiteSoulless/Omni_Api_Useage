using System.Collections.Generic;

namespace OmniKeyStudio.Models;

public class DetectionResult
{
    public string CandidateProvider { get; set; } = "Bilinmeyen Sağlayıcı";
    public double Confidence { get; set; } // 0.0 - 1.0
    public string Reason { get; set; } = string.Empty;
    public string PatternMatch { get; set; } = string.Empty;
    public bool RequiresProbing { get; set; }
    public List<string> AlternativeProviders { get; set; } = new();

    public string ConfidencePercentage => $"{Confidence * 100:0}%";
}
