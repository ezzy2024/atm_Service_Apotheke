using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStripeAndRenameEscrowStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Timesheets_JobApplicationId",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "InternalShifts");

            migrationBuilder.DropColumn(
                name: "StripeTransferId",
                table: "InternalShifts");

            migrationBuilder.RenameColumn(
                name: "EscrowStatus",
                table: "InternalShifts",
                newName: "PaymentStatus");

            migrationBuilder.AddColumn<bool>(
                name: "HasPremiumAccess",
                table: "Pharmacies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_JobApplicationId",
                table: "Timesheets",
                column: "JobApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Timesheets_JobApplicationId",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "HasPremiumAccess",
                table: "Pharmacies");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "InternalShifts",
                newName: "EscrowStatus");

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "InternalShifts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeTransferId",
                table: "InternalShifts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_JobApplicationId",
                table: "Timesheets",
                column: "JobApplicationId");
        }
    }
}
