using UniLibrary.Blazor.Models;
using UniLibrary.Blazor.Models.Requests;

namespace UniLibrary.Blazor.Helpers;

public static class BookFormHelper
{
    public const string PhysicalBookType = "Physical";
    public const string DigitalBookType = "Digital";

    public static CreateBookRequest CreateEmptyModel()
    {
        return new CreateBookRequest
        {
            IsDigital = false,
            TotalCopies = 1
        };
    }

    public static string? Validate(CreateBookRequest book)
    {
        if (string.IsNullOrWhiteSpace(book.Title))
        {
            return "Введіть назву книги.";
        }

        if (string.IsNullOrWhiteSpace(book.Author))
        {
            return "Введіть автора книги.";
        }

        if (!LibraryOptions.IsAllowedCategory(book.Category))
        {
            return "Оберіть категорію зі списку.";
        }

        if (!LibraryOptions.IsAllowedLanguage(book.Language))
        {
            return "Оберіть мову зі списку.";
        }

        if (book.PublicationYear < 1000 || book.PublicationYear > DateTime.Now.Year + 1)
        {
            return "Вкажіть коректний рік публікації.";
        }

        if (book.PageCount < 0)
        {
            return "Кількість сторінок не може бути від'ємною.";
        }

        if (!book.IsDigital && book.TotalCopies < 1)
        {
            return "Для фізичної книги потрібно вказати хоча б 1 копію.";
        }

        return null;
    }

    public static void ApplyBookType(CreateBookRequest book, string bookType)
    {
        book.IsDigital = bookType == DigitalBookType;

        if (book.IsDigital)
        {
            book.TotalCopies = 0;
        }
        else if (book.TotalCopies < 1)
        {
            book.TotalCopies = 1;
        }
    }

    public static void PrepareForSaving(CreateBookRequest book, string tagsText)
    {
        book.Title = book.Title.Trim();
        book.Author = book.Author.Trim();
        book.Description = book.Description.Trim();
        book.Publisher = book.Publisher.Trim();
        book.Isbn = book.Isbn.Trim();
        book.CoverImageUrl = book.CoverImageUrl?.Trim();
        book.Tags = ParseTags(tagsText);
    }

    private static List<string> ParseTags(string tagsText)
    {
        return tagsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
