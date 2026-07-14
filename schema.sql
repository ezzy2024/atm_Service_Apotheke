CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "Pharmacies" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Pharmacies" PRIMARY KEY AUTOINCREMENT,
    "PharmacyName" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "PhoneNumber" TEXT NOT NULL,
    "Address" TEXT NOT NULL,
    "LicenseNumber" TEXT NOT NULL,
    "IsEmailConfirmed" INTEGER NOT NULL,
    "EmailConfirmationToken" TEXT NULL,
    "IsVerified" INTEGER NOT NULL,
    "ContactPerson" TEXT NULL,
    "SoftwareSystem" TEXT NULL,
    "FocusAreas" TEXT NULL,
    "StaffSupport" TEXT NULL,
    "InvoiceBillingPossible" INTEGER NOT NULL,
    "TargetHourlyRate" TEXT NULL,
    "ParkingAvailable" INTEGER NOT NULL,
    "AccommodationProvided" TEXT NULL,
    "DataProcessingAgreementSignedAt" TEXT NULL,
    "GdprAnonymizedAt" TEXT NULL
);

CREATE TABLE "PharmacistFeedbacks" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PharmacistFeedbacks" PRIMARY KEY AUTOINCREMENT,
    "JobPostId" INTEGER NOT NULL,
    "PharmacistId" INTEGER NOT NULL,
    "OnboardingScore" INTEGER NOT NULL,
    "WorkspaceSetupScore" INTEGER NOT NULL,
    "WorkloadHV" TEXT NOT NULL,
    "WorkloadRecipe" TEXT NOT NULL,
    "BtmProcessScore" INTEGER NOT NULL,
    "DataProtectionScore" INTEGER NOT NULL,
    "OverallScore" INTEGER NOT NULL,
    "CriticalIncidents" TEXT NOT NULL,
    "PositiveAspects" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL
);

CREATE TABLE "Pharmacists" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Pharmacists" PRIMARY KEY AUTOINCREMENT,
    "MaxDistanceKm" INTEGER NOT NULL,
    "AvailableDaysPerWeek" INTEGER NOT NULL,
    "FullName" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "PhoneNumber" TEXT NOT NULL,
    "Address" TEXT NOT NULL,
    "IsEmailConfirmed" INTEGER NOT NULL,
    "EmailConfirmationToken" TEXT NULL,
    "IsVerified" INTEGER NOT NULL,
    "PreferredContactMethod" TEXT NULL,
    "HasApprobation" INTEGER NOT NULL,
    "ApprobationCountry" TEXT NULL,
    "ExperienceYears" TEXT NULL,
    "Specialties" TEXT NULL,
    "SoftwareExperience" TEXT NULL,
    "RadiusKm" INTEGER NOT NULL,
    "PreferredStates" TEXT NULL,
    "TravelWillingness" TEXT NULL,
    "Mobility" TEXT NULL,
    "AvailabilityType" TEXT NULL,
    "ShortNoticeAvailability" TEXT NULL,
    "EmergencyServiceWillingness" INTEGER NOT NULL,
    "WeekendWillingness" INTEGER NOT NULL,
    "FeeModel" TEXT NULL,
    "HourlyRate" TEXT NOT NULL,
    "VatSubject" TEXT NULL,
    "TravelExpenses" TEXT NULL,
    "ApprobationDocumentPath" TEXT NULL,
    "CvDocumentPath" TEXT NULL,
    "IsKycVerified" INTEGER NOT NULL,
    "IdCardDocumentPath" TEXT NULL,
    "LiabilityInsuranceDocumentPath" TEXT NULL,
    "TaxId" TEXT NULL,
    "GdprAnonymizedAt" TEXT NULL,
    "TermsAcceptedAt" TEXT NULL
);

CREATE TABLE "PharmacyFeedbacks" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PharmacyFeedbacks" PRIMARY KEY AUTOINCREMENT,
    "JobPostId" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "ActualStartTime" TEXT NOT NULL,
    "ActualEndTime" TEXT NOT NULL,
    "CompetenceScore" INTEGER NOT NULL,
    "IndependenceScore" INTEGER NOT NULL,
    "CarefulnessScore" INTEGER NOT NULL,
    "StressHandlingScore" INTEGER NOT NULL,
    "TeamworkScore" INTEGER NOT NULL,
    "OverallScore" INTEGER NOT NULL,
    "WouldBookAgain" INTEGER NOT NULL,
    "PositiveAspects" TEXT NOT NULL,
    "ImprovementAspects" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL
);

CREATE TABLE "JobPosts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_JobPosts" PRIMARY KEY AUTOINCREMENT,
    "PharmacyId" INTEGER NOT NULL,
    "RequestType" TEXT NOT NULL,
    "Urgency" TEXT NOT NULL,
    "StartDate" TEXT NOT NULL,
    "EndDate" TEXT NULL,
    "StartTime" TEXT NOT NULL,
    "EndTime" TEXT NOT NULL,
    "SoftwareSystem" TEXT NOT NULL,
    "FocusAreas" TEXT NOT NULL,
    "Salary" TEXT NOT NULL,
    "Accommodation" TEXT NOT NULL,
    "BillingByInvoice" INTEGER NOT NULL,
    "ParkingAvailable" INTEGER NOT NULL,
    "Notes" TEXT NULL,
    "Status" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_JobPosts_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "JobApplications" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_JobApplications" PRIMARY KEY AUTOINCREMENT,
    "JobPostId" INTEGER NOT NULL,
    "PharmacistId" INTEGER NOT NULL,
    "Status" TEXT NOT NULL,
    "AppliedAt" TEXT NOT NULL,
    "TimesheetPath" TEXT NULL,
    CONSTRAINT "FK_JobApplications_JobPosts_JobPostId" FOREIGN KEY ("JobPostId") REFERENCES "JobPosts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_JobApplications_Pharmacists_PharmacistId" FOREIGN KEY ("PharmacistId") REFERENCES "Pharmacists" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Timesheets" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Timesheets" PRIMARY KEY AUTOINCREMENT,
    "JobApplicationId" INTEGER NOT NULL,
    "ActualStartDate" TEXT NOT NULL,
    "ActualStartTime" TEXT NOT NULL,
    "ActualEndTime" TEXT NOT NULL,
    "HourlyRate" TEXT NOT NULL,
    "TravelCosts" TEXT NOT NULL,
    "AccommodationCosts" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    CONSTRAINT "FK_Timesheets_JobApplications_JobApplicationId" FOREIGN KEY ("JobApplicationId") REFERENCES "JobApplications" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Invoices" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Invoices" PRIMARY KEY AUTOINCREMENT,
    "InvoiceNumber" TEXT NOT NULL,
    "TimesheetId" INTEGER NOT NULL,
    "Type" TEXT NOT NULL,
    "TotalAmount" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "PdfFilePath" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Invoices_Timesheets_TimesheetId" FOREIGN KEY ("TimesheetId") REFERENCES "Timesheets" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Invoices_TimesheetId" ON "Invoices" ("TimesheetId");

CREATE INDEX "IX_JobApplications_JobPostId" ON "JobApplications" ("JobPostId");

CREATE INDEX "IX_JobApplications_PharmacistId" ON "JobApplications" ("PharmacistId");

CREATE INDEX "IX_JobPosts_PharmacyId" ON "JobPosts" ("PharmacyId");

CREATE INDEX "IX_Pharmacies_PharmacyName" ON "Pharmacies" ("PharmacyName");

CREATE UNIQUE INDEX "IX_Pharmacists_Email" ON "Pharmacists" ("Email");

CREATE INDEX "IX_Timesheets_JobApplicationId" ON "Timesheets" ("JobApplicationId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260620112636_InitialCleanState', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "JobPosts" RENAME COLUMN "Urgency" TO "Title";

ALTER TABLE "JobPosts" RENAME COLUMN "StartTime" TO "RequiredQualifications";

ALTER TABLE "JobPosts" RENAME COLUMN "SoftwareSystem" TO "Description";

CREATE TABLE "ef_temp_JobPosts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_JobPosts" PRIMARY KEY AUTOINCREMENT,
    "CreatedAt" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "EndDate" TEXT NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "RequiredQualifications" TEXT NOT NULL,
    "Salary" TEXT NOT NULL,
    "StartDate" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    CONSTRAINT "FK_JobPosts_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

INSERT INTO "ef_temp_JobPosts" ("Id", "CreatedAt", "Description", "EndDate", "PharmacyId", "RequiredQualifications", "Salary", "StartDate", "Status", "Title")
SELECT "Id", "CreatedAt", "Description", IFNULL("EndDate", '0001-01-01 00:00:00'), "PharmacyId", "RequiredQualifications", "Salary", "StartDate", "Status", "Title"
FROM "JobPosts";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

DROP TABLE "JobPosts";

ALTER TABLE "ef_temp_JobPosts" RENAME TO "JobPosts";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;

CREATE INDEX "IX_JobPosts_PharmacyId" ON "JobPosts" ("PharmacyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260623074307_SeedInitialTimesheet', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260629142041_AddJobDescriptionColumn', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "Invoices" ADD "PaidAt" TEXT NULL;

CREATE TABLE "ef_temp_JobPosts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_JobPosts" PRIMARY KEY AUTOINCREMENT,
    "CreatedAt" TEXT NOT NULL,
    "Description" TEXT NULL,
    "EndDate" TEXT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "RequiredQualifications" TEXT NULL,
    "Salary" TEXT NULL,
    "StartDate" TEXT NULL,
    "Status" TEXT NULL,
    "Title" TEXT NULL,
    CONSTRAINT "FK_JobPosts_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

INSERT INTO "ef_temp_JobPosts" ("Id", "CreatedAt", "Description", "EndDate", "PharmacyId", "RequiredQualifications", "Salary", "StartDate", "Status", "Title")
SELECT "Id", "CreatedAt", "Description", "EndDate", "PharmacyId", "RequiredQualifications", "Salary", "StartDate", "Status", "Title"
FROM "JobPosts";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

DROP TABLE "JobPosts";

ALTER TABLE "ef_temp_JobPosts" RENAME TO "JobPosts";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;

CREATE INDEX "IX_JobPosts_PharmacyId" ON "JobPosts" ("PharmacyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260701140229_AddInvoicePaidAt', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "Pharmacies" ADD "ApiKey" TEXT NULL;

CREATE TABLE "TemperatureLogs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_TemperatureLogs" PRIMARY KEY AUTOINCREMENT,
    "PharmacyId" INTEGER NOT NULL,
    "Temperature" REAL NOT NULL,
    "RecordedAt" TEXT NOT NULL,
    "IsAnomaly" INTEGER NOT NULL,
    CONSTRAINT "FK_TemperatureLogs_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_TemperatureLogs_PharmacyId" ON "TemperatureLogs" ("PharmacyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260701142421_AddTemperatureLog', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

CREATE TABLE "AuditLogs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AuditLogs" PRIMARY KEY AUTOINCREMENT,
    "EntityName" TEXT NOT NULL,
    "EntityId" TEXT NOT NULL,
    "Action" TEXT NOT NULL,
    "Changes" jsonb NULL,
    "Timestamp" TEXT NOT NULL,
    "PerformedBy" TEXT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260701170308_AddAuditLogging', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "Pharmacies" RENAME COLUMN "Address" TO "Street";

ALTER TABLE "Pharmacists" ADD "Latitude" REAL NULL;

ALTER TABLE "Pharmacists" ADD "Longitude" REAL NULL;

ALTER TABLE "Pharmacists" ADD "Qualification" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacists" ADD "WwsProficiency" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacies" ADD "City" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacies" ADD "HouseNumber" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacies" ADD "Latitude" REAL NULL;

ALTER TABLE "Pharmacies" ADD "Longitude" REAL NULL;

ALTER TABLE "Pharmacies" ADD "PostalCode" TEXT NOT NULL DEFAULT '';

ALTER TABLE "JobPosts" ADD "ReasonForVacancy" TEXT NULL;

ALTER TABLE "JobPosts" ADD "RequiredWws" TEXT NULL;

ALTER TABLE "JobPosts" ADD "xmin" xid NOT NULL DEFAULT 0;

CREATE TABLE "Notifications" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Notifications" PRIMARY KEY AUTOINCREMENT,
    "UserId" TEXT NOT NULL,
    "Role" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Message" TEXT NOT NULL,
    "Type" TEXT NOT NULL,
    "IsRead" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260702201315_AddressAndNotifications', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260704123412_AddPharmacyLicenseDocument', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "PharmacyFeedbacks" RENAME COLUMN "WouldBookAgain" TO "PharmacistId";

ALTER TABLE "PharmacistFeedbacks" RENAME COLUMN "WorkspaceSetupScore" TO "TimesheetConfirmed";

ALTER TABLE "PharmacyFeedbacks" ADD "AccuracyRating" INTEGER NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "ActualEnd" TEXT NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "ActualPauseMinutes" INTEGER NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "ActualStart" TEXT NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "CommunicationRating" INTEGER NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "CompetenceRating" INTEGER NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "Improvements" TEXT NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "IndependenceRating" INTEGER NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "NextDemand" TEXT NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "OnboardingRequired" TEXT NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "OverallGrade" TEXT NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "Positives" TEXT NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "Punctuality" TEXT NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "StressManagementRating" INTEGER NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "TeamworkRating" INTEGER NULL;

ALTER TABLE "PharmacyFeedbacks" ADD "WouldHireAgain" TEXT NULL;

ALTER TABLE "Pharmacists" ADD "BillingModel" TEXT NULL;

ALTER TABLE "Pharmacists" ADD "CountryOfLicense" TEXT NULL;

ALTER TABLE "Pharmacists" ADD "IsVatRequired" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Pharmacists" ADD "TravelCostModel" TEXT NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "ActualEnd" TEXT NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "ActualPauseMinutes" INTEGER NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "ActualStart" TEXT NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "BtmComplianceRating" INTEGER NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "Improvements" TEXT NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "OrganizationRating" INTEGER NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "OverallRating" INTEGER NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "Positives" TEXT NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "PrivacyRating" INTEGER NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "RelevantAreas" TEXT NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "SupportRating" INTEGER NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "WorkloadLevel" TEXT NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "WorkspacePrepRating" INTEGER NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "WouldWorkAgain" TEXT NULL;

ALTER TABLE "JobPosts" ADD "ShiftDetails" TEXT NULL;

CREATE INDEX "IX_PharmacyFeedbacks_JobPostId" ON "PharmacyFeedbacks" ("JobPostId");

CREATE INDEX "IX_PharmacyFeedbacks_PharmacistId" ON "PharmacyFeedbacks" ("PharmacistId");

CREATE INDEX "IX_PharmacistFeedbacks_JobPostId" ON "PharmacistFeedbacks" ("JobPostId");

CREATE INDEX "IX_PharmacistFeedbacks_PharmacistId" ON "PharmacistFeedbacks" ("PharmacistId");

CREATE TABLE "ef_temp_PharmacyFeedbacks" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PharmacyFeedbacks" PRIMARY KEY AUTOINCREMENT,
    "AccuracyRating" INTEGER NULL,
    "ActualEnd" TEXT NULL,
    "ActualPauseMinutes" INTEGER NULL,
    "ActualStart" TEXT NULL,
    "CommunicationRating" INTEGER NULL,
    "CompetenceRating" INTEGER NULL,
    "CreatedAt" TEXT NOT NULL,
    "Improvements" TEXT NULL,
    "IndependenceRating" INTEGER NULL,
    "JobPostId" INTEGER NOT NULL,
    "NextDemand" TEXT NULL,
    "OnboardingRequired" TEXT NULL,
    "OverallGrade" TEXT NULL,
    "PharmacistId" INTEGER NOT NULL,
    "Positives" TEXT NULL,
    "Punctuality" TEXT NULL,
    "StressManagementRating" INTEGER NULL,
    "TeamworkRating" INTEGER NULL,
    "WouldHireAgain" TEXT NULL,
    CONSTRAINT "FK_PharmacyFeedbacks_JobPosts_JobPostId" FOREIGN KEY ("JobPostId") REFERENCES "JobPosts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PharmacyFeedbacks_Pharmacists_PharmacistId" FOREIGN KEY ("PharmacistId") REFERENCES "Pharmacists" ("Id") ON DELETE CASCADE
);

INSERT INTO "ef_temp_PharmacyFeedbacks" ("Id", "AccuracyRating", "ActualEnd", "ActualPauseMinutes", "ActualStart", "CommunicationRating", "CompetenceRating", "CreatedAt", "Improvements", "IndependenceRating", "JobPostId", "NextDemand", "OnboardingRequired", "OverallGrade", "PharmacistId", "Positives", "Punctuality", "StressManagementRating", "TeamworkRating", "WouldHireAgain")
SELECT "Id", "AccuracyRating", "ActualEnd", "ActualPauseMinutes", "ActualStart", "CommunicationRating", "CompetenceRating", "CreatedAt", "Improvements", "IndependenceRating", "JobPostId", "NextDemand", "OnboardingRequired", "OverallGrade", "PharmacistId", "Positives", "Punctuality", "StressManagementRating", "TeamworkRating", "WouldHireAgain"
FROM "PharmacyFeedbacks";

CREATE TABLE "ef_temp_PharmacistFeedbacks" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PharmacistFeedbacks" PRIMARY KEY AUTOINCREMENT,
    "ActualEnd" TEXT NULL,
    "ActualPauseMinutes" INTEGER NULL,
    "ActualStart" TEXT NULL,
    "BtmComplianceRating" INTEGER NULL,
    "CreatedAt" TEXT NOT NULL,
    "CriticalIncidents" TEXT NULL,
    "Improvements" TEXT NULL,
    "JobPostId" INTEGER NOT NULL,
    "OrganizationRating" INTEGER NULL,
    "OverallRating" INTEGER NULL,
    "PharmacistId" INTEGER NOT NULL,
    "Positives" TEXT NULL,
    "PrivacyRating" INTEGER NULL,
    "RelevantAreas" TEXT NULL,
    "SupportRating" INTEGER NULL,
    "TimesheetConfirmed" INTEGER NOT NULL,
    "WorkloadLevel" TEXT NULL,
    "WorkspacePrepRating" INTEGER NULL,
    "WouldWorkAgain" TEXT NULL,
    CONSTRAINT "FK_PharmacistFeedbacks_JobPosts_JobPostId" FOREIGN KEY ("JobPostId") REFERENCES "JobPosts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PharmacistFeedbacks_Pharmacists_PharmacistId" FOREIGN KEY ("PharmacistId") REFERENCES "Pharmacists" ("Id") ON DELETE CASCADE
);

INSERT INTO "ef_temp_PharmacistFeedbacks" ("Id", "ActualEnd", "ActualPauseMinutes", "ActualStart", "BtmComplianceRating", "CreatedAt", "CriticalIncidents", "Improvements", "JobPostId", "OrganizationRating", "OverallRating", "PharmacistId", "Positives", "PrivacyRating", "RelevantAreas", "SupportRating", "TimesheetConfirmed", "WorkloadLevel", "WorkspacePrepRating", "WouldWorkAgain")
SELECT "Id", "ActualEnd", "ActualPauseMinutes", "ActualStart", "BtmComplianceRating", "CreatedAt", "CriticalIncidents", "Improvements", "JobPostId", "OrganizationRating", "OverallRating", "PharmacistId", "Positives", "PrivacyRating", "RelevantAreas", "SupportRating", "TimesheetConfirmed", "WorkloadLevel", "WorkspacePrepRating", "WouldWorkAgain"
FROM "PharmacistFeedbacks";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

DROP TABLE "PharmacyFeedbacks";

ALTER TABLE "ef_temp_PharmacyFeedbacks" RENAME TO "PharmacyFeedbacks";

DROP TABLE "PharmacistFeedbacks";

ALTER TABLE "ef_temp_PharmacistFeedbacks" RENAME TO "PharmacistFeedbacks";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;

CREATE INDEX "IX_PharmacyFeedbacks_JobPostId" ON "PharmacyFeedbacks" ("JobPostId");

CREATE INDEX "IX_PharmacyFeedbacks_PharmacistId" ON "PharmacyFeedbacks" ("PharmacistId");

CREATE INDEX "IX_PharmacistFeedbacks_JobPostId" ON "PharmacistFeedbacks" ("JobPostId");

CREATE INDEX "IX_PharmacistFeedbacks_PharmacistId" ON "PharmacistFeedbacks" ("PharmacistId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260704155831_AddFeedbackModels', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "Pharmacists" RENAME COLUMN "Address" TO "Street";

ALTER TABLE "Pharmacists" ADD "City" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacists" ADD "HouseNumber" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacists" ADD "PostalCode" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacists" ADD "ProfilePicturePath" TEXT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260704182034_SplitPharmacistAddress', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "Timesheets" ADD "DisputeReason" TEXT NULL;

ALTER TABLE "Timesheets" ADD "DisputedAt" TEXT NULL;

CREATE TABLE "ConsentAgreements" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ConsentAgreements" PRIMARY KEY AUTOINCREMENT,
    "PharmacyId" INTEGER NOT NULL,
    "PatientName" TEXT NOT NULL,
    "HealthInsuranceName" TEXT NOT NULL,
    "HealthInsuranceNumber" TEXT NOT NULL,
    "IkNumber" TEXT NOT NULL,
    "SignatureBlob" BLOB NOT NULL,
    "SignedDate" TEXT NOT NULL,
    CONSTRAINT "FK_ConsentAgreements_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "KioskTerminals" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_KioskTerminals" PRIMARY KEY AUTOINCREMENT,
    "PharmacyId" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "DeviceToken" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_KioskTerminals_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "SessionTelemetries" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SessionTelemetries" PRIMARY KEY AUTOINCREMENT,
    "PharmacyId" INTEGER NOT NULL,
    "SessionId" TEXT NOT NULL,
    "EventType" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_SessionTelemetries_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AtmBillingRecords" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AtmBillingRecords" PRIMARY KEY AUTOINCREMENT,
    "PharmacyId" INTEGER NOT NULL,
    "ConsentId" INTEGER NOT NULL,
    "ServiceType" TEXT NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "DateOfService" TEXT NOT NULL,
    "Sonderkennzeichen" TEXT NOT NULL,
    "ReportPath" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_AtmBillingRecords_ConsentAgreements_ConsentId" FOREIGN KEY ("ConsentId") REFERENCES "ConsentAgreements" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AtmBillingRecords_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AtmBillingRecords_ConsentId" ON "AtmBillingRecords" ("ConsentId");

CREATE INDEX "IX_AtmBillingRecords_PharmacyId" ON "AtmBillingRecords" ("PharmacyId");

CREATE INDEX "IX_ConsentAgreements_PharmacyId" ON "ConsentAgreements" ("PharmacyId");

CREATE INDEX "IX_KioskTerminals_PharmacyId" ON "KioskTerminals" ("PharmacyId");

CREATE INDEX "IX_SessionTelemetries_PharmacyId" ON "SessionTelemetries" ("PharmacyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260705095355_AddUnifiedMegaSchema_ATM', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

CREATE TABLE "Patients" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Patients" PRIMARY KEY AUTOINCREMENT,
    "PharmacyId" INTEGER NOT NULL,
    "KdnNr" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Vorname" TEXT NOT NULL,
    "Geburt" TEXT NOT NULL,
    "Gender" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Patients_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PdlServices" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PdlServices" PRIMARY KEY AUTOINCREMENT,
    "PatientId" INTEGER NOT NULL,
    "Status" TEXT NOT NULL,
    "ServiceType" TEXT NOT NULL,
    "AiAnalysisResultJson" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "PerformedAt" TEXT NULL,
    "BilledAt" TEXT NULL,
    CONSTRAINT "FK_PdlServices_Patients_PatientId" FOREIGN KEY ("PatientId") REFERENCES "Patients" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PdlDocuments" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PdlDocuments" PRIMARY KEY AUTOINCREMENT,
    "PatientId" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "PdlServiceId" INTEGER NOT NULL,
    "PdfUrl" TEXT NOT NULL,
    "BillingAmount" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_PdlDocuments_Patients_PatientId" FOREIGN KEY ("PatientId") REFERENCES "Patients" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PdlDocuments_PdlServices_PdlServiceId" FOREIGN KEY ("PdlServiceId") REFERENCES "PdlServices" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PdlDocuments_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Patients_PharmacyId" ON "Patients" ("PharmacyId");

CREATE INDEX "IX_PdlDocuments_PatientId" ON "PdlDocuments" ("PatientId");

CREATE INDEX "IX_PdlDocuments_PdlServiceId" ON "PdlDocuments" ("PdlServiceId");

CREATE INDEX "IX_PdlDocuments_PharmacyId" ON "PdlDocuments" ("PharmacyId");

CREATE INDEX "IX_PdlServices_PatientId" ON "PdlServices" ("PatientId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260705101841_AddUnifiedMegaSchema_PDL', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "Patients" ADD "IsEligibleForAmts" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Patients" ADD "MedicationCount" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Patients" ADD "MedicationsJson" TEXT NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260705120900_AddPatientMedicationCount', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "Pharmacies" ADD "UtmCampaign" TEXT NULL;

ALTER TABLE "Pharmacies" ADD "UtmMedium" TEXT NULL;

ALTER TABLE "Pharmacies" ADD "UtmSource" TEXT NULL;

ALTER TABLE "Pharmacies" ADD "UtmTerm" TEXT NULL;

CREATE TABLE "ef_temp_JobPosts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_JobPosts" PRIMARY KEY AUTOINCREMENT,
    "CreatedAt" TEXT NOT NULL,
    "Description" TEXT NULL,
    "EndDate" TEXT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "ReasonForVacancy" TEXT NULL,
    "RequiredQualifications" TEXT NULL,
    "RequiredWws" TEXT NULL,
    "Salary" TEXT NULL,
    "ShiftDetails" TEXT NULL,
    "StartDate" TEXT NULL,
    "Status" TEXT NULL,
    "Title" TEXT NULL,
    CONSTRAINT "FK_JobPosts_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

INSERT INTO "ef_temp_JobPosts" ("Id", "CreatedAt", "Description", "EndDate", "PharmacyId", "ReasonForVacancy", "RequiredQualifications", "RequiredWws", "Salary", "ShiftDetails", "StartDate", "Status", "Title")
SELECT "Id", "CreatedAt", "Description", "EndDate", "PharmacyId", "ReasonForVacancy", "RequiredQualifications", "RequiredWws", "Salary", "ShiftDetails", "StartDate", "Status", "Title"
FROM "JobPosts";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

DROP TABLE "JobPosts";

ALTER TABLE "ef_temp_JobPosts" RENAME TO "JobPosts";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;

CREATE INDEX "IX_JobPosts_PharmacyId" ON "JobPosts" ("PharmacyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260705130455_AddUtmTrackingToPharmacy', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "ConsentAgreements" ADD "IsTelepharmacyConsentGranted" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "ConsentAgreements" ADD "IsWwsExportGranted" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260705180047_AddKioskConsentFlags', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "Pharmacies" ADD "CreatedAt" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260706090839_AddPharmacyCreatedAt', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

CREATE TABLE "PharmacyEmployees" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PharmacyEmployees" PRIMARY KEY AUTOINCREMENT,
    "PharmacyId" INTEGER NOT NULL,
    "FirstName" TEXT NOT NULL,
    "LastName" TEXT NOT NULL,
    "Role" TEXT NOT NULL,
    "ColorCode" TEXT NOT NULL,
    CONSTRAINT "FK_PharmacyEmployees_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "InternalShifts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_InternalShifts" PRIMARY KEY AUTOINCREMENT,
    "PharmacyId" INTEGER NOT NULL,
    "PharmacyEmployeeId" INTEGER NOT NULL,
    "Date" TEXT NOT NULL,
    "StartTime" TEXT NOT NULL,
    "EndTime" TEXT NOT NULL,
    "IsEmergencyDuty" INTEGER NOT NULL,
    CONSTRAINT "FK_InternalShifts_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_InternalShifts_PharmacyEmployees_PharmacyEmployeeId" FOREIGN KEY ("PharmacyEmployeeId") REFERENCES "PharmacyEmployees" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_InternalShifts_PharmacyEmployeeId" ON "InternalShifts" ("PharmacyEmployeeId");

CREATE INDEX "IX_InternalShifts_PharmacyId" ON "InternalShifts" ("PharmacyId");

CREATE INDEX "IX_PharmacyEmployees_PharmacyId" ON "PharmacyEmployees" ("PharmacyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260712132520_AddDienstplan', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "Pharmacists" ADD "AugContractStatus" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacists" ADD "IsApprobationVerified" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Pharmacists" ADD "UstIdValidationStatus" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacies" ADD "AugContractStatus" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacies" ADD "UstIdNr" TEXT NULL;

ALTER TABLE "Pharmacies" ADD "UstIdValidationStatus" TEXT NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260712152741_AddComplianceAndLegalTracking', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

CREATE TABLE "Consumers" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Consumers" PRIMARY KEY AUTOINCREMENT,
    "Email" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "FirstName" TEXT NOT NULL,
    "LastName" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "HasAcceptedBgbWaiver" INTEGER NOT NULL,
    "BgbWaiverAcceptedAt" TEXT NULL
);

CREATE TABLE "Holidays" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Holidays" PRIMARY KEY AUTOINCREMENT,
    "Date" date NOT NULL,
    "Name" TEXT NOT NULL,
    "StateCode" TEXT NOT NULL
);

CREATE TABLE "SaturdayRotationTeams" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SaturdayRotationTeams" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "PharmacistIds" TEXT NOT NULL,
    CONSTRAINT "FK_SaturdayRotationTeams_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "SaturdayRotations" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SaturdayRotations" PRIMARY KEY AUTOINCREMENT,
    "Date" date NOT NULL,
    "TeamId" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    CONSTRAINT "FK_SaturdayRotations_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SaturdayRotations_SaturdayRotationTeams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "SaturdayRotationTeams" ("Id") ON DELETE CASCADE
);

INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (1, '2026-01-01 00:00:00', 'Neujahrstag', 'DE');
SELECT changes();

INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (2, '2026-04-03 00:00:00', 'Karfreitag', 'DE');
SELECT changes();

INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (3, '2026-04-06 00:00:00', 'Ostermontag', 'DE');
SELECT changes();

INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (4, '2026-05-01 00:00:00', 'Tag der Arbeit', 'DE');
SELECT changes();

INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (5, '2026-05-14 00:00:00', 'Christi Himmelfahrt', 'DE');
SELECT changes();

INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (6, '2026-05-25 00:00:00', 'Pfingstmontag', 'DE');
SELECT changes();

INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (7, '2026-10-03 00:00:00', 'Tag der Deutschen Einheit', 'DE');
SELECT changes();

INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (8, '2026-12-25 00:00:00', '1. Weihnachtstag', 'DE');
SELECT changes();

INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (9, '2026-12-26 00:00:00', '2. Weihnachtstag', 'DE');
SELECT changes();


CREATE INDEX "IX_SaturdayRotations_PharmacyId" ON "SaturdayRotations" ("PharmacyId");

CREATE INDEX "IX_SaturdayRotations_TeamId" ON "SaturdayRotations" ("TeamId");

CREATE INDEX "IX_SaturdayRotationTeams_PharmacyId" ON "SaturdayRotationTeams" ("PharmacyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260712160829_Phase5_And_Scheduling', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "Pharmacists" ADD "AugContractDocumentPath" TEXT NULL;

ALTER TABLE "Pharmacies" ADD "AugContractDocumentPath" TEXT NULL;

ALTER TABLE "Pharmacies" ADD "IsTelepharmacyConsentGranted" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Pharmacies" ADD "TelepharmacyConsentDocumentPath" TEXT NULL;

CREATE TABLE "DeviceTokens" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DeviceTokens" PRIMARY KEY AUTOINCREMENT,
    "PharmacistId" INTEGER NOT NULL,
    "FcmToken" TEXT NOT NULL,
    "DevicePlatform" TEXT NOT NULL,
    "DeviceId" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "LastActive" TEXT NOT NULL,
    CONSTRAINT "FK_DeviceTokens_Pharmacists_PharmacistId" FOREIGN KEY ("PharmacistId") REFERENCES "Pharmacists" ("Id") ON DELETE CASCADE
);

CREATE TABLE "MobileRefreshTokens" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_MobileRefreshTokens" PRIMARY KEY AUTOINCREMENT,
    "PharmacistId" INTEGER NOT NULL,
    "TokenHash" TEXT NOT NULL,
    "DeviceId" TEXT NOT NULL,
    "ExpiresAt" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "RevokedAt" TEXT NULL,
    CONSTRAINT "FK_MobileRefreshTokens_Pharmacists_PharmacistId" FOREIGN KEY ("PharmacistId") REFERENCES "Pharmacists" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_DeviceTokens_PharmacistId" ON "DeviceTokens" ("PharmacistId");

CREATE INDEX "IX_MobileRefreshTokens_PharmacistId" ON "MobileRefreshTokens" ("PharmacistId");

CREATE TABLE "ef_temp_ConsentAgreements" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ConsentAgreements" PRIMARY KEY AUTOINCREMENT,
    "HealthInsuranceName" TEXT NOT NULL,
    "HealthInsuranceNumber" TEXT NOT NULL,
    "IkNumber" TEXT NOT NULL,
    "IsWwsExportGranted" INTEGER NOT NULL,
    "PatientName" TEXT NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "SignatureBlob" BLOB NOT NULL,
    "SignedDate" TEXT NOT NULL,
    CONSTRAINT "FK_ConsentAgreements_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

INSERT INTO "ef_temp_ConsentAgreements" ("Id", "HealthInsuranceName", "HealthInsuranceNumber", "IkNumber", "IsWwsExportGranted", "PatientName", "PharmacyId", "SignatureBlob", "SignedDate")
SELECT "Id", "HealthInsuranceName", "HealthInsuranceNumber", "IkNumber", "IsWwsExportGranted", "PatientName", "PharmacyId", "SignatureBlob", "SignedDate"
FROM "ConsentAgreements";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

DROP TABLE "ConsentAgreements";

ALTER TABLE "ef_temp_ConsentAgreements" RENAME TO "ConsentAgreements";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;

CREATE INDEX "IX_ConsentAgreements_PharmacyId" ON "ConsentAgreements" ("PharmacyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260713040543_MobileGateway', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "Patients" RENAME COLUMN "Vorname" TO "IvBase64";

ALTER TABLE "Patients" RENAME COLUMN "Name" TO "CiphertextBase64";

CREATE TABLE "ef_temp_Patients" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Patients" PRIMARY KEY AUTOINCREMENT,
    "CiphertextBase64" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "IvBase64" TEXT NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    CONSTRAINT "FK_Patients_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

INSERT INTO "ef_temp_Patients" ("Id", "CiphertextBase64", "CreatedAt", "IvBase64", "PharmacyId")
SELECT "Id", "CiphertextBase64", "CreatedAt", "IvBase64", "PharmacyId"
FROM "Patients";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

DROP TABLE "Patients";

ALTER TABLE "ef_temp_Patients" RENAME TO "Patients";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;

CREATE INDEX "IX_Patients_PharmacyId" ON "Patients" ("PharmacyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260713105141_E2EE_PatientData', '8.0.4');

COMMIT;

