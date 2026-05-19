namespace UniLibrary.Api.Repos;

public interface IBookFileRepository
{
    void Upload(string fileId, string fileName, Stream stream);
    MemoryStream? DownloadToMemory(string fileId);
    void Delete(string? fileId);
}
