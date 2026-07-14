using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class LegalAndRestructuringUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AugContractStatus",
                table: "Pharmacists",
                newName: "FreelanceContractStatus");

            migrationBuilder.RenameColumn(
                name: "AugContractDocumentPath",
                table: "Pharmacists",
                newName: "FreelanceContractDocumentPath");

            migrationBuilder.RenameColumn(
                name: "AugContractStatus",
                table: "Pharmacies",
                newName: "FreelanceContractStatus");

            migrationBuilder.RenameColumn(
                name: "AugContractDocumentPath",
                table: "Pharmacies",
                newName: "FreelanceContractDocumentPath");

            migrationBuilder.AddColumn<string>(
                name: "ApprobationNumber",
                table: "Pharmacists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFreelancerConfirmed",
                table: "Pharmacists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Pharmacists",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BetriebserlaubnisNumber",
                table: "Pharmacies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Pharmacies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                table: "InternalShifts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RateNegotiatedBy",
                table: "InternalShifts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprobationNumber",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "IsFreelancerConfirmed",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "BetriebserlaubnisNumber",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "InternalShifts");

            migrationBuilder.DropColumn(
                name: "RateNegotiatedBy",
                table: "InternalShifts");

            migrationBuilder.RenameColumn(
                name: "FreelanceContractStatus",
                table: "Pharmacists",
                newName: "AugContractStatus");

            migrationBuilder.RenameColumn(
                name: "FreelanceContractDocumentPath",
                table: "Pharmacists",
                newName: "AugContractDocumentPath");

            migrationBuilder.RenameColumn(
                name: "FreelanceContractStatus",
                table: "Pharmacies",
                newName: "AugContractStatus");

            migrationBuilder.RenameColumn(
                name: "FreelanceContractDocumentPath",
                table: "Pharmacies",
                newName: "AugContractDocumentPath");
        }
    }
}
