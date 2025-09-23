-- Fix Production Data Consistency Issues
-- Addresses Foreign Key constraint failures between Conversations and PersonalityProfiles

-- Step 1: Identify orphaned conversations (FK constraint violations)
SELECT
    'Orphaned Conversations' as Issue,
    COUNT(*) as Count
FROM Conversations c
LEFT JOIN PersonalityProfiles pp ON c.PersonalityProfileId = pp.Id
WHERE pp.Id IS NULL;

-- Step 2: Identify missing PersonalityProfiles referenced by conversations
SELECT DISTINCT
    c.PersonalityProfileId as MissingProfileId,
    COUNT(*) as ConversationCount
FROM Conversations c
LEFT JOIN PersonalityProfiles pp ON c.PersonalityProfileId = pp.Id
WHERE pp.Id IS NULL
GROUP BY c.PersonalityProfileId;

-- Step 3: Create Ivan's PersonalityProfile if missing
-- This should match the seeded data from IvanDataSeeder.cs
INSERT OR IGNORE INTO PersonalityProfiles (Id, Name, Description, CreatedAt, UpdatedAt)
SELECT
    CASE
        WHEN EXISTS (SELECT 1 FROM Conversations WHERE PersonalityProfileId IS NOT NULL LIMIT 1)
        THEN (SELECT DISTINCT PersonalityProfileId FROM Conversations WHERE PersonalityProfileId IS NOT NULL LIMIT 1)
        ELSE lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6)))
    END as Id,
    'Ivan' as Name,
    'MVP Personality Profile for Ivan - Head of R&D, 34, Pragmatic technical leader focused on practical solutions and team efficiency.' as Description,
    datetime('now') as CreatedAt,
    datetime('now') as UpdatedAt
WHERE NOT EXISTS (SELECT 1 FROM PersonalityProfiles WHERE Name = 'Ivan');

-- Step 4: Create personality traits for Ivan if missing
INSERT OR IGNORE INTO PersonalityTraits (Id, PersonalityProfileId, Category, Name, Description, Weight, CreatedAt, UpdatedAt)
SELECT
    lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))) as Id,
    pp.Id as PersonalityProfileId,
    trait.Category,
    trait.Name,
    trait.Description,
    trait.Weight,
    datetime('now') as CreatedAt,
    datetime('now') as UpdatedAt
FROM PersonalityProfiles pp,
    (SELECT 'Communication' as Category, 'Direct' as Name, 'Предпочитает прямое и честное общение, избегает излишней дипломатии' as Description, 0.9 as Weight
     UNION ALL SELECT 'Technical', 'Pragmatic', 'Практический подход к техническим решениям, фокус на результате', 0.95
     UNION ALL SELECT 'Leadership', 'Team-Focused', 'Ориентирован на командную работу и развитие специалистов', 0.8
     UNION ALL SELECT 'Decision Making', 'Data-Driven', 'Принимает решения на основе данных и фактов', 0.85
     UNION ALL SELECT 'Technical', 'C# Expert', 'Глубокие знания C#/.NET, предпочтение строгой типизации', 0.9
     UNION ALL SELECT 'Management', 'Efficiency-Oriented', 'Фокус на эффективности процессов и автоматизации', 0.8
     UNION ALL SELECT 'Values', 'Honesty', 'Честность и прозрачность в профессиональных отношениях', 0.95
     UNION ALL SELECT 'Approach', 'Problem-Solving', 'Структурированный подход к решению сложных задач', 0.85
     UNION ALL SELECT 'Technical', 'Architecture', 'Системное мышление, понимание архитектурных принципов', 0.9
     UNION ALL SELECT 'Leadership', 'Mentoring', 'Развитие и обучение команды, передача знаний', 0.7
     UNION ALL SELECT 'Values', 'Quality', 'Стремление к качественному коду и решениям', 0.9) as trait
WHERE pp.Name = 'Ivan'
    AND NOT EXISTS (
        SELECT 1 FROM PersonalityTraits pt
        WHERE pt.PersonalityProfileId = pp.Id
        AND pt.Name = trait.Name
    );

-- Step 5: Update orphaned conversations to use Ivan's PersonalityProfile
UPDATE Conversations
SET PersonalityProfileId = (
    SELECT Id FROM PersonalityProfiles WHERE Name = 'Ivan' LIMIT 1
),
UpdatedAt = datetime('now')
WHERE PersonalityProfileId NOT IN (
    SELECT Id FROM PersonalityProfiles
) OR PersonalityProfileId IS NULL;

-- Step 6: Verify fix - should return 0
SELECT
    'Remaining Orphaned Conversations' as Status,
    COUNT(*) as Count
FROM Conversations c
LEFT JOIN PersonalityProfiles pp ON c.PersonalityProfileId = pp.Id
WHERE pp.Id IS NULL;

-- Step 7: Summary report
SELECT
    'PersonalityProfiles' as Table_Name,
    COUNT(*) as Total_Records
FROM PersonalityProfiles
UNION ALL
SELECT
    'PersonalityTraits' as Table_Name,
    COUNT(*) as Total_Records
FROM PersonalityTraits
UNION ALL
SELECT
    'Conversations' as Table_Name,
    COUNT(*) as Total_Records
FROM Conversations
UNION ALL
SELECT
    'Conversations with valid FK' as Table_Name,
    COUNT(*) as Total_Records
FROM Conversations c
INNER JOIN PersonalityProfiles pp ON c.PersonalityProfileId = pp.Id;