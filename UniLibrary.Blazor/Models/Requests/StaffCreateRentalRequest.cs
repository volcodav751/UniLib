namespace UniLibrary.Blazor.Models.Requests;

public class StaffCreateRentalRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ReaderGroup { get; set; }
    public DateTime? DueAt { get; set; }
    public string? Note { get; set; }
}
