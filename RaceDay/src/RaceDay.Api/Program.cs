using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Services;
using RaceDay.Api.Settings;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuration ----------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
          ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");

// ---------- Database (EF Core Code-First, SQL Server) ----------
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Application services ----------
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// ---------- Authentication & role-based authorisation ----------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization();

// ---------- Controllers & Swagger ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "REST API for the RaceDay event management system (Organisers and Participants)."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Log in via POST /api/auth/login, then paste the returned token here (no 'Bearer ' prefix needed)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ---------- Create the database (Code-First) and seed the master category list ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    c.RoutePrefix = string.Empty; // Swagger UI is served at the site root
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Required so the integration tests can reference the entry point.
public partial class Program { }
