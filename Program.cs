using System.Text;
using GridAcademy.Data;
using GridAcademy.Helpers;
using GridAcademy.Jobs;
using GridAcademy.Middleware;
using GridAcademy.Services;
using GridAcademy.Services.ExamContent;
using GridAcademy.Services.ExamContent.AI;
using GridAcademy.Services.ExamContent.Options;
using GridAcademy.Services.ExamContent.Scraping;
using GridAcademy.Services.ExamContent.Scraping.Options;
using GridAcademy.Services.ExamContent.Scraping.Scrapers;
using GridAcademy.Modules.AiGeneration;
using GridAcademy.Services.Marketplace;
using GridAcademy.Services.Payment;
using GridAcademy.Repositories.ExamContent;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);
var config  = builder.Configuration;

// ── Port binding ─────────────────────────────────────────────────────────────
// Railway sets ASPNETCORE_URLS=http://*:8080 automatically — we do NOT override
// it via ConfigureKestrel (that caused a Kestrel warning and potential 502s).
// For local dev, launchSettings.json handles the URL (http://localhost:5000).
var portEnv = Environment.GetEnvironmentVariable("PORT");
Console.WriteLine(string.IsNullOrEmpty(portEnv)
    ? "[Startup] No PORT env var — using launchSettings / default (localhost:5000)"
    : $"[Startup] PORT={portEnv} — Railway will set ASPNETCORE_URLS automatically");

// Increase request size limit for video uploads (2 GB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options => {
    options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(serverOptions => {
    // Only set request size limits — do NOT call ListenAnyIP/ListenLocalhost here.
    // Railway's ASPNETCORE_URLS env var handles port binding correctly.
    serverOptions.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024;
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

// Configure media URL helper (makes /uploads/... absolute for production)
GridAcademy.Helpers.MediaUrlHelper.Configure(config);

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

    // Always return JSON for 401/403 — empty body causes "unexpected end of JSON" in API clients
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async ctx =>
        {
            ctx.HandleResponse(); // suppress default empty-body 401
            ctx.Response.StatusCode  = 401;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(
                    new { success = false, message = "Session expired. Please log in again." }));
        },
        OnForbidden = async ctx =>
        {
            ctx.Response.StatusCode  = 403;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(
                    new { success = false, message = "Access denied." }));
        }
    };
});

builder.Services.AddAuthorization();

// SuperAdmin gets Admin + Instructor roles injected so all existing [Authorize(Roles = "Admin")]
// checks pass without having to update every page model.
builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation,
                            GridAcademy.Helpers.SuperAdminClaimsTransformer>();

// ═══════════════════════════════════════════════════════════════════════════
// 3. HANGFIRE — Background jobs
//    Both prod and dev use PostgreSQL storage so AI generation jobs survive
//    Railway restarts. DATABASE_PUBLIC_URL is always DNS-resolvable (no
//    private networking required).
// ═══════════════════════════════════════════════════════════════════════════
Console.WriteLine("[Hangfire] Using PostgreSQL storage.");
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;
    options.Queues      = ["default", "critical"];
});

// ═══════════════════════════════════════════════════════════════════════════
// 4. APPLICATION SERVICES
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IMasterService, MasterService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<ITestService, TestService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<JwtHelper>();

// Mathpix OCR — IHttpClientFactory required by MathpixService
builder.Services.AddHttpClient();
builder.Services.AddScoped<IMathpixService, MathpixService>();

builder.Services.Configure<AiRewriteOptions>(builder.Configuration.GetSection(AiRewriteOptions.SectionName));
builder.Services.AddHttpClient("OpenAI", (sp, http) =>
{
    var aiOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiRewriteOptions>>().Value;
    http.BaseAddress = new Uri(aiOptions.BaseUrl.TrimEnd('/'));
});

builder.Services.AddScoped<InactiveUserJob>();
builder.Services.AddScoped<EmailJob>();
builder.Services.AddScoped<ExamScrapingJob>();

// ── Exam Module ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IExamContentRepository, ExamContentRepository>();
builder.Services.AddScoped<IContentProcessingService, ContentProcessingService>();
builder.Services.AddScoped<IExamContentService, ExamContentService>();
builder.Services.AddScoped<IContentWorkflowService, ContentWorkflowService>();
builder.Services.AddScoped<IAiApiClient, OpenAiApiClient>();
builder.Services.AddScoped<IAIRewriteService, AIRewriteService>();
builder.Services.AddScoped<ScraperOrchestrator>();
builder.Services.Configure<ScrapingOptions>(builder.Configuration.GetSection(ScrapingOptions.SectionName));
builder.Services.AddHttpClient<IScraper, SscScraper>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
builder.Services.AddHttpClient<IScraper, UpscScraper>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
builder.Services.AddHttpClient<IScraper, RrbScraper>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

// ── Marketplace Module ─────────────────────────────────────────────────────
builder.Services.AddScoped<IOtpService,              OtpService>();
builder.Services.AddScoped<IRazorpayService,         RazorpayService>();
builder.Services.AddScoped<IStorefrontService,       StorefrontService>();
builder.Services.AddScoped<IOrderService,            OrderService>();
builder.Services.AddScoped<IStudentService,          StudentService>();
builder.Services.AddScoped<IProviderService,         ProviderService>();
builder.Services.AddScoped<IMarketplaceAdminService, MarketplaceAdminService>();
builder.Services.AddScoped<IExamPaymentService, ExamPaymentService>();
builder.Services.AddScoped<IExamOfferService, ExamOfferService>();

// ── Career Guide ───────────────────────────────────────────────────────────
builder.Services.AddScoped<GridAcademy.Services.CareerGuide.ICareerGuideService,
                            GridAcademy.Services.CareerGuide.CareerGuideService>();

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

// ── AI Question Generation Module ──────────────────────────────────────────
builder.Services.AddAiGenerationModule(builder.Configuration);

// ═══════════════════════════════════════════════════════════════════════════
// 5. CONTROLLERS, RAZOR PAGES & API
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddControllers();
builder.Services.AddRazorPages()        // Admin panel + student portal server-rendered pages
    .AddRazorPagesOptions(o =>
    {
        // Clean URL aliases for student-facing pages
        // /attempt/{AttemptId} → /Student/Assessment/Take
        o.Conventions.AddPageRoute("/Student/Assessment/Take", "attempt/{AttemptId:guid}");
        // /instructions/{assignmentId} → /Student/Assessment/Instructions
        o.Conventions.AddPageRoute("/Student/Assessment/Instructions", "instructions/{assignmentId:guid}");
    });
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

    c.OperationFilter<SwaggerAuthorizeResponsesOperationFilter>();
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

        // ── Reset orphaned Running jobs ──────────────────────────────────────
        // If the server was restarted (Railway redeploy) while a job was running,
        // generation_jobs.status stays "Running" forever because the process was
        // killed before RunJobAsync could write the final status.
        // On every startup, flip those orphaned Running jobs to Failed so the
        // user can see them and re-queue if needed.
        try
        {
            var orphaned = await db.GenerationJobs
                .Where(j => j.Status == GridAcademy.Modules.AiGeneration.Domain.Entities.GenerationJobStatus.Running)
                .ToListAsync();
            if (orphaned.Count > 0)
            {
                foreach (var j in orphaned)
                {
                    j.Status       = GridAcademy.Modules.AiGeneration.Domain.Entities.GenerationJobStatus.Failed;
                    j.ErrorMessage = "Job interrupted — server restarted while this job was running. " +
                                     "Any questions generated before the restart are in the Review queue. " +
                                     "Re-queue this job to generate the remainder.";
                    j.CompletedAt  = j.CompletedAt ?? DateTime.UtcNow;
                }
                await db.SaveChangesAsync();
                Console.WriteLine($"[Migration] Reset {orphaned.Count} orphaned Running job(s) to Failed.");
            }
        }
        catch (Exception ex)
        {
            // Non-fatal — orphaned jobs are cosmetic, don't crash startup.
            Console.Error.WriteLine($"[Migration] Orphan-cleanup skipped: {ex.Message}");
        }
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

app.UseStaticFiles();   // Serve wwwroot (admin.css, samples, etc.)

// Serve uploaded files — supports both wwwroot/uploads (local dev) AND
// a Railway persistent volume mounted at /app/uploads (production).
// Set UPLOADS_PATH env var in Railway to /app/uploads and add a Volume there.
{
    var uploadsEnv  = Environment.GetEnvironmentVariable("UPLOADS_PATH");
    var uploadsRoot = !string.IsNullOrEmpty(uploadsEnv)
        ? uploadsEnv
        : Path.Combine(builder.Environment.WebRootPath, "uploads");

    Directory.CreateDirectory(uploadsRoot);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsRoot),
        RequestPath  = "/uploads",
        OnPrepareResponse = ctx =>
        {
            // Cache images for 7 days in browser / CDN
            ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=604800";
        }
    });

    Console.WriteLine($"[Uploads] Serving from: {uploadsRoot}");
}

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
var scrapeIntervalHours = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ScrapingOptions>>().Value.IntervalHours;
JobScheduler.RegisterAll(scrapeIntervalHours);
Console.WriteLine("[Hangfire] Dashboard and jobs registered.");

app.MapControllers();
app.MapRazorPages();    // Admin panel routes

// "/" is served by Pages/Index.cshtml (public home page)
// Admin panel is at /Admin

app.Run();
