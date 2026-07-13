using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class MobileGateway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTelepharmacyConsentGranted",
                table: "ConsentAgreements");

            migrationBuilder.AddColumn<string>(
                name: "AugContractDocumentPath",
                table: "Pharmacists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AugContractDocumentPath",
                table: "Pharmacies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTelepharmacyConsentGranted",
                table: "Pharmacies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TelepharmacyConsentDocumentPath",
                table: "Pharmacies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PharmacistId = table.Column<int>(type: "INTEGER", nullable: false),
                    FcmToken = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DevicePlatform = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActive = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PharmacistId = table.Column<int>(type: "INTEGER", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_PharmacistId",
                table: "DeviceTokens",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_MobileRefreshTokens_PharmacistId",
                table: "MobileRefreshTokens",
                column: "PharmacistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceTokens");

            migrationBuilder.DropTable(
                name: "MobileRefreshTokens");

            migrationBuilder.DropColumn(
                name: "AugContractDocumentPath",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "AugContractDocumentPath",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "IsTelepharmacyConsentGranted",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "TelepharmacyConsentDocumentPath",
                table: "Pharmacies");

            migrationBuilder.AddColumn<bool>(
                name: "IsTelepharmacyConsentGranted",
                table: "ConsentAgreements",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
