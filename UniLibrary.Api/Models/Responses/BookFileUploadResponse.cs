namespace UniLibrary.Api.Models.Responses;

public class BookFileUploadResponse
{
    public string Message { get; set; } = string.Empty;
    public string? PreviewStatus { get; set; }
    public string? PreviewError { get; set; }
    public int Id { get; set; }
    public string? FileId { get; set; }
    public string? OriginalFileName { get; set; }
    public string? StoredFileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? PreviewFileId { get; set; }
    public string? PreviewFileName { get; set; }
}
