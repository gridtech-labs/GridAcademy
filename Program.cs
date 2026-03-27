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
// Railway injects PORT at runtime. Use TryParse so an empty/missing value
// never throws — default to 8080 which matches EXPOSE in the Dockerfile.
var listenPort = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 8080;
Console.WriteLine($"[Startup] Binding to port {listenPort}");

// Increase request size limit for video uploads (2 GB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options => {
    options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(serverOptions => {
    serverOptions.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024;
    serverOptions.ListenAnyIP(listenPort);
});

// ═══════════════════════════════════════════════════════════════════════════
// 1. DATABASE — PostgreSQL via EF Core
//    Railway provides DATABASE_URL as:  postgresql://user:pass@host:port/db
//    Fall back to appsettings ConnectionStrings:DefaultConnection for local dev.
// ═══════════════════════════════════════════════════════════════════════════
static string BuildConnectionString(IConfiguration cfg)
{
    var url = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(url))
    {
        // Railway provides DATABASE_URL as: postgresql://user:pass@host:port/dbname
        // Internal Railway connections (.railway.internal) don't need SSL.
        // External connections need SSL Mode=Require.
        // Using Prefer covers both cases automatically.
        var uri      = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user     = Uri.UnescapeDataString(userInfo[0]);
        var pass     = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var db       = uri.AbsolutePath.TrimStart('/');
        var host     = uri.Host;
        var port     = uri.Port > 0 ? uri.Port : 5432;

        // For Railway internal hostnames, disable SSL; for external, prefer SSL
        var sslMode = host.EndsWith(".railway.internal") ? "Disable" : "Prefer";

        return $"Host={host};Port={port};Database={db};" +
               $"Username={user};Password={pass};" +
               $"SSL Mode={sslMode};Trust Server Certificate=true;" +
               "Pooling=true;Minimum Pool Size=1;Maximum Pool Size=20;" +
               "Connection Idle Lifetime=300;";
    }
    return cfg.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("No database connection string found.");
}

string connectionString;
try
{
    connectionString = BuildConnectionString(config);
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
// 3. HANGFIRE — Background jobs (PostgreSQL storage)
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(connectionString)));

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

// ── Migrate DB + Seed on startup ───────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbSeeder.SeedAsync(db, logger);
}

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

app.MapControllers();
app.MapRazorPages();    // Admin panel routes

// Root → admin panel (redirects to login if not authenticated)
app.MapGet("/", () => Results.Redirect("/Admin")).AllowAnonymous();

app.Run();
