using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using UniLibrary.Blazor.Models;

namespace UniLibrary.Blazor.Services
{
    public class AuthApiService
    {
        private const string CurrentUserStorageKey = "unilibrary_current_user";

        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;

        public event Action? AuthStateChanged;

        public AuthApiService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task<(bool Success, string Message, UserResponse? User)> RegisterAsync(RegisterRequest request)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/auth/register", request);

                if (response.IsSuccessStatusCode)
                {
                    UserResponse? user = await response.Content.ReadFromJsonAsync<UserResponse>();

                    if (user is not null)
                    {
                        await SaveCurrentUserAsync(user);
                    }

                    return (true, "Реєстрація успішна", user);
                }

                string error = await response.Content.ReadAsStringAsync();

                return (false, error, null);
            }
            catch (Exception ex)
            {
                return (false, $"Помилка підключення до сервера: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, UserResponse? User)> LoginAsync(LoginRequest request)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

                if (response.IsSuccessStatusCode)
                {
                    UserResponse? user = await response.Content.ReadFromJsonAsync<UserResponse>();

                    if (user is not null)
                    {
                        await SaveCurrentUserAsync(user);
                    }

                    return (true, "Вхід виконано успішно", user);
                }

                string error = await response.Content.ReadAsStringAsync();

                return (false, error, null);
            }
            catch (Exception ex)
            {
                return (false, $"Помилка підключення до сервера: {ex.Message}", null);
            }
        }

        public async Task<List<UserResponse>> GetUsersAsync()
        {
            try
            {
                using HttpRequestMessage message = await CreateAuthorizedRequestAsync(
                    HttpMethod.Get,
                    "api/auth/users"
                );

                HttpResponseMessage response = await _httpClient.SendAsync(message);

                if (!response.IsSuccessStatusCode)
                {
                    return new List<UserResponse>();
                }

                List<UserResponse>? users = await response.Content.ReadFromJsonAsync<List<UserResponse>>();

                return users ?? new List<UserResponse>();
            }
            catch
            {
                return new List<UserResponse>();
            }
        }

        public async Task<(bool Success, string Message)> DeleteUserAsync(int id)
        {
            try
            {
                using HttpRequestMessage message = await CreateAuthorizedRequestAsync(
                    HttpMethod.Delete,
                    $"api/auth/users/{id}"
                );

                HttpResponseMessage response = await _httpClient.SendAsync(message);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Користувача видалено.");
                }

                string error = await response.Content.ReadAsStringAsync();

                return (false, string.IsNullOrWhiteSpace(error) ? "Не вдалося видалити користувача." : error);
            }
            catch (Exception ex)
            {
                return (false, $"Помилка: {ex.Message}");
            }
        }

        public async Task<UserResponse?> GetCurrentUserAsync()
        {
            try
            {
                string? json = await _jsRuntime.InvokeAsync<string?>(
                    "localStorage.getItem",
                    CurrentUserStorageKey
                );

                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<UserResponse>(json);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> IsTeacherAsync()
        {
            UserResponse? user = await GetCurrentUserAsync();

            return user?.Role == "Teacher";
        }

        public async Task<bool> IsAdminAsync()
        {
            UserResponse? user = await GetCurrentUserAsync();

            return user?.Role == "Admin";
        }

        public async Task<bool> IsTeacherOrAdminAsync()
        {
            UserResponse? user = await GetCurrentUserAsync();

            return user?.Role == "Teacher" || user?.Role == "Admin";
        }

        public async Task LogoutAsync()
        {
            await _jsRuntime.InvokeVoidAsync(
                "localStorage.removeItem",
                CurrentUserStorageKey
            );

            AuthStateChanged?.Invoke();
        }

        private async Task SaveCurrentUserAsync(UserResponse user)
        {
            string json = JsonSerializer.Serialize(user);

            await _jsRuntime.InvokeVoidAsync(
                "localStorage.setItem",
                CurrentUserStorageKey,
                json
            );

            AuthStateChanged?.Invoke();
        }

        private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string url)
        {
            HttpRequestMessage message = new HttpRequestMessage(method, url);

            UserResponse? currentUser = await GetCurrentUserAsync();

            if (!string.IsNullOrWhiteSpace(currentUser?.Role))
            {
                message.Headers.Add("X-User-Role", currentUser.Role);
            }

            if (currentUser is not null)
            {
                message.Headers.Add("X-User-Id", currentUser.Id.ToString());
            }

            return message;
        }
    }
}
