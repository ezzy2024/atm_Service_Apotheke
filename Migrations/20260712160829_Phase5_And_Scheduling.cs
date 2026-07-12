using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_And_Scheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Consumers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HasAcceptedBgbWaiver = table.Column<bool>(type: "INTEGER", nullable: false),
                    BgbWaiverAcceptedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StateCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaturdayRotationTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PharmacyId = table.Column<int>(type: "INTEGER", nullable: false),
                    PharmacistIds = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaturdayRotationTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaturdayRotationTeams_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaturdayRotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    PharmacyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaturdayRotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaturdayRotations_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaturdayRotations_SaturdayRotationTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "SaturdayRotationTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Holidays",
                columns: new[] { "Id", "Date", "Name", "StateCode" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Neujahrstag", "DE" },
                    { 2, new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Karfreitag", "DE" },
                    { 3, new DateTime(2026, 4, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ostermontag", "DE" },
                    { 4, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tag der Arbeit", "DE" },
                    { 5, new DateTime(2026, 5, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), "Christi Himmelfahrt", "DE" },
                    { 6, new DateTime(2026, 5, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Pfingstmontag", "DE" },
                    { 7, new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tag der Deutschen Einheit", "DE" },
                    { 8, new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "1. Weihnachtstag", "DE" },
                    { 9, new DateTime(2026, 12, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), "2. Weihnachtstag", "DE" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaturdayRotations_PharmacyId",
                table: "SaturdayRotations",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_SaturdayRotations_TeamId",
                table: "SaturdayRotations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SaturdayRotationTeams_PharmacyId",
                table: "SaturdayRotationTeams",
                column: "PharmacyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Consumers");

            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "SaturdayRotations");

            migrationBuilder.DropTable(
                name: "SaturdayRotationTeams");
        }
    }
}
