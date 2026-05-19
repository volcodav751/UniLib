using UniLibrary.Blazor.Models;
using UniLibrary.Blazor.Models.ViewModels;

namespace UniLibrary.Blazor.Helpers;

public static class RentalUiHelper
{
    private static readonly string[] LegacyPendingStatuses =
    [
        "PendingReturn",
        "ReturnRequested",
        "Pending"
    ];

    public static bool IsActive(BookRental rental)
    {
        return IsActiveStatus(rental.Status);
    }

    public static bool IsActiveOrPending(BookRental rental)
    {
        return IsActiveStatus(rental.Status) || IsPendingStatus(rental.Status);
    }

    public static bool IsActiveStatus(string? status)
    {
        return string.Equals(status, RentalStatuses.Active, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPendingStatus(string? status)
    {
        return string.Equals(status, RentalStatuses.ReturnPending, StringComparison.OrdinalIgnoreCase)
            || LegacyPendingStatuses.Any(oldStatus => string.Equals(status, oldStatus, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetStatusName(string? status)
    {
        if (IsActiveStatus(status))
        {
            return "Видана читачу";
        }

        if (IsPendingStatus(status))
        {
            return "Очікує службового повернення";
        }

        if (string.Equals(status, RentalStatuses.Returned, StringComparison.OrdinalIgnoreCase))
        {
            return "Повернено";
        }

        return string.IsNullOrWhiteSpace(status) ? "Невідомо" : status;
    }

    public static List<RentalListItem> BuildActiveRentalRows(IEnumerable<Book> books)
    {
        return books
            .SelectMany(book => book.Rentals
                .Where(IsActiveOrPending)
                .Select(rental => new RentalListItem(book, rental)))
            .OrderBy(row => row.Rental.DueAt)
            .ToList();
    }

    public static List<RentalListItem> BuildUserRentalRows(IEnumerable<Book> books, int userId)
    {
        return books
            .SelectMany(book => book.Rentals
                .Where(rental => rental.UserId == userId && IsActiveOrPending(rental))
                .Select(rental => new RentalListItem(book, rental)))
            .OrderBy(row => row.Rental.DueAt)
            .ToList();
    }

    public static List<RentalListItem> FilterRows(IEnumerable<RentalListItem> rows, string? query)
    {
        string searchText = query?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return rows.ToList();
        }

        return rows
            .Where(row => Contains(row.Book.Title, searchText)
                || Contains(row.Book.Author, searchText)
                || Contains(row.Rental.FullName, searchText)
                || Contains(row.Rental.Email, searchText)
                || Contains(row.Rental.ReaderGroup, searchText))
            .ToList();
    }

    public static string FormatDate(DateTime? value)
    {
        return value.HasValue ? value.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm") : "—";
    }

    public static string DisplayText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "—" : value;
    }

    private static bool Contains(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
