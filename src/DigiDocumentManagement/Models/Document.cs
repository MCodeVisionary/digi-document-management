namespace DigiDocumentManagement.Models;

/// <summary>Document metadata; the binary lives on local storage at StorageLocation.</summary>
public class Document
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Filename { get; set; } = "";
    /// <summary>image | transcript | handwritten_note | pdf</summary>
    public string DocType { get; set; } = "";
    public string Owner { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public long SizeBytes { get; set; }
    public string UploadedBy { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
