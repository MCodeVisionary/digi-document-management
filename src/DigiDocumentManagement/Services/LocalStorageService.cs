namespace DigiDocumentManagement.Services;

/// <summary>Stores document binaries on the local machine filesystem.</summary>
public class LocalStorageService : IStorageService
{
    private readonly string _root;

    public LocalStorageService(IConfiguration config)
    {
        _root = config["Storage:RootPath"] ?? "/data/documents";
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(string documentId, string filename, Stream content)
    {
        var dir = Path.Combine(_root, documentId);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, Path.GetFileName(filename));
        await using var fs = File.Create(path);
        await content.CopyToAsync(fs);
        return path;
    }

    public Task<Stream> OpenAsync(string storageLocation) =>
        Task.FromResult<Stream>(File.OpenRead(storageLocation));

    public bool Exists(string storageLocation) => File.Exists(storageLocation);
}
