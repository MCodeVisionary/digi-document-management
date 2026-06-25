using DigiDocumentManagement.Data;
using DigiDocumentManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigiDocumentManagement.Services;

public class DocumentService
{
    private static readonly HashSet<string> Allowed =
        new(StringComparer.OrdinalIgnoreCase) { "image", "transcript", "handwritten_note", "pdf" };

    private readonly AppDbContext _db;
    private readonly IStorageService _storage;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(AppDbContext db, IStorageService storage, ILogger<DocumentService> logger)
    {
        _db      = db;
        _storage = storage;
        _logger  = logger;
    }

    public static bool IsValidType(string docType) => Allowed.Contains(docType);

    public async Task<Document> UploadAsync(string owner, string docType, string filename,
                                            Stream content, long size)
    {
        if (!IsValidType(docType))
        {
            _logger.LogError("UploadAsync called with invalid docType={DocType}", docType);
            throw new ArgumentException($"Unsupported docType '{docType}'.");
        }

        var doc = new Document
        {
            Filename = filename, DocType = docType.ToLowerInvariant(),
            Owner = owner, UploadedBy = owner, SizeBytes = size,
        };
        _logger.LogDebug("Saving file to storage: id={Id} filename={Filename}", doc.Id, filename);

        try
        {
            doc.StorageLocation = await _storage.SaveAsync(doc.Id, filename, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage write failed for id={Id} filename={Filename}", doc.Id, filename);
            throw;
        }

        _db.Documents.Add(doc);
        _db.AuditEntries.Add(new AuditEntry { DocumentId = doc.Id, Action = "uploaded", Actor = owner });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database save failed for document id={Id}", doc.Id);
            throw;
        }

        _logger.LogInformation("Document saved: id={Id} owner={Owner} size={Size}", doc.Id, owner, size);
        return doc;
    }

    public async Task<List<Document>> ListAsync(string owner)
    {
        var results = await _db.Documents
            .Where(d => d.Owner == owner)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
        _logger.LogDebug("ListAsync for owner={Owner} returned {Count} records", owner, results.Count);
        return results;
    }

    public async Task<Document?> GetAsync(string id)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id);
        _logger.LogDebug("GetAsync id={Id}: {Result}", id, doc is null ? "not found" : "found");
        return doc;
    }
}
