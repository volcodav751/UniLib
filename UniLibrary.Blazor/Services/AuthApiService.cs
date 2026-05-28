using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using UniLibrary.Blazor.Helpers;
using UniLibrary.Blazor.Models;
using UniLibrary.Blazor.Models.Requests;

namespace UniLibrary.Blazor.Services
{
    public class AuthApiService
    {
        private const string AuthStorageKey = "unilibrary_auth";
        private const string OldCurrentUserStorageKey = "unilibrary_current_user";

        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public event Action? AuthStateChanged;

        public AuthApiService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task<(bool Success, string Message, UserResponse? User, bool RequiresApproval)> RegisterAsync(RegisterRequest request)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    AuthResponse? auth = JsonSerializer.Deserialize<AuthResponse>(responseText, JsonOptions);

                    if (auth is not null)
                    {
                        if (auth.RequiresApproval || string.IsNullOrWhiteSpace(auth.Token))
                        {
                            string approvalMessage = string.IsNullOrWhiteSpace(auth.Message)
                                ? "Акаунт викладача створено. Вхід буде доступний після підтвердження адміністратором."
                                : auth.Message;

                            return (true, approvalMessage, auth.User, true);
                        }

                        await SaveAuthAsync(auth);

                        string message = !string.IsNullOrWhiteSpace(auth.Message)
                            ? auth.Message
                            : auth.User.Role == UserRoleHelper.Admin
                                ? "Реєстрація успішна. Акаунт активовано."
                                : "Реєстрація успішна";

                        return (true, message, auth.User, false);
                    }

                    return (false, "Сервер повернув порожню відповідь.", null, false);
                }

                return (false, CleanError(responseText), null, false);
            }
            catch
            {
                return (false, "Не вдалося підключитися до сервера. Перевірте підключення до сервера і спробуйте ще раз.", null, false);
            }
        }

        public async Task<(bool Success, string Message, UserResponse? User)> LoginAsync(LoginRequest request)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    AuthResponse? auth = JsonSerializer.Deserialize<AuthResponse>(responseText, JsonOptions);

                    if (auth is not null)
                    {
                        await SaveAuthAsync(auth);
                        return (true, "Вхід виконано успішно", auth.User);
                    }

                    return (false, "Сервер повернув порожню відповідь.", null);
                }

                return (false, CleanError(responseText), null);
            }
            catch
            {
                return (false, "Не вдалося підключитися до сервера. Перевірте підключення до сервера і спробуйте ще раз.", null);
            }
        }

        public async Task<UserResponse?> GetCurrentUserAsync()
        {
            AuthResponse? auth = await GetAuthAsync();
            return auth?.User;
        }

        public async Task<string?> GetTokenAsync()
        {
            AuthResponse? auth = await GetAuthAsync();
            return auth?.Token;
        }

        public async Task<bool> IsTeacherOrAdminAsync()
        {
            UserResponse? user = await GetCurrentUserAsync();
            return UserRoleHelper.CanManageBooks(user?.Role);
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
                return (false, string.IsNullOrWhiteSpace(error) ? "Не вдалося видалити користувача." : CleanError(error));
            }
            catch
            {
                return (false, "Не вдалося виконати дію. Спробуйте ще раз.");
            }
        }
        public async Task<(bool Success, string Message)> ApproveTeacherAsync(int id)
        {
            try
            {
                using HttpRequestMessage message = await CreateAuthorizedRequestAsync(
                    HttpMethod.Put,
                    $"api/auth/users/{id}/approve-teacher"
                );

                HttpResponseMessage response = await _httpClient.SendAsync(message);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Акаунт викладача підтверджено.");
                }

                string error = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(error) ? "Не вдалося підтвердити акаунт викладача." : CleanError(error));
            }
            catch
            {
                return (false, "Не вдалося виконати дію. Спробуйте ще раз.");
            }
        }


        public async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string url)
        {
            HttpRequestMessage message = new(method, url);
            string? token = await GetTokenAsync();

            if (!string.IsNullOrWhiteSpace(token))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return message;
        }

        public async Task LogoutAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AuthStorageKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", OldCurrentUserStorageKey);
            AuthStateChanged?.Invoke();
        }

        private async Task<AuthResponse?> GetAuthAsync()
        {
            try
            {
                string? json = await _jsRuntime.InvokeAsync<string?>(
                    "localStorage.getItem",
                    AuthStorageKey
                );

                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                AuthResponse? auth = JsonSerializer.Deserialize<AuthResponse>(json, JsonOptions);

                if (auth is null || string.IsNullOrWhiteSpace(auth.Token))
                {
                    return null;
                }

                if (auth.ExpiresAt <= DateTime.UtcNow)
                {
                    await LogoutAsync();
                    return null;
                }

                return auth;
            }
            catch
            {
                return null;
            }
        }

        private async Task SaveAuthAsync(AuthResponse auth)
        {
            string json = JsonSerializer.Serialize(auth);

            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AuthStorageKey, json);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", OldCurrentUserStorageKey);

            AuthStateChanged?.Invoke();
        }

        private static string CleanError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return "Сталася помилка. Спробуйте ще раз.";
            }

            string cleaned = error.Trim('\"', ' ', '\n', '\r', '\t');

            if (cleaned.Contains("Exception", StringComparison.OrdinalIgnoreCase)
                || cleaned.Contains("StackTrace", StringComparison.OrdinalIgnoreCase)
                || cleaned.Contains("Microsoft.", StringComparison.OrdinalIgnoreCase)
                || cleaned.Contains("System.", StringComparison.OrdinalIgnoreCase)
                || cleaned.StartsWith("{", StringComparison.OrdinalIgnoreCase)
                || cleaned.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
            {
                return "Сталася помилка на сервері. Спробуйте ще раз або зверніться до адміністратора.";
            }

            return cleaned;
        }
    }
}
