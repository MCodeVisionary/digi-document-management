using DigiDocumentManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigiDocumentManagement.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _docs;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(DocumentService docs, ILogger<DocumentsController> logger)
    {
        _docs   = docs;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string owner)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            _logger.LogWarning("List called without owner parameter");
            return BadRequest("owner is required");
        }
        _logger.LogDebug("Listing documents for owner={Owner}", owner);
        var items = await _docs.ListAsync(owner);
        _logger.LogInformation("Listed {Count} document(s) for owner={Owner}", items.Count, owner);
        return Ok(items.Select(Map));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        _logger.LogDebug("Get document id={Id}", id);
        var doc = await _docs.GetAsync(id);
        if (doc is null)
        {
            _logger.LogWarning("Document not found: id={Id}", id);
            return NotFound();
        }
        _logger.LogDebug("Found document id={Id} filename={Filename}", id, doc.Filename);
        return Ok(Map(doc));
    }

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload(
        [FromForm] string owner, [FromForm] string docType,
        [FromForm] string filename, IFormFile file)
    {
        if (!DocumentService.IsValidType(docType))
        {
            _logger.LogWarning("Upload rejected: unsupported docType={DocType} owner={Owner}", docType, owner);
            return UnprocessableEntity($"Unsupported docType '{docType}'.");
        }

        _logger.LogInformation("Uploading {Filename} ({Bytes} bytes) docType={DocType} owner={Owner}",
            filename, file.Length, docType, owner);

        await using var stream = file.OpenReadStream();
        var doc = await _docs.UploadAsync(owner, docType, filename, stream, file.Length);

        _logger.LogInformation("Upload complete: id={Id} filename={Filename} storedAt={Path}",
            doc.Id, doc.Filename, doc.StorageLocation);
        return CreatedAtAction(nameof(Get), new { id = doc.Id }, Map(doc));
    }

    private static object Map(Models.Document d) => new
    {
        id = d.Id, filename = d.Filename, doc_type = d.DocType,
        uploaded_by = d.UploadedBy,
        uploaded_at = d.UploadedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        updated_by = d.UpdatedBy, size_bytes = d.SizeBytes,
        storage_location = d.StorageLocation,
    };
}
