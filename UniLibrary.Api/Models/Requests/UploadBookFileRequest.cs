using Microsoft.AspNetCore.Http;

namespace UniLibrary.Api.Models.Requests
{
    public class UploadBookFileRequest
    {
        public IFormFile File { get; set; } = default!;
    }
}