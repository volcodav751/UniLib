using UniLibrary.Blazor.Models;
using UniLibrary.Blazor.Models.ViewModels;

namespace UniLibrary.Blazor.Helpers;

public static class RentalUiHelper
{
    public static bool IsActive(BookRental rental)
    {
        return string.Equals(rental.Status, RentalStatuses.Active, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetStatusName(string status)
    {
        if (string.Equals(status, RentalStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            return "Видано";
        }

        if (string.Equals(status, RentalStatuses.Returned, StringComparison.OrdinalIgnoreCase))
        {
            return "Повернено";
        }

        return "Невідомо";
    }

    public static string DisplayText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
    }

    public static string FormatDate(DateTime? value)
    {
        return value.HasValue
            ? value.Value.ToLocalTime().ToString("dd.MM.yyyy")
            : "—";
    }

    public static string FormatDateTime(DateTime? value)
    {
        return value.HasValue
            ? value.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            : "—";
    }

    public static List<RentalListItem> BuildActiveRentalRows(IEnumerable<Book> books)
    {
        return books
            .SelectMany(book => (book.Rentals ?? [])
                .Where(IsActive)
                .Select(rental => new RentalListItem(book, rental)))
            .OrderBy(row => row.Rental.DueAt)
            .ThenBy(row => row.Book.Title)
            .ToList();
    }

    public static List<RentalListItem> BuildUserRentalRows(IEnumerable<Book> books, int userId)
    {
        return books
            .SelectMany(book => (book.Rentals ?? [])
                .Where(rental => rental.UserId == userId && IsActive(rental))
                .Select(rental => new RentalListItem(book, rental)))
            .OrderBy(row => row.Rental.DueAt)
            .ThenBy(row => row.Book.Title)
            .ToList();
    }

    public static List<RentalListItem> FilterRows(IEnumerable<RentalListItem> rows, string searchText)
    {
        string query = searchText.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            return rows.ToList();
        }

        return rows
            .Where(row => Contains(row.Book.Title, query)
                || Contains(row.Book.Author, query)
                || Contains(row.Rental.FullName, query)
                || Contains(row.Rental.Email, query)
                || Contains(row.Rental.ReaderGroup, query))
            .ToList();
    }

    private static bool Contains(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
