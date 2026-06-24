using DigiDocumentManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigiDocumentManagement.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _docs;

    public DocumentsController(DocumentService docs) => _docs = docs;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string owner)
    {
        if (string.IsNullOrWhiteSpace(owner)) return BadRequest("owner is required");
        var items = await _docs.ListAsync(owner);
        return Ok(items.Select(Map));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var doc = await _docs.GetAsync(id);
        return doc is null ? NotFound() : Ok(Map(doc));
    }

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload(
        [FromForm] string owner, [FromForm] string docType,
        [FromForm] string filename, IFormFile file)
    {
        if (!DocumentService.IsValidType(docType))
            return UnprocessableEntity($"Unsupported docType '{docType}'.");
        await using var stream = file.OpenReadStream();
        var doc = await _docs.UploadAsync(owner, docType, filename, stream, file.Length);
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
