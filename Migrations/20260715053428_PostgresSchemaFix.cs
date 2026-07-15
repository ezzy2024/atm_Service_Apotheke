using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class PostgresSchemaFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix Booleans
            var booleanColumns = new (string Table, string Column)[]
            {
                ("Consumers", "HasAcceptedBgbWaiver"),
                ("InternalShifts", "IsEmergencyDuty"),
                ("Notifications", "IsRead"),
                ("Pharmacists", "IsEmailConfirmed"),
                ("Pharmacists", "IsVerified"),
                ("Pharmacists", "IsApprobationVerified"),
                ("Pharmacists", "IsFreelancerConfirmed"),
                ("Pharmacists", "HasApprobation"),
                ("Pharmacists", "EmergencyServiceWillingness"),
                ("Pharmacists", "WeekendWillingness"),
                ("Pharmacists", "IsVatRequired"),
                ("Pharmacists", "IsKycVerified"),
                ("PharmacistFeedbacks", "TimesheetConfirmed"),
                ("Pharmacies", "IsEmailConfirmed"),
                ("Pharmacies", "IsVerified"),
                ("Pharmacies", "InvoiceBillingPossible"),
                ("Pharmacies", "ParkingAvailable"),
                ("Pharmacies", "IsTelepharmacyConsentGranted"),
                ("TemperatureLogs", "IsAnomaly")
            };

            foreach (var col in booleanColumns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{col.Table}\" ALTER COLUMN \"{col.Column}\" DROP DEFAULT;");
                migrationBuilder.Sql($"ALTER TABLE \"{col.Table}\" ALTER COLUMN \"{col.Column}\" TYPE boolean USING CASE WHEN \"{col.Column}\"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;");
            }

            // Fix Dates and Timestamps
            var timestampColumns = new (string Table, string Column)[]
            {
                ("AuditLogs", "Timestamp"),
                ("Consumers", "CreatedAt"),
                ("Consumers", "BgbWaiverAcceptedAt"),
                ("InternalShifts", "AcceptedAt"),
                ("Invoices", "CreatedAt"),
                ("Invoices", "PaidAt"),
                ("JobApplications", "AppliedAt"),
                ("JobPosts", "StartDate"),
                ("JobPosts", "EndDate"),
                ("JobPosts", "CreatedAt"),
                ("Notifications", "CreatedAt"),
                ("Pharmacists", "EmailConfirmationTokenExpiry"),
                ("Pharmacists", "GdprAnonymizedAt"),
                ("Pharmacists", "TermsAcceptedAt"),
                ("PharmacistFeedbacks", "ActualStart"),
                ("PharmacistFeedbacks", "ActualEnd"),
                ("PharmacistFeedbacks", "CreatedAt"),
                ("Pharmacies", "CreatedAt"),
                ("Pharmacies", "EmailConfirmationTokenExpiry"),
                ("Pharmacies", "DataProcessingAgreementSignedAt"),
                ("Pharmacies", "GdprAnonymizedAt"),
                ("PharmacyFeedbacks", "ActualStart"),
                ("PharmacyFeedbacks", "ActualEnd"),
                ("PharmacyFeedbacks", "CreatedAt"),
                ("TemperatureLogs", "RecordedAt"),
                ("Timesheets", "ActualStartDate"),
                ("Timesheets", "DisputedAt")
            };

            foreach (var col in timestampColumns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{col.Table}\" ALTER COLUMN \"{col.Column}\" DROP DEFAULT;");
                migrationBuilder.Sql($"ALTER TABLE \"{col.Table}\" ALTER COLUMN \"{col.Column}\" TYPE timestamp with time zone USING \"{col.Column}\"::timestamp with time zone;");
            }

            // Fix Dates
            var dateColumns = new (string Table, string Column)[]
            {
                ("Holidays", "Date"),
                ("InternalShifts", "Date"),
                ("SaturdayRotations", "Date")
            };

            foreach (var col in dateColumns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{col.Table}\" ALTER COLUMN \"{col.Column}\" DROP DEFAULT;");
                migrationBuilder.Sql($"ALTER TABLE \"{col.Table}\" ALTER COLUMN \"{col.Column}\" TYPE date USING \"{col.Column}\"::date;");
            }

            // Fix Decimals (Numeric)
            var numericColumns = new (string Table, string Column)[]
            {
                ("Invoices", "TotalAmount"),
                ("JobPosts", "Salary"),
                ("Pharmacists", "HourlyRate"),
                ("Pharmacies", "TargetHourlyRate"),
                ("Timesheets", "HourlyRate"),
                ("Timesheets", "TravelCosts"),
                ("Timesheets", "AccommodationCosts")
            };

            foreach (var col in numericColumns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{col.Table}\" ALTER COLUMN \"{col.Column}\" DROP DEFAULT;");
                migrationBuilder.Sql($"ALTER TABLE \"{col.Table}\" ALTER COLUMN \"{col.Column}\" TYPE numeric USING \"{col.Column}\"::numeric;");
            }

            // Fix TimeSpans
            var intervalColumns = new (string Table, string Column)[]
            {
                ("Timesheets", "ActualStartTime"),
                ("Timesheets", "ActualEndTime")
            };

            foreach (var col in intervalColumns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{col.Table}\" ALTER COLUMN \"{col.Column}\" DROP DEFAULT;");
                migrationBuilder.Sql($"ALTER TABLE \"{col.Table}\" ALTER COLUMN \"{col.Column}\" TYPE interval USING \"{col.Column}\"::interval;");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse operations back to TEXT and INTEGER for safe rollback
            var booleanColumns = new (string Table, string Column)[]
            {
                ("Consumers", "HasAcceptedBgbWaiver"),
                ("InternalShifts", "IsEmergencyDuty"),
                ("Notifications", "IsRead"),
                ("Pharmacists", "IsEmailConfirmed"),
                ("Pharmacists", "IsVerified"),
                ("Pharmacists", "IsApprobationVerified"),
                ("Pharmacists", "IsFreelancerConfirmed"),
                ("Pharmacists", "HasApprobation"),
                ("Pharmacists", "EmergencyServiceWillingness"),
                ("Pharmacists", "WeekendWillingness"),
                ("Pharmacists", "IsVatRequired"),
                ("Pharmacists", "IsKycVerified"),
                ("PharmacistFeedbacks", "TimesheetConfirmed"),
                ("Pharmacies", "IsEmailConfirmed"),
                ("Pharmacies", "IsVerified"),
                ("Pharmacies", "InvoiceBillingPossible"),
                ("Pharmacies", "ParkingAvailable"),
                ("Pharmacies", "IsTelepharmacyConsentGranted"),
                ("TemperatureLogs", "IsAnomaly")
            };

            foreach (var col in booleanColumns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{col.Table}\" ALTER COLUMN \"{col.Column}\" TYPE integer USING CASE WHEN \"{col.Column}\" THEN 1 ELSE 0 END;");
            }

            var textColumns = new (string Table, string Column)[]
            {
                // Dates and Timestamps
                ("AuditLogs", "Timestamp"),
                ("Consumers", "CreatedAt"),
                ("Consumers", "BgbWaiverAcceptedAt"),
                ("InternalShifts", "AcceptedAt"),
                ("Invoices", "CreatedAt"),
                ("Invoices", "PaidAt"),
                ("JobApplications", "AppliedAt"),
                ("JobPosts", "StartDate"),
                ("JobPosts", "EndDate"),
                ("JobPosts", "CreatedAt"),
                ("Notifications", "CreatedAt"),
                ("Pharmacists", "EmailConfirmationTokenExpiry"),
                ("Pharmacists", "GdprAnonymizedAt"),
                ("Pharmacists", "TermsAcceptedAt"),
                ("PharmacistFeedbacks", "ActualStart"),
                ("PharmacistFeedbacks", "ActualEnd"),
                ("PharmacistFeedbacks", "CreatedAt"),
                ("Pharmacies", "CreatedAt"),
                ("Pharmacies", "EmailConfirmationTokenExpiry"),
                ("Pharmacies", "DataProcessingAgreementSignedAt"),
                ("Pharmacies", "GdprAnonymizedAt"),
                ("PharmacyFeedbacks", "ActualStart"),
                ("PharmacyFeedbacks", "ActualEnd"),
                ("PharmacyFeedbacks", "CreatedAt"),
                ("TemperatureLogs", "RecordedAt"),
                ("Timesheets", "ActualStartDate"),
                ("Timesheets", "DisputedAt"),
                // Dates
                ("Holidays", "Date"),
                ("InternalShifts", "Date"),
                ("SaturdayRotations", "Date"),
                // Decimals
                ("Invoices", "TotalAmount"),
                ("JobPosts", "Salary"),
                ("Pharmacists", "HourlyRate"),
                ("Pharmacies", "TargetHourlyRate"),
                ("Timesheets", "HourlyRate"),
                ("Timesheets", "TravelCosts"),
                ("Timesheets", "AccommodationCosts"),
                // Intervals
                ("Timesheets", "ActualStartTime"),
                ("Timesheets", "ActualEndTime")
            };

            foreach (var col in textColumns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{col.Table}\" ALTER COLUMN \"{col.Column}\" TYPE text USING \"{col.Column}\"::text;");
            }
        }
    }
}
