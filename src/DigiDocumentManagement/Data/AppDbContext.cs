using DigiDocumentManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace DigiDocumentManagement.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
}
