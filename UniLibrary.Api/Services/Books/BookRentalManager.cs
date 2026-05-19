using UniLibrary.Api.Models;
using UniLibrary.Api.Services.Results;

namespace UniLibrary.Api.Services.Books;

public static class BookRentalManager
{
    public const int DefaultRentDays = 14;

    private static readonly string[] LegacyPendingStatuses =
    [
        "PendingReturn",
        "ReturnRequested",
        "Pending"
    ];

    public static Book NormalizeRentalData(Book book)
    {
        book.Rentals ??= [];
        MigrateLegacyRental(book);
        NormalizeCopyCounters(book);
        UpdateLegacyRentalFields(book);
        return book;
    }

    public static int CountOccupiedCopies(Book book)
    {
        return book.Rentals.Count(IsActiveOrPendingRental);
    }

    public static bool HasActiveOrPendingRentalForUser(Book book, int userId)
    {
        return book.Rentals.Any(rental => rental.UserId == userId && IsActiveOrPendingRental(rental));
    }

    public static bool IsActiveRental(BookRental rental)
    {
        return string.Equals(rental.Status, RentalStatuses.Active, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsReturnPendingRental(BookRental rental)
    {
        return string.Equals(rental.Status, RentalStatuses.ReturnPending, StringComparison.OrdinalIgnoreCase)
            || LegacyPendingStatuses.Any(status => string.Equals(rental.Status, status, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsReturnedRental(BookRental rental)
    {
        return string.Equals(rental.Status, RentalStatuses.Returned, StringComparison.OrdinalIgnoreCase);
    }

    public static int GetNextRentalId(Book book)
    {
        return book.Rentals.Count == 0 ? 1 : book.Rentals.Max(rental => rental.RentalId) + 1;
    }

    public static BookRental CreateStaffRental(
        Book book,
        AppUser staff,
        string fullName,
        string email,
        string? readerGroup,
        string? note,
        DateTime dueAt,
        int? matchedUserId)
    {
        return new BookRental
        {
            RentalId = GetNextRentalId(book),
            UserId = matchedUserId,
            FullName = fullName,
            Email = email,
            ReaderGroup = readerGroup,
            Note = note,
            RentedAt = DateTime.UtcNow,
            DueAt = dueAt,
            IssuedByUserId = staff.Id,
            IssuedByFullName = staff.FullName,
            Status = RentalStatuses.Active
        };
    }

    public static void MarkCopyIssued(Book book)
    {
        book.AvailableCopies = Math.Max(0, book.AvailableCopies - 1);
        book.IsAvailable = book.IsDigital || book.AvailableCopies > 0;
        book.UpdatedAt = DateTime.UtcNow;
        UpdateLegacyRentalFields(book);
    }

    public static ServiceResult ValidateCanReturn(BookRental rental)
    {
        if (IsReturnedRental(rental))
        {
            return ServiceResult.BadRequest("Цю книгу вже позначено як повернену.");
        }

        if (!IsActiveOrPendingRental(rental))
        {
            return ServiceResult.BadRequest("Цей запис не є активною видачею книги.");
        }

        return ServiceResult.Ok();
    }

    public static void MarkRentalReturned(Book book, BookRental rental, AppUser staff, string? returnNote)
    {
        rental.Status = RentalStatuses.Returned;
        rental.ReturnConfirmedAt = DateTime.UtcNow;
        rental.ConfirmedByUserId = staff.Id;
        rental.ReturnedByUserId = staff.Id;
        rental.ReturnedByFullName = staff.FullName;
        rental.ReturnNote = returnNote;

        book.AvailableCopies = Math.Min(book.TotalCopies, book.AvailableCopies + 1);
        book.IsAvailable = book.IsDigital || book.AvailableCopies > 0;
        book.UpdatedAt = DateTime.UtcNow;

        UpdateLegacyRentalFields(book);
    }

    public static void UpdateLegacyRentalFields(Book book)
    {
        BookRental? firstActiveRental = book.Rentals
            .Where(IsActiveOrPendingRental)
            .OrderBy(rental => rental.RentedAt)
            .FirstOrDefault();

        if (firstActiveRental is null)
        {
            book.RentedByUserId = null;
            book.RentedByFullName = null;
            book.RentedByEmail = null;
            book.RentedAt = null;
            book.RentDueAt = null;
            return;
        }

        book.RentedByUserId = firstActiveRental.UserId;
        book.RentedByFullName = firstActiveRental.FullName;
        book.RentedByEmail = firstActiveRental.Email;
        book.RentedAt = firstActiveRental.RentedAt;
        book.RentDueAt = firstActiveRental.DueAt;
    }

    public static bool MarkRentalsReturnedForUser(Book book, int userId)
    {
        book.Rentals ??= [];
        bool changed = false;

        foreach (BookRental rental in book.Rentals.Where(rental => rental.UserId == userId && IsActiveOrPendingRental(rental)))
        {
            rental.Status = RentalStatuses.Returned;
            rental.ReturnConfirmedAt = DateTime.UtcNow;
            book.AvailableCopies = Math.Min(book.TotalCopies, book.AvailableCopies + 1);
            changed = true;
        }

        if (book.RentedByUserId == userId)
        {
            book.RentedByUserId = null;
            book.RentedByFullName = null;
            book.RentedByEmail = null;
            book.RentedAt = null;
            book.RentDueAt = null;
            book.AvailableCopies = Math.Min(book.TotalCopies, Math.Max(book.AvailableCopies, 1));
            changed = true;
        }

        if (changed)
        {
            book.IsAvailable = book.IsDigital || book.AvailableCopies > 0;
            book.UpdatedAt = DateTime.UtcNow;
            UpdateLegacyRentalFields(book);
        }

        return changed;
    }

    private static bool IsActiveOrPendingRental(BookRental rental)
    {
        return IsActiveRental(rental) || IsReturnPendingRental(rental);
    }

    private static void MigrateLegacyRental(Book book)
    {
        if (book.Rentals.Count > 0 || !book.RentedByUserId.HasValue)
        {
            return;
        }

        book.Rentals.Add(new BookRental
        {
            RentalId = 1,
            UserId = book.RentedByUserId.Value,
            FullName = book.RentedByFullName ?? string.Empty,
            Email = book.RentedByEmail ?? string.Empty,
            RentedAt = book.RentedAt ?? DateTime.UtcNow,
            DueAt = book.RentDueAt ?? DateTime.UtcNow.AddDays(DefaultRentDays),
            Status = RentalStatuses.Active
        });
    }

    private static void NormalizeCopyCounters(Book book)
    {
        if (book.IsDigital)
        {
            book.TotalCopies = 0;
            book.AvailableCopies = 0;
            book.IsAvailable = true;
            return;
        }

        if (book.TotalCopies < 1)
        {
            book.TotalCopies = 1;
        }

        int occupiedCopies = CountOccupiedCopies(book);
        int maxAvailable = Math.Max(0, book.TotalCopies - occupiedCopies);
        book.AvailableCopies = Math.Clamp(book.AvailableCopies, 0, maxAvailable);

        if (book.AvailableCopies == 0 && occupiedCopies == 0 && book.IsAvailable)
        {
            book.AvailableCopies = book.TotalCopies;
        }

        book.IsAvailable = book.AvailableCopies > 0;
    }
}
