using Microsoft.AspNetCore.Mvc;
using UniLibrary.Api.Data;
using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;

namespace UniLibrary.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly LiteDbContext _context;

        public BooksController(LiteDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<List<Book>> GetAll()
        {
            return Ok(_context.Books.FindAll().ToList());
        }

        [HttpGet("{id:int}")]
        public ActionResult<Book> GetById(int id)
        {
            var book = _context.Books.FindById(id);

            if (book == null)
                return NotFound();

            return Ok(book);
        }

        [HttpPost]
        [HttpPost]
        public ActionResult<Book> Create([FromBody] CreateBookRequest request)
        {
            var nextId = _context.Books.Count() == 0
                ? 1
                : _context.Books.Max(x => x.Id) + 1;

            var book = new Book
            {
                Id = nextId,
                Title = request.Title,
                Author = request.Author,
                Description = request.Description,
                Category = request.Category,
                Language = request.Language,
                Publisher = request.Publisher,
                PublicationYear = request.PublicationYear,
                PageCount = request.PageCount,
                Isbn = request.Isbn,
                Tags = request.Tags,
                IsAvailable = request.IsAvailable,
                IsDigital = request.IsDigital,
                CoverImageUrl = request.CoverImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Books.Insert(book);

            return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
        }

        [HttpPut("{id:int}")]
        public ActionResult<Book> Update(int id, [FromBody] CreateBookRequest request)
        {
            var book = _context.Books.FindById(id);

            if (book == null)
                return NotFound();

            book.Title = request.Title;
            book.Author = request.Author;
            book.Description = request.Description;
            book.Category = request.Category;
            book.Language = request.Language;
            book.Publisher = request.Publisher;
            book.PublicationYear = request.PublicationYear;
            book.PageCount = request.PageCount;
            book.Isbn = request.Isbn;
            book.Tags = request.Tags;
            book.IsAvailable = request.IsAvailable;
            book.IsDigital = request.IsDigital;
            book.CoverImageUrl = request.CoverImageUrl;
            book.UpdatedAt = DateTime.UtcNow;

            _context.Books.Update(book);

            return Ok(book);
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var book = _context.Books.FindById(id);

            if (book == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(book.FileId))
            {
                _context.FileStorage.Delete(book.FileId);
            }

            _context.Books.Delete(id);

            return NoContent();
        }

        [HttpPost("{id:int}/file")]
        public IActionResult UploadFile(int id, [FromForm] UploadBookFileRequest request)
        {
            var book = _context.Books.FindById(id);

            if (book == null)
                return NotFound("Book not found.");

            if (request.File == null || request.File.Length == 0)
                return BadRequest("File is empty.");

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };
            var originalFileName = Path.GetFileName(request.File.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Unsupported file type.");

            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var fileId = $"books/{id}/{storedFileName}";

            if (!string.IsNullOrWhiteSpace(book.FileId))
            {
                _context.FileStorage.Delete(book.FileId);
            }

            using (var stream = request.File.OpenReadStream())
            {
                _context.FileStorage.Upload(fileId, storedFileName, stream);
            }

            book.FileId = fileId;
            book.OriginalFileName = originalFileName;
            book.StoredFileName = storedFileName;
            book.ContentType = string.IsNullOrWhiteSpace(request.File.ContentType)
                ? "application/octet-stream"
                : request.File.ContentType;
            book.FileSizeBytes = request.File.Length;
            book.FileUploadedAt = DateTime.UtcNow;
            book.UpdatedAt = DateTime.UtcNow;

            _context.Books.Update(book);

            return Ok(new
            {
                message = "File uploaded successfully.",
                book.Id,
                book.FileId,
                book.OriginalFileName,
                book.StoredFileName,
                book.ContentType,
                book.FileSizeBytes
            });
        }

        [HttpGet("{id:int}/file")]
        public IActionResult DownloadFile(int id)
        {
            var book = _context.Books.FindById(id);

            if (book == null)
                return NotFound("Book not found.");

            if (string.IsNullOrWhiteSpace(book.FileId))
                return NotFound("File not found for this book.");

            var fileInfo = _context.FileStorage.FindById(book.FileId);

            if (fileInfo == null)
                return NotFound("Stored file not found.");

            var memoryStream = new MemoryStream();
            fileInfo.CopyTo(memoryStream);
            memoryStream.Position = 0;

            return File(
                memoryStream,
                book.ContentType ?? "application/octet-stream",
                book.OriginalFileName ?? "downloaded-file");
        }

        [HttpDelete("{id:int}/file")]
        public IActionResult DeleteFile(int id)
        {
            var book = _context.Books.FindById(id);

            if (book == null)
                return NotFound("Book not found.");

            if (string.IsNullOrWhiteSpace(book.FileId))
                return NotFound("File not attached.");

            _context.FileStorage.Delete(book.FileId);

            book.FileId = null;
            book.OriginalFileName = null;
            book.StoredFileName = null;
            book.ContentType = null;
            book.FileSizeBytes = null;
            book.FileUploadedAt = null;
            book.UpdatedAt = DateTime.UtcNow;

            _context.Books.Update(book);

            return NoContent();
        }
        [HttpGet("{id:int}/preview")]
        public IActionResult PreviewFile(int id)
        {
            var book = _context.Books.FindById(id);

            if (book == null)
                return NotFound("Book not found.");

            if (string.IsNullOrWhiteSpace(book.FileId))
                return NotFound("File not attached to this book.");

            var fileInfo = _context.FileStorage.FindById(book.FileId);

            if (fileInfo == null)
                return NotFound("File not found in LiteDB FileStorage.");

            var memoryStream = new MemoryStream();
            fileInfo.CopyTo(memoryStream);
            memoryStream.Position = 0;

            Response.Headers["Content-Disposition"] =
                $"inline; filename=\"{book.OriginalFileName ?? "preview"}\"";

            return File(memoryStream, book.ContentType, book.OriginalFileName);
        }
    }
    
}