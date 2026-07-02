using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialTimesheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Accommodation",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "BillingByInvoice",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "FocusAreas",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "ParkingAvailable",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "RequestType",
                table: "JobPosts");

            migrationBuilder.RenameColumn(
                name: "Urgency",
                table: "JobPosts",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "JobPosts",
                newName: "RequiredQualifications");

            migrationBuilder.RenameColumn(
                name: "SoftwareSystem",
                table: "JobPosts",
                newName: "Description");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "JobPosts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "JobPosts",
                newName: "Urgency");

            migrationBuilder.RenameColumn(
                name: "RequiredQualifications",
                table: "JobPosts",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "JobPosts",
                newName: "SoftwareSystem");

            migrationBuilder.AlterColumn<string>(
                name: "EndDate",
                table: "JobPosts",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Accommodation",
                table: "JobPosts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "BillingByInvoice",
                table: "JobPosts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EndTime",
                table: "JobPosts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FocusAreas",
                table: "JobPosts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "JobPosts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ParkingAvailable",
                table: "JobPosts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequestType",
                table: "JobPosts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
