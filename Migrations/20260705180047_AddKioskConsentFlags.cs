using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class AddKioskConsentFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTelepharmacyConsentGranted",
                table: "ConsentAgreements",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWwsExportGranted",
                table: "ConsentAgreements",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTelepharmacyConsentGranted",
                table: "ConsentAgreements");

            migrationBuilder.DropColumn(
                name: "IsWwsExportGranted",
                table: "ConsentAgreements");
        }
    }
}
