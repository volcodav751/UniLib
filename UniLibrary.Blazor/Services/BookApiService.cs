using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using UniLibrary.Blazor.Models;
using UniLibrary.Blazor.Models.Requests;

namespace UniLibrary.Blazor.Services
{
    public class BookApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthApiService _authApiService;

        public BookApiService(HttpClient httpClient, AuthApiService authApiService)
        {
            _httpClient = httpClient;
            _authApiService = authApiService;
        }

        public async Task<List<Book>> GetBooksAsync()
        {
            using HttpRequestMessage message = await _authApiService.CreateAuthorizedRequestAsync(
                HttpMethod.Get,
                "api/books"
            );

            HttpResponseMessage response = await _httpClient.SendAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                return new List<Book>();
            }

            List<Book>? books = await response.Content.ReadFromJsonAsync<List<Book>>();
            return books ?? new List<Book>();
        }

        public async Task<Book?> GetBookByIdAsync(int id)
        {
            using HttpRequestMessage message = await _authApiService.CreateAuthorizedRequestAsync(
                HttpMethod.Get,
                $"api/books/{id}"
            );

            HttpResponseMessage response = await _httpClient.SendAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Book>();
        }

        public async Task<Book?> CreateBookAsync(CreateBookRequest request)
        {
            using HttpRequestMessage message = await _authApiService.CreateAuthorizedRequestAsync(
                HttpMethod.Post,
                "api/books"
            );

            message.Content = JsonContent.Create(request);
            HttpResponseMessage response = await _httpClient.SendAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Book>();
        }

        public async Task<(bool Success, string Message)> UploadBookFileAsync(int id, IBrowserFile file)
        {
            try
            {
                using MultipartFormDataContent content = new();
                await using Stream fileStream = file.OpenReadStream(maxAllowedSize: 100 * 1024 * 1024);
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

        public async Task<string> GetBookPreviewUrlAsync(int id)
        {
            return await CreateUrlWithTokenAsync($"api/books/{id}/preview");
        }

        public async Task<string> GetBookFileUrlAsync(int id)
        {
            return await CreateUrlWithTokenAsync($"api/books/{id}/file");
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
    }
}
