namespace DigiDocumentManagement.Models;

public class AuditEntry
{
    public int Id { get; set; }
    public string DocumentId { get; set; } = "";
    public string Action { get; set; } = "";   // uploaded | updated | viewed
    public string Actor { get; set; } = "";
    public DateTime At { get; set; } = DateTime.UtcNow;
}
