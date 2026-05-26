using Microsoft.AspNetCore.Http;
using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;
using UniLibrary.Api.Models.Responses;
using UniLibrary.Api.Repos;
using UniLibrary.Api.Services.Results;

namespace UniLibrary.Api.Services.Files;

public class BookFileService : IBookFileService
{
    private readonly IBookRepository _books;
    private readonly IBookFileRepository _files;
    private readonly PdfPreviewService _pdfPreviewService;

    public BookFileService(
        IBookRepository books,
        IBookFileRepository files,
        PdfPreviewService pdfPreviewService)
    {
        _books = books;
        _files = files;
        _pdfPreviewService = pdfPreviewService;
    }

    public async Task<ServiceResult<BookFileUploadResponse>> UploadFileAsync(int bookId, UploadBookFileRequest request)
    {
        Book? book = _books.GetById(bookId);

        if (book is null)
        {
            return ServiceResult<BookFileUploadResponse>.NotFound("Book not found.");
        }

        if (request.File == null || request.File.Length == 0)
        {
            return ServiceResult<BookFileUploadResponse>.BadRequest("File is empty.");
        }

        string originalFileName = Path.GetFileName(request.File.FileName);
        string extension = Path.GetExtension(originalFileName).ToLowerInvariant();

        if (!FileContentTypeResolver.AllowedExtensions.Contains(extension))
        {
            return ServiceResult<BookFileUploadResponse>.BadRequest("Unsupported file type. Дозволено: PDF, DOC, DOCX, TXT, RTF, ODT.");
        }

        byte[] originalBytes = await ReadUploadedFileAsync(request.File);

        DeleteStoredFiles(book);

        string storedFileName = $"{Guid.NewGuid()}{extension}";
        string fileId = $"books/{bookId}/original/{storedFileName}";

        using (MemoryStream originalStream = new(originalBytes))
        {
            _files.Upload(fileId, storedFileName, originalStream);
        }

        book.FileId = fileId;
        book.OriginalFileName = originalFileName;
        book.StoredFileName = storedFileName;
        book.ContentType = FileContentTypeResolver.Resolve(extension, request.File.ContentType);
        book.FileSizeBytes = request.File.Length;
        book.FileUploadedAt = DateTime.UtcNow;
        book.UpdatedAt = DateTime.UtcNow;

        await SavePreviewAsync(book, originalBytes, originalFileName, fileId, bookId);

        _books.Update(book);

        BookFileUploadResponse response = new()
        {
            Message = book.PreviewStatus == "Ready"
                ? "File uploaded successfully. PDF preview is ready."
                : "File uploaded successfully, but PDF preview was not created.",
            PreviewStatus = book.PreviewStatus,
            PreviewError = book.PreviewError,
            Id = book.Id,
            FileId = book.FileId,
            OriginalFileName = book.OriginalFileName,
            StoredFileName = book.StoredFileName,
            ContentType = book.ContentType,
            FileSizeBytes = book.FileSizeBytes,
            PreviewFileId = book.PreviewFileId,
            PreviewFileName = book.PreviewFileName
        };

        return ServiceResult<BookFileUploadResponse>.Ok(response);
    }

    public ServiceResult<BookFileDownloadResult> DownloadFile(int bookId)
    {
        Book? book = _books.GetById(bookId);

        if (book is null)
        {
            return ServiceResult<BookFileDownloadResult>.NotFound("Book not found.");
        }

        if (string.IsNullOrWhiteSpace(book.FileId))
        {
            return ServiceResult<BookFileDownloadResult>.NotFound("File not found for this book.");
        }

        MemoryStream? stream = _files.DownloadToMemory(book.FileId);

        if (stream is null)
        {
            return ServiceResult<BookFileDownloadResult>.NotFound("Stored file not found.");
        }

        return ServiceResult<BookFileDownloadResult>.Ok(new BookFileDownloadResult
        {
            Stream = stream,
            ContentType = book.ContentType ?? "application/octet-stream",
            FileName = book.OriginalFileName ?? "downloaded-file"
        });
    }

    public ServiceResult<BookFileDownloadResult> PreviewFile(int bookId)
    {
        Book? book = _books.GetById(bookId);

        if (book is null)
        {
            return ServiceResult<BookFileDownloadResult>.NotFound("Book not found.");
        }

        string? previewFileId = book.PreviewFileId;

        if (string.IsNullOrWhiteSpace(previewFileId)
            && FileContentTypeResolver.IsPdf(book.OriginalFileName, book.ContentType))
        {
            previewFileId = book.FileId;
        }

        if (string.IsNullOrWhiteSpace(previewFileId))
        {
            return ServiceResult<BookFileDownloadResult>.NotFound(book.PreviewError ?? "PDF preview is not ready for this file.");
        }

        MemoryStream? stream = _files.DownloadToMemory(previewFileId);

        if (stream is null)
        {
            return ServiceResult<BookFileDownloadResult>.NotFound("Preview file not found in LiteDB FileStorage.");
        }

        string fileName = book.PreviewFileName
            ?? $"{Path.GetFileNameWithoutExtension(book.OriginalFileName ?? "preview")}.pdf";

        return ServiceResult<BookFileDownloadResult>.Ok(new BookFileDownloadResult
        {
            Stream = stream,
            ContentType = "application/pdf",
            FileName = fileName,
            Inline = true
        });
    }

    public ServiceResult DeleteFile(int bookId)
    {
        Book? book = _books.GetById(bookId);

        if (book is null)
        {
            return ServiceResult.NotFound("Book not found.");
        }

        if (string.IsNullOrWhiteSpace(book.FileId))
        {
            return ServiceResult.NotFound("File not attached.");
        }

        DeleteStoredFiles(book);
        ClearFileMetadata(book);
        _books.Update(book);

        return ServiceResult.NoContent();
    }

    public void DeleteStoredFiles(Book book)
    {
        _files.Delete(book.FileId);

        if (!string.IsNullOrWhiteSpace(book.PreviewFileId)
            && book.PreviewFileId != book.FileId)
        {
            _files.Delete(book.PreviewFileId);
        }
    }

    private async Task SavePreviewAsync(Book book, byte[] originalBytes, string originalFileName, string fileId, int bookId)
    {
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
                string previewFileId = $"books/{bookId}/preview/{Guid.NewGuid()}.pdf";

                using MemoryStream previewStream = new(previewResult.PdfBytes);
                _files.Upload(previewFileId, previewFileName, previewStream);

                book.PreviewFileId = previewFileId;
                book.PreviewFileName = previewFileName;
            }

            book.PreviewContentType = "application/pdf";
            book.PreviewGeneratedAt = DateTime.UtcNow;
            book.PreviewStatus = "Ready";
            book.PreviewError = null;
            return;
        }

        book.PreviewFileId = null;
        book.PreviewFileName = null;
        book.PreviewContentType = null;
        book.PreviewGeneratedAt = null;
        book.PreviewStatus = "Failed";
        book.PreviewError = previewResult.ErrorMessage;
    }

    private static async Task<byte[]> ReadUploadedFileAsync(IFormFile file)
    {
        await using Stream uploadStream = file.OpenReadStream();
        using MemoryStream memoryStream = new();
        await uploadStream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private static void ClearFileMetadata(Book book)
    {
        book.ClearFileInfo();
        book.UpdatedAt = DateTime.UtcNow;
    }
}
