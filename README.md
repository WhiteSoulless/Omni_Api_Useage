# ⚡ OmniKey AI Studio (Windows WPF)

> **Herhangi bir AI API anahtarını otomatik algılayan, doğrulayan, tüm ücretsiz/hızlı modelleri (Groq, Gemini, OpenRouter Free vb.) tek çatı altında sunan ve özel API kodlarınızı yöneten modern Windows WPF uygulaması.**

![Platform](https://img.shields.io/badge/Platform-Windows%20WPF-blue?style=flat-square)
![Framework](https://img.shields.io/badge/.NET-9.0--windows-purple?style=flat-square)
![Security](https://img.shields.io/badge/Security-Windows%20DPAPI%20Encrypted-success?style=flat-square)
![Tests](https://img.shields.io/badge/Tests-12%20Passing-brightgreen?style=flat-square)

---

## 🚀 Öne Çıkan Özellikler

### 1. 🔍 Akıllı API Anahtarı Tespiti ve Canlı Doğrulama
- Herhangi bir AI sağlayıcısına ait anahtarı yapıştırdığınız anda formatı, deseni ve sağlayıcısı otomatik tespit edilir:
  - **Google Gemini / AI Studio:** `AIzaSy...`
  - **Groq LPU:** `gsk_...`
  - **OpenRouter:** `sk-or-v1-...`
  - **Anthropic Claude:** `sk-ant-...`
  - **OpenAI Project:** `sk-proj-...`
  - **DeepSeek / OpenAI Generic:** `sk-...`
  - **Hugging Face:** `hf_...`
  - **GitHub Models:** `ghp_...` / `github_pat_...`
  - **Perplexity:** `pplx-...`
  - **Özel / Yerel Uç Noktalar:** Ollama (`http://localhost:11434/v1`), LM Studio (`http://localhost:1234/v1`).
- **Canlı Doğrulama (Live Probe):** Anahtarın gerçekten aktif olup olmadığını test eder, hesap kotasını kontrol eder ve o anahtara tanımlı tüm aktif modelleri otomatik listeler.

### 2. ⚡ Ücretsiz ve Ultra Hızlı Model Kataloğu
- Hazır entegre, anında kullanılabilir modeller:
  - **Groq:** LPU çipleri üzerinde çalışan dünyanın en hızlı LLM altyapısı (~500 - 750+ token/saniye): `llama-3.3-70b-versatile`, `llama-3.1-8b-instant`, `mixtral-8x7b-32768`.
  - **Google Gemini Free Tier:** `gemini-2.0-flash` ve devasa 1 milyon token bağlamlı `gemini-1.5-flash`.
  - **OpenRouter Free Tier (`:free`):** Topluluğa tamamen sıfır maliyetle sunulan `meta-llama/llama-3.3-70b-instruct:free`, `deepseek/deepseek-r1:free`, `qwen/qwen-2.5-coder-32b-instruct:free`.
  - **Yerel LLM (Ollama & LM Studio):** İnternetsiz, %100 gizli ve limitsiz yerel yapay zeka entegrasyonu.
- **Hız & Gecikme Testi (Benchmark):** Tek tıkla modellerin ilk yanıt süresini (TTFT), toplam süresini ve saniye başına token hızını ölçer.

### 3. 💬 Evrensel Sohbet ve Test Konsolu (Playground)
- Farklı sağlayıcılar arasında tek bir tıkla model değiştirerek anında sohbet edebilme.
- Gerçek zamanlı harf harf yanıt akışı (SSE Streaming).
- Canlı gecikme (ms) ve hız (tok/s) telemetrisi.
- Sistem istemi (System Prompt) yapılandırması.
- Yanıtları tek tıkla kopyalama ve sohbet geçmişini temizleme.

### 4. 📜 Özel API Kodları ve Snippet Kasası
- Kendi özel API endpoint'lerinizi, header ayarlarınızı ve istem şablonlarınızı kaydetme.
- Kayıtlı anahtarınız için tek tıkla kullanıma hazır entegrasyon kodları üretme:
  - **C#** (`HttpClient` / `System.Text.Json`)
  - **Python** (`openai` / `google-genai` / `requests`)
  - **cURL** (Terminal komutları)
  - **JavaScript** (Node.js 18+ / Browser Fetch)

### 5. 🔒 Üst Düzey Güvenlik (Windows DPAPI)
- API anahtarlarınız asla düz metin (plain text) olarak saklanmaz.
- Windows'un yerel `System.Security.Cryptography.ProtectedData` (DPAPI) mekanizması kullanılarak yalnızca geçerli Windows oturum açmış kullanıcı hesabınızın çözebileceği şekilde şifrelenir.

---

## 🛠️ Kurulum ve Çalıştırma

### Gereksinimler
- **Windows 10 / 11**
- **.NET 9.0 SDK** (veya üzeri)

### Adımlar

1. Depoyu klonlayın:
```bash
git clone https://github.com/KULLANICI_ADINIZ/OmniKeyStudio.git
cd OmniKeyStudio
```

2. Bağımlılıkları geri yükleyin ve derleyin:
```bash
dotnet build
```

3. Birim testleri çalıştırın:
```bash
dotnet test OmniKeyStudio.Tests/OmniKeyStudio.Tests.csproj
```

4. Uygulamayı başlatın:
```bash
dotnet run --project OmniKeyStudio.csproj
```

---

## 📂 Proje Yapısı

```
OmniKeyStudio/
├── OmniKeyStudio.slnx                 # .NET Çözüm dosyası
├── OmniKeyStudio.csproj               # WPF .NET 9 Projesi
├── App.xaml / App.xaml.cs             # DI Servis başlangıcı ve kaynaklar
├── MainWindow.xaml / MainWindow.xaml.cs # Ana modern arayüz ve sekme yönetimi
├── Models/
│   ├── ApiKeyEntry.cs                 # API anahtar veri modeli ve DPAPI maskeleme
│   ├── ModelInfo.cs                   # Model katalog tanımı ve hız etiketleri
│   ├── ChatMessage.cs                 # Mesaj geçmişi ve telemetri verileri
│   ├── CustomApiSnippet.cs            # Özel kodlar ve endpoint şablonları
│   └── DetectionResult.cs             # Heuristik tespit sonuç modeli
├── Services/
│   ├── KeyDetectorService.cs          # Regex ve heuristik desen analizi
│   ├── KeyValidatorService.cs         # Canlı sağlayıcı tarama ve model keşfi
│   ├── SecureStorageService.cs        # DPAPI şifreli kasa servisi
│   ├── LlmClientService.cs            # Birleşik streaming API istemcisi
│   └── CodeGeneratorService.cs        # C#, Python, cURL, JS kod üretici
├── ViewModels/
│   ├── MainViewModel.cs               # Ana orkestrasyon ViewModel'i
│   ├── KeyManagerViewModel.cs         # Anahtar ekleme, canlı doğrulama ve liste
│   ├── ChatPlaygroundViewModel.cs     # Çoklu model streaming sohbet konsolu
│   ├── FreeModelsViewModel.cs         # Ücretsiz model kataloğu ve benchmark
│   └── CodeVaultViewModel.cs          # Özel API kod kasası
├── Views/
│   ├── KeyManagerView.xaml            # Anahtar algılama ve kasa görünümü
│   ├── ChatPlaygroundView.xaml        # Canlı sohbet arayüzü
│   ├── FreeModelsView.xaml            # Hızlı modeller ve hız testi arayüzü
│   └── CodeVaultView.xaml             # Özel kod yöneticisi arayüzü
├── Styles/
│   ├── Colors.xaml                    # Koyu tema renk paleti
│   └── ModernControls.xaml            # Modern kart, buton ve input stilleri
└── OmniKeyStudio.Tests/               # xUnit Birim ve Doğrulama Testleri
    ├── KeyDetectorTests.cs            # Desen tespit testleri
    └── ServiceTests.cs                # DPAPI şifreleme ve kod üretim testleri
```

---

## 📄 Lisans
Bu proje MIT lisansı ile lisanslanmıştır.
