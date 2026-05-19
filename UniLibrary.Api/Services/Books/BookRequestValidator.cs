using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;

namespace UniLibrary.Api.Services.Books;

public static class BookRequestValidator
{
    public static string? Validate(CreateBookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return "Введіть назву книги.";
        }

        if (string.IsNullOrWhiteSpace(request.Author))
        {
            return "Введіть автора книги.";
        }

        if (!LibraryOptions.IsAllowedCategory(request.Category))
        {
            return "Оберіть категорію зі списку.";
        }

        if (!LibraryOptions.IsAllowedLanguage(request.Language))
        {
            return "Оберіть мову зі списку.";
        }

        if (request.PublicationYear < 1000 || request.PublicationYear > DateTime.Now.Year + 1)
        {
            return "Вкажіть коректний рік публікації.";
        }

        if (request.PageCount < 0)
        {
            return "Кількість сторінок не може бути від'ємною.";
        }

        if (!request.IsDigital && request.TotalCopies < 1)
        {
            return "Для фізичної книги потрібно вказати хоча б 1 копію.";
        }

        return null;
    }
}
