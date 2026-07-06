using DigiDocumentManagement.Data;
using DigiDocumentManagement.Services;
using Microsoft.AspNetCore.Diagnostics;
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

app.Logger.LogInformation("Hello World from digi-document-management v0.1.16");

app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var feature = ctx.Features.Get<IExceptionHandlerFeature>();
        var logger  = ctx.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");
        logger.LogError(feature?.Error, "Unhandled exception on {Method} {Path}",
            ctx.Request.Method, ctx.Request.Path);
        ctx.Response.StatusCode  = 500;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new { error = "Internal server error" });
    });
});

// ensure database + storage exist
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();

// exposed so the functional test project can reference the entrypoint
public partial class Program { }
