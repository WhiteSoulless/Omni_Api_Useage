using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OmniKeyStudio.Models;

namespace OmniKeyStudio.Services;

public class SecureStorageService
{
    private readonly string _storageDir;
    private readonly string _keysFilePath;
    private readonly string _snippetsFilePath;

    public SecureStorageService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _storageDir = Path.Combine(appData, "OmniKeyStudio");
        Directory.CreateDirectory(_storageDir);

        _keysFilePath = Path.Combine(_storageDir, "keys_vault.dat");
        _snippetsFilePath = Path.Combine(_storageDir, "snippets.json");
    }

    public List<ApiKeyEntry> LoadKeys()
    {
        try
        {
            if (!File.Exists(_keysFilePath)) return new List<ApiKeyEntry>();

            byte[] encryptedBytes = File.ReadAllBytes(_keysFilePath);
            if (encryptedBytes.Length == 0) return new List<ApiKeyEntry>();

            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            string json = Encoding.UTF8.GetString(decryptedBytes);

            return JsonSerializer.Deserialize<List<ApiKeyEntry>>(json) ?? new List<ApiKeyEntry>();
        }
        catch (Exception)
        {
            // If decryption fails (e.g. moved machine or corrupted), return empty list safely
            return new List<ApiKeyEntry>();
        }
    }

    public void SaveKeys(List<ApiKeyEntry> keys)
    {
        try
        {
            string json = JsonSerializer.Serialize(keys, new JsonSerializerOptions { WriteIndented = true });
            byte[] plainBytes = Encoding.UTF8.GetBytes(json);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(_keysFilePath, encryptedBytes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save keys: {ex.Message}");
        }
    }

    public List<CustomApiSnippet> LoadSnippets()
    {
        try
        {
            if (!File.Exists(_snippetsFilePath))
            {
                return GetDefaultSnippets();
            }

            string json = File.ReadAllText(_snippetsFilePath);
            return JsonSerializer.Deserialize<List<CustomApiSnippet>>(json) ?? GetDefaultSnippets();
        }
        catch
        {
            return GetDefaultSnippets();
        }
    }

    public void SaveSnippets(List<CustomApiSnippet> snippets)
    {
        try
        {
            string json = JsonSerializer.Serialize(snippets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_snippetsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save snippets: {ex.Message}");
        }
    }

    private List<CustomApiSnippet> GetDefaultSnippets()
    {
        return new List<CustomApiSnippet>
        {
            new CustomApiSnippet
            {
                Title = "Groq LPU Ultra-Hızlı Çağrı (C#)",
                Language = "csharp",
                Provider = "Groq",
                EndpointUrl = "https://api.groq.com/openai/v1/chat/completions",
                Description = "Groq LPU donanımında Llama 3.3 70B modelini 500+ token/sn hızında çağırma şablonu.",
                Code = @"using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = new(""Bearer"", ""YOUR_GROQ_KEY"");

var payload = new
{
    model = ""llama-3.3-70b-versatile"",
    messages = new[] { new { role = ""user"", content = ""Merhaba dünya!"" } }
};

var response = await client.PostAsJsonAsync(""https://api.groq.com/openai/v1/chat/completions"", payload);
var result = await response.Content.ReadAsStringAsync();
Console.WriteLine(result);"
            },
            new CustomApiSnippet
            {
                Title = "Google Gemini 2.0 Flash Streaming (Python)",
                Language = "python",
                Provider = "Google Gemini",
                EndpointUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:streamGenerateContent",
                Description = "Google Gemini 2.0 Flash API ile ücretsiz kota üzerinden streaming metin üretimi.",
                Code = @"from google import genai

client = genai.Client(api_key=""YOUR_GEMINI_KEY"")
response = client.models.generate_content_stream(
    model=""gemini-2.0-flash"",
    contents=""Bana hızlı bir Python örneği ver""
)

for chunk in response:
    print(chunk.text, end="""", flush=True)"
            },
            new CustomApiSnippet
            {
                Title = "OpenRouter Sıfır Maliyetli Model Çağrısı (cURL)",
                Language = "curl",
                Provider = "OpenRouter",
                EndpointUrl = "https://openrouter.ai/api/v1/chat/completions",
                Description = "OpenRouter ücretsiz model katmanına (:free) cURL komutu.",
                Code = @"curl https://openrouter.ai/api/v1/chat/completions \
  -H ""Authorization: Bearer YOUR_OPENROUTER_KEY"" \
  -H ""Content-Type: application/json"" \
  -d '{
    ""model"": ""meta-llama/llama-3.3-70b-instruct:free"",
    ""messages"": [{""role"": ""user"", ""content"": ""Selam!""}]
  }'"
            }
        };
    }
}
