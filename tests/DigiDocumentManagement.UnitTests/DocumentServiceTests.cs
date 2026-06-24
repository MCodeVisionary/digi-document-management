using DigiDocumentManagement.Data;
using DigiDocumentManagement.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Xunit;

namespace DigiDocumentManagement.UnitTests;

public class DocumentServiceTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FakeStorage : IStorageService
    {
        public Task<string> SaveAsync(string id, string filename, Stream content)
            => Task.FromResult($"/fake/{id}/{filename}");
        public Task<Stream> OpenAsync(string loc) => Task.FromResult<Stream>(new MemoryStream());
        public bool Exists(string loc) => true;
    }

    [Theory]
    [InlineData("pdf", true)]
    [InlineData("image", true)]
    [InlineData("handwritten_note", true)]
    [InlineData("transcript", true)]
    [InlineData("spreadsheet", false)]
    public void IsValidType_validates_allowed_types(string type, bool expected)
        => Assert.Equal(expected, DocumentService.IsValidType(type));

    [Fact]
    public async Task UploadAsync_persists_metadata_and_audit()
    {
        var db = NewDb();
        var svc = new DocumentService(db, new FakeStorage());
        var bytes = Encoding.UTF8.GetBytes("hello");

        var doc = await svc.UploadAsync("a@b.io", "pdf", "x.pdf", new MemoryStream(bytes), bytes.Length);

        Assert.Equal("a@b.io", doc.UploadedBy);
        Assert.Equal(5, doc.SizeBytes);
        Assert.Single(db.AuditEntries);
        Assert.Equal("uploaded", db.AuditEntries.First().Action);
    }

    [Fact]
    public async Task UploadAsync_rejects_invalid_type()
    {
        var svc = new DocumentService(NewDb(), new FakeStorage());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.UploadAsync("a@b.io", "spreadsheet", "x.xlsx", new MemoryStream(), 0));
    }

    [Fact]
    public async Task ListAsync_returns_only_owners_documents()
    {
        var db = NewDb();
        var svc = new DocumentService(db, new FakeStorage());
        await svc.UploadAsync("a@b.io", "pdf", "1.pdf", new MemoryStream(new byte[1]), 1);
        await svc.UploadAsync("c@d.io", "pdf", "2.pdf", new MemoryStream(new byte[1]), 1);

        var mine = await svc.ListAsync("a@b.io");
        Assert.Single(mine);
        Assert.Equal("1.pdf", mine[0].Filename);
    }
}
