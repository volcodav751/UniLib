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
    }
}