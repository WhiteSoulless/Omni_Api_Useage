using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniKeyStudio.Models;
using OmniKeyStudio.Services;

namespace OmniKeyStudio.ViewModels;

public partial class FreeModelsViewModel : ObservableObject
{
    private readonly LlmClientService _llmService;
    private readonly SecureStorageService _storageService;

    [ObservableProperty]
    private ModelInfo? _selectedModel;

    [ObservableProperty]
    private string _benchmarkStatus = "Bir model seçip hız testi yapabilir veya doğrudan sohbette kullanabilirsiniz.";

    [ObservableProperty]
    private bool _isBenchmarking;

    public ObservableCollection<ModelInfo> FreeModelsList { get; } = new();

    public event Action<string, string>? OnSelectModelForChat; // provider, modelId

    public FreeModelsViewModel(LlmClientService llmService, SecureStorageService storageService)
    {
        _llmService = llmService;
        _storageService = storageService;

        LoadFreeModels();
    }

    private void LoadFreeModels()
    {
        FreeModelsList.Clear();

        // 1. Groq
        FreeModelsList.Add(new ModelInfo
        {
            Id = "llama-3.3-70b-versatile",
            DisplayName = "Groq Llama 3.3 70B Versatile",
            Provider = "Groq",
            SpeedRating = "⚡ 550+ tok/sn (Ultra Hızlı)",
            IsFree = true,
            ContextWindow = "128k token",
            Description = "Groq LPU donanımında çalışan en güçlü ve akıcı açık kaynak model. Çok karmaşık muhakeme ve kodlamada harika."
        });

        FreeModelsList.Add(new ModelInfo
        {
            Id = "llama-3.1-8b-instant",
            DisplayName = "Groq Llama 3.1 8B Instant",
            Provider = "Groq",
            SpeedRating = "⚡ 750+ tok/sn (Işık Hızında)",
            IsFree = true,
            ContextWindow = "128k token",
            Description = "Dünyanın en hızlı yanıt veren modellerinden biri. Anlık özetleme, sınıflandırma ve hızlı asistanlık için ideal."
        });

        FreeModelsList.Add(new ModelInfo
        {
            Id = "mixtral-8x7b-32768",
            DisplayName = "Groq Mixtral 8x7B MoE",
            Provider = "Groq",
            SpeedRating = "⚡ 480+ tok/sn",
            IsFree = true,
            ContextWindow = "32k token",
            Description = "Mistral AI'nın Mixture of Experts mimarisi. Çok dilli görevlerde ve mantıksal sorularda yüksek başarı."
        });

        // 2. Google Gemini Free Tier
        FreeModelsList.Add(new ModelInfo
        {
            Id = "gemini-2.0-flash",
            DisplayName = "Google Gemini 2.0 Flash",
            Provider = "Google Gemini",
            SpeedRating = "🚀 Yeni Nesil Hızlı (~180 tok/sn)",
            IsFree = true,
            ContextWindow = "1M token",
            Description = "Google'ın en yeni 2. nesil hızlı mimarisi. Geliştiriciler için AI Studio üzerinden ücretsiz kota ile sunulur."
        });

        FreeModelsList.Add(new ModelInfo
        {
            Id = "gemini-1.5-flash",
            DisplayName = "Google Gemini 1.5 Flash",
            Provider = "Google Gemini",
            SpeedRating = "🚀 Yüksek Hız (~150 tok/sn)",
            IsFree = true,
            ContextWindow = "1M token devasa bağlam",
            Description = "Büyük belgeler, PDF'ler ve uzun kod tabanlarını tek seferde inceleyebilen 1 milyon token bağlamlı ücretsiz model."
        });

        // 3. OpenRouter Free Models
        FreeModelsList.Add(new ModelInfo
        {
            Id = "meta-llama/llama-3.3-70b-instruct:free",
            DisplayName = "OpenRouter Llama 3.3 70B (Free)",
            Provider = "OpenRouter",
            SpeedRating = "★ Sıfır Maliyet",
            IsFree = true,
            ContextWindow = "128k token",
            Description = "OpenRouter tarafından topluluğa tamamen 0$ maliyetle ücretsiz sunulan tam ölçekli Llama 3.3 70B."
        });

        FreeModelsList.Add(new ModelInfo
        {
            Id = "deepseek/deepseek-r1:free",
            DisplayName = "OpenRouter DeepSeek R1 (Free)",
            Provider = "OpenRouter",
            SpeedRating = "🧠 Derin Akıl Yürütme",
            IsFree = true,
            ContextWindow = "64k token",
            Description = "OpenAI o1 dengi açık ağırlıklı muhakeme modeli. Matematik, algoritmalar ve karmaşık mantıkta zirve performans."
        });

        FreeModelsList.Add(new ModelInfo
        {
            Id = "qwen/qwen-2.5-coder-32b-instruct:free",
            DisplayName = "OpenRouter Qwen 2.5 Coder 32B (Free)",
            Provider = "OpenRouter",
            SpeedRating = "💻 Yazılım Uzmanı",
            IsFree = true,
            ContextWindow = "32k token",
            Description = "Kod yazma, refactor ve hata ayıklama konusunda optimize edilmiş ücretsiz yazılım mühendisi modeli."
        });

        // 4. Local Ollama
        FreeModelsList.Add(new ModelInfo
        {
            Id = "llama3.2",
            DisplayName = "Local Ollama Llama 3.2",
            Provider = "Local Ollama",
            SpeedRating = "💻 Yerel Donanım Hızı",
            IsFree = true,
            ContextWindow = "128k token",
            Description = "Kendi bilgisayarınızda çalışan, internet gerektirmeyen, tamamen gizli ve limitsiz yerel yapay zeka."
        });

        SelectedModel = FreeModelsList[0];
    }

    [RelayCommand]
    private async Task BenchmarkSelectedModelAsync()
    {
        if (SelectedModel == null || IsBenchmarking) return;

        var keys = _storageService.LoadKeys();
        var keyEntry = keys.FirstOrDefault(k => k.Provider == SelectedModel.Provider);
        string apiKey = keyEntry?.Key ?? "";

        if (string.IsNullOrEmpty(apiKey) && SelectedModel.Provider != "Local Ollama")
        {
            BenchmarkStatus = $"Hata: {SelectedModel.Provider} için kayıtlı API anahtarı yok. Lütfen Anahtar Yöneticisi'nden ekleyin.";
            return;
        }

        IsBenchmarking = true;
        BenchmarkStatus = $"{SelectedModel.DisplayName} için hız ve gecikme testi başlatıldı...";

        var testHistory = new System.Collections.Generic.List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "1'den 10'a kadar say ve her sayının yanına bir kelimelik bir meyve adı yaz." }
        };

        var sw = Stopwatch.StartNew();
        int tokenEstimate = 0;
        long ttftMs = 0;

        try
        {
            await foreach (var chunk in _llmService.StreamChatAsync(
                SelectedModel.Provider,
                apiKey,
                SelectedModel.Id,
                testHistory,
                "Kısa ve öz yanıt ver."))
            {
                if (ttftMs == 0)
                {
                    ttftMs = sw.ElapsedMilliseconds;
                }
                tokenEstimate += Math.Max(1, chunk.Length / 4);
            }

            sw.Stop();
            long totalMs = sw.ElapsedMilliseconds;
            double tokSec = (double)tokenEstimate / (totalMs / 1000.0);

            BenchmarkStatus = $"Sonuç: İlk Yanıt Gecikmesi (TTFT): {ttftMs} ms | Toplam Süre: {totalMs} ms | Tahmini Hız: {tokSec:F1} token/saniye ({tokenEstimate} token)";
        }
        catch (Exception ex)
        {
            BenchmarkStatus = $"Hız testi başarısız: {ex.Message}";
        }
        finally
        {
            IsBenchmarking = false;
        }
    }

    [RelayCommand]
    private void UseInChat()
    {
        if (SelectedModel != null)
        {
            OnSelectModelForChat?.Invoke(SelectedModel.Provider, SelectedModel.Id);
        }
    }
}
