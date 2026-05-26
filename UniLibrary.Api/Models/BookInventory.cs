using LiteDB;

namespace UniLibrary.Api.Models;

public class BookInventory
{
    public bool IsDigital { get; set; }
    public int TotalCopies { get; set; } = 1;
    public int AvailableCopies { get; set; } = 1;

    [BsonIgnore]
    public bool IsAvailable => IsDigital || AvailableCopies > 0;
}
