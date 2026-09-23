namespace OmniKeyStudio.Models;

public class ModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string SpeedRating { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ContextWindow { get; set; } = string.Empty;

    public string FullDisplayText => $"{DisplayName} [{Provider}] - {SpeedRating} {(IsFree ? "(ÜCRETSİZ)" : "")}";
}
