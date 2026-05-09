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
            return Ok(_context.Books.FindAll().OrderBy(book => book.Title).ToList());
        }

        [HttpGet("{id:int}")]
        public ActionResult<Book> GetById(int id)
        {
            Book? book = _context.Books.FindById(id);

            if (book == null)
            {
                return NotFound();
            }

            return Ok(book);
        }

        [Authorize(Roles = UserRoles.TeacherOrAdmin)]
        [HttpPost]
        public ActionResult<Book> Create([FromBody] CreateBookRequest request)
        {
            int nextId = _context.Books.Count() == 0
                ? 1
                : _context.Books.Max(x => x.Id) + 1;

            Book book = new()
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

        [Authorize(Roles = UserRoles.TeacherOrAdmin)]
        [HttpPut("{id:int}")]
        public ActionResult<Book> Update(int id, [FromBody] CreateBookRequest request)
        {
            Book? book = _context.Books.FindById(id);

            if (book == null)
            {
                return NotFound();
            }

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
