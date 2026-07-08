using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PharmaDocs.Api.Configuration;
using PharmaDocs.Api.Data;
using PharmaDocs.Api.Middleware;
using PharmaDocs.Api.Repositories;
using PharmaDocs.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Databank (EF Core + PostgreSQL) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection ontbreekt.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector())); // pgvector-typemapping (RAG)

// --- JWT-instellingen (Key komt uit user-secrets / env, niet uit Git) ---
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Sectie 'Jwt' ontbreekt in de configuratie.");

// Startup-guard: weiger te starten met een ontbrekende of te korte sleutel.
if (string.IsNullOrWhiteSpace(jwtSettings.Key) || Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
    throw new InvalidOperationException(
        "Jwt:Key ontbreekt of is te kort (min. 32 bytes). Zet hem via user-secrets of een omgevingsvariabele.");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Behoud de originele claim-namen (sub, email) i.p.v. ze te hermappen.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromSeconds(30),
            // De rol zit in de "role"-claim (niet de lange URI); zo werkt [Authorize(Roles = ...)].
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

// --- Rate limiting (M1): remt brute-force op auth en kostenmisbruik op de AI-endpoints ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Auth (login/registratie): per client-IP — vertraagt brute-force.
    options.AddPolicy("auth", http =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientIp(http),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

    // AI (duur: Claude/Voyage): per ingelogde gebruiker, anders per IP.
    options.AddPolicy("ai", http =>
        RateLimitPartition.GetFixedWindowLimiter(
            UserOrIp(http),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));

    // Consistente 429 in hetzelfde JSON-formaat als de rest van de API.
    options.OnRejected = async (ctx, ct) =>
    {
        if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            ctx.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            status = StatusCodes.Status429TooManyRequests,
            detail = "Te veel aanvragen. Probeer het straks opnieuw."
        }, ct);
    };
});

// Achter de nginx/Container Apps-ingress: lees de echte client-IP uit X-Forwarded-For,
// zodat de per-IP-limiet per bezoeker telt i.p.v. per proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Er zit precies één ingress-proxy voor de container: vertrouw enkel de laatste
    // hop. Zonder deze limiet zou een client een eigen X-Forwarded-For kunnen
    // meesturen en zo zijn per-IP-rate-limit-partitie (o.a. de brute-force-rem op
    // login) omzeilen.
    options.ForwardLimit = 1;
    // De ingress is het enige pad naar de container; proxy-IP's zijn dynamisch.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// --- AI-service (interne Python-microservice) ---
var aiSettings = builder.Configuration.GetSection(AiServiceSettings.SectionName).Get<AiServiceSettings>()
    ?? throw new InvalidOperationException("Sectie 'AiService' ontbreekt in de configuratie.");

if (string.IsNullOrWhiteSpace(aiSettings.BaseUrl))
    throw new InvalidOperationException("AiService:BaseUrl ontbreekt in de configuratie.");

builder.Services.Configure<AiServiceSettings>(builder.Configuration.GetSection(AiServiceSettings.SectionName));

// Typed HttpClients: de backend is de enige die de AI-service aanroept (orchestrator).
// Gedeelde config incl. het optionele interne geheim (L4, defense-in-depth).
void ConfigureAiClient(HttpClient client)
{
    client.BaseAddress = new Uri(aiSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(aiSettings.TimeoutSeconds);
    if (!string.IsNullOrWhiteSpace(aiSettings.InternalKey))
        client.DefaultRequestHeaders.Add("X-Internal-Key", aiSettings.InternalKey);
}
builder.Services.AddHttpClient<IInvoiceExtractionClient, InvoiceExtractionClient>(ConfigureAiClient);
builder.Services.AddHttpClient<IEmbeddingClient, EmbeddingClient>(ConfigureAiClient);
builder.Services.AddHttpClient<IRagAnswerClient, RagAnswerClient>(ConfigureAiClient);

// --- Dependency injection: gelaagde structuur ---
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();

// --- Web API ---
builder.Services.AddControllers();

// Modelvalidatiefouten in hetzelfde { status, detail }-formaat als de rest van de API
// (ExceptionHandlingMiddleware + de rate limiter), i.p.v. het afwijkende
// ValidationProblemDetails-formaat. Zo heeft de client één foutcontract.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var detail = context.ModelState
            .SelectMany(kvp => kvp.Value!.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
            ?? "Ongeldige invoer.";

        return new BadRequestObjectResult(new
        {
            status = StatusCodes.Status400BadRequest,
            detail,
        });
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PharmaDocs API", Version = "v1" });

    // "Authorize"-knop in Swagger zodat je met een bearer-token kan testen.
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Plak hier je JWT-token (zonder 'Bearer ').",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

var app = builder.Build();

// --- Migraties automatisch toepassen bij het opstarten (handig voor lokale demo) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Eerste admin seeden (registratie is admin-only) uit de sectie "Seed".
    DbSeeder.SeedAdmin(db, app.Configuration,
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
}

// --- HTTP-pipeline ---
// Eerst: echte client-IP/scheme uit de proxy-headers halen.
app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Enkel lokaal: in productie termineert de Container Apps-ingress TLS en spreekt
    // ze de container over HTTP aan — een redirect zou die interne call breken.
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
// Na authenticatie: de "ai"-policy partitioneert op de ingelogde gebruiker.
app.UseRateLimiter();
app.MapControllers();

app.Run();

// Partitiesleutels voor de rate limiter.
static string ClientIp(HttpContext http) =>
    http.Connection.RemoteIpAddress?.ToString() ?? "unknown";

static string UserOrIp(HttpContext http) =>
    http.User.FindFirst("sub")?.Value
    ?? http.Connection.RemoteIpAddress?.ToString()
    ?? "unknown";
