using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace tests.Support;

public class ApiTestBrowser
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiTestBrowser(HttpClient client)
    {
        _client = client;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string url)
    {
        var response = await _client.GetAsync(url);
        return await CreateResponse<T>(response);
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string url, object? body = null)
    {
        var response = await _client.PostAsJsonAsync(url, body, _jsonOptions);
        return await CreateResponse<T>(response);
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string url, object? body = null)
    {
        var response = await _client.PutAsJsonAsync(url, body, _jsonOptions);
        return await CreateResponse<T>(response);
    }

    public async Task<ApiResponse> DeleteAsync(string url)
    {
        var response = await _client.DeleteAsync(url);
        return new ApiResponse(response.StatusCode, response.Headers);
    }

    private async Task<ApiResponse<T>> CreateResponse<T>(HttpResponseMessage response)
    {
        T? data = default;
        string? rawContent = null;

        if (response.Content.Headers.ContentLength > 0)
        {
            rawContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(rawContent))
            {
                data = JsonSerializer.Deserialize<T>(rawContent, _jsonOptions);
            }
        }

        return new ApiResponse<T>(response.StatusCode, response.Headers, data, rawContent);
    }
}

public class ApiResponse
{
    public System.Net.HttpStatusCode StatusCode { get; }
    public System.Net.Http.Headers.HttpResponseHeaders Headers { get; }

    public ApiResponse(System.Net.HttpStatusCode statusCode, System.Net.Http.Headers.HttpResponseHeaders headers)
    {
        StatusCode = statusCode;
        Headers = headers;
    }

    public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode < 300;
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; }
    public string? RawContent { get; }

    public ApiResponse(
        System.Net.HttpStatusCode statusCode,
        System.Net.Http.Headers.HttpResponseHeaders headers,
        T? data,
        string? rawContent)
        : base(statusCode, headers)
    {
        Data = data;
        RawContent = rawContent;
    }
}
