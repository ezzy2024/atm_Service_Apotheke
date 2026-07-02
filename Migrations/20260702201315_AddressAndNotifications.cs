using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class AddressAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Pharmacies",
                newName: "Street");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Pharmacists",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Pharmacists",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Qualification",
                table: "Pharmacists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WwsProficiency",
                table: "Pharmacists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Pharmacies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HouseNumber",
                table: "Pharmacies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Pharmacies",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Pharmacies",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Pharmacies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReasonForVacancy",
                table: "JobPosts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredWws",
                table: "JobPosts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "JobPosts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "Qualification",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "WwsProficiency",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "HouseNumber",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "ReasonForVacancy",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "RequiredWws",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "JobPosts");

            migrationBuilder.RenameColumn(
                name: "Street",
                table: "Pharmacies",
                newName: "Address");
        }
    }
}
