using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeConnectAccountId",
                table: "Pharmacists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Pharmacies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionTier",
                table: "Pharmacies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EscrowStatus",
                table: "InternalShifts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "InternalShifts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeTransferId",
                table: "InternalShifts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeConnectAccountId",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "EscrowStatus",
                table: "InternalShifts");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "InternalShifts");

            migrationBuilder.DropColumn(
                name: "StripeTransferId",
                table: "InternalShifts");
        }
    }
}
