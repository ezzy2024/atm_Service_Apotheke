using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Changes = table.Column<string>(type: "jsonb", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Consumers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    HasAcceptedBgbWaiver = table.Column<bool>(type: "boolean", nullable: false),
                    BgbWaiverAcceptedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pharmacies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PharmacyName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    SessionVersion = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Street = table.Column<string>(type: "text", nullable: false),
                    HouseNumber = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    LicenseNumber = table.Column<string>(type: "text", nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "text", nullable: true),
                    SubscriptionTier = table.Column<string>(type: "text", nullable: false),
                    EmailConfirmationToken = table.Column<string>(type: "text", nullable: true),
                    EmailConfirmationTokenExpiry = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    FreelanceContractStatus = table.Column<string>(type: "text", nullable: false),
                    UstIdValidationStatus = table.Column<string>(type: "text", nullable: false),
                    UstIdNr = table.Column<string>(type: "text", nullable: true),
                    BetriebserlaubnisNumber = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactPerson = table.Column<string>(type: "text", nullable: true),
                    SoftwareSystem = table.Column<string>(type: "text", nullable: true),
                    FocusAreas = table.Column<string>(type: "text", nullable: true),
                    StaffSupport = table.Column<string>(type: "text", nullable: true),
                    InvoiceBillingPossible = table.Column<bool>(type: "boolean", nullable: false),
                    TargetHourlyRate = table.Column<decimal>(type: "numeric", nullable: true),
                    ParkingAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    AccommodationProvided = table.Column<string>(type: "text", nullable: true),
                    DataProcessingAgreementSignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GdprAnonymizedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FreelanceContractDocumentPath = table.Column<string>(type: "text", nullable: true),
                    IsTelepharmacyConsentGranted = table.Column<bool>(type: "boolean", nullable: false),
                    TelepharmacyConsentDocumentPath = table.Column<string>(type: "text", nullable: true),
                    UtmSource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UtmMedium = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UtmCampaign = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UtmTerm = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pharmacies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pharmacists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaxDistanceKm = table.Column<int>(type: "integer", nullable: false),
                    AvailableDaysPerWeek = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    SessionVersion = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    StripeConnectAccountId = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: false),
                    HouseNumber = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    IsEmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    EmailConfirmationToken = table.Column<string>(type: "text", nullable: true),
                    EmailConfirmationTokenExpiry = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsApprobationVerified = table.Column<bool>(type: "boolean", nullable: false),
                    FreelanceContractStatus = table.Column<string>(type: "text", nullable: false),
                    UstIdValidationStatus = table.Column<string>(type: "text", nullable: false),
                    ApprobationNumber = table.Column<string>(type: "text", nullable: true),
                    IsFreelancerConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PreferredContactMethod = table.Column<string>(type: "text", nullable: true),
                    HasApprobation = table.Column<bool>(type: "boolean", nullable: false),
                    ApprobationCountry = table.Column<string>(type: "text", nullable: true),
                    ExperienceYears = table.Column<string>(type: "text", nullable: true),
                    Specialties = table.Column<string>(type: "text", nullable: true),
                    SoftwareExperience = table.Column<string>(type: "text", nullable: true),
                    Qualification = table.Column<string>(type: "text", nullable: false),
                    WwsProficiency = table.Column<string>(type: "text", nullable: false),
                    RadiusKm = table.Column<int>(type: "integer", nullable: false),
                    PreferredStates = table.Column<string>(type: "text", nullable: true),
                    TravelWillingness = table.Column<string>(type: "text", nullable: true),
                    Mobility = table.Column<string>(type: "text", nullable: true),
                    AvailabilityType = table.Column<string>(type: "text", nullable: true),
                    ShortNoticeAvailability = table.Column<string>(type: "text", nullable: true),
                    EmergencyServiceWillingness = table.Column<bool>(type: "boolean", nullable: false),
                    WeekendWillingness = table.Column<bool>(type: "boolean", nullable: false),
                    FeeModel = table.Column<string>(type: "text", nullable: true),
                    BillingModel = table.Column<string>(type: "text", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    IsVatRequired = table.Column<bool>(type: "boolean", nullable: false),
                    VatSubject = table.Column<string>(type: "text", nullable: true),
                    TravelCostModel = table.Column<string>(type: "text", nullable: true),
                    TravelExpenses = table.Column<string>(type: "text", nullable: true),
                    CountryOfLicense = table.Column<string>(type: "text", nullable: true),
                    ApprobationDocumentPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    FreelanceContractDocumentPath = table.Column<string>(type: "text", nullable: true),
                    CvDocumentPath = table.Column<string>(type: "text", nullable: true),
                    ProfilePicturePath = table.Column<string>(type: "text", nullable: true),
                    IsKycVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IdCardDocumentPath = table.Column<string>(type: "text", nullable: true),
                    LiabilityInsuranceDocumentPath = table.Column<string>(type: "text", nullable: true),
                    TaxId = table.Column<string>(type: "text", nullable: true),
                    GdprAnonymizedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TermsAcceptedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pharmacists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_registry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Street = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PLZ = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pharmacy_registry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsentAgreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PharmacyId = table.Column<int>(type: "integer", nullable: false),
                    PatientName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    HealthInsuranceName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    HealthInsuranceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IkNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SignatureBlob = table.Column<byte[]>(type: "bytea", nullable: false),
                    IsWwsExportGranted = table.Column<bool>(type: "boolean", nullable: false),
                    SignedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentAgreements_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PharmacyId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RequiredQualifications = table.Column<string>(type: "text", nullable: true),
                    RequiredWws = table.Column<string>(type: "text", nullable: true),
                    ReasonForVacancy = table.Column<string>(type: "text", nullable: true),
                    ShiftDetails = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Salary = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobPosts_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KioskTerminals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PharmacyId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DeviceToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KioskTerminals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KioskTerminals_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PharmacyId = table.Column<int>(type: "integer", nullable: false),
                    CiphertextBase64 = table.Column<string>(type: "text", nullable: false),
                    IvBase64 = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PharmacyEmployees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PharmacyId = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    ColorCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacyEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacyEmployees_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaturdayRotationTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PharmacyId = table.Column<int>(type: "integer", nullable: false),
                    PharmacistIds = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaturdayRotationTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaturdayRotationTeams_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionTelemetries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PharmacyId = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTelemetries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionTelemetries_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemperatureLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PharmacyId = table.Column<int>(type: "integer", nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsAnomaly = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemperatureLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemperatureLogs_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PharmacistId = table.Column<int>(type: "integer", nullable: false),
                    FcmToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DevicePlatform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastActive = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceTokens_Pharmacists_PharmacistId",
                        column: x => x.PharmacistId,
                        principalTable: "Pharmacists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MobileRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PharmacistId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MobileRefreshTokens_Pharmacists_PharmacistId",
                        column: x => x.PharmacistId,
                        principalTable: "Pharmacists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtmBillingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PharmacyId = table.Column<int>(type: "integer", nullable: false),
                    ConsentId = table.Column<int>(type: "integer", nullable: false),
                    ServiceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DateOfService = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Sonderkennzeichen = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReportPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtmBillingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtmBillingRecords_ConsentAgreements_ConsentId",
                        column: x => x.ConsentId,
                        principalTable: "ConsentAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AtmBillingRecords_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobPostId = table.Column<int>(type: "integer", nullable: false),
                    PharmacistId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TimesheetPath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobApplications_JobPosts_JobPostId",
                        column: x => x.JobPostId,
                        principalTable: "JobPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobApplications_Pharmacists_PharmacistId",
                        column: x => x.PharmacistId,
                        principalTable: "Pharmacists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PharmacistFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobPostId = table.Column<int>(type: "integer", nullable: false),
                    PharmacistId = table.Column<int>(type: "integer", nullable: false),
                    ActualStart = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ActualPauseMinutes = table.Column<int>(type: "integer", nullable: true),
                    WorkloadLevel = table.Column<string>(type: "text", nullable: true),
                    RelevantAreas = table.Column<string>(type: "text", nullable: true),
                    CriticalIncidents = table.Column<string>(type: "text", nullable: true),
                    OrganizationRating = table.Column<int>(type: "integer", nullable: true),
                    SupportRating = table.Column<int>(type: "integer", nullable: true),
                    WorkspacePrepRating = table.Column<int>(type: "integer", nullable: true),
                    BtmComplianceRating = table.Column<int>(type: "integer", nullable: true),
                    PrivacyRating = table.Column<int>(type: "integer", nullable: true),
                    OverallRating = table.Column<int>(type: "integer", nullable: true),
                    WouldWorkAgain = table.Column<string>(type: "text", nullable: true),
                    Positives = table.Column<string>(type: "text", nullable: true),
                    Improvements = table.Column<string>(type: "text", nullable: true),
                    TimesheetConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacistFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacistFeedbacks_JobPosts_JobPostId",
                        column: x => x.JobPostId,
                        principalTable: "JobPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PharmacistFeedbacks_Pharmacists_PharmacistId",
                        column: x => x.PharmacistId,
                        principalTable: "Pharmacists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PharmacyFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobPostId = table.Column<int>(type: "integer", nullable: false),
                    PharmacistId = table.Column<int>(type: "integer", nullable: false),
                    ActualStart = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ActualPauseMinutes = table.Column<int>(type: "integer", nullable: true),
                    CompetenceRating = table.Column<int>(type: "integer", nullable: true),
                    IndependenceRating = table.Column<int>(type: "integer", nullable: true),
                    AccuracyRating = table.Column<int>(type: "integer", nullable: true),
                    StressManagementRating = table.Column<int>(type: "integer", nullable: true),
                    TeamworkRating = table.Column<int>(type: "integer", nullable: true),
                    CommunicationRating = table.Column<int>(type: "integer", nullable: true),
                    Punctuality = table.Column<string>(type: "text", nullable: true),
                    OnboardingRequired = table.Column<string>(type: "text", nullable: true),
                    OverallGrade = table.Column<string>(type: "text", nullable: true),
                    WouldHireAgain = table.Column<string>(type: "text", nullable: true),
                    Positives = table.Column<string>(type: "text", nullable: true),
                    Improvements = table.Column<string>(type: "text", nullable: true),
                    NextDemand = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacyFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmacyFeedbacks_JobPosts_JobPostId",
                        column: x => x.JobPostId,
                        principalTable: "JobPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PharmacyFeedbacks_Pharmacists_PharmacistId",
                        column: x => x.PharmacistId,
                        principalTable: "Pharmacists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PdlServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ServiceType = table.Column<string>(type: "text", nullable: false),
                    AiAnalysisResultJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BilledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdlServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PdlServices_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InternalShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PharmacyId = table.Column<int>(type: "integer", nullable: false),
                    PharmacyEmployeeId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsEmergencyDuty = table.Column<bool>(type: "boolean", nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    StripeTransferId = table.Column<string>(type: "text", nullable: true),
                    EscrowStatus = table.Column<string>(type: "text", nullable: false),
                    RateNegotiatedBy = table.Column<string>(type: "text", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternalShifts_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InternalShifts_PharmacyEmployees_PharmacyEmployeeId",
                        column: x => x.PharmacyEmployeeId,
                        principalTable: "PharmacyEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaturdayRotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    PharmacyId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaturdayRotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaturdayRotations_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaturdayRotations_SaturdayRotationTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "SaturdayRotationTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Timesheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobApplicationId = table.Column<int>(type: "integer", nullable: false),
                    ActualStartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ActualStartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ActualEndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    TravelCosts = table.Column<decimal>(type: "numeric", nullable: false),
                    AccommodationCosts = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DisputeReason = table.Column<string>(type: "text", nullable: true),
                    DisputedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TimesheetPath = table.Column<string>(type: "text", nullable: true),
                    DigitalSignatureHash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheets_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PdlDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    PharmacyId = table.Column<int>(type: "integer", nullable: false),
                    PdlServiceId = table.Column<int>(type: "integer", nullable: false),
                    PdfUrl = table.Column<string>(type: "text", nullable: false),
                    BillingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdlDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PdlDocuments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PdlDocuments_PdlServices_PdlServiceId",
                        column: x => x.PdlServiceId,
                        principalTable: "PdlServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PdlDocuments_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    TimesheetId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PdfFilePath = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Timesheets_TimesheetId",
                        column: x => x.TimesheetId,
                        principalTable: "Timesheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Holidays",
                columns: new[] { "Id", "Date", "Name", "StateCode" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Neujahrstag", "DE" },
                    { 2, new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Karfreitag", "DE" },
                    { 3, new DateTime(2026, 4, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ostermontag", "DE" },
                    { 4, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tag der Arbeit", "DE" },
                    { 5, new DateTime(2026, 5, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), "Christi Himmelfahrt", "DE" },
                    { 6, new DateTime(2026, 5, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Pfingstmontag", "DE" },
                    { 7, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tag der Deutschen Einheit", "DE" },
                    { 8, new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "1. Weihnachtstag", "DE" },
                    { 9, new DateTime(2026, 12, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), "2. Weihnachtstag", "DE" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtmBillingRecords_ConsentId",
                table: "AtmBillingRecords",
                column: "ConsentId");

            migrationBuilder.CreateIndex(
                name: "IX_AtmBillingRecords_PharmacyId",
                table: "AtmBillingRecords",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentAgreements_PharmacyId",
                table: "ConsentAgreements",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_PharmacistId",
                table: "DeviceTokens",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalShifts_PharmacyEmployeeId",
                table: "InternalShifts",
                column: "PharmacyEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalShifts_PharmacyId",
                table: "InternalShifts",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TimesheetId",
                table: "Invoices",
                column: "TimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_JobPostId",
                table: "JobApplications",
                column: "JobPostId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_PharmacistId",
                table: "JobApplications",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_PharmacyId",
                table: "JobPosts",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskTerminals_PharmacyId",
                table: "KioskTerminals",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_MobileRefreshTokens_PharmacistId",
                table: "MobileRefreshTokens",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PharmacyId",
                table: "Patients",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_PdlDocuments_PatientId",
                table: "PdlDocuments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PdlDocuments_PdlServiceId",
                table: "PdlDocuments",
                column: "PdlServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PdlDocuments_PharmacyId",
                table: "PdlDocuments",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_PdlServices_PatientId",
                table: "PdlServices",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_PharmacyName",
                table: "Pharmacies",
                column: "PharmacyName");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacistFeedbacks_JobPostId",
                table: "PharmacistFeedbacks",
                column: "JobPostId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacistFeedbacks_PharmacistId",
                table: "PharmacistFeedbacks",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacists_Email",
                table: "Pharmacists",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyEmployees_PharmacyId",
                table: "PharmacyEmployees",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyFeedbacks_JobPostId",
                table: "PharmacyFeedbacks",
                column: "JobPostId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyFeedbacks_PharmacistId",
                table: "PharmacyFeedbacks",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_SaturdayRotations_PharmacyId",
                table: "SaturdayRotations",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_SaturdayRotations_TeamId",
                table: "SaturdayRotations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SaturdayRotationTeams_PharmacyId",
                table: "SaturdayRotationTeams",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionTelemetries_PharmacyId",
                table: "SessionTelemetries",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_TemperatureLogs_PharmacyId",
                table: "TemperatureLogs",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_JobApplicationId",
                table: "Timesheets",
                column: "JobApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtmBillingRecords");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Consumers");

            migrationBuilder.DropTable(
                name: "DeviceTokens");

            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "InternalShifts");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "KioskTerminals");

            migrationBuilder.DropTable(
                name: "MobileRefreshTokens");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PdlDocuments");

            migrationBuilder.DropTable(
                name: "PharmacistFeedbacks");

            migrationBuilder.DropTable(
                name: "pharmacy_registry");

            migrationBuilder.DropTable(
                name: "PharmacyFeedbacks");

            migrationBuilder.DropTable(
                name: "SaturdayRotations");

            migrationBuilder.DropTable(
                name: "SessionTelemetries");

            migrationBuilder.DropTable(
                name: "TemperatureLogs");

            migrationBuilder.DropTable(
                name: "ConsentAgreements");

            migrationBuilder.DropTable(
                name: "PharmacyEmployees");

            migrationBuilder.DropTable(
                name: "Timesheets");

            migrationBuilder.DropTable(
                name: "PdlServices");

            migrationBuilder.DropTable(
                name: "SaturdayRotationTeams");

            migrationBuilder.DropTable(
                name: "JobApplications");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "JobPosts");

            migrationBuilder.DropTable(
                name: "Pharmacists");

            migrationBuilder.DropTable(
                name: "Pharmacies");
        }
    }
}
