namespace UniLibrary.Api.Models;

public class BookPreviewAttachment
{
    public string? FileId { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public string? Status { get; set; }
    public string? Error { get; set; }
}
