using Microsoft.Extensions.Logging;

namespace DigiDocumentManagement.Services;

public class LocalStorageService : IStorageService
{
    private readonly string _root;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IConfiguration config, ILogger<LocalStorageService> logger)
    {
        _logger = logger;
        _root   = config["Storage:RootPath"] ?? "/data/documents";
        Directory.CreateDirectory(_root);
        _logger.LogInformation("Storage root: {Root}", _root);
    }

    public async Task<string> SaveAsync(string documentId, string filename, Stream content)
    {
        var dir  = Path.Combine(_root, documentId);
        var path = Path.Combine(dir, Path.GetFileName(filename));
        _logger.LogDebug("Saving document {Id} → {Path}", documentId, path);

        try
        {
            Directory.CreateDirectory(dir);
            await using var fs = File.Create(path);
            await content.CopyToAsync(fs);
            _logger.LogDebug("Saved {Path} ({Bytes} bytes)", path, fs.Length);
            return path;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error writing {Path}", path);
            throw;
        }
    }

    public Task<Stream> OpenAsync(string storageLocation)
    {
        _logger.LogDebug("Opening {Path}", storageLocation);
        if (!File.Exists(storageLocation))
        {
            _logger.LogError("File not found at storage location: {Path}", storageLocation);
            throw new FileNotFoundException("Stored document not found.", storageLocation);
        }
        return Task.FromResult<Stream>(File.OpenRead(storageLocation));
    }

    public bool Exists(string storageLocation) => File.Exists(storageLocation);
}
