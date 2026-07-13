using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class E2EE_PatientData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Geburt",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "IsEligibleForAmts",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "KdnNr",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "MedicationCount",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "MedicationsJson",
                table: "Patients");

            migrationBuilder.RenameColumn(
                name: "Vorname",
                table: "Patients",
                newName: "IvBase64");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Patients",
                newName: "CiphertextBase64");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IvBase64",
                table: "Patients",
                newName: "Vorname");

            migrationBuilder.RenameColumn(
                name: "CiphertextBase64",
                table: "Patients",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "Geburt",
                table: "Patients",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Patients",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsEligibleForAmts",
                table: "Patients",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KdnNr",
                table: "Patients",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MedicationCount",
                table: "Patients",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MedicationsJson",
                table: "Patients",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
