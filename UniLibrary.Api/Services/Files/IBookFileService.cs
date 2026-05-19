using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;
using UniLibrary.Api.Models.Responses;
using UniLibrary.Api.Services.Results;

namespace UniLibrary.Api.Services.Files;

public interface IBookFileService
{
    Task<ServiceResult<BookFileUploadResponse>> UploadFileAsync(int bookId, UploadBookFileRequest request);
    ServiceResult<BookFileDownloadResult> DownloadFile(int bookId);
    ServiceResult<BookFileDownloadResult> PreviewFile(int bookId);
    ServiceResult DeleteFile(int bookId);
    void DeleteStoredFiles(Book book);
}
