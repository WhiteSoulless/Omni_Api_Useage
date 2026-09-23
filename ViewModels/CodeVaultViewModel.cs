using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniKeyStudio.Models;
using OmniKeyStudio.Services;

namespace OmniKeyStudio.ViewModels;

public partial class CodeVaultViewModel : ObservableObject
{
    private readonly SecureStorageService _storageService;
    private readonly CodeGeneratorService _codeGenService;

    [ObservableProperty]
    private CustomApiSnippet? _selectedSnippet;

    [ObservableProperty]
    private string _snippetTitle = string.Empty;

    [ObservableProperty]
    private string _snippetLanguage = "csharp";

    [ObservableProperty]
    private string _snippetProvider = "Custom";

    [ObservableProperty]
    private string _snippetEndpointUrl = string.Empty;

    [ObservableProperty]
    private string _snippetCustomHeaders = string.Empty;

    [ObservableProperty]
    private string _snippetCode = string.Empty;

    [ObservableProperty]
    private string _snippetDescription = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Hazır.";

    public ObservableCollection<CustomApiSnippet> Snippets { get; } = new();

    public CodeVaultViewModel(SecureStorageService storageService, CodeGeneratorService codeGenService)
    {
        _storageService = storageService;
        _codeGenService = codeGenService;

        LoadSnippets();
    }

    private void LoadSnippets()
    {
        Snippets.Clear();
        var list = _storageService.LoadSnippets();
        foreach (var s in list)
        {
            Snippets.Add(s);
        }

        if (Snippets.Count > 0)
        {
            SelectedSnippet = Snippets[0];
        }
    }

    partial void OnSelectedSnippetChanged(CustomApiSnippet? value)
    {
        if (value == null) return;
        SnippetTitle = value.Title;
        SnippetLanguage = value.Language;
        SnippetProvider = value.Provider;
        SnippetEndpointUrl = value.EndpointUrl;
        SnippetCustomHeaders = value.CustomHeaders;
        SnippetCode = value.Code;
        SnippetDescription = value.Description;
    }

    [RelayCommand]
    private void SaveSnippet()
    {
        if (string.IsNullOrWhiteSpace(SnippetTitle))
        {
            StatusMessage = "Lütfen bir başlık girin.";
            return;
        }

        if (SelectedSnippet != null && Snippets.Contains(SelectedSnippet))
        {
            SelectedSnippet.Title = SnippetTitle;
            SelectedSnippet.Language = SnippetLanguage;
            SelectedSnippet.Provider = SnippetProvider;
            SelectedSnippet.EndpointUrl = SnippetEndpointUrl;
            SelectedSnippet.CustomHeaders = SnippetCustomHeaders;
            SelectedSnippet.Code = SnippetCode;
            SelectedSnippet.Description = SnippetDescription;
            StatusMessage = $"'{SnippetTitle}' güncellendi.";
        }
        else
        {
            var newSnippet = new CustomApiSnippet
            {
                Title = SnippetTitle,
                Language = SnippetLanguage,
                Provider = SnippetProvider,
                EndpointUrl = SnippetEndpointUrl,
                CustomHeaders = SnippetCustomHeaders,
                Code = SnippetCode,
                Description = SnippetDescription
            };
            Snippets.Insert(0, newSnippet);
            SelectedSnippet = newSnippet;
            StatusMessage = $"Yeni API kod bloğu '{SnippetTitle}' kaydedildi.";
        }

        _storageService.SaveSnippets(Snippets.ToList());
        OnPropertyChanged(nameof(Snippets));
    }

    [RelayCommand]
    private void NewSnippet()
    {
        SelectedSnippet = null;
        SnippetTitle = "Yeni API Entegrasyonu";
        SnippetLanguage = "csharp";
        SnippetProvider = "Custom";
        SnippetEndpointUrl = "https://api.openai.com/v1/chat/completions";
        SnippetCustomHeaders = "Authorization: Bearer YOUR_KEY";
        SnippetCode = @"// Özel API Çağrınız buraya
";
        SnippetDescription = "Kendi özel API çağrım ve kodlarım";
        StatusMessage = "Yeni snippet düzenleme modu açıldı.";
    }

    [RelayCommand]
    private void DeleteSnippet(CustomApiSnippet? snippet)
    {
        var target = snippet ?? SelectedSnippet;
        if (target != null && Snippets.Contains(target))
        {
            Snippets.Remove(target);
            _storageService.SaveSnippets(Snippets.ToList());
            StatusMessage = $"'{target.Title}' silindi.";
            if (Snippets.Count > 0) SelectedSnippet = Snippets[0];
            else NewSnippet();
        }
    }

    [RelayCommand]
    private void CopyCode()
    {
        if (!string.IsNullOrWhiteSpace(SnippetCode))
        {
            Clipboard.SetText(SnippetCode);
            StatusMessage = "Kod panoya kopyalandı!";
        }
    }

    [RelayCommand]
    private void GenerateCodeForProvider(string lang)
    {
        var keys = _storageService.LoadKeys();
        var activeKey = keys.FirstOrDefault(k => k.IsValid) ?? keys.FirstOrDefault();
        string keyVal = activeKey?.Key ?? "YOUR_API_KEY";
        string prov = activeKey?.Provider ?? "Groq";
        string model = prov == "Google Gemini" ? "gemini-2.0-flash" : (prov == "Groq" ? "llama-3.3-70b-versatile" : "gpt-4o-mini");

        string generated = lang.ToLowerInvariant() switch
        {
            "csharp" => _codeGenService.GenerateCSharpCode(prov, model, keyVal, activeKey?.CustomBaseUrl ?? ""),
            "python" => _codeGenService.GeneratePythonCode(prov, model, keyVal, activeKey?.CustomBaseUrl ?? ""),
            "curl" => _codeGenService.GenerateCurlCode(prov, model, keyVal, activeKey?.CustomBaseUrl ?? ""),
            "javascript" => _codeGenService.GenerateJavaScriptCode(prov, model, keyVal, activeKey?.CustomBaseUrl ?? ""),
            _ => "// Desteklenmeyen dil"
        };

        SnippetCode = generated;
        SnippetLanguage = lang.ToLowerInvariant();
        SnippetTitle = $"{prov} {lang.ToUpper()} İstemcisi";
        SnippetProvider = prov;
        StatusMessage = $"{prov} için {lang.ToUpper()} kodu otomatik üretildi.";
    }
}
