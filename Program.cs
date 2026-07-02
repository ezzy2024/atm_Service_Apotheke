using System.Text.Json.Serialization;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Environment.WebRootPath = Path.GetTempPath();

builder.Services.AddApiConfiguration();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
builder.Services.AddDbContext<DataContext>((sp, options) => {
    if (connectionString.Contains("Host=")) {
        options.UseNpgsql(connectionString);
    } else {
        var dbPath = Path.Combine(Path.GetTempPath(), "app.db");
        options.UseSqlite($"Data Source={dbPath}");
    }
});



builder.Services.AddControllers().AddJsonOptions(options =>
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
builder.Services.AddScoped<IMatchingService, MatchingService>();

builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"] ?? builder.Configuration["JWT_SECRET"] ?? throw new InvalidOperationException("Missing JWT Secret"))),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidIssuer = "ServiceApotheke.API",
            ValidAudience = "ServiceApotheke.Clients"
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Only use cookies if the Authorization header is missing
                if (!context.Request.Headers.ContainsKey("Authorization"))
                {
                    if (context.Request.Cookies.ContainsKey("auth_token"))
                        context.Token = context.Request.Cookies["auth_token"];
                    else if (context.Request.Cookies.ContainsKey("sa_auth"))
                        context.Token = context.Request.Cookies["sa_auth"];
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddDataProtection()
    .SetApplicationName("ServiceApothekeAPI")
    .PersistKeysToGoogleCloudStorage("serviceapotheke-dp-keys", "keys.xml")
    .ProtectKeysWithGoogleKms("projects/830781040278/locations/europe-west3/keyRings/sa-keyring/cryptoKeys/dp-key");

builder.Services.AddHostedService<ServiceApotheke.API.Services.Workers.DataRetentionWorker>();
builder.Services.AddScoped<IRedMedicalService, RedMedicalService>();

var app = builder.Build();

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

app.UseRouting();
app.UseCors("StrictCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    if (db.Database.IsRelational())
    {
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
            Console.WriteLine($"Column might already exist or error: {ex.Message}");
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
            ");
            Console.WriteLine("Successfully added coordinates to Pharmacies and Pharmacists.");
        } 
        catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
}

await app.RunAsync();

