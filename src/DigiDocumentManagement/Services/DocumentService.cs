using DigiDocumentManagement.Data;
using DigiDocumentManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace DigiDocumentManagement.Services;

public class DocumentService
{
    private static readonly HashSet<string> Allowed =
        new(StringComparer.OrdinalIgnoreCase) { "image", "transcript", "handwritten_note", "pdf" };

    private readonly AppDbContext _db;
    private readonly IStorageService _storage;

    public DocumentService(AppDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public static bool IsValidType(string docType) => Allowed.Contains(docType);

    public async Task<Document> UploadAsync(string owner, string docType, string filename,
                                            Stream content, long size)
    {
        if (!IsValidType(docType))
            throw new ArgumentException($"Unsupported docType '{docType}'.");

        var doc = new Document
        {
            Filename = filename, DocType = docType.ToLowerInvariant(),
            Owner = owner, UploadedBy = owner, SizeBytes = size,
        };
        doc.StorageLocation = await _storage.SaveAsync(doc.Id, filename, content);

        _db.Documents.Add(doc);
        _db.AuditEntries.Add(new AuditEntry { DocumentId = doc.Id, Action = "uploaded", Actor = owner });
        await _db.SaveChangesAsync();
        return doc;
    }

    public Task<List<Document>> ListAsync(string owner) =>
        _db.Documents.Where(d => d.Owner == owner)
                     .OrderByDescending(d => d.UploadedAt).ToListAsync();

    public Task<Document?> GetAsync(string id) =>
        _db.Documents.FirstOrDefaultAsync(d => d.Id == id);
}
