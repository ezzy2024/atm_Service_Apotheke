using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUtmTrackingToPharmacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "JobPosts");

            migrationBuilder.AddColumn<string>(
                name: "UtmCampaign",
                table: "Pharmacies",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmMedium",
                table: "Pharmacies",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmSource",
                table: "Pharmacies",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmTerm",
                table: "Pharmacies",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UtmCampaign",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "UtmMedium",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "UtmSource",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "UtmTerm",
                table: "Pharmacies");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "JobPosts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }
    }
}
