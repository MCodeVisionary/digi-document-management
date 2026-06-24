using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DigiDocumentManagement.FunctionalTests;

public class DocumentsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public DocumentsApiTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_ok()
    {
        var resp = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task List_requires_owner()
    {
        var resp = await _client.GetAsync("/api/documents");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Upload_then_list_roundtrip()
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "scan.pdf");
        content.Add(new StringContent("e2e@docportal.io"), "owner");
        content.Add(new StringContent("pdf"), "docType");
        content.Add(new StringContent("scan.pdf"), "filename");

        var up = await _client.PostAsync("/api/documents", content);
        Assert.Equal(HttpStatusCode.Created, up.StatusCode);

        var list = await _client.GetAsync("/api/documents?owner=e2e@docportal.io");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains("scan.pdf", await list.Content.ReadAsStringAsync());
    }
}
