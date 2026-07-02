using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCleanState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pharmacies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PharmacyName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    LicenseNumber = table.Column<string>(type: "TEXT", nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailConfirmationToken = table.Column<string>(type: "TEXT", nullable: true),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContactPerson = table.Column<string>(type: "TEXT", nullable: true),
                    SoftwareSystem = table.Column<string>(type: "TEXT", nullable: true),
                    FocusAreas = table.Column<string>(type: "TEXT", nullable: true),
                    StaffSupport = table.Column<string>(type: "TEXT", nullable: true),
                    InvoiceBillingPossible = table.Column<bool>(type: "INTEGER", nullable: false),
                    TargetHourlyRate = table.Column<string>(type: "TEXT", nullable: true),
                    ParkingAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccommodationProvided = table.Column<string>(type: "TEXT", nullable: true),
                    DataProcessingAgreementSignedAt = table.Column<string>(type: "TEXT", nullable: true),
                    GdprAnonymizedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pharmacies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PharmacistFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobPostId = table.Column<int>(type: "INTEGER", nullable: false),
                    PharmacistId = table.Column<int>(type: "INTEGER", nullable: false),
                    OnboardingScore = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkspaceSetupScore = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkloadHV = table.Column<string>(type: "TEXT", nullable: false),
                    WorkloadRecipe = table.Column<string>(type: "TEXT", nullable: false),
                    BtmProcessScore = table.Column<int>(type: "INTEGER", nullable: false),
                    DataProtectionScore = table.Column<int>(type: "INTEGER", nullable: false),
                    OverallScore = table.Column<int>(type: "INTEGER", nullable: false),
                    CriticalIncidents = table.Column<string>(type: "TEXT", nullable: false),
                    PositiveAspects = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacistFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pharmacists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaxDistanceKm = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableDaysPerWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailConfirmationToken = table.Column<string>(type: "TEXT", nullable: true),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreferredContactMethod = table.Column<string>(type: "TEXT", nullable: true),
                    HasApprobation = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApprobationCountry = table.Column<string>(type: "TEXT", nullable: true),
                    ExperienceYears = table.Column<string>(type: "TEXT", nullable: true),
                    Specialties = table.Column<string>(type: "TEXT", nullable: true),
                    SoftwareExperience = table.Column<string>(type: "TEXT", nullable: true),
                    RadiusKm = table.Column<int>(type: "INTEGER", nullable: false),
                    PreferredStates = table.Column<string>(type: "TEXT", nullable: true),
                    TravelWillingness = table.Column<string>(type: "TEXT", nullable: true),
                    Mobility = table.Column<string>(type: "TEXT", nullable: true),
                    AvailabilityType = table.Column<string>(type: "TEXT", nullable: true),
                    ShortNoticeAvailability = table.Column<string>(type: "TEXT", nullable: true),
                    EmergencyServiceWillingness = table.Column<bool>(type: "INTEGER", nullable: false),
                    WeekendWillingness = table.Column<bool>(type: "INTEGER", nullable: false),
                    FeeModel = table.Column<string>(type: "TEXT", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    VatSubject = table.Column<string>(type: "TEXT", nullable: true),
                    TravelExpenses = table.Column<string>(type: "TEXT", nullable: true),
                    ApprobationDocumentPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    CvDocumentPath = table.Column<string>(type: "TEXT", nullable: true),
                    IsKycVerified = table.Column<int>(type: "INTEGER", nullable: false),
                    IdCardDocumentPath = table.Column<string>(type: "TEXT", nullable: true),
                    LiabilityInsuranceDocumentPath = table.Column<string>(type: "TEXT", nullable: true),
                    TaxId = table.Column<string>(type: "TEXT", nullable: true),
                    GdprAnonymizedAt = table.Column<string>(type: "TEXT", nullable: true),
                    TermsAcceptedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pharmacists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PharmacyFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobPostId = table.Column<int>(type: "INTEGER", nullable: false),
                    PharmacyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualStartTime = table.Column<string>(type: "TEXT", nullable: false),
                    ActualEndTime = table.Column<string>(type: "TEXT", nullable: false),
                    CompetenceScore = table.Column<int>(type: "INTEGER", nullable: false),
                    IndependenceScore = table.Column<int>(type: "INTEGER", nullable: false),
                    CarefulnessScore = table.Column<int>(type: "INTEGER", nullable: false),
                    StressHandlingScore = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamworkScore = table.Column<int>(type: "INTEGER", nullable: false),
                    OverallScore = table.Column<int>(type: "INTEGER", nullable: false),
                    WouldBookAgain = table.Column<bool>(type: "INTEGER", nullable: false),
                    PositiveAspects = table.Column<string>(type: "TEXT", nullable: false),
                    ImprovementAspects = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmacyFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PharmacyId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestType = table.Column<string>(type: "TEXT", nullable: false),
                    Urgency = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<string>(type: "TEXT", nullable: false),
                    EndDate = table.Column<string>(type: "TEXT", nullable: true),
                    StartTime = table.Column<string>(type: "TEXT", nullable: false),
                    EndTime = table.Column<string>(type: "TEXT", nullable: false),
                    SoftwareSystem = table.Column<string>(type: "TEXT", nullable: false),
                    FocusAreas = table.Column<string>(type: "TEXT", nullable: false),
                    Salary = table.Column<decimal>(type: "TEXT", nullable: false),
                    Accommodation = table.Column<string>(type: "TEXT", nullable: false),
                    BillingByInvoice = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParkingAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "JobApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobPostId = table.Column<int>(type: "INTEGER", nullable: false),
                    PharmacistId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimesheetPath = table.Column<string>(type: "TEXT", nullable: true)
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
                name: "Timesheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobApplicationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualStartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualStartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    ActualEndTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    TravelCosts = table.Column<decimal>(type: "TEXT", nullable: false),
                    AccommodationCosts = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
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
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    TimesheetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    PdfFilePath = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "IX_Pharmacies_PharmacyName",
                table: "Pharmacies",
                column: "PharmacyName");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacists_Email",
                table: "Pharmacists",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_JobApplicationId",
                table: "Timesheets",
                column: "JobApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "PharmacistFeedbacks");

            migrationBuilder.DropTable(
                name: "PharmacyFeedbacks");

            migrationBuilder.DropTable(
                name: "Timesheets");

            migrationBuilder.DropTable(
                name: "JobApplications");

            migrationBuilder.DropTable(
                name: "JobPosts");

            migrationBuilder.DropTable(
                name: "Pharmacists");

            migrationBuilder.DropTable(
                name: "Pharmacies");
        }
    }
}
