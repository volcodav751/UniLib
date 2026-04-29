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
            var books = await _httpClient.GetFromJsonAsync<List<Book>>("api/books");
            return books ?? new List<Book>();
        }

        public async Task<Book?> GetBookByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<Book>($"api/books/{id}");
        }

        public async Task<Book?> CreateBookAsync(CreateBookRequest request)
        {
            using var message = await CreateRequestWithUserRoleAsync(HttpMethod.Post, "api/books");

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
                using var content = new MultipartFormDataContent();

                await using var fileStream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
                using var streamContent = new StreamContent(fileStream);

                if (!string.IsNullOrWhiteSpace(file.ContentType))
                {
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                }

                content.Add(streamContent, "File", file.Name);

                using var message = await CreateRequestWithUserRoleAsync(
                    HttpMethod.Post,
                    $"api/books/{id}/file"
                );

                message.Content = content;

                HttpResponseMessage response = await _httpClient.SendAsync(message);
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"Помилка завантаження: {response.StatusCode}. {responseText}");
                }

                return (true, "Файл успішно завантажено.");
            }
            catch (Exception ex)
            {
                return (false, $"Помилка: {ex.Message}");
            }
        }

        public string GetBookPreviewUrl(int id)
        {
            string? baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/');

            return $"{baseUrl}/api/books/{id}/preview";
        }

        public string GetBookFileUrl(int id)
        {
            string? baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/');

            return $"{baseUrl}/api/books/{id}/file";
        }

        private async Task<HttpRequestMessage> CreateRequestWithUserRoleAsync(HttpMethod method, string url)
        {
            var message = new HttpRequestMessage(method, url);

            UserResponse? currentUser = await _authApiService.GetCurrentUserAsync();

            if (!string.IsNullOrWhiteSpace(currentUser?.Role))
            {
                message.Headers.Add("X-User-Role", currentUser.Role);
            }

            return message;
        }
    }
}