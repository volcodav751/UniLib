using UniLibrary.Api.Models;
using UniLibrary.Api.Services.Results;

namespace UniLibrary.Api.Services.Books;

public static class BookRentalManager
{
    public const int DefaultRentDays = 14;

    public static Book NormalizeRentalData(Book book)
    {
        book.Rentals ??= [];
        NormalizeCopyCounters(book);
        return book;
    }

    public static int CountOccupiedCopies(Book book)
    {
        return book.Rentals.Count(IsActiveRental);
    }

    public static bool HasActiveRentalForUser(Book book, int userId)
    {
        return book.Rentals.Any(rental => rental.UserId == userId && IsActiveRental(rental));
    }

    public static bool IsActiveRental(BookRental rental)
    {
        return string.Equals(rental.Status, RentalStatuses.Active, StringComparison.OrdinalIgnoreCase);
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
        book.UpdatedAt = DateTime.UtcNow;
    }

    public static ServiceResult ValidateCanReturn(BookRental rental)
    {
        if (IsReturnedRental(rental))
        {
            return ServiceResult.BadRequest("Цю книгу вже позначено як повернену.");
        }

        if (!IsActiveRental(rental))
        {
            return ServiceResult.BadRequest("Цей запис не є активною видачею книги.");
        }

        return ServiceResult.Ok();
    }

    public static void MarkRentalReturned(Book book, BookRental rental, AppUser staff, string? returnNote)
    {
        rental.Status = RentalStatuses.Returned;
        rental.ReturnConfirmedAt = DateTime.UtcNow;
        rental.ReturnedByUserId = staff.Id;
        rental.ReturnedByFullName = staff.FullName;
        rental.ReturnNote = returnNote;

        book.AvailableCopies = Math.Min(book.TotalCopies, book.AvailableCopies + 1);
        book.UpdatedAt = DateTime.UtcNow;
    }

    public static bool MarkRentalsReturnedForUser(Book book, int userId)
    {
        book.Rentals ??= [];
        bool changed = false;

        foreach (BookRental rental in book.Rentals.Where(rental => rental.UserId == userId && IsActiveRental(rental)))
        {
            rental.Status = RentalStatuses.Returned;
            rental.ReturnConfirmedAt = DateTime.UtcNow;
            book.AvailableCopies = Math.Min(book.TotalCopies, book.AvailableCopies + 1);
            changed = true;
        }

        if (changed)
        {
            book.UpdatedAt = DateTime.UtcNow;
        }

        return changed;
    }

    private static void NormalizeCopyCounters(Book book)
    {
        if (book.IsDigital)
        {
            book.TotalCopies = 0;
            book.AvailableCopies = 0;
            return;
        }

        if (book.TotalCopies < 1)
        {
            book.TotalCopies = 1;
        }

        int occupiedCopies = CountOccupiedCopies(book);
        int maxAvailable = Math.Max(0, book.TotalCopies - occupiedCopies);

        if (book.AvailableCopies == 0 && occupiedCopies == 0)
        {
            book.AvailableCopies = book.TotalCopies;
            return;
        }

        book.AvailableCopies = Math.Clamp(book.AvailableCopies, 0, maxAvailable);
    }
}
