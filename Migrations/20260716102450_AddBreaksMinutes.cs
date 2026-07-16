using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBreaksMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BreaksMinutes",
                table: "Timesheets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BreaksMinutes",
                table: "Timesheets");
        }
    }
}
