namespace UniLibrary.Blazor.Models;

public static class LibraryOptions
{
    public static readonly IReadOnlyList<string> Categories =
    [
        "Комп'ютерні науки",
        "Програмування",
        "Математика",
        "Фізика",
        "Хімія",
        "Біологія",
        "Історія",
        "Література",
        "Електротехніка",
        "Економіка",
        "Право",
        "Іноземні мови"
    ];

    public static readonly IReadOnlyList<string> Languages =
    [
        "Українська",
        "Англійська",
        "Польська",
        "Німецька",
        "Французька"
    ];

    public static bool IsAllowedCategory(string? value)
    {
        return Categories.Contains(value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsAllowedLanguage(string? value)
    {
        return Languages.Contains(value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }
}
