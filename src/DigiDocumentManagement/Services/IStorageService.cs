namespace DigiDocumentManagement.Services;

public interface IStorageService
{
    /// <summary>Persist bytes and return the absolute storage location.</summary>
    Task<string> SaveAsync(string documentId, string filename, Stream content);
    Task<Stream> OpenAsync(string storageLocation);
    bool Exists(string storageLocation);
}
