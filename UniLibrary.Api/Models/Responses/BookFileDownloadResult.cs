namespace UniLibrary.Api.Models.Responses;

public class BookFileDownloadResult
{
    public MemoryStream Stream { get; set; } = new();
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = "downloaded-file";
    public bool Inline { get; set; }
}
