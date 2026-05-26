using Microsoft.JSInterop;

namespace UniLibrary.Blazor.Services;

public sealed class ThemeService
{
    private readonly IJSRuntime _jsRuntime;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string CurrentTheme { get; private set; } = "light";

    public async Task InitializeAsync()
    {
        string? savedTheme = await _jsRuntime.InvokeAsync<string?>("uniLibraryTheme.get");
        CurrentTheme = NormalizeTheme(savedTheme);
        await _jsRuntime.InvokeVoidAsync("uniLibraryTheme.set", CurrentTheme);
    }

    public async Task<string> ToggleAsync()
    {
        CurrentTheme = CurrentTheme == "dark" ? "light" : "dark";
        await _jsRuntime.InvokeVoidAsync("uniLibraryTheme.set", CurrentTheme);
        return CurrentTheme;
    }

    private static string NormalizeTheme(string? theme)
    {
        return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
    }
}
