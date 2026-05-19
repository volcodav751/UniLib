using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;
using UniLibrary.Api.Services.Results;

namespace UniLibrary.Api.Services.Books;

public partial class BookService
{
    public ServiceResult<Book> StaffRentBook(int id, StaffCreateRentalRequest request)
    {
        ServiceResult<AppUser> staffResult = GetStaffUser("Оформлювати видачу книг може тільки викладач або адміністратор.");

        if (!staffResult.IsSuccess || staffResult.Value is null)
        {
            return ServiceResult<Book>.From(staffResult);
        }

        ServiceResult<Book> bookResult = GetPhysicalBookForIssue(id);

        if (!bookResult.IsSuccess || bookResult.Value is null)
        {
            return bookResult;
        }

        ServiceResult<NormalizedRentalRequest> rentalRequestResult = NormalizeRentalRequest(request);

        if (!rentalRequestResult.IsSuccess || rentalRequestResult.Value is null)
        {
            return ServiceResult<Book>.From(rentalRequestResult);
        }

        Book book = bookResult.Value;
        NormalizedRentalRequest rentalRequest = rentalRequestResult.Value;
        AppUser? matchedUser = GetMatchedReader(rentalRequest.Email);

        if (matchedUser is not null && BookRentalManager.HasActiveOrPendingRentalForUser(book, matchedUser.Id))
        {
            return ServiceResult<Book>.BadRequest("Цей користувач уже має активну видачу цієї книги.");
        }

        BookRental rental = BookRentalManager.CreateStaffRental(
            book,
            staffResult.Value,
            rentalRequest.FullName,
            rentalRequest.Email,
            rentalRequest.ReaderGroup,
            rentalRequest.Note,
            rentalRequest.DueAt,
            matchedUser?.Id
        );

        book.Rentals.Add(rental);
        BookRentalManager.MarkCopyIssued(book);
        _books.Update(book);

        return ServiceResult<Book>.Ok(book);
    }

    public ServiceResult<Book> StaffReturnBook(int id, int rentalId, StaffReturnRentalRequest? request = null)
    {
        ServiceResult<AppUser> staffResult = GetStaffUser("Фіксувати повернення книг може тільки викладач або адміністратор.");

        if (!staffResult.IsSuccess || staffResult.Value is null)
        {
            return ServiceResult<Book>.From(staffResult);
        }

        Book? book = _books.GetById(id);

        if (book is null)
        {
            return ServiceResult<Book>.NotFound("Книгу не знайдено.");
        }

        BookRentalManager.NormalizeRentalData(book);
        BookRental? rental = book.Rentals.FirstOrDefault(item => item.RentalId == rentalId);

        if (rental is null)
        {
            return ServiceResult<Book>.NotFound("Запис видачі книги не знайдено.");
        }

        ServiceResult canReturnResult = BookRentalManager.ValidateCanReturn(rental);

        if (!canReturnResult.IsSuccess)
        {
            return ServiceResult<Book>.From(canReturnResult);
        }

        BookRentalManager.MarkRentalReturned(
            book,
            rental,
            staffResult.Value,
            string.IsNullOrWhiteSpace(request?.ReturnNote) ? null : request.ReturnNote.Trim()
        );

        _books.Update(book);
        return ServiceResult<Book>.Ok(book);
    }

    private ServiceResult<Book> GetPhysicalBookForIssue(int id)
    {
        Book? book = _books.GetById(id);

        if (book is null)
        {
            return ServiceResult<Book>.NotFound("Книгу не знайдено.");
        }

        BookRentalManager.NormalizeRentalData(book);

        if (book.IsDigital)
        {
            return ServiceResult<Book>.BadRequest("Цифрові книги не потрібно видавати як фізичні примірники.");
        }

        if (!book.IsAvailable || book.AvailableCopies <= 0)
        {
            return ServiceResult<Book>.BadRequest("Немає доступних копій цієї книги.");
        }

        return ServiceResult<Book>.Ok(book);
    }

    private ServiceResult<AppUser> GetStaffUser(string forbiddenMessage)
    {
        ServiceResult<AppUser> userResult = GetCurrentUser();

        if (!userResult.IsSuccess || userResult.Value is null)
        {
            return ServiceResult<AppUser>.Unauthorized();
        }

        return IsStaff(userResult.Value)
            ? userResult
            : ServiceResult<AppUser>.Forbidden(forbiddenMessage);
    }

    private ServiceResult<NormalizedRentalRequest> NormalizeRentalRequest(StaffCreateRentalRequest request)
    {
        string fullName = request.FullName.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return ServiceResult<NormalizedRentalRequest>.BadRequest("Вкажіть ПІБ читача, якому видано книгу.");
        }

        DateTime now = DateTime.UtcNow;
        DateTime dueAt = request.DueAt.HasValue
            ? DateTime.SpecifyKind(request.DueAt.Value, DateTimeKind.Local).ToUniversalTime()
            : now.AddDays(BookRentalManager.DefaultRentDays);

        if (dueAt <= now.Date)
        {
            return ServiceResult<NormalizedRentalRequest>.BadRequest("Дата повернення повинна бути пізніше поточної дати.");
        }

        NormalizedRentalRequest normalizedRequest = new(
            fullName,
            request.Email?.Trim() ?? string.Empty,
            NormalizeOptionalText(request.ReaderGroup),
            NormalizeOptionalText(request.Note),
            dueAt
        );

        return ServiceResult<NormalizedRentalRequest>.Ok(normalizedRequest);
    }

    private AppUser? GetMatchedReader(string email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : _users.GetByEmail(email.ToLowerInvariant());
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsStaff(AppUser user)
    {
        return user.Role == UserRoles.Teacher || user.Role == UserRoles.Admin;
    }

    private sealed record NormalizedRentalRequest(
        string FullName,
        string Email,
        string? ReaderGroup,
        string? Note,
        DateTime DueAt
    );
}
