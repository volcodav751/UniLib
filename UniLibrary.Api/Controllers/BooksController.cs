using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniLibrary.Api.Data;
using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;
using UniLibrary.Api.Services;

namespace UniLibrary.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private const int DefaultRentDays = 14;

        private readonly LiteDbContext _context;
        private readonly PdfPreviewService _pdfPreviewService;

        public BooksController(LiteDbContext context, PdfPreviewService pdfPreviewService)
        {
            _context = context;
            _pdfPreviewService = pdfPreviewService;
        }

        [HttpGet]
        public ActionResult<List<Book>> GetAll()
        {
            List<Book> books = _context.Books.FindAll()
                .Select(NormalizeRentalData)
                .OrderBy(book => book.Title)
                .ToList();

            return Ok(books);
        }

        [HttpGet("{id:int}")]
        public ActionResult<Book> GetById(int id)
        {
            Book? book = _context.Books.FindById(id);

            if (book == null)
            {
                return NotFound();
            }

            NormalizeRentalData(book);
            return Ok(book);
        }

        [Authorize(Roles = UserRoles.TeacherOrAdmin)]
        [HttpGet("return-requests")]
        public ActionResult<List<Book>> GetReturnRequests()
        {
            List<Book> booksWithPendingReturns = _context.Books.FindAll()
                .Select(NormalizeRentalData)
                .Where(book => book.Rentals.Any(IsReturnPendingRental))
                .OrderBy(book => book.Title)
                .ToList();

            return Ok(booksWithPendingReturns);
        }

        [Authorize(Roles = UserRoles.TeacherOrAdmin)]
        [HttpPost]
        public ActionResult<Book> Create([FromBody] CreateBookRequest request)
        {
            string? validationError = ValidateBookRequest(request);

            if (validationError is not null)
            {
                return BadRequest(validationError);
            }

            int nextId = _context.Books.Count() == 0
                ? 1
                : _context.Books.Max(x => x.Id) + 1;

            bool isDigital = request.IsDigital;
            int totalCopies = isDigital ? 0 : request.TotalCopies;
            int availableCopies = isDigital
                ? 0
                : request.IsAvailable ? totalCopies : 0;

            Book book = new()
            {
                Id = nextId,
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

            _context.Books.Insert(book);

            return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
        }

        [Authorize(Roles = UserRoles.TeacherOrAdmin)]
        [HttpPut("{id:int}")]
        public ActionResult<Book> Update(int id, [FromBody] CreateBookRequest request)
        {
            string? validationError = ValidateBookRequest(request);

            if (validationError is not null)
            {
                return BadRequest(validationError);
            }

            Book? book = _context.Books.FindById(id);

            if (book == null)
            {
                return NotFound();
            }

            NormalizeRentalData(book);
            int occupiedCopies = CountOccupiedCopies(book);

            if (occupiedCopies > 0 && request.IsDigital)
            {
                return BadRequest("Не можна зробити книгу цифровою, поки хоча б одна копія знаходиться в оренді або очікує підтвердження повернення.");
            }

            if (!request.IsDigital && request.TotalCopies < occupiedCopies)
            {
                return BadRequest($"Кількість копій не може бути меншою за кількість зайнятих копій: {occupiedCopies}.");
            }

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

            UpdateLegacyRentalFields(book);
            _context.Books.Update(book);

            return Ok(book);
        }

        [HttpPost("{id:int}/rent")]
        public ActionResult<Book> RentBook(int id)
        {
            Book? book = _context.Books.FindById(id);

            if (book == null)
            {
                return NotFound("Книгу не знайдено.");
            }

            AppUser? currentUser = GetCurrentUser();

            if (currentUser is null)
            {
                return Unauthorized();
            }

            NormalizeRentalData(book);

            if (book.IsDigital)
            {
                return BadRequest("Цифрову книгу не потрібно орендувати. Її можна переглядати або скачати через файл.");
            }

            if (HasActiveOrPendingRentalForUser(book, currentUser.Id))
            {
                return BadRequest("Ви вже орендували цю книгу або вже подали запит на її повернення.");
            }

            if (!book.IsAvailable || book.AvailableCopies <= 0)
            {
                return BadRequest("Зараз немає доступних копій цієї книги.");
            }

            BookRental rental = new()
            {
                RentalId = GetNextRentalId(book),
                UserId = currentUser.Id,
                FullName = currentUser.FullName,
                Email = currentUser.Email,
                RentedAt = DateTime.UtcNow,
                DueAt = DateTime.UtcNow.AddDays(DefaultRentDays),
                Status = RentalStatuses.Active
            };

            book.Rentals.Add(rental);
            book.AvailableCopies = Math.Max(0, book.AvailableCopies - 1);
            book.IsAvailable = book.AvailableCopies > 0;
            book.UpdatedAt = DateTime.UtcNow;

            UpdateLegacyRentalFields(book);
            _context.Books.Update(book);

            return Ok(book);
        }

        [HttpPost("{id:int}/return-request")]
        public ActionResult<Book> RequestReturnBook(int id)
        {
            Book? book = _context.Books.FindById(id);

            if (book == null)
            {
                return NotFound("Книгу не знайдено.");
            }

            int? currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            NormalizeRentalData(book);

            BookRental? rental = book.Rentals
                .FirstOrDefault(item => item.UserId == currentUserId.Value && IsActiveRental(item));

            if (rental is null)
            {
                return BadRequest("У вас немає активної оренди цієї книги або запит на повернення вже подано.");
            }

            rental.Status = RentalStatuses.ReturnPending;
            rental.ReturnRequestedAt = DateTime.UtcNow;
            book.UpdatedAt = DateTime.UtcNow;

            UpdateLegacyRentalFields(book);
            _context.Books.Update(book);

            return Ok(book);
        }

        [Authorize(Roles = UserRoles.TeacherOrAdmin)]
        [HttpPost("{id:int}/rentals/{rentalId:int}/confirm-return")]
        public ActionResult<Book> ConfirmReturnBook(int id, int rentalId)
        {
            Book? book = _context.Books.FindById(id);

            if (book == null)
            {
                return NotFound("Книгу не знайдено.");
            }

            int? currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            NormalizeRentalData(book);

            BookRental? rental = book.Rentals.FirstOrDefault(item => item.RentalId == rentalId);

            if (rental is null)
            {
                return NotFound("Запис оренди не знайдено.");
            }

            if (IsReturnedRental(rental))
            {
                return BadRequest("Повернення цієї копії вже підтверджено.");
            }

            if (!IsReturnPendingRental(rental))
            {
                return BadRequest("Ця оренда ще не має запиту на повернення.");
            }

            rental.Status = RentalStatuses.Returned;
            rental.ReturnConfirmedAt = DateTime.UtcNow;
            rental.ConfirmedByUserId = currentUserId.Value;

            book.AvailableCopies = Math.Min(book.TotalCopies, book.AvailableCopies + 1);
            book.IsAvailable = !book.IsDigital && book.AvailableCopies > 0;
            book.UpdatedAt = DateTime.UtcNow;

            UpdateLegacyRentalFields(book);
            _context.Books.Update(book);

            return Ok(book);
        }

        [HttpPost("{id:int}/return")]
        public ActionResult<Book> ReturnBook(int id)
        {
            if (User.IsInRole(UserRoles.Teacher) || User.IsInRole(UserRoles.Admin))
            {
                Book? book = _context.Books.FindById(id);

                if (book == null)
                {
                    return NotFound("Книгу не знайдено.");
                }

                NormalizeRentalData(book);
                BookRental? pendingRental = book.Rentals.FirstOrDefault(IsReturnPendingRental);

                if (pendingRental is null)
                {
                    return BadRequest("Немає запитів на повернення, які потрібно підтвердити.");
                }

                return ConfirmReturnBook(id, pendingRental.RentalId);
            }

            return RequestReturnBook(id);
        }

        [Authorize(Roles = UserRoles.Admin)]
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            Book? book = _context.Books.FindById(id);

            if (book == null)
            {
                return NotFound();
            }

            DeleteStoredFiles(book);
            _context.Books.Delete(id);

            return NoContent();
        }

        [Authorize(Roles = UserRoles.TeacherOrAdmin)]
        [HttpPost("{id:int}/file")]
        public async Task<IActionResult> UploadFile(int id, [FromForm] UploadBookFileRequest request)
        {
            Book? book = _context.Books.FindById(id);

            if (book == null)
            {
                return NotFound("Book not found.");
            }

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest("File is empty.");
            }

            string[] allowedExtensions = [".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt"];
            string originalFileName = Path.GetFileName(request.File.FileName);
            string extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Unsupported file type. Дозволено: PDF, DOC, DOCX, TXT, RTF, ODT.");
            }

            byte[] originalBytes;

            await using (Stream uploadStream = request.File.OpenReadStream())
            using (MemoryStream memoryStream = new())
            {
                await uploadStream.CopyToAsync(memoryStream);
                originalBytes = memoryStream.ToArray();
            }

            DeleteStoredFiles(book);

            string storedFileName = $"{Guid.NewGuid()}{extension}";
            string fileId = $"books/{id}/original/{storedFileName}";

            using (MemoryStream originalStream = new(originalBytes))
            {
                _context.FileStorage.Upload(fileId, storedFileName, originalStream);
            }

            book.FileId = fileId;
            book.OriginalFileName = originalFileName;
            book.StoredFileName = storedFileName;
            book.ContentType = ResolveContentType(extension, request.File.ContentType);
            book.FileSizeBytes = request.File.Length;
            book.FileUploadedAt = DateTime.UtcNow;
            book.UpdatedAt = DateTime.UtcNow;

            PdfPreviewResult previewResult = await _pdfPreviewService.CreatePreviewPdfAsync(originalBytes, originalFileName);

            if (previewResult.Success && previewResult.PdfBytes is not null)
            {
                if (previewResult.OriginalIsPdf)
                {
                    book.PreviewFileId = fileId;
                    book.PreviewFileName = originalFileName;
                }
                else
                {
                    string previewFileName = $"{Path.GetFileNameWithoutExtension(originalFileName)}.pdf";
                    string previewFileId = $"books/{id}/preview/{Guid.NewGuid()}.pdf";

                    using MemoryStream previewStream = new(previewResult.PdfBytes);
                    _context.FileStorage.Upload(previewFileId, previewFileName, previewStream);

                    book.PreviewFileId = previewFileId;
                    book.PreviewFileName = previewFileName;
                }

                book.PreviewContentType = "application/pdf";
                book.PreviewGeneratedAt = DateTime.UtcNow;
                book.PreviewStatus = "Ready";
                book.PreviewError = null;
            }
            else
            {
                book.PreviewFileId = null;
                book.PreviewFileName = null;
                book.PreviewContentType = null;
                book.PreviewGeneratedAt = null;
                book.PreviewStatus = "Failed";
                book.PreviewError = previewResult.ErrorMessage;
            }

            _context.Books.Update(book);

            return Ok(new
            {
                message = previewResult.Success
                    ? "File uploaded successfully. PDF preview is ready."
                    : "File uploaded successfully, but PDF preview was not created.",
                previewStatus = book.PreviewStatus,
                previewError = book.PreviewError,
                book.Id,
                book.FileId,
                book.OriginalFileName,
                book.StoredFileName,
                book.ContentType,
                book.FileSizeBytes,
                book.PreviewFileId,
                book.PreviewFileName
            });
        }

        [HttpGet("{id:int}/file")]
        public IActionResult DownloadFile(int id)
        {
            Book? book = _context.Books.FindById(id);

            if (book == null)
            {
                return NotFound("Book not found.");
            }

            if (string.IsNullOrWhiteSpace(book.FileId))
            {
                return NotFound("File not found for this book.");
            }

            var fileInfo = _context.FileStorage.FindById(book.FileId);

            if (fileInfo == null)
            {
                return NotFound("Stored file not found.");
            }

            MemoryStream memoryStream = new();
            fileInfo.CopyTo(memoryStream);
            memoryStream.Position = 0;

            return File(
                memoryStream,
                book.ContentType ?? "application/octet-stream",
                book.OriginalFileName ?? "downloaded-file"
            );
        }

        [Authorize(Roles = UserRoles.TeacherOrAdmin)]
        [HttpDelete("{id:int}/file")]
        public IActionResult DeleteFile(int id)
        {
            Book? book = _context.Books.FindById(id);

            if (book == null)
            {
                return NotFound("Book not found.");
            }

            if (string.IsNullOrWhiteSpace(book.FileId))
            {
                return NotFound("File not attached.");
            }

            DeleteStoredFiles(book);

            book.FileId = null;
            book.OriginalFileName = null;
            book.StoredFileName = null;
            book.ContentType = null;
            book.FileSizeBytes = null;
            book.FileUploadedAt = null;
            book.PreviewFileId = null;
            book.PreviewFileName = null;
            book.PreviewContentType = null;
            book.PreviewGeneratedAt = null;
            book.PreviewStatus = null;
            book.PreviewError = null;
            book.UpdatedAt = DateTime.UtcNow;

            _context.Books.Update(book);

            return NoContent();
        }

        [HttpGet("{id:int}/preview")]
        public IActionResult PreviewFile(int id)
        {
            Book? book = _context.Books.FindById(id);

            if (book == null)
            {
                return NotFound("Book not found.");
            }

            string? previewFileId = book.PreviewFileId;

            if (string.IsNullOrWhiteSpace(previewFileId)
                && IsPdf(book.OriginalFileName, book.ContentType))
            {
                previewFileId = book.FileId;
            }

            if (string.IsNullOrWhiteSpace(previewFileId))
            {
                return NotFound(book.PreviewError ?? "PDF preview is not ready for this file.");
            }

            var fileInfo = _context.FileStorage.FindById(previewFileId);

            if (fileInfo == null)
            {
                return NotFound("Preview file not found in LiteDB FileStorage.");
            }

            MemoryStream memoryStream = new();
            fileInfo.CopyTo(memoryStream);
            memoryStream.Position = 0;

            string fileName = book.PreviewFileName
                ?? $"{Path.GetFileNameWithoutExtension(book.OriginalFileName ?? "preview")}.pdf";

            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";

            return File(memoryStream, "application/pdf");
        }

        private static string? ValidateBookRequest(CreateBookRequest request)
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

        private AppUser? GetCurrentUser()
        {
            int? userId = GetCurrentUserId();
            return userId is null ? null : _context.Users.FindById(userId.Value);
        }

        private int? GetCurrentUserId()
        {
            string? userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdText, out int userId) ? userId : null;
        }

        private static Book NormalizeRentalData(Book book)
        {
            book.Rentals ??= new List<BookRental>();

            if (book.Rentals.Count == 0 && book.RentedByUserId.HasValue)
            {
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

            if (book.IsDigital)
            {
                book.TotalCopies = 0;
                book.AvailableCopies = 0;
                book.IsAvailable = true;
            }
            else
            {
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

            UpdateLegacyRentalFields(book);
            return book;
        }

        private static int CountOccupiedCopies(Book book)
        {
            return book.Rentals.Count(item => IsActiveRental(item) || IsReturnPendingRental(item));
        }

        private static bool HasActiveOrPendingRentalForUser(Book book, int userId)
        {
            return book.Rentals.Any(item => item.UserId == userId
                && (IsActiveRental(item) || IsReturnPendingRental(item)));
        }

        private static bool IsActiveRental(BookRental rental)
        {
            return string.Equals(rental.Status, RentalStatuses.Active, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReturnPendingRental(BookRental rental)
        {
            return string.Equals(rental.Status, RentalStatuses.ReturnPending, StringComparison.OrdinalIgnoreCase)
                || string.Equals(rental.Status, "PendingReturn", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rental.Status, "ReturnRequested", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rental.Status, "Pending", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReturnedRental(BookRental rental)
        {
            return string.Equals(rental.Status, RentalStatuses.Returned, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetNextRentalId(Book book)
        {
            return book.Rentals.Count == 0 ? 1 : book.Rentals.Max(item => item.RentalId) + 1;
        }

        private static void UpdateLegacyRentalFields(Book book)
        {
            BookRental? firstActiveRental = book.Rentals
                .Where(item => IsActiveRental(item) || IsReturnPendingRental(item))
                .OrderBy(item => item.RentedAt)
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

        private void DeleteStoredFiles(Book book)
        {
            if (!string.IsNullOrWhiteSpace(book.FileId))
            {
                _context.FileStorage.Delete(book.FileId);
            }

            if (!string.IsNullOrWhiteSpace(book.PreviewFileId)
                && book.PreviewFileId != book.FileId)
            {
                _context.FileStorage.Delete(book.PreviewFileId);
            }
        }

        private static bool IsPdf(string? fileName, string? contentType)
        {
            return string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
                || fileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static string ResolveContentType(string extension, string? browserContentType)
        {
            if (!string.IsNullOrWhiteSpace(browserContentType)
                && browserContentType != "application/octet-stream")
            {
                return browserContentType;
            }

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".rtf" => "application/rtf",
                ".odt" => "application/vnd.oasis.opendocument.text",
                _ => "application/octet-stream"
            };
        }
    }
}
