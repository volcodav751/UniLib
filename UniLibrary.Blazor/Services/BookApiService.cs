using System.Net.Http.Json;
using UniLibrary.Blazor.Models;

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
}