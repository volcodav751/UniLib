using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using UniLibrary.Blazor.Models;
using UniLibrary.Blazor.Models.Requests;

namespace UniLibrary.Blazor.Services;

public class BookApiService
{
    private const long MaxUploadSizeBytes = 100 * 1024 * 1024;

    private readonly HttpClient _httpClient;
    private readonly AuthApiService _authApiService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BookApiService(HttpClient httpClient, AuthApiService authApiService)
    {
        _httpClient = httpClient;
        _authApiService = authApiService;
    }

    public Task<List<Book>> GetBooksAsync()
    {
        return GetListAsync<Book>("api/books");
    }

    public Task<List<Book>> GetActiveRentalBooksAsync()
    {
        return GetListAsync<Book>("api/books/rentals/active");
    }

    public Task<Book?> GetBookByIdAsync(int id)
    {
        return GetAsync<Book>($"api/books/{id}");
    }

    public async Task<Book?> CreateBookAsync(CreateBookRequest request)
    {
        var result = await SendJsonAsync<Book>(HttpMethod.Post, "api/books", request);
        return result.Value;
    }

    public async Task<(bool Success, string Message)> UploadBookFileAsync(int id, IBrowserFile file)
    {
        try
        {
            using MultipartFormDataContent content = new();
            await using Stream fileStream = file.OpenReadStream(maxAllowedSize: MaxUploadSizeBytes);
            using StreamContent streamContent = new(fileStream);

            if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            }

            content.Add(streamContent, "File", file.Name);

            using HttpRequestMessage message = await _authApiService.CreateAuthorizedRequestAsync(
                HttpMethod.Post,
                $"api/books/{id}/file"
            );

            message.Content = content;

            HttpResponseMessage response = await _httpClient.SendAsync(message);
            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return (false, $"Помилка завантаження: {response.StatusCode}. {CleanError(responseText)}");
            }

            if (responseText.Contains("Failed", StringComparison.OrdinalIgnoreCase))
            {
                return (true, "Файл завантажено, але PDF-передперегляд не створено. Перевір LibreOffice на API-комп'ютері.");
            }

            return (true, "Файл завантажено, PDF-передперегляд готовий.");
        }
        catch (Exception ex)
        {
            return (false, $"Помилка: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, Book? Book)> StaffRentBookAsync(int bookId, StaffCreateRentalRequest request)
    {
        var result = await SendJsonAsync<Book>(HttpMethod.Post, $"api/books/{bookId}/staff-rent", request);
        return (result.Success, result.Success ? "Видачу книги успішно записано в базу." : result.Message, result.Value);
    }

    public async Task<(bool Success, string Message, Book? Book)> StaffReturnBookAsync(int bookId, int rentalId, StaffReturnRentalRequest? request = null)
    {
        var result = await SendJsonAsync<Book>(
            HttpMethod.Post,
            $"api/books/{bookId}/rentals/{rentalId}/staff-return",
            request ?? new StaffReturnRentalRequest()
        );

        return (result.Success, result.Success ? "Повернення книги зафіксовано. Копія знову доступна." : result.Message, result.Value);
    }

    public async Task<(bool Success, string Message)> DeleteBookAsync(int id)
    {
        try
        {
            using HttpRequestMessage message = await _authApiService.CreateAuthorizedRequestAsync(
                HttpMethod.Delete,
                $"api/books/{id}"
            );

            HttpResponseMessage response = await _httpClient.SendAsync(message);

            if (response.IsSuccessStatusCode)
            {
                return (true, "Книгу видалено.");
            }

            string error = await response.Content.ReadAsStringAsync();
            return (false, CleanError(error));
        }
        catch (Exception ex)
        {
            return (false, $"Помилка: {ex.Message}");
        }
    }

    public Task<string> GetBookPreviewUrlAsync(int id)
    {
        return CreateUrlWithTokenAsync($"api/books/{id}/preview");
    }

    public Task<string> GetBookFileUrlAsync(int id)
    {
        return CreateUrlWithTokenAsync($"api/books/{id}/file");
    }

    private async Task<List<T>> GetListAsync<T>(string url)
    {
        try
        {
            T[]? values = await SendAuthorizedAsync<T[]>(HttpMethod.Get, url);
            return values?.ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            return await SendAuthorizedAsync<T>(HttpMethod.Get, url);
        }
        catch
        {
            return default;
        }
    }

    private async Task<T?> SendAuthorizedAsync<T>(HttpMethod method, string url)
    {
        using HttpRequestMessage message = await _authApiService.CreateAuthorizedRequestAsync(method, url);
        HttpResponseMessage response = await _httpClient.SendAsync(message);

        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task<ApiResult<T>> SendJsonAsync<T>(HttpMethod method, string url, object payload)
    {
        try
        {
            using HttpRequestMessage message = await _authApiService.CreateAuthorizedRequestAsync(method, url);
            message.Content = JsonContent.Create(payload);

            HttpResponseMessage response = await _httpClient.SendAsync(message);
            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<T>.Fail(CleanError(responseText));
            }

            T? value = JsonSerializer.Deserialize<T>(responseText, JsonOptions);
            return ApiResult<T>.Ok(value);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Fail($"Помилка: {ex.Message}");
        }
    }

    private async Task<string> CreateUrlWithTokenAsync(string relativeUrl)
    {
        string baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
        string url = $"{baseUrl}/{relativeUrl}";
        string? token = await _authApiService.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            return url;
        }

        return $"{url}?access_token={Uri.EscapeDataString(token)}";
    }

    private static string CleanError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return "Сталася помилка.";
        }

        return error.Trim('"', ' ', '\n', '\r', '\t');
    }

    private readonly record struct ApiResult<T>(bool Success, string Message, T? Value)
    {
        public static ApiResult<T> Ok(T? value)
        {
            return new ApiResult<T>(true, string.Empty, value);
        }

        public static ApiResult<T> Fail(string message)
        {
            return new ApiResult<T>(false, message, default);
        }
    }
}
