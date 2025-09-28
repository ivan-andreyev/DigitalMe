-- Migration script to transfer data from digitalme_clean to digitalme_fixed
-- Run this after confirming digitalme_fixed works correctly with proper column names

-- Connect to digitalme_fixed database to import data from digitalme_clean

-- ================================================================
-- STEP 1: Backup existing data in digitalme_clean (if any exists)
-- ================================================================

-- First, let's see what data exists in the old database
-- Run manually: SELECT COUNT(*) FROM digitalme_clean.public."PersonalityProfiles";

-- ================================================================
-- STEP 2: Transfer PersonalityProfiles data
-- ================================================================

-- Insert PersonalityProfiles from old database to new one
INSERT INTO "PersonalityProfiles" (
    "Id", "Name", "Description", "Age", "Profession", "Location",
    "CoreValues", "CommunicationStyle", "TechnicalPreferences",
    "IsActive", "AccuracyScore", "CreatedAt", "UpdatedAt", "LastProfileUpdate"
)
SELECT
    "Id", "Name", "Description", "Age", "Profession", "Location",
    "CoreValues", "CommunicationStyle", "TechnicalPreferences",
    true as "IsActive",  -- Default to active since old DB might have inconsistent data
    "AccuracyScore", "CreatedAt", "UpdatedAt", "LastProfileUpdate"
FROM digitalme_clean.public."PersonalityProfiles"
ON CONFLICT ("Id") DO NOTHING; -- Avoid duplicates

-- ================================================================
-- STEP 3: Transfer PersonalityTraits data
-- ================================================================

INSERT INTO "PersonalityTraits" (
    "Id", "PersonalityProfileId", "Name", "Category", "Description",
    "Weight", "CreatedAt", "UpdatedAt"
)
SELECT
    "Id", "PersonalityProfileId", "Name", "Category", "Description",
    "Weight", "CreatedAt", "UpdatedAt"
FROM digitalme_clean.public."PersonalityTraits"
ON CONFLICT ("Id") DO NOTHING;

-- ================================================================
-- STEP 4: Transfer Conversations data
-- ================================================================

INSERT INTO "Conversations" (
    "Id", "UserId", "StartedAt", "EndedAt", "IsActive",
    "CreatedAt", "UpdatedAt"
)
SELECT
    "Id", "UserId", "StartedAt", "EndedAt",
    true as "IsActive",  -- Default to active
    "CreatedAt", "UpdatedAt"
FROM digitalme_clean.public."Conversations"
ON CONFLICT ("Id") DO NOTHING;

-- ================================================================
-- STEP 5: Transfer Messages data
-- ================================================================

INSERT INTO "Messages" (
    "Id", "ConversationId", "Content", "Role", "Timestamp",
    "TokenCount", "CreatedAt", "UpdatedAt"
)
SELECT
    "Id", "ConversationId", "Content", "Role", "Timestamp",
    "TokenCount", "CreatedAt", "UpdatedAt"
FROM digitalme_clean.public."Messages"
ON CONFLICT ("Id") DO NOTHING;

-- ================================================================
-- STEP 6: Transfer ASP.NET Identity data
-- ================================================================

-- Transfer Roles
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
SELECT "Id", "Name", "NormalizedName", "ConcurrencyStamp"
FROM digitalme_clean.public."AspNetRoles"
ON CONFLICT ("Id") DO NOTHING;

-- Transfer Users
INSERT INTO "AspNetUsers" (
    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled",
    "LockoutEnd", "LockoutEnabled", "AccessFailedCount"
)
SELECT
    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled",
    "LockoutEnd", "LockoutEnabled", "AccessFailedCount"
FROM digitalme_clean.public."AspNetUsers"
ON CONFLICT ("Id") DO NOTHING;

-- ================================================================
-- STEP 7: Verify data migration
-- ================================================================

-- Check counts match between old and new database
SELECT 'PersonalityProfiles' as table_name, COUNT(*) as count FROM "PersonalityProfiles"
UNION ALL
SELECT 'PersonalityTraits' as table_name, COUNT(*) as count FROM "PersonalityTraits"
UNION ALL
SELECT 'Conversations' as table_name, COUNT(*) as count FROM "Conversations"
UNION ALL
SELECT 'Messages' as table_name, COUNT(*) as count FROM "Messages"
UNION ALL
SELECT 'AspNetUsers' as table_name, COUNT(*) as count FROM "AspNetUsers"
UNION ALL
SELECT 'AspNetRoles' as table_name, COUNT(*) as count FROM "AspNetRoles";

-- ================================================================
-- STEP 8: Validate column name fix worked
-- ================================================================

-- This query should work in digitalme_fixed but fail in digitalme_clean
SELECT COUNT(*) as "ProfilesWithIsActiveColumn"
FROM "PersonalityProfiles"
WHERE "IsActive" = true;

-- ================================================================
-- MANUAL STEPS AFTER RUNNING THIS SCRIPT:
-- ================================================================

-- 1. Update cloudbuild.yaml to use digitalme_fixed permanently
-- 2. Test all application functionality
-- 3. Consider dropping digitalme_clean after confirmation
-- 4. Update documentation to reflect the database name change