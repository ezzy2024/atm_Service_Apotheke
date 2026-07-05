using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUnifiedMegaSchema_ATM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisputeReason",
                table: "Timesheets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisputedAt",
                table: "Timesheets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsentAgreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PharmacyId = table.Column<int>(type: "INTEGER", nullable: false),
                    PatientName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    HealthInsuranceName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    HealthInsuranceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IkNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SignatureBlob = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SignedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "KioskTerminals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PharmacyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DeviceToken = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "SessionTelemetries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PharmacyId = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "AtmBillingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PharmacyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsentId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DateOfService = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Sonderkennzeichen = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReportPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "IX_KioskTerminals_PharmacyId",
                table: "KioskTerminals",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionTelemetries_PharmacyId",
                table: "SessionTelemetries",
                column: "PharmacyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtmBillingRecords");

            migrationBuilder.DropTable(
                name: "KioskTerminals");

            migrationBuilder.DropTable(
                name: "SessionTelemetries");

            migrationBuilder.DropTable(
                name: "ConsentAgreements");

            migrationBuilder.DropColumn(
                name: "DisputeReason",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "DisputedAt",
                table: "Timesheets");
        }
    }
}
