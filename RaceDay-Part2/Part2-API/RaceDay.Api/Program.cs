using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---- Database: SQL Server via EF Core (code-first) ----
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- Session management: keeps the user's authenticated state and role between requests ----
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "API for the RaceDay system. Authentication is session based: call POST /api/auth/login first. " +
                      "Swagger UI sends the session cookie automatically, so you can then test the protected endpoints."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Create / update the database on startup (applies EF migrations).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    if (db.Database.IsRelational() && db.Database.GetMigrations().Any()) db.Database.Migrate();
    else db.Database.EnsureCreated(); // in-memory tests, or no migration generated yet
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    c.DocumentTitle = "RaceDay API";
});

app.UseSession();          // must come before MapControllers
app.MapControllers();

app.Run();

// Makes Program visible to the test project (WebApplicationFactory<Program>).
public partial class Program { }
