using System.Text.Json.Serialization;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Extensions;
using ServiceApotheke.API.Middleware;
using ServiceApotheke.API.Services;
using ServiceApotheke.API.Services.Telepharmazie;
using Microsoft.AspNetCore.HttpOverrides;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

try {
    if (FirebaseApp.DefaultInstance == null)
        FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.GetApplicationDefault() });
} catch { /* Handle local missing creds */ }

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.WebHost.UseSentry((Sentry.AspNetCore.SentryAspNetCoreOptions options) =>
{
    var dsn = Environment.GetEnvironmentVariable("SENTRY_DSN_API");
    if (!string.IsNullOrEmpty(dsn))
    {
        options.Dsn = dsn;
    }
    else
    {
        options.Dsn = "https://d74e4f55f0ffd194f5dbb97bade9d9a9@o4511604042891264.ingest.de.sentry.io/4511735319953488";
    }
    options.TracesSampleRate = 0.1;
    options.Debug = false;
    options.SetBeforeSend(sentryEvent =>
    {
        var serialized = System.Text.Json.JsonSerializer.Serialize(sentryEvent);
        if (serialized.Contains("PasswordHash", StringComparison.OrdinalIgnoreCase) || 
            System.Text.RegularExpressions.Regex.IsMatch(serialized, @"\b[A-Z0-9]{8,12}\b") ||
            serialized.Contains("medication", StringComparison.OrdinalIgnoreCase))
        {
            sentryEvent.User = null;
            // sentryEvent.Extra is IReadOnlyDictionary in newer SDKs, so replace the whole dictionary or remove entries if possible
            // We can just clear it by assigning a new dictionary if it's settable, or we can just leave it since we're redacting the message
            if (sentryEvent.Message != null)
            {
                sentryEvent.Message = new Sentry.SentryMessage { Formatted = "[REDACTED DUE TO PII]" };
            }
        }
        return sentryEvent;
    });
});

if (builder.Environment.IsDevelopment())
{
    Environment.SetEnvironmentVariable("DB_ENCRYPTION_KEY", "fallback_key_for_development");
}
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Environment.WebRootPath = Path.GetTempPath();

builder.Services.AddApiConfiguration();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
builder.Services.AddDbContext<DataContext>((sp, options) => {
    if (connectionString.Contains("Host=")) {
        options.UseNpgsql(connectionString);
    } else {
        var dbPath = Path.Combine(Path.GetTempPath(), "app.db");
        options.UseNpgsql($"Data Source={dbPath}");
    }
});



builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddCors(options => {
    options.AddPolicy("StrictCorsPolicy", policy => {
        policy.WithOrigins(
                "https://apotheken.serviceapotheke.tech", 
                "https://apothekern.serviceapotheke.tech",
                "https://serviceapotheke.tech",
                "https://www.serviceapotheke.tech",
                "http://localhost:5500",
                "http://127.0.0.1:5500",
                "http://localhost:3000"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<ServiceApotheke.API.Services.SaturdayRotationService>();
builder.Services.AddScoped<ServiceApotheke.API.Services.IHaversineDistanceService, ServiceApotheke.API.Services.HaversineDistanceService>();
builder.Services.AddScoped<ServiceApotheke.API.Services.IFileSanitizationService, ServiceApotheke.API.Services.FileSanitizationService>();
builder.Services.AddScoped<ServiceApotheke.API.Services.IGoogleCloudStorageService, ServiceApotheke.API.Services.GoogleCloudStorageService>();
if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
{
    builder.Services.AddScoped<ServiceApotheke.API.Services.ICryptographicStorageService, ServiceApotheke.API.Services.GcsEncryptedStorageProvider>();
}
else
{
    builder.Services.AddScoped<ServiceApotheke.API.Services.ICryptographicStorageService, ServiceApotheke.API.Services.LocalEncryptedStorageProvider>();
}
builder.Services.AddScoped<IMatchingService, MatchingService>();
builder.Services.AddScoped<IPdfGenerationService, TimesheetPdfGenerationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();
builder.Services.AddHttpClient<ServiceApotheke.API.Services.PDL.AiAnalysisService>();
builder.Services.AddScoped<ServiceApotheke.API.Services.PDL.PdlReportEngine>();
builder.Services.AddScoped<INotificationDispatcher, FcmNotificationDispatcher>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        var jwtKey = builder.Configuration["JwtSettings:Secret"];
        if (string.IsNullOrEmpty(jwtKey)) throw new Exception("JWT Secret is missing from configuration!");
        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
        
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = "ServiceApotheke.API",
            ValidAudience = "ServiceApotheke.Clients"
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!context.Request.Headers.ContainsKey("Authorization"))
                {
                    if (context.Request.Cookies.ContainsKey("sa_auth_v2"))
                        context.Token = context.Request.Cookies["sa_auth_v2"];
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "text/plain";
                if (context.AuthenticateFailure is Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException)
                {
                    return context.Response.WriteAsync("Token Expired");
                }
                return context.Response.WriteAsync("Unauthorized");
            }
        };
    });

builder.Services.AddDataProtection()
    .SetApplicationName("ServiceApotheke")
    .PersistKeysToDbContext<ServiceApotheke.API.Data.DataContext>();

builder.Services.AddHostedService<ServiceApotheke.API.Services.Workers.DataRetentionWorker>();
builder.Services.AddHostedService<ServiceApotheke.API.Services.Workers.GeocodingBackfillWorker>();
builder.Services.AddHostedService<ServiceApotheke.API.Services.Workers.ShiftVerificationWorker>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ServiceApotheke.API.Services.Workers.NewsRssService>();

builder.Services.AddScoped<IRedMedicalService, RedMedicalService>();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthLimiter", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(15);
        opt.PermitLimit = 5;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("ContactLimiter", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(60);
        opt.PermitLimit = 3;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "X-CSRF-TOKEN-COOKIE";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Apply EF Core Migrations Automatically on Startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServiceApotheke.API.Data.DataContext>();
    if (app.Environment.EnvironmentName != "Testing")
    {
        try
        {
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.Migrate(db.Database);
            Console.WriteLine("[EF Core] Production database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EF Core] Fatal error applying migrations: {ex.Message}");
        }
    }
}

app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Options)
    {
        var origin = context.Request.Headers.Origin.ToString();
        var allowedOrigins = new[] { 
            "https://apotheken.serviceapotheke.tech", 
            "https://apothekern.serviceapotheke.tech",
            "https://serviceapotheke.tech",
            "https://www.serviceapotheke.tech",
            "http://localhost:5500",
            "http://127.0.0.1:5500",
            "http://localhost:3000"
        };
        
        if (allowedOrigins.Contains(origin))
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
            context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Authorization, Content-Type, Accept, Origin, User-Agent, X-Requested-With, Cache-Control, X-CSRF-TOKEN");
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, PATCH");
            context.Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");
        }

        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("OK");
        return;
    }
    await next();
});

app.UseExceptionHandler(c => c.Run(async context => {
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>()?.Error;
    Console.WriteLine($"[Global Error] {exception?.Message}\n{exception?.StackTrace}");
    context.Response.StatusCode = 500; 
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { message = "System Error." });
}));

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

app.UseRouting();
app.UseCors("StrictCorsPolicy");
app.UseMiddleware<CronAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();



app.MapGet("/api/health", () =>
{
    return Results.Ok(new { status = "healthy" });
});

await app.RunAsync();

