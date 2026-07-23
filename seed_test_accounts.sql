-- Seed file for test accounts (Pharmacy & Pharmacist)
-- Usage: psql -h <host> -U <user> -d serviceapotheke-db -f seed_test_accounts.sql

DO $$
DECLARE
    v_hash text := '$2a$11$QCFF12Nbz.6Bc0BCEmjlDu3bPTGU4ACxiPYKZthExGQs88x68s0oC'; -- Hash for "P@ssw0rd123!"
BEGIN
    -- 1. Seed Pharmacy
    IF EXISTS (SELECT 1 FROM "Pharmacies" WHERE "Email" = 'test_pharmacy@serviceapotheke.tech') THEN
        UPDATE "Pharmacies"
        SET "PasswordHash" = v_hash,
            "IsEmailConfirmed" = true,
            "IsVerified" = 1,
            "Status" = 2 -- VerificationStatus.Verified
        WHERE "Email" = 'test_pharmacy@serviceapotheke.tech';
    ELSE
        INSERT INTO "Pharmacies" (
            "PharmacyName", "Email", "PasswordHash", "PhoneNumber", "Address", "LicenseNumber",
            "IsEmailConfirmed", "IsVerified", "Status", "InvoiceBillingPossible", "ParkingAvailable", "FreelanceContractStatus", "UstIdValidationStatus"
        )
        VALUES (
            'Test Apotheke (Seed)', 'test_pharmacy@serviceapotheke.tech', v_hash, '0123456789', 'Teststraße 1, 12345 Teststadt', 'LIC123',
            true, 1, 2, 0, 0, 'Pending', 'Pending'
        );
    END IF;

    -- 2. Seed Pharmacist
    IF EXISTS (SELECT 1 FROM "Pharmacists" WHERE "Email" = 'test_pharmacist@serviceapotheke.tech') THEN
        UPDATE "Pharmacists"
        SET "PasswordHash" = v_hash,
            "IsEmailConfirmed" = true,
            "IsVerified" = 1,
            "Status" = 2 -- VerificationStatus.Verified
        WHERE "Email" = 'test_pharmacist@serviceapotheke.tech';
    ELSE
        INSERT INTO "Pharmacists" (
            "FullName", "Email", "PasswordHash", "PhoneNumber", "Address", "MaxDistanceKm", "AvailableDaysPerWeek",
            "IsEmailConfirmed", "IsVerified", "Status", "HasApprobation", "RadiusKm", "FreelanceContractStatus", "UstIdValidationStatus"
        )
        VALUES (
            'Test Pharmacist (Seed)', 'test_pharmacist@serviceapotheke.tech', v_hash, '0123456789', 'Teststraße 2, 12345 Teststadt', 50, 5,
            true, 1, 2, 1, 50, 'Pending', 'Pending'
        );
    END IF;

    RAISE NOTICE 'Test accounts seeded successfully. Password for both is: P@ssw0rd123!';
END $$;
