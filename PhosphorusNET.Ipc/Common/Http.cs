using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PhosphorusNET.Ipc.Common;

[IpcExpose]
public class Http
{
    private readonly HttpClient _httpClient;
    
    public Http(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }
    
    public async Task<object?> GetJson(string url)
    {
        var response = await _httpClient.GetAsync(url);
        return await response.Content.ReadFromJsonAsync<object>();
    }
    
    public async Task<object?> PostJson(string url, object? data)
    {
        var response = await _httpClient.PostAsJsonAsync(url, data);
        return await response.Content.ReadFromJsonAsync<object>();
    }
    
    public async Task<object?> PutJson(string url, object? data)
    {
        var response = await _httpClient.PutAsJsonAsync(url, data);
        return await response.Content.ReadFromJsonAsync<object>();
    }
    
    public async Task<object?> PatchJson(string url, object? data)
    {
        var response = await _httpClient.SendAsync(new HttpRequestMessage()
        {
            Method = new HttpMethod("PATCH"),
            RequestUri = new Uri(url),
            Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
        });
        return await response.Content.ReadFromJsonAsync<object>();
    }
    
    public async Task<object?> DeleteJson(string url)
    {
        var response = await _httpClient.DeleteAsync(url);
        return await response.Content.ReadFromJsonAsync<object>();
    }
}