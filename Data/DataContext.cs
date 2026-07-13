using System;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Security.Claims;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Models.ATM;
using ServiceApotheke.API.Models.PDL;

namespace ServiceApotheke.API.Data
{
    public sealed class AesEncryptionConverter : ValueConverter<string, string>
    {
        private const int NonceSize = 12; 
        private const int TagSize = 16;   

        public AesEncryptionConverter(string secretKey)
            : base(
                  model => Encrypt(model, secretKey),
                  provider => Decrypt(provider, secretKey),
                  convertsNulls: true)
        {
        }

        private static byte[] DeriveKey(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("Encryption key must be provided and non-empty.", nameof(secret));

            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(secret));
        }

        private static string Encrypt(string plainText, string secret)
        {
            if (plainText is null) return null;

            var key = DeriveKey(secret);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);

            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            var cipher = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using (var aesGcm = new AesGcm(key))
            {
                aesGcm.Encrypt(nonce, plainBytes, cipher, tag, null);
            }

            var outBytes = new byte[nonce.Length + tag.Length + cipher.Length];
            Buffer.BlockCopy(nonce, 0, outBytes, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, outBytes, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipher, 0, outBytes, nonce.Length + tag.Length, cipher.Length);

            return Convert.ToBase64String(outBytes);
        }

        private static string Decrypt(string cipherTextBase64, string secret)
        {
            if (cipherTextBase64 is null) return null;

            byte[] allBytes;
            try
            {
                allBytes = Convert.FromBase64String(cipherTextBase64);
            }
            catch
            {
                return cipherTextBase64;
            }

            if (allBytes.Length < NonceSize + TagSize)
            {
                return cipherTextBase64;
            }

            var key = DeriveKey(secret);

            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var cipher = new byte[allBytes.Length - NonceSize - TagSize];

            Buffer.BlockCopy(allBytes, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(allBytes, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(allBytes, NonceSize + TagSize, cipher, 0, cipher.Length);

            var plain = new byte[cipher.Length];
            try
            {
                using var aesGcm = new AesGcm(key);
                aesGcm.Decrypt(nonce, cipher, tag, plain, null);
                return Encoding.UTF8.GetString(plain);
            }
            catch
            {
                return cipherTextBase64;
            }
        }
    }

    public class DataContext : DbContext
    {
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public DataContext(DbContextOptions<DataContext> options, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor = null) : base(options) 
        { 
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Pharmacist> Pharmacists { get; set; }
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<Pharmacy> Pharmacies { get; set; }
        public DbSet<PharmacistFeedback> PharmacistFeedbacks { get; set; }
        public DbSet<PharmacyFeedback> PharmacyFeedbacks { get; set; }
        public DbSet<Timesheet> Timesheets { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<TemperatureLog> TemperatureLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<MobileRefreshToken> MobileRefreshTokens { get; set; }
        public DbSet<DeviceToken> DeviceTokens { get; set; }

        public DbSet<KioskTerminal> KioskTerminals { get; set; }
        public DbSet<ConsentAgreement> ConsentAgreements { get; set; }
        public DbSet<AtmBillingRecord> AtmBillingRecords { get; set; }
        public DbSet<SessionTelemetry> SessionTelemetries { get; set; }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<PdlService> PdlServices { get; set; }
        public DbSet<PdlDocument> PdlDocuments { get; set; }

        public DbSet<PharmacyEmployee> PharmacyEmployees { get; set; }
        public DbSet<InternalShift> InternalShifts { get; set; }

        public DbSet<Consumer> Consumers { get; set; }
        public DbSet<SaturdayRotation> SaturdayRotations { get; set; }
        public DbSet<SaturdayRotationTeam> SaturdayRotationTeams { get; set; }
        public DbSet<Holiday> Holidays { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var encryptionKey = Environment.GetEnvironmentVariable("DB_ENCRYPTION_KEY") 
                                ?? throw new InvalidOperationException("DB_ENCRYPTION_KEY required.");
            var converter = new AesEncryptionConverter(encryptionKey);

            modelBuilder.Entity<Pharmacist>().Property(p => p.ApprobationDocumentPath).HasConversion(converter).HasMaxLength(2048); 
            modelBuilder.Entity<JobPost>().HasOne(j => j.Pharmacy).WithMany(p => p.JobPosts).HasForeignKey(j => j.PharmacyId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<JobApplication>().HasOne(a => a.JobPost).WithMany(j => j.JobApplications).HasForeignKey(a => a.JobPostId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Pharmacist>().HasIndex(p => p.Email).IsUnique();
            modelBuilder.Entity<Pharmacy>().HasIndex(p => p.PharmacyName);
            
            if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                modelBuilder.Entity<JobPost>().UseXminAsConcurrencyToken();
            }

            // ATM Module cascade configurations
            modelBuilder.Entity<KioskTerminal>()
                .HasOne(k => k.Pharmacy)
                .WithMany()
                .HasForeignKey(k => k.PharmacyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConsentAgreement>()
                .HasOne(c => c.Pharmacy)
                .WithMany()
                .HasForeignKey(c => c.PharmacyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AtmBillingRecord>()
                .HasOne(a => a.Pharmacy)
                .WithMany()
                .HasForeignKey(a => a.PharmacyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SessionTelemetry>()
                .HasOne(s => s.Pharmacy)
                .WithMany()
                .HasForeignKey(s => s.PharmacyId)
                .OnDelete(DeleteBehavior.Cascade);

            // PDL Module cascade configurations
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Pharmacy)
                .WithMany(ph => ph.Patients)
                .HasForeignKey(p => p.PharmacyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PdlService>()
                .HasOne(s => s.Patient)
                .WithMany(p => p.PdlServices)
                .HasForeignKey(s => s.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PdlDocument>()
                .HasOne(d => d.Pharmacy)
                .WithMany(ph => ph.PdlDocuments)
                .HasForeignKey(d => d.PharmacyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PdlDocument>()
                .HasOne(d => d.Patient)
                .WithMany(p => p.PdlDocuments)
                .HasForeignKey(d => d.PatientId)
                // We use restrict here because cascade from patient AND pharmacy causes multiple cascade paths in SQL Server/EF Core, 
                // but since deleting pharmacy deletes patient, it's fine.
                .OnDelete(DeleteBehavior.Restrict);

            if (Database.IsRelational())
            {
                modelBuilder.Entity<Pharmacist>().Property(p => p.IsKycVerified).HasConversion(v => v ? 1 : 0, v => v == 1);
                modelBuilder.Entity<Pharmacist>().Property(p => p.GdprAnonymizedAt).HasConversion(v => v.HasValue ? v.Value.ToString("O") : null, v => string.IsNullOrEmpty(v) ? (DateTime?)null : DateTime.Parse(v));
                modelBuilder.Entity<Pharmacist>().Property(p => p.TermsAcceptedAt).HasConversion(v => v.HasValue ? v.Value.ToString("O") : null, v => string.IsNullOrEmpty(v) ? (DateTime?)null : DateTime.Parse(v));
                modelBuilder.Entity<Pharmacy>().Property(p => p.DataProcessingAgreementSignedAt).HasConversion(v => v.HasValue ? v.Value.ToString("O") : null, v => string.IsNullOrEmpty(v) ? (DateTime?)null : DateTime.Parse(v));
                modelBuilder.Entity<Pharmacy>().Property(p => p.GdprAnonymizedAt).HasConversion(v => v.HasValue ? v.Value.ToString("O") : null, v => string.IsNullOrEmpty(v) ? (DateTime?)null : DateTime.Parse(v));
            }

            // Seed German Federal Holidays (Static Seeding per User Directive)
            modelBuilder.Entity<Holiday>().HasData(
                new Holiday { Id = 1, Date = new DateTime(2026, 1, 1), Name = "Neujahrstag", StateCode = "DE" },
                new Holiday { Id = 2, Date = new DateTime(2026, 4, 3), Name = "Karfreitag", StateCode = "DE" }, // 2026 Karfreitag
                new Holiday { Id = 3, Date = new DateTime(2026, 4, 6), Name = "Ostermontag", StateCode = "DE" }, // 2026 Ostermontag
                new Holiday { Id = 4, Date = new DateTime(2026, 5, 1), Name = "Tag der Arbeit", StateCode = "DE" },
                new Holiday { Id = 5, Date = new DateTime(2026, 5, 14), Name = "Christi Himmelfahrt", StateCode = "DE" }, // 2026
                new Holiday { Id = 6, Date = new DateTime(2026, 5, 25), Name = "Pfingstmontag", StateCode = "DE" }, // 2026
                new Holiday { Id = 7, Date = new DateTime(2026, 10, 3), Name = "Tag der Deutschen Einheit", StateCode = "DE" },
                new Holiday { Id = 8, Date = new DateTime(2026, 12, 25), Name = "1. Weihnachtstag", StateCode = "DE" },
                new Holiday { Id = 9, Date = new DateTime(2026, 12, 26), Name = "2. Weihnachtstag", StateCode = "DE" }
            );

            base.OnModelCreating(modelBuilder);
        }

        public override async System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            var auditEntries = new System.Collections.Generic.List<AuditLog>();
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var userId = _httpContextAccessor?.HttpContext?.User?.FindFirstValue("id") ?? 
                             _httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                var performedBy = string.IsNullOrEmpty(userId) ? "SYSTEM" : userId;

                var auditLog = new AuditLog
                {
                    EntityName = entry.Metadata.Name,
                    Action = entry.State.ToString(),
                    Timestamp = DateTime.UtcNow,
                    PerformedBy = performedBy
                };

                var primaryKey = System.Linq.Enumerable.FirstOrDefault(entry.Properties, p => p.Metadata.IsPrimaryKey());
                auditLog.EntityId = primaryKey?.CurrentValue?.ToString() ?? "Unknown";

                var changes = new System.Collections.Generic.Dictionary<string, object>();
                foreach (var property in entry.Properties)
                {
                    if (property.IsTemporary) continue;

                    if (entry.State == EntityState.Added)
                    {
                        changes[property.Metadata.Name] = new { New = property.CurrentValue };
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        changes[property.Metadata.Name] = new { Old = property.OriginalValue };
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        if (property.IsModified)
                        {
                            changes[property.Metadata.Name] = new { Old = property.OriginalValue, New = property.CurrentValue };
                        }
                    }
                }

                auditLog.Changes = System.Text.Json.JsonSerializer.Serialize(changes);
                auditEntries.Add(auditLog);
            }

            if (auditEntries.Count > 0)
            {
                AuditLogs.AddRange(auditEntries);
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
