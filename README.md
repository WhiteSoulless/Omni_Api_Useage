# ⚡ OmniKey AI Studio v2

> **Yapay zekanın gücünü tek bir tıkla, ücretsiz ve ışık hızında masaüstünüze getirin!**

[![Version](https://img.shields.io/badge/version-2.0.0-blue?style=flat-square)](https://github.com/WhiteSoulless/Omni_Api_Useage/releases)
[![Platform](https://img.shields.io/badge/Platform-Windows%20WPF-blue?style=flat-square)](https://www.microsoft.com/)
[![Framework](https://img.shields.io/badge/.NET-9.0--windows-purple?style=flat-square)](https://dotnet.microsoft.com/)
[![Security](https://img.shields.io/badge/Security-Windows%20DPAPI%20Encrypted-success?style=flat-square)](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata)
[![Tests](https://img.shields.io/badge/Tests-14%20Passing-brightgreen?style=flat-square)](https://github.com/WhiteSoulless/Omni_Api_Useage)

OmniKey AI Studio; ChatGPT, Google Gemini, Groq veya DeepSeek gibi popüler yapay zekaları tek bir yerden, tarayıcılarla boğuşmadan masaüstünüzde kullanmanızı sağlayan modern bir Windows uygulamasıdır.

---

### ❓ API Anahtarı (API Key) Nedir?
API Anahtarı; yapay zeka şirketlerinin (Google, Groq vb.) size sunduğu **ücretsiz bir dijital giriş kartıdır**. Bu anahtar sayesinde hiçbir abonelik ücreti ödemeden, doğrudan en hızlı yapay zeka modelleriyle konuşabilirsiniz.

---

## 🚀 3 Kolay Adımda Başlayın

### 1. Adım: Ücretsiz Anahtarınızı Alın (1 Dakika)
İstediğiniz sağlayıcıdan tamamen ücretsiz anahtar alabilirsiniz:
* **Google Gemini (Tavsiye Edilen):** [Google AI Studio](https://aistudio.google.com/) adresine gidin, Google hesabınızla giriş yapıp **"Get API Key"** butonuna tıklayın. (Anahtarınız `AQ.Ab...` veya `AIzaSy...` ile başlar).
* **Groq (Saniyede 500+ kelime ile dünyanın en hızlısı):** [Groq Console](https://console.groq.com/) adresinden ücretsiz bir `gsk_...` anahtarı oluşturun.

### 2. Adım: Uygulamaya Yapıştırın
* OmniKey AI Studio'yu açın.
* Üst kısımdaki **"Hemen bir API anahtarı yapıştırın..."** kutucuğuna anahtarınızı yapıştırın.
* **`⚡ Aktif Et`** butonuna basın. Uygulama anahtarın hangi şirkete ait olduğunu **otomatik olarak tanır** ve yeşil renkle aktif eder.

### 3. Adım: Sohbete Başlayın!
* Alt kısımdaki kutuya sorunuzu yazın ve **Gönder ✈** butonuna basın.
* Cevap anında, kelime kelime ekrana akmaya başlayacaktır!

---

## ✨ Neden OmniKey AI Studio?

* 🧠 **Akıllı Dedektör:** Hangi şirketin anahtarı olduğunu bilmenize gerek yok! Yapıştırdığınız an formatı (`AQ.Ab...`, `gsk_...`, `sk-...`, `sk-or-...` vb.) tespit eder.
* 🔒 **%100 Gizlilik & Güvenlik:** 
  * Anahtarlarınız bilgisayarınızın donanımına özel şifreleme kasasında (**Windows DPAPI**) saklanır.
  * **Diske Kayıt Yok:** Sorduğunuz sorular ve yaptığınız aramalar asla diske kaydedilmez; uygulama kapandığı an bellekten tamamen silinir.
* ⚡ **Dünyanın En Hızlı Modelleri Hazır (2026 Kataloğu):**
  * **Gemini 3.6 Flash & Gemini Flash Latest:** Google'ın en güncel, akıllı ve ücretsiz resmi modelleri.
  * **Groq Llama 3.3 70B:** Saniyede 500+ token hızla göz açıp kapayıncaya kadar yanıt verir.
  * **DeepSeek V3 & R1:** Karmaşık matematik, mantık ve kodlama soruları için ideal.
* 📜 **Kod Kasası:** Yazılımcılar için tek tıkla C#, Python ve cURL hazır entegrasyon kod şablonları üretir.

---

## 📊 Desteklenen Sağlayıcılar

| Sağlayıcı | Anahtar Formatı | Önerilen Model | Ücretsiz Kota? |
| :--- | :--- | :--- | :---: |
| **Google Gemini** | `AQ.Ab...` / `AIzaSy...` | `gemini-3.6-flash`, `gemini-flash-latest` | ✅ Evet (Cömert Ücretsiz Kota) |
| **Groq LPU** | `gsk_...` | `llama-3.3-70b-versatile` (500+ tok/s) | ✅ Evet (Ultra Hızlı) |
| **OpenRouter** | `sk-or-v1-...` | `meta-llama/llama-3.3-70b-instruct:free` | ✅ Evet (0$ Modeller) |
| **DeepSeek** | `sk-...` (32 hex) | `deepseek-chat` (V3), `deepseek-reasoner` (R1) | 💵 Çok Düşük Maliyet |
| **Anthropic Claude** | `sk-ant-...` | `claude-3-5-sonnet`, `claude-3-5-haiku` | 💵 Standart |
| **OpenAI** | `sk-proj-...` / `sk-...` | `gpt-4o`, `gpt-4o-mini` | 💵 Standart |

---

## ❓ Sıkça Sorulan Sorular (SSS)

**S: Bu uygulama ücretli mi?**  
C: Hayır, OmniKey AI Studio tamamen açık kaynaklı ve ücretsizdir. Kullandığınız sağlayıcıların (Google, Groq vb.) da ücretsiz kullanım kotaları bulunur.

**S: API anahtarım başkalarının eline geçer mi?**  
C: Kesinlikle hayır! Anahtarlarınız hiçbir harici aracı sunucuya gitmez; doğrudan sizin bilgisayarınızdan resmi yapay zeka servisine şifreli HTTPS ile iletilir. Bilgisayarınızda ise Windows DPAPI ile şifrelenir.

**S: "Model bulunamadı (404)" veya "Yetkisiz (401)" hatası alırsam ne yapmalıyım?**  
C: v2 sürümü eskiyen modelleri otomatik olarak güncel `gemini-3.6-flash` gibi modellere yönlendirir. 401 hatası alırsanız anahtarı kopyalarken eksik karakter almadığınızdan emin olun ve `⚡ Aktif Et` butonuna tıklayın.

---

## 💻 Gereksinimler & Kurulum

1. **İşletim Sistemi:** Windows 10 veya Windows 11 (64-bit)
2. **Çalışma Zamanı:** [.NET 9.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) yüklü olmalıdır.

### Adım Adım Çalıştırma:

```powershell
# 1. Projeyi bilgisayarınıza indirin
git clone https://github.com/WhiteSoulless/Omni_Api_Useage.git
cd Omni_Api_Useage

# 2. Testleri çalıştırın (Opsiyonel)
dotnet test

# 3. Uygulamayı başlatın
dotnet run
```
