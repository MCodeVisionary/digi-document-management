using DigiDocumentManagement.Data;
using DigiDocumentManagement.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Db")));
builder.Services.AddSingleton<IStorageService, LocalStorageService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ensure database + storage exist
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();

// exposed so the functional test project can reference the entrypoint
public partial class Program { }
