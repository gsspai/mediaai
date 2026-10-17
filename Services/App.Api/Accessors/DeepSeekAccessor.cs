using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace App.Api.Accessors;

public class DeepSeekAccessor : IDeepSeekAccessor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppConfiguration _config;

    public DeepSeekAccessor(IHttpClientFactory httpClientFactory, AppConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public async Task<string> GenerateText(string prompt)
    {
        if (string.IsNullOrWhiteSpace(_config.DeepSeekApiKey))
        {
            throw new InvalidOperationException("DeepSeek API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_config.DeepSeekBaseUrl))
        {
            throw new InvalidOperationException("DeepSeek base URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_config.DeepSeekModel))
        {
            throw new InvalidOperationException("DeepSeek model is not configured.");
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_config.DeepSeekBaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.DeepSeekApiKey);

        var payload = new
        {
            model = _config.DeepSeekModel,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant for a media operations platform." },
                new { role = "user", content = prompt }
            },
            stream = false
        };

        var requestJson = JsonSerializer.Serialize(payload);
        using var response = await client.PostAsync(
            "/chat/completions",
            new StringContent(requestJson, Encoding.UTF8, "application/json"));

        var responseJson = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"DeepSeek request failed: {response.StatusCode}. {responseJson}");
        }

        using var document = JsonDocument.Parse(responseJson);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("DeepSeek returned an empty response.");
        }

        return content;
    }
}
