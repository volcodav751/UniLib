using System.Net.Http.Json;
using System.Net.Http.Headers;
using UniLibrary.Blazor.Models;
using UniLibrary.Blazor.Models.Requests;
using Microsoft.AspNetCore.Components.Forms;

namespace UniLibrary.Blazor.Services;

public class BookApiService
{
    private readonly HttpClient _httpClient;

    public BookApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Book>> GetBooksAsync()
    {
        var books = await _httpClient.GetFromJsonAsync<List<Book>>("api/books");
        return books ?? new List<Book>();
    }

    public async Task<Book?> CreateBookAsync(CreateBookRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/books", request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<Book>();
    }

    public async Task<(bool Success, string Message)> UploadBookFileAsync(int id, IBrowserFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();

            await using var fileStream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
            using var streamContent = new StreamContent(fileStream);

            if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            }

            content.Add(streamContent, "File", file.Name);

            var response = await _httpClient.PostAsync($"api/books/{id}/file", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return (false, $"Upload error: {response.StatusCode}. {responseText}");
            }

            return (true, "Файл успішно завантажено.");
        }
        catch (Exception ex)
        {
            return (false, $"Exception: {ex.Message}");
        }
        
    }
    public string GetBookPreviewUrl(int id)
    {
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/');

        return $"{baseUrl}/api/books/{id}/preview";
    }
    public string GetBookFileUrl(int id)
    {
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/');

        return $"{baseUrl}/api/books/{id}/file";
    }
    public async Task<Book?> GetBookByIdAsync(int id)
{
    return await _httpClient.GetFromJsonAsync<Book>($"api/books/{id}");
}
}