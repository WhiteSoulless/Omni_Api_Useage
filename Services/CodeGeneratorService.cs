namespace OmniKeyStudio.Services;

public class CodeGeneratorService
{
    public string GenerateCSharpCode(string provider, string model, string apiKey, string endpointUrl)
    {
        if (provider == "Google Gemini")
        {
            return $@"// C# (.NET 8/9) - Google Gemini API
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

string apiKey = ""{apiKey}"";
string model = ""{model}"";
string url = $""https://generativelanguage.googleapis.com/v1beta/models/{{model}}:generateContent?key={{apiKey}}"";

using var client = new HttpClient();
var requestBody = new
{{
    contents = new[]
    {{
        new {{ parts = new[] {{ new {{ text = ""Merhaba! Bana kısaca kendini tanıt."" }} }} }}
    }}
}};

var json = JsonSerializer.Serialize(requestBody);
var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, ""application/json""));
var responseContent = await response.Content.ReadAsStringAsync();

Console.WriteLine(responseContent);";
        }

        string baseUrl = string.IsNullOrWhiteSpace(endpointUrl)
            ? (provider == "Groq" ? "https://api.groq.com/openai/v1/chat/completions"
               : provider == "OpenRouter" ? "https://openrouter.ai/api/v1/chat/completions"
               : provider == "DeepSeek" ? "https://api.deepseek.com/chat/completions"
               : "https://api.openai.com/v1/chat/completions")
            : endpointUrl;

        return $@"// C# (.NET 8/9) - {provider} OpenAI-Uyumlu Çağrı
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

string apiKey = ""{apiKey}"";
string endpoint = ""{baseUrl}"";

using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(""Bearer"", apiKey);

var requestBody = new
{{
    model = ""{model}"",
    messages = new[]
    {{
        new {{ role = ""system"", content = ""Sen yardımcı bir yapay zeka asistanısın."" }},
        new {{ role = ""user"", content = ""Merhaba!"" }}
    }},
    temperature = 0.7
}};

var json = JsonSerializer.Serialize(requestBody);
var response = await client.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, ""application/json""));
var responseContent = await response.Content.ReadAsStringAsync();

Console.WriteLine(responseContent);";
    }

    public string GeneratePythonCode(string provider, string model, string apiKey, string endpointUrl)
    {
        if (provider == "Google Gemini")
        {
            return $@"# Python - Google GenAI SDK
from google import genai

client = genai.Client(api_key=""{apiKey}"")

response = client.models.generate_content(
    model=""{model}"",
    contents=""Merhaba! Bana kısaca kendini tanıt.""
)

print(response.text)";
        }

        string baseUrl = string.IsNullOrWhiteSpace(endpointUrl)
            ? (provider == "Groq" ? "https://api.groq.com/openai/v1"
               : provider == "OpenRouter" ? "https://openrouter.ai/api/v1"
               : provider == "DeepSeek" ? "https://api.deepseek.com"
               : "https://api.openai.com/v1")
            : endpointUrl;

        return $@"# Python - OpenAI SDK ile {provider}
from openai import OpenAI

client = OpenAI(
    base_url=""{baseUrl}"",
    api_key=""{apiKey}""
)

completion = client.chat.completions.create(
    model=""{model}"",
    messages=[
        {{""role"": ""system"", ""content"": ""Sen yardımcı bir yapay zeka asistanısın.""}},
        {{""role"": ""user"", ""content"": ""Merhaba!""}}
    ],
    temperature=0.7
)

print(completion.choices[0].message.content)";
    }

    public string GenerateCurlCode(string provider, string model, string apiKey, string endpointUrl)
    {
        if (provider == "Google Gemini")
        {
            return $@"curl ""https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}"" \
  -H 'Content-Type: application/json' \
  -X POST \
  -d '{{
    ""contents"": [
      {{""parts"": [{{""text"": ""Merhaba!""}}]}}
    ]
  }}'";
        }

        string baseUrl = string.IsNullOrWhiteSpace(endpointUrl)
            ? (provider == "Groq" ? "https://api.groq.com/openai/v1/chat/completions"
               : provider == "OpenRouter" ? "https://openrouter.ai/api/v1/chat/completions"
               : provider == "DeepSeek" ? "https://api.deepseek.com/chat/completions"
               : "https://api.openai.com/v1/chat/completions")
            : endpointUrl;

        return $@"curl ""{baseUrl}"" \
  -H ""Content-Type: application/json"" \
  -H ""Authorization: Bearer {apiKey}"" \
  -d '{{
    ""model"": ""{model}"",
    ""messages"": [
      {{""role"": ""user"", ""content"": ""Merhaba!""}}
    ],
    ""temperature"": 0.7
  }}'";
    }

    public string GenerateJavaScriptCode(string provider, string model, string apiKey, string endpointUrl)
    {
        string baseUrl = string.IsNullOrWhiteSpace(endpointUrl)
            ? (provider == "Groq" ? "https://api.groq.com/openai/v1/chat/completions"
               : provider == "OpenRouter" ? "https://openrouter.ai/api/v1/chat/completions"
               : provider == "DeepSeek" ? "https://api.deepseek.com/chat/completions"
               : "https://api.openai.com/v1/chat/completions")
            : endpointUrl;

        return $@"// JavaScript (Node.js 18+ / Browser Fetch) - {provider}
async function run() {{
  const response = await fetch(""{baseUrl}"", {{
    method: ""POST"",
    headers: {{
      ""Content-Type"": ""application/json"",
      ""Authorization"": ""Bearer {apiKey}""
    }},
    body: JSON.stringify({{
      model: ""{model}"",
      messages: [{{ role: ""user"", content: ""Merhaba!"" }}],
      temperature: 0.7
    }})
  }});

  const data = await response.json();
  console.log(data.choices[0].message.content);
}}

run();";
    }
}
