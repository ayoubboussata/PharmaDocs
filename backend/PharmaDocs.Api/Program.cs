using Microsoft.EntityFrameworkCore;
using PharmaDocs.Api.Data;
using PharmaDocs.Api.Repositories;
using PharmaDocs.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Databank (EF Core + PostgreSQL) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection ontbreekt.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- Dependency injection: gelaagde structuur ---
// Controller → Service → Repository → DbContext
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// --- Web API ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "PharmaDocs API", Version = "v1" });
});

var app = builder.Build();

// --- Migraties automatisch toepassen bij het opstarten (handig voor lokale demo) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// --- HTTP-pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
