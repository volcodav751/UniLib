using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;
using UniLibrary.Api.Repos;
using UniLibrary.Api.Services.Common;
using UniLibrary.Api.Services.Files;
using UniLibrary.Api.Services.Results;

namespace UniLibrary.Api.Services.Books;

public partial class BookService : IBookService
{
    private readonly IBookRepository _books;
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IBookFileService _bookFileService;

    public BookService(
        IBookRepository books,
        IUserRepository users,
        ICurrentUserService currentUser,
        IBookFileService bookFileService)
    {
        _books = books;
        _users = users;
        _currentUser = currentUser;
        _bookFileService = bookFileService;
    }

    public List<Book> GetAll()
    {
        return _books.GetAll()
            .Select(BookRentalManager.NormalizeRentalData)
            .OrderBy(book => book.Title)
            .ToList();
    }

    public ServiceResult<Book> GetById(int id)
    {
        Book? book = _books.GetById(id);

        if (book is null)
        {
            return ServiceResult<Book>.NotFound();
        }

        BookRentalManager.NormalizeRentalData(book);
        return ServiceResult<Book>.Ok(book);
    }

    public List<Book> GetBooksWithActiveRentals()
    {
        return _books.GetAll()
            .Select(BookRentalManager.NormalizeRentalData)
            .Where(book => book.Rentals.Any(BookRentalManager.IsActiveRental))
            .OrderBy(book => book.Title)
            .ToList();
    }

    public ServiceResult<Book> Create(CreateBookRequest request)
    {
        string? validationError = BookRequestValidator.Validate(request);

        if (validationError is not null)
        {
            return ServiceResult<Book>.BadRequest(validationError);
        }

        Book book = BookMapper.CreateFromRequest(request, _books.GetNextId());
        _books.Add(book);
        return ServiceResult<Book>.Created(book);
    }

    public ServiceResult<Book> Update(int id, CreateBookRequest request)
    {
        string? validationError = BookRequestValidator.Validate(request);

        if (validationError is not null)
        {
            return ServiceResult<Book>.BadRequest(validationError);
        }

        Book? book = _books.GetById(id);

        if (book is null)
        {
            return ServiceResult<Book>.NotFound();
        }

        BookRentalManager.NormalizeRentalData(book);
        int occupiedCopies = BookRentalManager.CountOccupiedCopies(book);

        if (occupiedCopies > 0 && request.IsDigital)
        {
            return ServiceResult<Book>.BadRequest("Не можна зробити книгу цифровою, поки хоча б одна фізична копія видана читачу.");
        }

        if (!request.IsDigital && request.TotalCopies < occupiedCopies)
        {
            return ServiceResult<Book>.BadRequest($"Кількість копій не може бути меншою за кількість зайнятих копій: {occupiedCopies}.");
        }

        BookMapper.UpdateBook(book, request, occupiedCopies);
        _books.Update(book);

        return ServiceResult<Book>.Ok(book);
    }

    public ServiceResult Delete(int id)
    {
        Book? book = _books.GetById(id);

        if (book is null)
        {
            return ServiceResult.NotFound();
        }

        _bookFileService.DeleteStoredFiles(book);
        _books.Delete(id);

        return ServiceResult.NoContent();
    }

    private ServiceResult<AppUser> GetCurrentUser()
    {
        int? userId = _currentUser.UserId;

        if (userId is null)
        {
            return ServiceResult<AppUser>.Unauthorized();
        }

        AppUser? user = _users.GetById(userId.Value);
        return user is null
            ? ServiceResult<AppUser>.Unauthorized()
            : ServiceResult<AppUser>.Ok(user);
    }
}
