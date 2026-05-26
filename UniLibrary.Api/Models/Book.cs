using LiteDB;

namespace UniLibrary.Api.Models;

public partial class Book
{
    [BsonId]
    public int Id { get; set; }

    public BookInfo Info { get; set; } = new();
    public BookInventory Inventory { get; set; } = new();
    public BookFileAttachment? File { get; set; }
    public BookPreviewAttachment? Preview { get; set; }

    public List<string> Tags { get; set; } = [];
    public List<BookRental> Rentals { get; set; } = [];

    public string? CoverImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
