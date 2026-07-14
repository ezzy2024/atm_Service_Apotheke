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
    options.Debug = true;
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
        options.UseSqlite($"Data Source={dbPath}");
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
builder.Services.AddScoped<ServiceApotheke.API.Services.ICryptographicStorageService, ServiceApotheke.API.Services.LocalEncryptedStorageProvider>();
builder.Services.AddScoped<IMatchingService, MatchingService>();

builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();
builder.Services.AddHttpClient<ServiceApotheke.API.Services.PDL.AiAnalysisService>();
builder.Services.AddScoped<ServiceApotheke.API.Services.PDL.PdlReportEngine>();
builder.Services.AddScoped<INotificationDispatcher, FcmNotificationDispatcher>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("EIN_LANGER_GEHEIMER_SCHLUESSEL_MIT_MINDESTENS_32_ZEICHEN")),
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
            }
        };
    });

builder.Services.AddDataProtection()
    .SetApplicationName("ServiceApotheke")
    .PersistKeysToFileSystem(new DirectoryInfo(@"/tmp/keys"));

builder.Services.AddHostedService<ServiceApotheke.API.Services.Workers.DataRetentionWorker>();
builder.Services.AddHostedService<ServiceApotheke.API.Services.Workers.GeocodingBackfillWorker>();
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

// Apply EF Core Migrations Automatically on Startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServiceApotheke.API.Data.DataContext>();
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
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Authorization, Content-Type, Accept, Origin, User-Agent, X-Requested-With, Cache-Control");
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

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseRouting();
app.UseCors("StrictCorsPolicy");
app.UseMiddleware<CronAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
await Task.Delay(3000); // Wait for Cloud SQL proxy

    db.Database.EnsureCreated();
    if (db.Database.IsRelational())
    {
        try 
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""BillingModel"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""CountryOfLicense"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""IsVatRequired"" boolean DEFAULT false;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""TravelCostModel"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""Street"" text DEFAULT '';
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""HouseNumber"" text DEFAULT '';
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""PostalCode"" text DEFAULT '';
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""City"" text DEFAULT '';
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""ApprobationDocumentPath"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""CvDocumentPath"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""ProfilePicturePath"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""ContractTermsAccepted"" boolean DEFAULT false;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""ContractTermsAcceptedAt"" timestamp with time zone;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""TaxId"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""TradeRegisterNumber"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""UstIdValidationStatus"" text DEFAULT 'Pending';
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""HourlyRate"" numeric DEFAULT 0;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""IsKycVerified"" boolean DEFAULT false;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""IdCardDocumentPath"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""LiabilityInsuranceDocumentPath"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""GdprAnonymizedAt"" timestamp with time zone;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""TermsAcceptedAt"" timestamp with time zone;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""TravelWillingness"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""AvailabilityType"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""ShortNoticeAvailability"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""EmergencyServiceWillingness"" boolean DEFAULT false;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""WeekendWillingness"" boolean DEFAULT false;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""FeeModel"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""Mobility"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""PreferredContactMethod"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""PreferredStates"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""SoftwareExperience"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""Specialties"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""TravelExpenses"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""VatSubject"" text;
                ALTER TABLE ""Pharmacists"" DROP COLUMN IF EXISTS ""Address"";
            ");
            Console.WriteLine("Successfully added missing Pharmacists columns manually.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Raw SQL Migration failed: {ex.Message}");
        }

        try 
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE \"JobPosts\" ADD COLUMN \"Description\" text;");
            Console.WriteLine("Successfully added Description column to JobPosts.");
        } 
        catch (Exception ex) 
        {
            Console.WriteLine($"Column might already exist or error: {ex.Message}");
        }

        try 
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE \"JobPosts\" ADD COLUMN \"RequiredQualifications\" text DEFAULT '';");
            Console.WriteLine("Successfully added RequiredQualifications column to JobPosts.");
        } 
        catch (Exception ex) 
        {
            Console.WriteLine(ex.Message);
        }

        try 
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE \"JobPosts\" ADD COLUMN IF NOT EXISTS \"ShiftDetails\" text;");
            Console.WriteLine("Successfully added ShiftDetails column to JobPosts.");
        } 
        catch (Exception ex) 
        {
            Console.WriteLine(ex.Message);
        }

        try 
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE \"JobPosts\" ADD COLUMN \"Title\" text DEFAULT '';");
            Console.WriteLine("Successfully added Title column to JobPosts.");
        } 
        catch (Exception ex) 
        {
            Console.WriteLine($"Column might already exist or error: {ex.Message}");
        }

        try 
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE \"JobPosts\" ADD COLUMN \"RequiredWws\" text;");
            Console.WriteLine("Successfully added RequiredWws column to JobPosts.");
        } 
        catch (Exception ex) 
        {
            Console.WriteLine($"Column might already exist or error: {ex.Message}");
        }

        try 
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE \"JobPosts\" ADD COLUMN \"ReasonForVacancy\" text;");
            Console.WriteLine("Successfully added ReasonForVacancy column to JobPosts.");
        } 
        catch (Exception ex) 
        {
            Console.WriteLine($"Column might already exist or error: {ex.Message}");
        }

        try 
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE \"Pharmacists\" ADD COLUMN \"Qualification\" text DEFAULT 'Approbation';");
            Console.WriteLine("Successfully added Qualification column to Pharmacists.");
        } 
        catch (Exception ex) 
        {
            Console.WriteLine($"Column might already exist or error: {ex.Message}");
        }

        try 
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE \"Pharmacists\" ADD COLUMN \"WwsProficiency\" text DEFAULT '';");
            Console.WriteLine("Successfully added WwsProficiency column to Pharmacists.");
        } 
        catch (Exception ex) 
        {
            Console.WriteLine($"Column might already exist or error: {ex.Message}");
        }

        try 
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""JobPosts"" 
                DROP COLUMN IF EXISTS ""Accommodation"",
                DROP COLUMN IF EXISTS ""BillingByInvoice"",
                DROP COLUMN IF EXISTS ""EndTime"",
                DROP COLUMN IF EXISTS ""FocusAreas"",
                DROP COLUMN IF EXISTS ""Notes"",
                DROP COLUMN IF EXISTS ""ParkingAvailable"",
                DROP COLUMN IF EXISTS ""RequestType"",
                DROP COLUMN IF EXISTS ""Urgency"",
                DROP COLUMN IF EXISTS ""StartTime"",
                DROP COLUMN IF EXISTS ""SoftwareSystem"";
            ");
            Console.WriteLine("Successfully dropped obsolete columns from JobPosts.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error dropping obsolete columns: {ex.Message}");
        }

        try 
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""JobPosts""
                ALTER COLUMN ""StartDate"" TYPE timestamp with time zone USING ""StartDate""::timestamp with time zone,
                ALTER COLUMN ""EndDate"" TYPE timestamp with time zone USING ""EndDate""::timestamp with time zone,
                ALTER COLUMN ""CreatedAt"" TYPE timestamp with time zone USING ""CreatedAt""::timestamp with time zone;
            ");
            Console.WriteLine("Successfully altered datetime columns to timestamp.");
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"Error altering datetime columns: {ex.Message}");
        }

        try 
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE \"Pharmacies\" ADD COLUMN \"ApiKey\" character varying(100);");
            Console.WriteLine("Successfully added ApiKey column to Pharmacies.");
        } 
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        try 
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""TemperatureLogs"" (
                    ""Id"" serial PRIMARY KEY,
                    ""PharmacyId"" integer NOT NULL REFERENCES ""Pharmacies"" (""Id"") ON DELETE CASCADE,
                    ""Temperature"" double precision NOT NULL,
                    ""RecordedAt"" timestamp with time zone NOT NULL,
                    ""IsAnomaly"" boolean NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_TemperatureLogs_PharmacyId"" ON ""TemperatureLogs"" (""PharmacyId"");
            ");
            Console.WriteLine("Successfully created TemperatureLogs table.");
        } 
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        try 
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""AuditLogs"" (
                    ""Id"" serial PRIMARY KEY,
                    ""EntityName"" character varying(200) NOT NULL,
                    ""EntityId"" text NOT NULL,
                    ""Action"" character varying(50) NOT NULL,
                    ""Changes"" jsonb,
                    ""Timestamp"" timestamp with time zone NOT NULL,
                    ""PerformedBy"" character varying(100)
                );
            ");
            Console.WriteLine("Successfully created AuditLogs table.");
        } 
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        try 
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""Latitude"" double precision;
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""Longitude"" double precision;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""Latitude"" double precision;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""Longitude"" double precision;
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""LicenseDocumentPath"" text;
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""SoftwareSystem"" text;
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""HouseNumber"" text DEFAULT '';
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""City"" text DEFAULT '';
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""PostalCode"" text DEFAULT '';
            ");
            Console.WriteLine("Successfully added coordinates and new pharmacy fields.");
        } 
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        try 
        {
            db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Pharmacies"" RENAME COLUMN ""Address"" TO ""Street"";");
            Console.WriteLine("Successfully renamed Address to Street.");
        } 
        catch (Exception ex) { Console.WriteLine($"Address rename failed (probably already done): {ex.Message}"); }
        
        try 
        {
            db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW();");
            Console.WriteLine("Successfully added CreatedAt column to Pharmacies.");
        } 
        catch (Exception ex) { Console.WriteLine($"CreatedAt column addition failed: {ex.Message}"); }
        
        try 
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""Notifications"" (
                    ""Id"" serial PRIMARY KEY,
                    ""UserId"" text NOT NULL,
                    ""Role"" text NOT NULL,
                    ""Title"" text NOT NULL,
                    ""Message"" text NOT NULL,
                    ""Type"" text NOT NULL,
                    ""IsRead"" boolean NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL
                );
            ");
            Console.WriteLine("Successfully created Notifications table.");
        } 
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        try 
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""PharmacyEmployees"" (
                    ""Id"" serial PRIMARY KEY,
                    ""PharmacyId"" integer NOT NULL REFERENCES ""Pharmacies"" (""Id"") ON DELETE CASCADE,
                    ""FirstName"" text NOT NULL,
                    ""LastName"" text NOT NULL,
                    ""Role"" text NOT NULL,
                    ""ColorCode"" text NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_PharmacyEmployees_PharmacyId"" ON ""PharmacyEmployees"" (""PharmacyId"");

                CREATE TABLE IF NOT EXISTS ""InternalShifts"" (
                    ""Id"" serial PRIMARY KEY,
                    ""PharmacyId"" integer NOT NULL REFERENCES ""Pharmacies"" (""Id"") ON DELETE CASCADE,
                    ""PharmacyEmployeeId"" integer NOT NULL REFERENCES ""PharmacyEmployees"" (""Id"") ON DELETE CASCADE,
                    ""Date"" timestamp with time zone NOT NULL,
                    ""StartTime"" interval NOT NULL,
                    ""EndTime"" interval NOT NULL,
                    ""IsEmergencyDuty"" boolean NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_InternalShifts_PharmacyId"" ON ""InternalShifts"" (""PharmacyId"");
                CREATE INDEX IF NOT EXISTS ""IX_InternalShifts_PharmacyEmployeeId"" ON ""InternalShifts"" (""PharmacyEmployeeId"");
            ");
            Console.WriteLine("Successfully created PharmacyEmployees and InternalShifts tables.");
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        try 
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""Consumers"" (
                    ""Id"" serial PRIMARY KEY,
                    ""Email"" character varying(256) NOT NULL,
                    ""PasswordHash"" text NOT NULL,
                    ""FirstName"" character varying(100) NOT NULL,
                    ""LastName"" character varying(100) NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""HasAcceptedBgbWaiver"" boolean NOT NULL,
                    ""BgbWaiverAcceptedAt"" timestamp with time zone
                );

                CREATE TABLE IF NOT EXISTS ""Holidays"" (
                    ""Id"" serial PRIMARY KEY,
                    ""Date"" date NOT NULL,
                    ""Name"" character varying(100) NOT NULL,
                    ""StateCode"" character varying(2) NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ""SaturdayRotationTeams"" (
                    ""Id"" serial PRIMARY KEY,
                    ""Name"" text NOT NULL,
                    ""PharmacyId"" integer NOT NULL REFERENCES ""Pharmacies"" (""Id"") ON DELETE CASCADE,
                    ""PharmacistIds"" text NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_SaturdayRotationTeams_PharmacyId"" ON ""SaturdayRotationTeams"" (""PharmacyId"");

                CREATE TABLE IF NOT EXISTS ""SaturdayRotations"" (
                    ""Id"" serial PRIMARY KEY,
                    ""Date"" date NOT NULL,
                    ""TeamId"" integer NOT NULL REFERENCES ""SaturdayRotationTeams"" (""Id"") ON DELETE CASCADE,
                    ""PharmacyId"" integer NOT NULL REFERENCES ""Pharmacies"" (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_SaturdayRotations_PharmacyId"" ON ""SaturdayRotations"" (""PharmacyId"");
                CREATE INDEX IF NOT EXISTS ""IX_SaturdayRotations_TeamId"" ON ""SaturdayRotations"" (""TeamId"");
            ");
            Console.WriteLine("Successfully created Phase 5 scheduling tables.");
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        try
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Timesheets"" ADD COLUMN IF NOT EXISTS ""DisputeReason"" text;
                ALTER TABLE ""Timesheets"" ADD COLUMN IF NOT EXISTS ""DisputedAt"" timestamp with time zone;
            ");
            Console.WriteLine("Successfully added Dispute columns to Timesheets.");
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        try 
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""UtmSource"" text;
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""UtmMedium"" text;
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""UtmCampaign"" text;
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""UtmTerm"" text;
            ");
            Console.WriteLine("Successfully added UTM columns.");
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        try 
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""AugContractDocumentPath"" text;
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""AugContractStatus"" text DEFAULT 'Pending';
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""TelepharmacyConsentDocumentPath"" text;
                ALTER TABLE ""Pharmacies"" ADD COLUMN IF NOT EXISTS ""IsTelepharmacyConsentGranted"" boolean DEFAULT false;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""AugContractDocumentPath"" text;
                ALTER TABLE ""Pharmacists"" ADD COLUMN IF NOT EXISTS ""AugContractStatus"" text DEFAULT 'Pending';
            ");
            Console.WriteLine("Successfully added Phase 8 DMS and Telepharmacy columns.");
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        try 
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Invoices"" ADD COLUMN IF NOT EXISTS ""PaidAt"" timestamp with time zone;
            ");
            Console.WriteLine("Successfully added PaidAt column to Invoices.");
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        try 
        {
            if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite") 
            {
                db.Database.ExecuteSqlRaw(@"
                    ALTER TABLE ""Pharmacists"" ALTER COLUMN ""IsApprobationVerified"" TYPE boolean USING CASE WHEN ""IsApprobationVerified""::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;
                    ALTER TABLE ""Pharmacies"" ALTER COLUMN ""IsTelepharmacyConsentGranted"" TYPE boolean USING CASE WHEN ""IsTelepharmacyConsentGranted""::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;
                    ALTER TABLE ""ConsentAgreements"" ALTER COLUMN ""IsTelepharmacyConsentGranted"" TYPE boolean USING CASE WHEN ""IsTelepharmacyConsentGranted""::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;
                    ALTER TABLE ""ConsentAgreements"" ALTER COLUMN ""IsWwsExportGranted"" TYPE boolean USING CASE WHEN ""IsWwsExportGranted""::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;
                ");
                Console.WriteLine("Successfully casted integer boolean columns to PostgreSQL boolean.");
            }
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        try 
        {
            await db.Database.MigrateAsync();
            Console.WriteLine("Successfully applied pending EF Core migrations.");
        }
        catch (Exception ex) { Console.WriteLine($"EF Migration error: {ex.Message}"); }
    }
}

app.MapGet("/api/sentry-test", () =>
{
    throw new Exception("Sentry Integration Test Error - .NET Backend");
});

await app.RunAsync();

