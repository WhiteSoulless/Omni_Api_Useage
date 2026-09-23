using System;

namespace OmniKeyStudio.Models;

public class CustomApiSnippet
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Language { get; set; } = "csharp"; // csharp, python, curl, javascript, json
    public string Provider { get; set; } = "Custom";
    public string EndpointUrl { get; set; } = string.Empty;
    public string CustomHeaders { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string LanguageBadge => Language.ToUpperInvariant();
}
