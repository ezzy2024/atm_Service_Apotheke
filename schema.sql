CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Pharmacies" (
    "Id" INTEGER NOT NULL,
    "PharmacyName" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "PhoneNumber" TEXT NOT NULL,
    "Address" TEXT NOT NULL,
    "LicenseNumber" TEXT NOT NULL,
    "IsEmailConfirmed" INTEGER NOT NULL,
    "EmailConfirmationToken" TEXT,
    "IsVerified" INTEGER NOT NULL,
    "ContactPerson" TEXT,
    "SoftwareSystem" TEXT,
    "FocusAreas" TEXT,
    "StaffSupport" TEXT,
    "InvoiceBillingPossible" INTEGER NOT NULL,
    "TargetHourlyRate" TEXT,
    "ParkingAvailable" INTEGER NOT NULL,
    "AccommodationProvided" TEXT,
    "DataProcessingAgreementSignedAt" TEXT,
    "GdprAnonymizedAt" TEXT,
    CONSTRAINT "PK_Pharmacies" PRIMARY KEY ("Id")
);

CREATE TABLE "PharmacistFeedbacks" (
    "Id" INTEGER NOT NULL,
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
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "PK_PharmacistFeedbacks" PRIMARY KEY ("Id")
);

CREATE TABLE "Pharmacists" (
    "Id" INTEGER NOT NULL,
    "MaxDistanceKm" INTEGER NOT NULL,
    "AvailableDaysPerWeek" INTEGER NOT NULL,
    "FullName" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "PhoneNumber" TEXT NOT NULL,
    "Address" TEXT NOT NULL,
    "IsEmailConfirmed" INTEGER NOT NULL,
    "EmailConfirmationToken" TEXT,
    "IsVerified" INTEGER NOT NULL,
    "PreferredContactMethod" TEXT,
    "HasApprobation" INTEGER NOT NULL,
    "ApprobationCountry" TEXT,
    "ExperienceYears" TEXT,
    "Specialties" TEXT,
    "SoftwareExperience" TEXT,
    "RadiusKm" INTEGER NOT NULL,
    "PreferredStates" TEXT,
    "TravelWillingness" TEXT,
    "Mobility" TEXT,
    "AvailabilityType" TEXT,
    "ShortNoticeAvailability" TEXT,
    "EmergencyServiceWillingness" INTEGER NOT NULL,
    "WeekendWillingness" INTEGER NOT NULL,
    "FeeModel" TEXT,
    "HourlyRate" TEXT NOT NULL,
    "VatSubject" TEXT,
    "TravelExpenses" TEXT,
    "ApprobationDocumentPath" TEXT,
    "CvDocumentPath" TEXT,
    "IsKycVerified" INTEGER NOT NULL,
    "IdCardDocumentPath" TEXT,
    "LiabilityInsuranceDocumentPath" TEXT,
    "TaxId" TEXT,
    "GdprAnonymizedAt" TEXT,
    "TermsAcceptedAt" TEXT,
    CONSTRAINT "PK_Pharmacists" PRIMARY KEY ("Id")
);

CREATE TABLE "PharmacyFeedbacks" (
    "Id" INTEGER NOT NULL,
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
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "PK_PharmacyFeedbacks" PRIMARY KEY ("Id")
);

CREATE TABLE "JobPosts" (
    "Id" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "RequestType" TEXT NOT NULL,
    "Urgency" TEXT NOT NULL,
    "StartDate" TEXT NOT NULL,
    "EndDate" TEXT,
    "StartTime" TEXT NOT NULL,
    "EndTime" TEXT NOT NULL,
    "SoftwareSystem" TEXT NOT NULL,
    "FocusAreas" TEXT NOT NULL,
    "Salary" TEXT NOT NULL,
    "Accommodation" TEXT NOT NULL,
    "BillingByInvoice" INTEGER NOT NULL,
    "ParkingAvailable" INTEGER NOT NULL,
    "Notes" TEXT,
    "Status" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "PK_JobPosts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_JobPosts_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "JobApplications" (
    "Id" INTEGER NOT NULL,
    "JobPostId" INTEGER NOT NULL,
    "PharmacistId" INTEGER NOT NULL,
    "Status" TEXT NOT NULL,
    "AppliedAt" TEXT NOT NULL,
    "TimesheetPath" TEXT,
    CONSTRAINT "PK_JobApplications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_JobApplications_JobPosts_JobPostId" FOREIGN KEY ("JobPostId") REFERENCES "JobPosts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_JobApplications_Pharmacists_PharmacistId" FOREIGN KEY ("PharmacistId") REFERENCES "Pharmacists" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Timesheets" (
    "Id" INTEGER NOT NULL,
    "JobApplicationId" INTEGER NOT NULL,
    "ActualStartDate" TEXT NOT NULL,
    "ActualStartTime" TEXT NOT NULL,
    "ActualEndTime" TEXT NOT NULL,
    "HourlyRate" TEXT NOT NULL,
    "TravelCosts" TEXT NOT NULL,
    "AccommodationCosts" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    CONSTRAINT "PK_Timesheets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Timesheets_JobApplications_JobApplicationId" FOREIGN KEY ("JobApplicationId") REFERENCES "JobApplications" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Invoices" (
    "Id" INTEGER NOT NULL,
    "InvoiceNumber" TEXT NOT NULL,
    "TimesheetId" INTEGER NOT NULL,
    "Type" TEXT NOT NULL,
    "TotalAmount" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "PdfFilePath" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "PK_Invoices" PRIMARY KEY ("Id"),
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

START TRANSACTION;

ALTER TABLE "JobPosts" DROP COLUMN "Accommodation";

ALTER TABLE "JobPosts" DROP COLUMN "BillingByInvoice";

ALTER TABLE "JobPosts" DROP COLUMN "EndTime";

ALTER TABLE "JobPosts" DROP COLUMN "FocusAreas";

ALTER TABLE "JobPosts" DROP COLUMN "Notes";

ALTER TABLE "JobPosts" DROP COLUMN "ParkingAvailable";

ALTER TABLE "JobPosts" DROP COLUMN "RequestType";

ALTER TABLE "JobPosts" RENAME COLUMN "Urgency" TO "Title";

ALTER TABLE "JobPosts" RENAME COLUMN "StartTime" TO "RequiredQualifications";

ALTER TABLE "JobPosts" RENAME COLUMN "SoftwareSystem" TO "Description";

UPDATE "JobPosts" SET "EndDate" = '0001-01-01 00:00:00' WHERE "EndDate" IS NULL;
ALTER TABLE "JobPosts" ALTER COLUMN "EndDate" SET NOT NULL;
ALTER TABLE "JobPosts" ALTER COLUMN "EndDate" SET DEFAULT '0001-01-01 00:00:00';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260623074307_SeedInitialTimesheet', '8.0.4');

COMMIT;

START TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260629142041_AddJobDescriptionColumn', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "JobPosts" ALTER COLUMN "Title" DROP NOT NULL;

ALTER TABLE "JobPosts" ALTER COLUMN "Status" DROP NOT NULL;

ALTER TABLE "JobPosts" ALTER COLUMN "StartDate" DROP NOT NULL;

ALTER TABLE "JobPosts" ALTER COLUMN "Salary" DROP NOT NULL;

ALTER TABLE "JobPosts" ALTER COLUMN "RequiredQualifications" DROP NOT NULL;

ALTER TABLE "JobPosts" ALTER COLUMN "EndDate" DROP NOT NULL;

ALTER TABLE "JobPosts" ALTER COLUMN "Description" DROP NOT NULL;

ALTER TABLE "Invoices" ADD "PaidAt" TEXT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260701140229_AddInvoicePaidAt', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Pharmacies" ADD "ApiKey" TEXT;

CREATE TABLE "TemperatureLogs" (
    "Id" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "Temperature" REAL NOT NULL,
    "RecordedAt" TEXT NOT NULL,
    "IsAnomaly" INTEGER NOT NULL,
    CONSTRAINT "PK_TemperatureLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TemperatureLogs_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_TemperatureLogs_PharmacyId" ON "TemperatureLogs" ("PharmacyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260701142421_AddTemperatureLog', '8.0.4');

COMMIT;

START TRANSACTION;

CREATE TABLE "AuditLogs" (
    "Id" INTEGER NOT NULL,
    "EntityName" TEXT NOT NULL,
    "EntityId" TEXT NOT NULL,
    "Action" TEXT NOT NULL,
    "Changes" jsonb,
    "Timestamp" TEXT NOT NULL,
    "PerformedBy" TEXT,
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260701170308_AddAuditLogging', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Pharmacies" RENAME COLUMN "Address" TO "Street";

ALTER TABLE "Pharmacists" ADD "Latitude" REAL;

ALTER TABLE "Pharmacists" ADD "Longitude" REAL;

ALTER TABLE "Pharmacists" ADD "Qualification" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacists" ADD "WwsProficiency" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacies" ADD "City" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacies" ADD "HouseNumber" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacies" ADD "Latitude" REAL;

ALTER TABLE "Pharmacies" ADD "Longitude" REAL;

ALTER TABLE "Pharmacies" ADD "PostalCode" TEXT NOT NULL DEFAULT '';

ALTER TABLE "JobPosts" ADD "ReasonForVacancy" TEXT;

ALTER TABLE "JobPosts" ADD "RequiredWws" TEXT;

CREATE TABLE "Notifications" (
    "Id" INTEGER NOT NULL,
    "UserId" TEXT NOT NULL,
    "Role" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Message" TEXT NOT NULL,
    "Type" TEXT NOT NULL,
    "IsRead" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260702201315_AddressAndNotifications', '8.0.4');

COMMIT;

START TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260704123412_AddPharmacyLicenseDocument', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "PharmacyFeedbacks" DROP COLUMN "ActualEndTime";

ALTER TABLE "PharmacyFeedbacks" DROP COLUMN "ActualStartTime";

ALTER TABLE "PharmacyFeedbacks" DROP COLUMN "CarefulnessScore";

ALTER TABLE "PharmacyFeedbacks" DROP COLUMN "CompetenceScore";

ALTER TABLE "PharmacyFeedbacks" DROP COLUMN "ImprovementAspects";

ALTER TABLE "PharmacyFeedbacks" DROP COLUMN "IndependenceScore";

ALTER TABLE "PharmacyFeedbacks" DROP COLUMN "OverallScore";

ALTER TABLE "PharmacyFeedbacks" DROP COLUMN "PharmacyId";

ALTER TABLE "PharmacyFeedbacks" DROP COLUMN "PositiveAspects";

ALTER TABLE "PharmacyFeedbacks" DROP COLUMN "StressHandlingScore";

ALTER TABLE "PharmacyFeedbacks" DROP COLUMN "TeamworkScore";

ALTER TABLE "PharmacistFeedbacks" DROP COLUMN "BtmProcessScore";

ALTER TABLE "PharmacistFeedbacks" DROP COLUMN "DataProtectionScore";

ALTER TABLE "PharmacistFeedbacks" DROP COLUMN "OnboardingScore";

ALTER TABLE "PharmacistFeedbacks" DROP COLUMN "OverallScore";

ALTER TABLE "PharmacistFeedbacks" DROP COLUMN "PositiveAspects";

ALTER TABLE "PharmacistFeedbacks" DROP COLUMN "WorkloadHV";

ALTER TABLE "PharmacistFeedbacks" DROP COLUMN "WorkloadRecipe";

ALTER TABLE "PharmacyFeedbacks" RENAME COLUMN "WouldBookAgain" TO "PharmacistId";

ALTER TABLE "PharmacistFeedbacks" RENAME COLUMN "WorkspaceSetupScore" TO "TimesheetConfirmed";

ALTER TABLE "PharmacyFeedbacks" ADD "AccuracyRating" INTEGER;

ALTER TABLE "PharmacyFeedbacks" ADD "ActualEnd" TEXT;

ALTER TABLE "PharmacyFeedbacks" ADD "ActualPauseMinutes" INTEGER;

ALTER TABLE "PharmacyFeedbacks" ADD "ActualStart" TEXT;

ALTER TABLE "PharmacyFeedbacks" ADD "CommunicationRating" INTEGER;

ALTER TABLE "PharmacyFeedbacks" ADD "CompetenceRating" INTEGER;

ALTER TABLE "PharmacyFeedbacks" ADD "Improvements" TEXT;

ALTER TABLE "PharmacyFeedbacks" ADD "IndependenceRating" INTEGER;

ALTER TABLE "PharmacyFeedbacks" ADD "NextDemand" TEXT;

ALTER TABLE "PharmacyFeedbacks" ADD "OnboardingRequired" TEXT;

ALTER TABLE "PharmacyFeedbacks" ADD "OverallGrade" TEXT;

ALTER TABLE "PharmacyFeedbacks" ADD "Positives" TEXT;

ALTER TABLE "PharmacyFeedbacks" ADD "Punctuality" TEXT;

ALTER TABLE "PharmacyFeedbacks" ADD "StressManagementRating" INTEGER;

ALTER TABLE "PharmacyFeedbacks" ADD "TeamworkRating" INTEGER;

ALTER TABLE "PharmacyFeedbacks" ADD "WouldHireAgain" TEXT;

ALTER TABLE "Pharmacists" ADD "BillingModel" TEXT;

ALTER TABLE "Pharmacists" ADD "CountryOfLicense" TEXT;

ALTER TABLE "Pharmacists" ADD "IsVatRequired" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Pharmacists" ADD "TravelCostModel" TEXT;

ALTER TABLE "PharmacistFeedbacks" ALTER COLUMN "CriticalIncidents" DROP NOT NULL;

ALTER TABLE "PharmacistFeedbacks" ADD "ActualEnd" TEXT;

ALTER TABLE "PharmacistFeedbacks" ADD "ActualPauseMinutes" INTEGER;

ALTER TABLE "PharmacistFeedbacks" ADD "ActualStart" TEXT;

ALTER TABLE "PharmacistFeedbacks" ADD "BtmComplianceRating" INTEGER;

ALTER TABLE "PharmacistFeedbacks" ADD "Improvements" TEXT;

ALTER TABLE "PharmacistFeedbacks" ADD "OrganizationRating" INTEGER;

ALTER TABLE "PharmacistFeedbacks" ADD "OverallRating" INTEGER;

ALTER TABLE "PharmacistFeedbacks" ADD "Positives" TEXT;

ALTER TABLE "PharmacistFeedbacks" ADD "PrivacyRating" INTEGER;

ALTER TABLE "PharmacistFeedbacks" ADD "RelevantAreas" TEXT;

ALTER TABLE "PharmacistFeedbacks" ADD "SupportRating" INTEGER;

ALTER TABLE "PharmacistFeedbacks" ADD "WorkloadLevel" TEXT;

ALTER TABLE "PharmacistFeedbacks" ADD "WorkspacePrepRating" INTEGER;

ALTER TABLE "PharmacistFeedbacks" ADD "WouldWorkAgain" TEXT;

ALTER TABLE "JobPosts" ADD "ShiftDetails" TEXT;

CREATE INDEX "IX_PharmacyFeedbacks_JobPostId" ON "PharmacyFeedbacks" ("JobPostId");

CREATE INDEX "IX_PharmacyFeedbacks_PharmacistId" ON "PharmacyFeedbacks" ("PharmacistId");

CREATE INDEX "IX_PharmacistFeedbacks_JobPostId" ON "PharmacistFeedbacks" ("JobPostId");

CREATE INDEX "IX_PharmacistFeedbacks_PharmacistId" ON "PharmacistFeedbacks" ("PharmacistId");

ALTER TABLE "PharmacistFeedbacks" ADD CONSTRAINT "FK_PharmacistFeedbacks_JobPosts_JobPostId" FOREIGN KEY ("JobPostId") REFERENCES "JobPosts" ("Id") ON DELETE CASCADE;

ALTER TABLE "PharmacistFeedbacks" ADD CONSTRAINT "FK_PharmacistFeedbacks_Pharmacists_PharmacistId" FOREIGN KEY ("PharmacistId") REFERENCES "Pharmacists" ("Id") ON DELETE CASCADE;

ALTER TABLE "PharmacyFeedbacks" ADD CONSTRAINT "FK_PharmacyFeedbacks_JobPosts_JobPostId" FOREIGN KEY ("JobPostId") REFERENCES "JobPosts" ("Id") ON DELETE CASCADE;

ALTER TABLE "PharmacyFeedbacks" ADD CONSTRAINT "FK_PharmacyFeedbacks_Pharmacists_PharmacistId" FOREIGN KEY ("PharmacistId") REFERENCES "Pharmacists" ("Id") ON DELETE CASCADE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260704155831_AddFeedbackModels', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Pharmacists" RENAME COLUMN "Address" TO "Street";

ALTER TABLE "Pharmacists" ADD "City" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacists" ADD "HouseNumber" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacists" ADD "PostalCode" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacists" ADD "ProfilePicturePath" TEXT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260704182034_SplitPharmacistAddress', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Timesheets" ADD "DisputeReason" TEXT;

ALTER TABLE "Timesheets" ADD "DisputedAt" TEXT;

CREATE TABLE "ConsentAgreements" (
    "Id" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "PatientName" TEXT NOT NULL,
    "HealthInsuranceName" TEXT NOT NULL,
    "HealthInsuranceNumber" TEXT NOT NULL,
    "IkNumber" TEXT NOT NULL,
    "SignatureBlob" BLOB NOT NULL,
    "SignedDate" TEXT NOT NULL,
    CONSTRAINT "PK_ConsentAgreements" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ConsentAgreements_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "KioskTerminals" (
    "Id" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "DeviceToken" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "PK_KioskTerminals" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_KioskTerminals_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "SessionTelemetries" (
    "Id" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "SessionId" TEXT NOT NULL,
    "EventType" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "PK_SessionTelemetries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SessionTelemetries_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AtmBillingRecords" (
    "Id" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "ConsentId" INTEGER NOT NULL,
    "ServiceType" TEXT NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "DateOfService" TEXT NOT NULL,
    "Sonderkennzeichen" TEXT NOT NULL,
    "ReportPath" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "PK_AtmBillingRecords" PRIMARY KEY ("Id"),
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

START TRANSACTION;

CREATE TABLE "Patients" (
    "Id" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "KdnNr" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Vorname" TEXT NOT NULL,
    "Geburt" TEXT NOT NULL,
    "Gender" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "PK_Patients" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Patients_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PdlServices" (
    "Id" INTEGER NOT NULL,
    "PatientId" INTEGER NOT NULL,
    "Status" TEXT NOT NULL,
    "ServiceType" TEXT NOT NULL,
    "AiAnalysisResultJson" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "PerformedAt" TEXT,
    "BilledAt" TEXT,
    CONSTRAINT "PK_PdlServices" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PdlServices_Patients_PatientId" FOREIGN KEY ("PatientId") REFERENCES "Patients" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PdlDocuments" (
    "Id" INTEGER NOT NULL,
    "PatientId" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "PdlServiceId" INTEGER NOT NULL,
    "PdfUrl" TEXT NOT NULL,
    "BillingAmount" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "PK_PdlDocuments" PRIMARY KEY ("Id"),
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

START TRANSACTION;

ALTER TABLE "Patients" ADD "IsEligibleForAmts" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Patients" ADD "MedicationCount" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Patients" ADD "MedicationsJson" TEXT NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260705120900_AddPatientMedicationCount', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Pharmacies" ADD "UtmCampaign" TEXT;

ALTER TABLE "Pharmacies" ADD "UtmMedium" TEXT;

ALTER TABLE "Pharmacies" ADD "UtmSource" TEXT;

ALTER TABLE "Pharmacies" ADD "UtmTerm" TEXT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260705130455_AddUtmTrackingToPharmacy', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "ConsentAgreements" ADD "IsTelepharmacyConsentGranted" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "ConsentAgreements" ADD "IsWwsExportGranted" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260705180047_AddKioskConsentFlags', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Pharmacies" ADD "CreatedAt" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260706090839_AddPharmacyCreatedAt', '8.0.4');

COMMIT;

START TRANSACTION;

CREATE TABLE "PharmacyEmployees" (
    "Id" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "FirstName" TEXT NOT NULL,
    "LastName" TEXT NOT NULL,
    "Role" TEXT NOT NULL,
    "ColorCode" TEXT NOT NULL,
    CONSTRAINT "PK_PharmacyEmployees" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PharmacyEmployees_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "InternalShifts" (
    "Id" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "PharmacyEmployeeId" INTEGER NOT NULL,
    "Date" TEXT NOT NULL,
    "StartTime" TEXT NOT NULL,
    "EndTime" TEXT NOT NULL,
    "IsEmergencyDuty" INTEGER NOT NULL,
    CONSTRAINT "PK_InternalShifts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_InternalShifts_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_InternalShifts_PharmacyEmployees_PharmacyEmployeeId" FOREIGN KEY ("PharmacyEmployeeId") REFERENCES "PharmacyEmployees" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_InternalShifts_PharmacyEmployeeId" ON "InternalShifts" ("PharmacyEmployeeId");

CREATE INDEX "IX_InternalShifts_PharmacyId" ON "InternalShifts" ("PharmacyId");

CREATE INDEX "IX_PharmacyEmployees_PharmacyId" ON "PharmacyEmployees" ("PharmacyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260712132520_AddDienstplan', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Pharmacists" ADD "AugContractStatus" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacists" ADD "IsApprobationVerified" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Pharmacists" ADD "UstIdValidationStatus" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacies" ADD "AugContractStatus" TEXT NOT NULL DEFAULT '';

ALTER TABLE "Pharmacies" ADD "UstIdNr" TEXT;

ALTER TABLE "Pharmacies" ADD "UstIdValidationStatus" TEXT NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260712152741_AddComplianceAndLegalTracking', '8.0.4');

COMMIT;

START TRANSACTION;

CREATE TABLE "Consumers" (
    "Id" INTEGER NOT NULL,
    "Email" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "FirstName" TEXT NOT NULL,
    "LastName" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "HasAcceptedBgbWaiver" INTEGER NOT NULL,
    "BgbWaiverAcceptedAt" TEXT,
    CONSTRAINT "PK_Consumers" PRIMARY KEY ("Id")
);

CREATE TABLE "Holidays" (
    "Id" INTEGER NOT NULL,
    "Date" date NOT NULL,
    "Name" TEXT NOT NULL,
    "StateCode" TEXT NOT NULL,
    CONSTRAINT "PK_Holidays" PRIMARY KEY ("Id")
);

CREATE TABLE "SaturdayRotationTeams" (
    "Id" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    "PharmacistIds" TEXT NOT NULL,
    CONSTRAINT "PK_SaturdayRotationTeams" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SaturdayRotationTeams_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "SaturdayRotations" (
    "Id" INTEGER NOT NULL,
    "Date" date NOT NULL,
    "TeamId" INTEGER NOT NULL,
    "PharmacyId" INTEGER NOT NULL,
    CONSTRAINT "PK_SaturdayRotations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SaturdayRotations_Pharmacies_PharmacyId" FOREIGN KEY ("PharmacyId") REFERENCES "Pharmacies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SaturdayRotations_SaturdayRotationTeams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "SaturdayRotationTeams" ("Id") ON DELETE CASCADE
);

INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (1, DATE '2026-01-01', 'Neujahrstag', 'DE');
INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (2, DATE '2026-04-03', 'Karfreitag', 'DE');
INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (3, DATE '2026-04-06', 'Ostermontag', 'DE');
INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (4, DATE '2026-05-01', 'Tag der Arbeit', 'DE');
INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (5, DATE '2026-05-14', 'Christi Himmelfahrt', 'DE');
INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (6, DATE '2026-05-25', 'Pfingstmontag', 'DE');
INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (7, DATE '2026-10-03', 'Tag der Deutschen Einheit', 'DE');
INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (8, DATE '2026-12-25', '1. Weihnachtstag', 'DE');
INSERT INTO "Holidays" ("Id", "Date", "Name", "StateCode")
VALUES (9, DATE '2026-12-26', '2. Weihnachtstag', 'DE');

CREATE INDEX "IX_SaturdayRotations_PharmacyId" ON "SaturdayRotations" ("PharmacyId");

CREATE INDEX "IX_SaturdayRotations_TeamId" ON "SaturdayRotations" ("TeamId");

CREATE INDEX "IX_SaturdayRotationTeams_PharmacyId" ON "SaturdayRotationTeams" ("PharmacyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260712160829_Phase5_And_Scheduling', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "ConsentAgreements" DROP COLUMN "IsTelepharmacyConsentGranted";

ALTER TABLE "Pharmacists" ADD "AugContractDocumentPath" TEXT;

ALTER TABLE "Pharmacies" ADD "AugContractDocumentPath" TEXT;

ALTER TABLE "Pharmacies" ADD "IsTelepharmacyConsentGranted" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Pharmacies" ADD "TelepharmacyConsentDocumentPath" TEXT;

CREATE TABLE "DeviceTokens" (
    "Id" INTEGER NOT NULL,
    "PharmacistId" INTEGER NOT NULL,
    "FcmToken" TEXT NOT NULL,
    "DevicePlatform" TEXT NOT NULL,
    "DeviceId" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "LastActive" TEXT NOT NULL,
    CONSTRAINT "PK_DeviceTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DeviceTokens_Pharmacists_PharmacistId" FOREIGN KEY ("PharmacistId") REFERENCES "Pharmacists" ("Id") ON DELETE CASCADE
);

CREATE TABLE "MobileRefreshTokens" (
    "Id" INTEGER NOT NULL,
    "PharmacistId" INTEGER NOT NULL,
    "TokenHash" TEXT NOT NULL,
    "DeviceId" TEXT NOT NULL,
    "ExpiresAt" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "RevokedAt" TEXT,
    CONSTRAINT "PK_MobileRefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MobileRefreshTokens_Pharmacists_PharmacistId" FOREIGN KEY ("PharmacistId") REFERENCES "Pharmacists" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_DeviceTokens_PharmacistId" ON "DeviceTokens" ("PharmacistId");

CREATE INDEX "IX_MobileRefreshTokens_PharmacistId" ON "MobileRefreshTokens" ("PharmacistId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260713040543_MobileGateway', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Patients" DROP COLUMN "Geburt";

ALTER TABLE "Patients" DROP COLUMN "Gender";

ALTER TABLE "Patients" DROP COLUMN "IsEligibleForAmts";

ALTER TABLE "Patients" DROP COLUMN "KdnNr";

ALTER TABLE "Patients" DROP COLUMN "MedicationCount";

ALTER TABLE "Patients" DROP COLUMN "MedicationsJson";

ALTER TABLE "Patients" RENAME COLUMN "Vorname" TO "IvBase64";

ALTER TABLE "Patients" RENAME COLUMN "Name" TO "CiphertextBase64";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260713105141_E2EE_PatientData', '8.0.4');

COMMIT;

START TRANSACTION;

CREATE TABLE pharmacy_registry (
    "Id" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Street" TEXT NOT NULL,
    "PLZ" TEXT NOT NULL,
    "City" TEXT NOT NULL,
    "Phone" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    CONSTRAINT "PK_pharmacy_registry" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260713132224_AddPharmacyRegistry', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Pharmacists" ADD "StripeConnectAccountId" TEXT;

ALTER TABLE "Pharmacies" ADD "StripeCustomerId" TEXT;

ALTER TABLE "Pharmacies" ADD "SubscriptionTier" TEXT NOT NULL DEFAULT '';

ALTER TABLE "InternalShifts" ADD "EscrowStatus" TEXT NOT NULL DEFAULT '';

ALTER TABLE "InternalShifts" ADD "StripePaymentIntentId" TEXT;

ALTER TABLE "InternalShifts" ADD "StripeTransferId" TEXT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260713185248_AddStripeBilling', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Timesheets" ADD "DigitalSignatureHash" TEXT;

ALTER TABLE "Timesheets" ADD "TimesheetPath" TEXT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260713213223_AddTimesheetDocument', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Pharmacists" RENAME COLUMN "AugContractStatus" TO "FreelanceContractStatus";

ALTER TABLE "Pharmacists" RENAME COLUMN "AugContractDocumentPath" TO "FreelanceContractDocumentPath";

ALTER TABLE "Pharmacies" RENAME COLUMN "AugContractStatus" TO "FreelanceContractStatus";

ALTER TABLE "Pharmacies" RENAME COLUMN "AugContractDocumentPath" TO "FreelanceContractDocumentPath";

ALTER TABLE "Pharmacists" ADD "ApprobationNumber" TEXT;

ALTER TABLE "Pharmacists" ADD "IsFreelancerConfirmed" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Pharmacists" ADD "Status" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Pharmacies" ADD "BetriebserlaubnisNumber" TEXT;

ALTER TABLE "Pharmacies" ADD "Status" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "InternalShifts" ADD "AcceptedAt" TEXT;

ALTER TABLE "InternalShifts" ADD "RateNegotiatedBy" TEXT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260714120502_LegalAndRestructuringUpdate', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Pharmacists" ADD "EmailConfirmationTokenExpiry" TEXT;

ALTER TABLE "Pharmacies" ADD "EmailConfirmationTokenExpiry" TEXT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260714121924_AddTokenExpiry', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Pharmacists" ADD "SessionVersion" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Pharmacies" ADD "SessionVersion" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260714122127_AddSessionVersion', '8.0.4');

COMMIT;

START TRANSACTION;

DELETE FROM "Pharmacies" WHERE "Id" IN (7, 8, 9, 10, 12, 13, 15);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260714142423_PurgeDummyPharmacies', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Consumers" ALTER COLUMN "HasAcceptedBgbWaiver" DROP DEFAULT;

ALTER TABLE "Consumers" ALTER COLUMN "HasAcceptedBgbWaiver" TYPE boolean USING CASE WHEN "HasAcceptedBgbWaiver"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "InternalShifts" ALTER COLUMN "IsEmergencyDuty" DROP DEFAULT;

ALTER TABLE "InternalShifts" ALTER COLUMN "IsEmergencyDuty" TYPE boolean USING CASE WHEN "IsEmergencyDuty"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Notifications" ALTER COLUMN "IsRead" DROP DEFAULT;

ALTER TABLE "Notifications" ALTER COLUMN "IsRead" TYPE boolean USING CASE WHEN "IsRead"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacists" ALTER COLUMN "IsEmailConfirmed" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "IsEmailConfirmed" TYPE boolean USING CASE WHEN "IsEmailConfirmed"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacists" ALTER COLUMN "IsVerified" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "IsVerified" TYPE boolean USING CASE WHEN "IsVerified"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacists" ALTER COLUMN "IsApprobationVerified" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "IsApprobationVerified" TYPE boolean USING CASE WHEN "IsApprobationVerified"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacists" ALTER COLUMN "IsFreelancerConfirmed" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "IsFreelancerConfirmed" TYPE boolean USING CASE WHEN "IsFreelancerConfirmed"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacists" ALTER COLUMN "HasApprobation" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "HasApprobation" TYPE boolean USING CASE WHEN "HasApprobation"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacists" ALTER COLUMN "EmergencyServiceWillingness" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "EmergencyServiceWillingness" TYPE boolean USING CASE WHEN "EmergencyServiceWillingness"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacists" ALTER COLUMN "WeekendWillingness" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "WeekendWillingness" TYPE boolean USING CASE WHEN "WeekendWillingness"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacists" ALTER COLUMN "IsVatRequired" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "IsVatRequired" TYPE boolean USING CASE WHEN "IsVatRequired"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacists" ALTER COLUMN "IsKycVerified" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "IsKycVerified" TYPE boolean USING CASE WHEN "IsKycVerified"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "PharmacistFeedbacks" ALTER COLUMN "TimesheetConfirmed" DROP DEFAULT;

ALTER TABLE "PharmacistFeedbacks" ALTER COLUMN "TimesheetConfirmed" TYPE boolean USING CASE WHEN "TimesheetConfirmed"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacies" ALTER COLUMN "IsEmailConfirmed" DROP DEFAULT;

ALTER TABLE "Pharmacies" ALTER COLUMN "IsEmailConfirmed" TYPE boolean USING CASE WHEN "IsEmailConfirmed"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacies" ALTER COLUMN "IsVerified" DROP DEFAULT;

ALTER TABLE "Pharmacies" ALTER COLUMN "IsVerified" TYPE boolean USING CASE WHEN "IsVerified"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacies" ALTER COLUMN "InvoiceBillingPossible" DROP DEFAULT;

ALTER TABLE "Pharmacies" ALTER COLUMN "InvoiceBillingPossible" TYPE boolean USING CASE WHEN "InvoiceBillingPossible"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacies" ALTER COLUMN "ParkingAvailable" DROP DEFAULT;

ALTER TABLE "Pharmacies" ALTER COLUMN "ParkingAvailable" TYPE boolean USING CASE WHEN "ParkingAvailable"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "Pharmacies" ALTER COLUMN "IsTelepharmacyConsentGranted" DROP DEFAULT;

ALTER TABLE "Pharmacies" ALTER COLUMN "IsTelepharmacyConsentGranted" TYPE boolean USING CASE WHEN "IsTelepharmacyConsentGranted"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "TemperatureLogs" ALTER COLUMN "IsAnomaly" DROP DEFAULT;

ALTER TABLE "TemperatureLogs" ALTER COLUMN "IsAnomaly" TYPE boolean USING CASE WHEN "IsAnomaly"::text IN ('1', 'true', 't', 'y', 'yes', 'on') THEN true ELSE false END;

ALTER TABLE "AuditLogs" ALTER COLUMN "Timestamp" DROP DEFAULT;

ALTER TABLE "AuditLogs" ALTER COLUMN "Timestamp" TYPE timestamp with time zone USING "Timestamp"::timestamp with time zone;

ALTER TABLE "Consumers" ALTER COLUMN "CreatedAt" DROP DEFAULT;

ALTER TABLE "Consumers" ALTER COLUMN "CreatedAt" TYPE timestamp with time zone USING "CreatedAt"::timestamp with time zone;

ALTER TABLE "Consumers" ALTER COLUMN "BgbWaiverAcceptedAt" DROP DEFAULT;

ALTER TABLE "Consumers" ALTER COLUMN "BgbWaiverAcceptedAt" TYPE timestamp with time zone USING "BgbWaiverAcceptedAt"::timestamp with time zone;

ALTER TABLE "InternalShifts" ALTER COLUMN "AcceptedAt" DROP DEFAULT;

ALTER TABLE "InternalShifts" ALTER COLUMN "AcceptedAt" TYPE timestamp with time zone USING "AcceptedAt"::timestamp with time zone;

ALTER TABLE "Invoices" ALTER COLUMN "CreatedAt" DROP DEFAULT;

ALTER TABLE "Invoices" ALTER COLUMN "CreatedAt" TYPE timestamp with time zone USING "CreatedAt"::timestamp with time zone;

ALTER TABLE "Invoices" ALTER COLUMN "PaidAt" DROP DEFAULT;

ALTER TABLE "Invoices" ALTER COLUMN "PaidAt" TYPE timestamp with time zone USING "PaidAt"::timestamp with time zone;

ALTER TABLE "JobApplications" ALTER COLUMN "AppliedAt" DROP DEFAULT;

ALTER TABLE "JobApplications" ALTER COLUMN "AppliedAt" TYPE timestamp with time zone USING "AppliedAt"::timestamp with time zone;

ALTER TABLE "JobPosts" ALTER COLUMN "StartDate" DROP DEFAULT;

ALTER TABLE "JobPosts" ALTER COLUMN "StartDate" TYPE timestamp with time zone USING "StartDate"::timestamp with time zone;

ALTER TABLE "JobPosts" ALTER COLUMN "EndDate" DROP DEFAULT;

ALTER TABLE "JobPosts" ALTER COLUMN "EndDate" TYPE timestamp with time zone USING "EndDate"::timestamp with time zone;

ALTER TABLE "JobPosts" ALTER COLUMN "CreatedAt" DROP DEFAULT;

ALTER TABLE "JobPosts" ALTER COLUMN "CreatedAt" TYPE timestamp with time zone USING "CreatedAt"::timestamp with time zone;

ALTER TABLE "Notifications" ALTER COLUMN "CreatedAt" DROP DEFAULT;

ALTER TABLE "Notifications" ALTER COLUMN "CreatedAt" TYPE timestamp with time zone USING "CreatedAt"::timestamp with time zone;

ALTER TABLE "Pharmacists" ALTER COLUMN "EmailConfirmationTokenExpiry" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "EmailConfirmationTokenExpiry" TYPE timestamp with time zone USING "EmailConfirmationTokenExpiry"::timestamp with time zone;

ALTER TABLE "Pharmacists" ALTER COLUMN "GdprAnonymizedAt" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "GdprAnonymizedAt" TYPE timestamp with time zone USING "GdprAnonymizedAt"::timestamp with time zone;

ALTER TABLE "Pharmacists" ALTER COLUMN "TermsAcceptedAt" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "TermsAcceptedAt" TYPE timestamp with time zone USING "TermsAcceptedAt"::timestamp with time zone;

ALTER TABLE "PharmacistFeedbacks" ALTER COLUMN "ActualStart" DROP DEFAULT;

ALTER TABLE "PharmacistFeedbacks" ALTER COLUMN "ActualStart" TYPE timestamp with time zone USING "ActualStart"::timestamp with time zone;

ALTER TABLE "PharmacistFeedbacks" ALTER COLUMN "ActualEnd" DROP DEFAULT;

ALTER TABLE "PharmacistFeedbacks" ALTER COLUMN "ActualEnd" TYPE timestamp with time zone USING "ActualEnd"::timestamp with time zone;

ALTER TABLE "PharmacistFeedbacks" ALTER COLUMN "CreatedAt" DROP DEFAULT;

ALTER TABLE "PharmacistFeedbacks" ALTER COLUMN "CreatedAt" TYPE timestamp with time zone USING "CreatedAt"::timestamp with time zone;

ALTER TABLE "Pharmacies" ALTER COLUMN "CreatedAt" DROP DEFAULT;

ALTER TABLE "Pharmacies" ALTER COLUMN "CreatedAt" TYPE timestamp with time zone USING "CreatedAt"::timestamp with time zone;

ALTER TABLE "Pharmacies" ALTER COLUMN "EmailConfirmationTokenExpiry" DROP DEFAULT;

ALTER TABLE "Pharmacies" ALTER COLUMN "EmailConfirmationTokenExpiry" TYPE timestamp with time zone USING "EmailConfirmationTokenExpiry"::timestamp with time zone;

ALTER TABLE "Pharmacies" ALTER COLUMN "DataProcessingAgreementSignedAt" DROP DEFAULT;

ALTER TABLE "Pharmacies" ALTER COLUMN "DataProcessingAgreementSignedAt" TYPE timestamp with time zone USING "DataProcessingAgreementSignedAt"::timestamp with time zone;

ALTER TABLE "Pharmacies" ALTER COLUMN "GdprAnonymizedAt" DROP DEFAULT;

ALTER TABLE "Pharmacies" ALTER COLUMN "GdprAnonymizedAt" TYPE timestamp with time zone USING "GdprAnonymizedAt"::timestamp with time zone;

ALTER TABLE "PharmacyFeedbacks" ALTER COLUMN "ActualStart" DROP DEFAULT;

ALTER TABLE "PharmacyFeedbacks" ALTER COLUMN "ActualStart" TYPE timestamp with time zone USING "ActualStart"::timestamp with time zone;

ALTER TABLE "PharmacyFeedbacks" ALTER COLUMN "ActualEnd" DROP DEFAULT;

ALTER TABLE "PharmacyFeedbacks" ALTER COLUMN "ActualEnd" TYPE timestamp with time zone USING "ActualEnd"::timestamp with time zone;

ALTER TABLE "PharmacyFeedbacks" ALTER COLUMN "CreatedAt" DROP DEFAULT;

ALTER TABLE "PharmacyFeedbacks" ALTER COLUMN "CreatedAt" TYPE timestamp with time zone USING "CreatedAt"::timestamp with time zone;

ALTER TABLE "TemperatureLogs" ALTER COLUMN "RecordedAt" DROP DEFAULT;

ALTER TABLE "TemperatureLogs" ALTER COLUMN "RecordedAt" TYPE timestamp with time zone USING "RecordedAt"::timestamp with time zone;

ALTER TABLE "Timesheets" ALTER COLUMN "ActualStartDate" DROP DEFAULT;

ALTER TABLE "Timesheets" ALTER COLUMN "ActualStartDate" TYPE timestamp with time zone USING "ActualStartDate"::timestamp with time zone;

ALTER TABLE "Timesheets" ALTER COLUMN "DisputedAt" DROP DEFAULT;

ALTER TABLE "Timesheets" ALTER COLUMN "DisputedAt" TYPE timestamp with time zone USING "DisputedAt"::timestamp with time zone;

ALTER TABLE "Holidays" ALTER COLUMN "Date" DROP DEFAULT;

ALTER TABLE "Holidays" ALTER COLUMN "Date" TYPE date USING "Date"::date;

ALTER TABLE "InternalShifts" ALTER COLUMN "Date" DROP DEFAULT;

ALTER TABLE "InternalShifts" ALTER COLUMN "Date" TYPE date USING "Date"::date;

ALTER TABLE "SaturdayRotations" ALTER COLUMN "Date" DROP DEFAULT;

ALTER TABLE "SaturdayRotations" ALTER COLUMN "Date" TYPE date USING "Date"::date;

ALTER TABLE "Invoices" ALTER COLUMN "TotalAmount" DROP DEFAULT;

ALTER TABLE "Invoices" ALTER COLUMN "TotalAmount" TYPE numeric USING "TotalAmount"::numeric;

ALTER TABLE "JobPosts" ALTER COLUMN "Salary" DROP DEFAULT;

ALTER TABLE "JobPosts" ALTER COLUMN "Salary" TYPE numeric USING "Salary"::numeric;

ALTER TABLE "Pharmacists" ALTER COLUMN "HourlyRate" DROP DEFAULT;

ALTER TABLE "Pharmacists" ALTER COLUMN "HourlyRate" TYPE numeric USING "HourlyRate"::numeric;

ALTER TABLE "Pharmacies" ALTER COLUMN "TargetHourlyRate" DROP DEFAULT;

ALTER TABLE "Pharmacies" ALTER COLUMN "TargetHourlyRate" TYPE numeric USING "TargetHourlyRate"::numeric;

ALTER TABLE "Timesheets" ALTER COLUMN "HourlyRate" DROP DEFAULT;

ALTER TABLE "Timesheets" ALTER COLUMN "HourlyRate" TYPE numeric USING "HourlyRate"::numeric;

ALTER TABLE "Timesheets" ALTER COLUMN "TravelCosts" DROP DEFAULT;

ALTER TABLE "Timesheets" ALTER COLUMN "TravelCosts" TYPE numeric USING "TravelCosts"::numeric;

ALTER TABLE "Timesheets" ALTER COLUMN "AccommodationCosts" DROP DEFAULT;

ALTER TABLE "Timesheets" ALTER COLUMN "AccommodationCosts" TYPE numeric USING "AccommodationCosts"::numeric;

ALTER TABLE "Timesheets" ALTER COLUMN "ActualStartTime" DROP DEFAULT;

ALTER TABLE "Timesheets" ALTER COLUMN "ActualStartTime" TYPE interval USING "ActualStartTime"::interval;

ALTER TABLE "Timesheets" ALTER COLUMN "ActualEndTime" DROP DEFAULT;

ALTER TABLE "Timesheets" ALTER COLUMN "ActualEndTime" TYPE interval USING "ActualEndTime"::interval;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260715053428_PostgresSchemaFix', '8.0.4');

COMMIT;

START TRANSACTION;

CREATE TABLE "DeviceTokens" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "PharmacistId" integer NOT NULL,
    "FcmToken" character varying(255) NOT NULL,
    "DevicePlatform" character varying(20) NOT NULL,
    "DeviceId" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastActive" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_DeviceTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DeviceTokens_Pharmacists_PharmacistId" FOREIGN KEY ("PharmacistId") REFERENCES "Pharmacists" ("Id") ON DELETE CASCADE
);

CREATE TABLE "MobileRefreshTokens" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "PharmacistId" integer NOT NULL,
    "TokenHash" character varying(255) NOT NULL,
    "DeviceId" character varying(100) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "RevokedAt" timestamp with time zone,
    CONSTRAINT "PK_MobileRefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MobileRefreshTokens_Pharmacists_PharmacistId" FOREIGN KEY ("PharmacistId") REFERENCES "Pharmacists" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_DeviceTokens_PharmacistId" ON "DeviceTokens" ("PharmacistId");

CREATE INDEX "IX_MobileRefreshTokens_PharmacistId" ON "MobileRefreshTokens" ("PharmacistId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260715061958_RestoreMissingMobileTables', '8.0.4');

COMMIT;

START TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260715063848_RemoveStringConversions', '8.0.4');

COMMIT;

START TRANSACTION;

ALTER TABLE "Timesheets" ADD "BreaksMinutes" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260716102450_AddBreaksMinutes', '8.0.4');

COMMIT;

START TRANSACTION;

CREATE TABLE "DataProtectionKeys" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "FriendlyName" text,
    "Xml" text,
    CONSTRAINT "PK_DataProtectionKeys" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260716113927_AddDataProtectionKeys', '8.0.4');

COMMIT;

START TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260716114202_CorrectIsApprobationVerifiedType', '8.0.4');

COMMIT;

