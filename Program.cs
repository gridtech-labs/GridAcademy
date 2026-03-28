using System.Text;
using GridAcademy.Data;
using GridAcademy.Helpers;
using GridAcademy.Jobs;
using GridAcademy.Middleware;
using GridAcademy.Services;
using GridAcademy.Services.Marketplace;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var config  = builder.Configuration;

// ── Port binding ─────────────────────────────────────────────────────────────
// Railway injects PORT at runtime — override Kestrel only when PORT is set.
// In local dev PORT is NOT set, so launchSettings.json (http://localhost:5000)
// is used automatically without any override.
var portEnv = Environment.GetEnvironmentVariable("PORT");
var railwayPort = int.TryParse(portEnv, out var p) ? p : (int?)null;

if (railwayPort.HasValue)
    Console.WriteLine($"[Startup] Binding to port {railwayPort} (from PORT env var)");
else
    Console.WriteLine("[Startup] No PORT env var — using launchSettings / default (localhost:5000)");

// Increase request size limit for video uploads (2 GB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options => {
    options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(serverOptions => {
    serverOptions.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024;
    // Only override port on Railway; locally let launchSettings.json handle it
    if (railwayPort.HasValue)
        serverOptions.ListenAnyIP(railwayPort.Value);
});

// ═══════════════════════════════════════════════════════════════════════════
// 1. DATABASE — PostgreSQL via EF Core
//    Railway provides DATABASE_URL as:  postgresql://user:pass@host:port/db
//    Fall back to appsettings ConnectionStrings:DefaultConnection for local dev.
// ═══════════════════════════════════════════════════════════════════════════
static string BuildConnectionString(IConfiguration cfg, bool isProduction)
{
    // Railway provides several DB URL variables. Priority:
    //   1. DATABASE_PUBLIC_URL  – public proxy, ALWAYS DNS-resolvable (preferred for Railway)
    //   2. DATABASE_URL         – may be private (.railway.internal), fails without private networking
    //   3. PGHOST/PGPORT/...    – individual Postgres variables (Railway also provides these)
    //   4. appsettings          – local dev fallback
    var url = Environment.GetEnvironmentVariable("DATABASE_PUBLIC_URL")
           ?? Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrEmpty(url))
    {
        var uri      = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user     = Uri.UnescapeDataString(userInfo[0]);
        var pass     = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var db       = uri.AbsolutePath.TrimStart('/');
        var host     = uri.Host;
        var port     = uri.Port > 0 ? uri.Port : 5432;
        var sslMode  = host.EndsWith(".railway.internal") ? "Disable" : "Prefer";
        Console.WriteLine($"[Startup] DB host={host}:{port} ssl={sslMode}");
        return $"Host={host};Port={port};Database={db};" +
               $"Username={user};Password={pass};" +
               $"SSL Mode={sslMode};Trust Server Certificate=true;" +
               "Pooling=true;Minimum Pool Size=1;Maximum Pool Size=20;" +
               "Connection Idle Lifetime=300;";
    }

    // Fallback: individual PG* env vars (also provided by Railway Postgres plugin)
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
    var pgUser = Environment.GetEnvironmentVariable("PGUSER");
    var pgPass = Environment.GetEnvironmentVariable("PGPASSWORD");
    var pgDb   = Environment.GetEnvironmentVariable("PGDATABASE");
    if (!string.IsNullOrEmpty(pgHost) && !string.IsNullOrEmpty(pgUser))
    {
        Console.WriteLine($"[Startup] DB host={pgHost}:{pgPort} (from PG* vars)");
        return $"Host={pgHost};Port={pgPort};Database={pgDb ?? "railway"};" +
               $"Username={pgUser};Password={pgPass ?? ""};" +
               "SSL Mode=Prefer;Trust Server Certificate=true;" +
               "Pooling=true;Minimum Pool Size=1;Maximum Pool Size=20;";
    }

    // Railway hardcoded fallback: only used in Production to avoid local dev hitting the prod DB
    if (isProduction)
    {
        var railwayCs = cfg.GetConnectionString("RailwayConnection");
        if (!string.IsNullOrEmpty(railwayCs) && !railwayCs.Contains("REPLACE_WITH_PGPASSWORD"))
        {
            Console.WriteLine("[Startup] DB: using appsettings RailwayConnection (Railway public)");
            return railwayCs;
        }
    }

    // Local dev: appsettings.json DefaultConnection
    var localCs = cfg.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(localCs))
    {
        Console.WriteLine("[Startup] DB: using appsettings DefaultConnection (local dev)");
        return localCs;
    }

    throw new InvalidOperationException(
        "No DB connection found. Set DATABASE_PUBLIC_URL in Railway Variables or RailwayConnection in appsettings.json.");
}

string connectionString;
try
{
    connectionString = BuildConnectionString(config, builder.Environment.IsProduction());
    Console.WriteLine($"[Startup] DB connection string built OK (host masked).");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[Startup] FATAL: Could not build DB connection string: {ex.Message}");
    throw;
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsql => npgsql.EnableRetryOnFailure(3)
    ));

// ═══════════════════════════════════════════════════════════════════════════
// 2. AUTHENTICATION — Cookie (admin panel) + JWT Bearer (REST API)
//    Cookie is the default scheme so Razor Pages work naturally with [Authorize].
//    API controllers explicitly opt-in to Bearer via AuthenticationSchemes.
// ═══════════════════════════════════════════════════════════════════════════
// Jwt:Secret can come from env var  Jwt__Secret  (Railway Variables tab)
// If missing in production we log a clear error instead of crashing silently.
var jwtSecret = config["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    // In production this MUST be set. Fall back to a placeholder so the app
    // starts and the admin panel is accessible, but API auth will reject all tokens.
    jwtSecret = "PLACEHOLDER_SET_Jwt__Secret_IN_RAILWAY_VARIABLES_NOW";
    Console.Error.WriteLine(
        "⚠️  WARNING: Jwt:Secret is not configured. " +
        "Set the  Jwt__Secret  environment variable in Railway → Variables.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath         = "/Account/Login";
    options.LogoutPath        = "/Account/Logout";
    options.AccessDeniedPath  = "/Account/AccessDenied";
    options.ExpireTimeSpan    = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.Name       = "GridAcademy.Admin";
    options.Cookie.HttpOnly    = true;
    options.Cookie.SameSite   = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidIssuer              = config["Jwt:Issuer"],
        ValidateAudience         = true,
        ValidAudience            = config["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ═══════════════════════════════════════════════════════════════════════════
// 3. HANGFIRE — Background jobs
//    Hangfire.PostgreSql.UsePostgreSqlStorage() opens a SYNCHRONOUS connection
//    inside a DI lazy factory — the exception escapes try/catch because it fires
//    when the DI container resolves IJobStorage (inside UseHangfireDashboard).
//    On Railway, postgres.railway.internal requires Private Networking (off by
//    default). Safest solution: use InMemoryStorage in Production so the app
//    always starts. Jobs still run; they just don't persist across restarts
//    (acceptable — Railway has no persistent filesystem either).
// ═══════════════════════════════════════════════════════════════════════════
var isProduction = builder.Environment.IsProduction();
if (isProduction)
{
    // InMemory — always safe, no DB connection at startup
    Console.WriteLine("[Hangfire] Production: using InMemory storage (no DB needed at startup).");
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseInMemoryStorage());
}
else
{
    // Local dev: use PostgreSQL storage
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
}

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;
    options.Queues      = ["default", "critical"];
});

// ═══════════════════════════════════════════════════════════════════════════
// 4. APPLICATION SERVICES
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMasterService, MasterService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<ITestService, TestService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<JwtHelper>();

// Mathpix OCR — IHttpClientFactory required by MathpixService
builder.Services.AddHttpClient();
builder.Services.AddScoped<IMathpixService, MathpixService>();

builder.Services.AddScoped<InactiveUserJob>();
builder.Services.AddScoped<EmailJob>();

// ── Exam Module ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IExamService, ExamService>();

// ── Marketplace Module ─────────────────────────────────────────────────────
builder.Services.AddScoped<IOtpService,              OtpService>();
builder.Services.AddScoped<IRazorpayService,         RazorpayService>();
builder.Services.AddScoped<IStorefrontService,       StorefrontService>();
builder.Services.AddScoped<IOrderService,            OrderService>();
builder.Services.AddScoped<IStudentService,          StudentService>();
builder.Services.AddScoped<IProviderService,         ProviderService>();
builder.Services.AddScoped<IMarketplaceAdminService, MarketplaceAdminService>();

// ── Video Learning Module ──────────────────────────────────────────────────
builder.Services.Configure<GridAcademy.Data.Entities.VideoLearning.VideoLearningFeatures>(
    builder.Configuration.GetSection("VideoLearning:Features"));
builder.Services.Configure<GridAcademy.Data.Entities.VideoLearning.VideoLearningStorageOptions>(
    builder.Configuration.GetSection("VideoLearning:Storage"));

builder.Services.AddScoped<GridAcademy.Services.VideoLearning.IDomainService,        GridAcademy.Services.VideoLearning.DomainService>();
builder.Services.AddScoped<GridAcademy.Services.VideoLearning.IVideoCategoryService, GridAcademy.Services.VideoLearning.VideoCategoryService>();
builder.Services.AddScoped<GridAcademy.Services.VideoLearning.IVideoService,         GridAcademy.Services.VideoLearning.VideoService>();
builder.Services.AddScoped<GridAcademy.Services.VideoLearning.ILearningPathService,  GridAcademy.Services.VideoLearning.LearningPathService>();
builder.Services.AddScoped<GridAcademy.Services.VideoLearning.IProgramService,       GridAcademy.Services.VideoLearning.ProgramService>();
builder.Services.AddScoped<GridAcademy.Services.VideoLearning.ICouponService,        GridAcademy.Services.VideoLearning.CouponService>();
builder.Services.AddScoped<GridAcademy.Services.VideoLearning.ISalesChannelService,  GridAcademy.Services.VideoLearning.SalesChannelService>();
builder.Services.AddScoped<GridAcademy.Services.VideoLearning.IEnrollmentService,    GridAcademy.Services.VideoLearning.EnrollmentService>();
builder.Services.AddScoped<GridAcademy.Services.VideoLearning.IContentFileService, GridAcademy.Services.VideoLearning.ContentFileService>();

// ═══════════════════════════════════════════════════════════════════════════
// 5. CONTROLLERS, RAZOR PAGES & API
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddControllers();
builder.Services.AddRazorPages();       // Admin panel server-rendered pages
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAntiforgery();      // CSRF protection for admin forms

// ═══════════════════════════════════════════════════════════════════════════
// 6. SWAGGER — JWT-enabled API explorer (now at /swagger)
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "GridAcademy API",
        Version     = "v1",
        Description = "Learning Management System — Backend API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Paste your JWT token here. Example: Bearer eyJhbG..."
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

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// ═══════════════════════════════════════════════════════════════════════════
// 7. CORS
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(config.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ═══════════════════════════════════════════════════════════════════════════
// BUILD
// ═══════════════════════════════════════════════════════════════════════════
var app = builder.Build();

// ── Migrate DB + Seed — runs in background so HTTP server starts immediately ──
// Railway's health check probes GET / within ~60s. If migration blocks startup
// the health check times out and Railway reports "connection refused".
// Running migration in a background task lets Kestrel bind first.
_ = Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(3)); // let Kestrel bind
    try
    {
        using var scope = app.Services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        Console.WriteLine("[Migration] Starting DB migration in background…");
        await DbSeeder.SeedAsync(db, logger);
        Console.WriteLine("[Migration] DB migration + seed completed successfully.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[Migration] FAILED: {ex.Message}");
        Console.Error.WriteLine(ex.ToString());
    }
});

// ═══════════════════════════════════════════════════════════════════════════
// MIDDLEWARE PIPELINE  (order matters!)
// ═══════════════════════════════════════════════════════════════════════════

app.UseMiddleware<ExceptionMiddleware>();

app.UseStaticFiles();   // Serve wwwroot (admin.css, etc.)

// Swagger at /swagger (root is now the admin panel)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GridAcademy API v1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
});

// Railway terminates HTTPS at the load balancer — no app-level redirect needed
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard at /hangfire
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // In production: add HangfireAdminAuthFilter here
});
JobScheduler.RegisterAll();
Console.WriteLine("[Hangfire] Dashboard and jobs registered.");

app.MapControllers();
app.MapRazorPages();    // Admin panel routes

// Root → admin panel (redirects to login if not authenticated)
app.MapGet("/", () => Results.Redirect("/Admin")).AllowAnonymous();

app.Run();
