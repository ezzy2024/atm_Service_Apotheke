using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class SplitPharmacistAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Pharmacists",
                newName: "Street");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Pharmacists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HouseNumber",
                table: "Pharmacists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Pharmacists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfilePicturePath",
                table: "Pharmacists",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "HouseNumber",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "ProfilePicturePath",
                table: "Pharmacists");

            migrationBuilder.RenameColumn(
                name: "Street",
                table: "Pharmacists",
                newName: "Address");
        }
    }
}
