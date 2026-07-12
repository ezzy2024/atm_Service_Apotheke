using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceAndLegalTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AugContractStatus",
                table: "Pharmacists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsApprobationVerified",
                table: "Pharmacists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UstIdValidationStatus",
                table: "Pharmacists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AugContractStatus",
                table: "Pharmacies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UstIdNr",
                table: "Pharmacies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UstIdValidationStatus",
                table: "Pharmacies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AugContractStatus",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "IsApprobationVerified",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "UstIdValidationStatus",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "AugContractStatus",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "UstIdNr",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "UstIdValidationStatus",
                table: "Pharmacies");
        }
    }
}
