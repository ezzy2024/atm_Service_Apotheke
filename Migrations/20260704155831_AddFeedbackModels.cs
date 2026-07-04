using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceApotheke.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualEndTime",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "ActualStartTime",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "CarefulnessScore",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "CompetenceScore",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "ImprovementAspects",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "IndependenceScore",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "OverallScore",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "PharmacyId",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "PositiveAspects",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "StressHandlingScore",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "TeamworkScore",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "BtmProcessScore",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "DataProtectionScore",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "OnboardingScore",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "OverallScore",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "PositiveAspects",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "WorkloadHV",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "WorkloadRecipe",
                table: "PharmacistFeedbacks");

            migrationBuilder.RenameColumn(
                name: "WouldBookAgain",
                table: "PharmacyFeedbacks",
                newName: "PharmacistId");

            migrationBuilder.RenameColumn(
                name: "WorkspaceSetupScore",
                table: "PharmacistFeedbacks",
                newName: "TimesheetConfirmed");

            migrationBuilder.AddColumn<int>(
                name: "AccuracyRating",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEnd",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualPauseMinutes",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStart",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommunicationRating",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompetenceRating",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Improvements",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IndependenceRating",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextDemand",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnboardingRequired",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverallGrade",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Positives",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Punctuality",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StressManagementRating",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamworkRating",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WouldHireAgain",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingModel",
                table: "Pharmacists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryOfLicense",
                table: "Pharmacists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVatRequired",
                table: "Pharmacists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TravelCostModel",
                table: "Pharmacists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CriticalIncidents",
                table: "PharmacistFeedbacks",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEnd",
                table: "PharmacistFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualPauseMinutes",
                table: "PharmacistFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStart",
                table: "PharmacistFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BtmComplianceRating",
                table: "PharmacistFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Improvements",
                table: "PharmacistFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationRating",
                table: "PharmacistFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverallRating",
                table: "PharmacistFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Positives",
                table: "PharmacistFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrivacyRating",
                table: "PharmacistFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelevantAreas",
                table: "PharmacistFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupportRating",
                table: "PharmacistFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkloadLevel",
                table: "PharmacistFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkspacePrepRating",
                table: "PharmacistFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WouldWorkAgain",
                table: "PharmacistFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShiftDetails",
                table: "JobPosts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyFeedbacks_JobPostId",
                table: "PharmacyFeedbacks",
                column: "JobPostId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyFeedbacks_PharmacistId",
                table: "PharmacyFeedbacks",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacistFeedbacks_JobPostId",
                table: "PharmacistFeedbacks",
                column: "JobPostId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacistFeedbacks_PharmacistId",
                table: "PharmacistFeedbacks",
                column: "PharmacistId");

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacistFeedbacks_JobPosts_JobPostId",
                table: "PharmacistFeedbacks",
                column: "JobPostId",
                principalTable: "JobPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacistFeedbacks_Pharmacists_PharmacistId",
                table: "PharmacistFeedbacks",
                column: "PharmacistId",
                principalTable: "Pharmacists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyFeedbacks_JobPosts_JobPostId",
                table: "PharmacyFeedbacks",
                column: "JobPostId",
                principalTable: "JobPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyFeedbacks_Pharmacists_PharmacistId",
                table: "PharmacyFeedbacks",
                column: "PharmacistId",
                principalTable: "Pharmacists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PharmacistFeedbacks_JobPosts_JobPostId",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacistFeedbacks_Pharmacists_PharmacistId",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyFeedbacks_JobPosts_JobPostId",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyFeedbacks_Pharmacists_PharmacistId",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyFeedbacks_JobPostId",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyFeedbacks_PharmacistId",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_PharmacistFeedbacks_JobPostId",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_PharmacistFeedbacks_PharmacistId",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "AccuracyRating",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "ActualEnd",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "ActualPauseMinutes",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "ActualStart",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "CommunicationRating",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "CompetenceRating",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "Improvements",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "IndependenceRating",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "NextDemand",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "OnboardingRequired",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "OverallGrade",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "Positives",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "Punctuality",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "StressManagementRating",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "TeamworkRating",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "WouldHireAgain",
                table: "PharmacyFeedbacks");

            migrationBuilder.DropColumn(
                name: "BillingModel",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "CountryOfLicense",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "IsVatRequired",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "TravelCostModel",
                table: "Pharmacists");

            migrationBuilder.DropColumn(
                name: "ActualEnd",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "ActualPauseMinutes",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "ActualStart",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "BtmComplianceRating",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "Improvements",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "OrganizationRating",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "OverallRating",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "Positives",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "PrivacyRating",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "RelevantAreas",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "SupportRating",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "WorkloadLevel",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "WorkspacePrepRating",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "WouldWorkAgain",
                table: "PharmacistFeedbacks");

            migrationBuilder.DropColumn(
                name: "ShiftDetails",
                table: "JobPosts");

            migrationBuilder.RenameColumn(
                name: "PharmacistId",
                table: "PharmacyFeedbacks",
                newName: "WouldBookAgain");

            migrationBuilder.RenameColumn(
                name: "TimesheetConfirmed",
                table: "PharmacistFeedbacks",
                newName: "WorkspaceSetupScore");

            migrationBuilder.AddColumn<string>(
                name: "ActualEndTime",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActualStartTime",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CarefulnessScore",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompetenceScore",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImprovementAspects",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "IndependenceScore",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OverallScore",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PharmacyId",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PositiveAspects",
                table: "PharmacyFeedbacks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StressHandlingScore",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TeamworkScore",
                table: "PharmacyFeedbacks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "CriticalIncidents",
                table: "PharmacistFeedbacks",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BtmProcessScore",
                table: "PharmacistFeedbacks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DataProtectionScore",
                table: "PharmacistFeedbacks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OnboardingScore",
                table: "PharmacistFeedbacks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OverallScore",
                table: "PharmacistFeedbacks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PositiveAspects",
                table: "PharmacistFeedbacks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkloadHV",
                table: "PharmacistFeedbacks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkloadRecipe",
                table: "PharmacistFeedbacks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
