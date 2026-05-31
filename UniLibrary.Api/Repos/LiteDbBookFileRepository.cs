using UniLibrary.Api.Data;
using UniLibrary.Api.Interfaces;

namespace UniLibrary.Api.Repos;

public class LiteDbBookFileRepository : IBookFileRepository
{
    private readonly LiteDbContext _context;

    public LiteDbBookFileRepository(LiteDbContext context)
    {
        _context = context;
    }

    public void Upload(string fileId, string fileName, Stream stream)
    {
        _context.FileStorage.Upload(fileId, fileName, stream);
    }

    public MemoryStream? DownloadToMemory(string fileId)
    {
        var fileInfo = _context.FileStorage.FindById(fileId);

        if (fileInfo is null)
        {
            return null;
        }

        MemoryStream memoryStream = new();
        fileInfo.CopyTo(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public void Delete(string? fileId)
    {
        if (!string.IsNullOrWhiteSpace(fileId))
        {
            _context.FileStorage.Delete(fileId);
        }
    }
}
