using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;

namespace UniLibrary.Api.Services.Books;

public static class BookMapper
{
    public static Book CreateFromRequest(CreateBookRequest request, int id)
    {
        bool isDigital = request.IsDigital;
        int totalCopies = isDigital ? 0 : request.TotalCopies;
        int availableCopies = isDigital
            ? 0
            : request.IsAvailable ? totalCopies : 0;

        return new Book
        {
            Id = id,
            Title = NormalizeText(request.Title),
            Author = NormalizeText(request.Author),
            Description = NormalizeText(request.Description),
            Category = NormalizeText(request.Category),
            Language = NormalizeText(request.Language),
            Publisher = NormalizeText(request.Publisher),
            PublicationYear = request.PublicationYear,
            PageCount = request.PageCount,
            Isbn = NormalizeText(request.Isbn),
            Tags = NormalizeTags(request.Tags),
            IsAvailable = isDigital || availableCopies > 0,
            IsDigital = isDigital,
            TotalCopies = totalCopies,
            AvailableCopies = availableCopies,
            Rentals = new List<BookRental>(),
            CoverImageUrl = request.CoverImageUrl?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static void UpdateBook(Book book, CreateBookRequest request, int occupiedCopies)
    {
        book.Title = NormalizeText(request.Title);
        book.Author = NormalizeText(request.Author);
        book.Description = NormalizeText(request.Description);
        book.Category = NormalizeText(request.Category);
        book.Language = NormalizeText(request.Language);
        book.Publisher = NormalizeText(request.Publisher);
        book.PublicationYear = request.PublicationYear;
        book.PageCount = request.PageCount;
        book.Isbn = NormalizeText(request.Isbn);
        book.Tags = NormalizeTags(request.Tags);
        book.IsDigital = request.IsDigital;
        book.TotalCopies = request.IsDigital ? 0 : request.TotalCopies;
        book.AvailableCopies = request.IsDigital
            ? 0
            : request.IsAvailable ? Math.Max(0, book.TotalCopies - occupiedCopies) : 0;
        book.IsAvailable = request.IsDigital || book.AvailableCopies > 0;
        book.CoverImageUrl = request.CoverImageUrl?.Trim();
        book.UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static List<string> NormalizeTags(List<string>? tags)
    {
        if (tags is null)
        {
            return new List<string>();
        }

        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
