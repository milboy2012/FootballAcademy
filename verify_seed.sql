-- ============================================================================
--  FootballAcademy — проверка тестовых данных (только чтение, ничего не меняет)
--
--  Запуск:
--    psql -h localhost -U postgres -d footbalAcademyAuth -f verify_seed.sql
-- ============================================================================

\echo '=== 1. Состав данных (ожидается: Users=38+, Players=24, Groups=4, Venues=3, Trainings=24) ==='
SELECT 'Users'         AS "сущность", count(*)::text AS "значение" FROM "Users"         WHERE "Email" LIKE '%@academy.local'
UNION ALL SELECT 'Players',        count(*)::text FROM "Players"        WHERE "IsDeleted" = false AND "ParentId" IN (SELECT "Id" FROM "Users" WHERE "Email" LIKE 'parent%@academy.local')
UNION ALL SELECT 'Coaches',        count(*)::text FROM "Coaches"        WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE "Email" LIKE '%@academy.local')
UNION ALL SELECT 'ParentProfiles', count(*)::text FROM "ParentProfiles" WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE "Email" LIKE 'parent%@academy.local')
UNION ALL SELECT 'Groups',         count(*)::text FROM "Groups"         WHERE "CoachId" IN (SELECT "Id" FROM "Coaches" WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE "Email" LIKE '%@academy.local'))
UNION ALL SELECT 'Venues',         count(*)::text FROM "Venues"         WHERE "Name" IN ('Стадион «Динамо»','Манеж «Олимпийский»','Поле «Академия»')
UNION ALL SELECT 'Trainings',      count(*)::text FROM "Trainings"      WHERE "StartsAt" >= '2026-08-01' AND "StartsAt" < '2026-09-01'
UNION ALL SELECT 'Subscriptions',  count(*)::text FROM "Subscriptions"  WHERE "PlayerId" IN (SELECT "Id" FROM "Players" WHERE "ParentId" IN (SELECT "Id" FROM "Users" WHERE "Email" LIKE 'parent%@academy.local'));

\echo ''
\echo '=== 2. Команды: тренер, число детей, число тренировок (ожидается 6 и 6) ==='
SELECT g."Name"                                                        AS "команда",
       u."FirstName" || ' ' || u."LastName"                            AS "тренер",
       g."MinBirthYear" || '-' || g."MaxBirthYear"                     AS "годы рождения",
       count(DISTINCT p."Id")                                          AS "детей",
       count(DISTINCT t."Id")                                          AS "тренировок"
FROM "Groups" g
JOIN "Coaches" c ON c."Id" = g."CoachId"
JOIN "Users" u ON u."Id" = c."UserId"
LEFT JOIN "Players" p ON p."GroupId" = g."Id" AND p."IsDeleted" = false
LEFT JOIN "Trainings" t ON t."GroupId" = g."Id"
     AND t."StartsAt" >= '2026-08-01' AND t."StartsAt" < '2026-09-01'
WHERE u."Email" LIKE '%@academy.local'
GROUP BY g."Name", u."FirstName", u."LastName", g."MinBirthYear", g."MaxBirthYear"
ORDER BY g."Name";

\echo ''
\echo '=== 3. Родители: по 2 ребёнка, возраст 10 и 16 (ожидается 12 строк) ==='
SELECT u."LastName" || ' ' || u."FirstName" AS "родитель",
       count(p."Id")                        AS "детей",
       string_agg(DISTINCT date_part('year', p."BirthDate")::text, ', ' ORDER BY date_part('year', p."BirthDate")::text) AS "годы рождения"
FROM "Users" u
JOIN "Players" p ON p."ParentId" = u."Id" AND p."IsDeleted" = false
WHERE u."Email" LIKE 'parent%@academy.local'
GROUP BY u."Id", u."LastName", u."FirstName"
ORDER BY u."LastName";

\echo ''
\echo '=== 4. Пересечения: площадка+время и тренер+время (обе выборки должны быть ПУСТЫ) ==='
SELECT 'площадка занята дважды' AS "проблема", a."VenueId"::text AS "кто", a."StartsAt"::text AS "когда"
FROM "Trainings" a JOIN "Trainings" b
  ON a."Id" < b."Id" AND a."VenueId" = b."VenueId"
 AND a."StartsAt" < b."EndsAt" AND a."EndsAt" > b."StartsAt"
WHERE a."StartsAt" >= '2026-08-01' AND a."StartsAt" < '2026-09-01'
UNION ALL
SELECT DISTINCT 'тренер в двух местах', u."FirstName" || ' ' || u."LastName", a."StartsAt"::text
FROM "Trainings" a
JOIN "Groups" ga ON ga."Id" = a."GroupId"
JOIN "Coaches" c ON c."Id" = ga."CoachId"
JOIN "Users" u ON u."Id" = c."UserId"
JOIN "Groups" gb ON gb."CoachId" = ga."CoachId" AND gb."Id" <> ga."Id"
JOIN "Trainings" b ON b."GroupId" = gb."Id"
 AND a."StartsAt" < b."EndsAt" AND a."EndsAt" > b."StartsAt"
WHERE a."StartsAt" >= '2026-08-01' AND a."StartsAt" < '2026-09-01';

\echo ''
\echo '=== 5. Посещаемость (В-1: 144 отметки, 120 был / 24 пропуска, статус 1) ==='
SELECT t."Status"                                              AS "статус",
       count(a."Id")                                           AS "всего отметок",
       count(a."Id") FILTER (WHERE a."Present")                AS "был",
       count(a."Id") FILTER (WHERE NOT a."Present")            AS "пропустил"
FROM "Trainings" t
LEFT JOIN "Attendances" a ON a."TrainingId" = t."Id" AND a."IsDeleted" = false
WHERE t."StartsAt" >= '2026-08-01' AND t."StartsAt" < '2026-09-01'
GROUP BY t."Status" ORDER BY t."Status";

\echo ''
\echo '=== 6. Абонементы: израсходовано / лимит (В-1 ожидается 5/16, В-2 — 0/16) ==='
SELECT min(s."TrainingsUsed") AS "минимум",
       max(s."TrainingsUsed") AS "максимум",
       min(s."TrainingsLimit") AS "лимит",
       count(*)               AS "абонементов"
FROM "Subscriptions" s
WHERE s."PlayerId" IN (SELECT "Id" FROM "Players" WHERE "ParentId" IN (SELECT "Id" FROM "Users" WHERE "Email" LIKE 'parent%@academy.local'));

\echo ''
\echo '=== 7. Прочие тренировки в БД остались нетронутыми (ваши собственные) ==='
SELECT count(*) AS "не-августовских тренировок"
FROM "Trainings"
WHERE "StartsAt" < '2026-08-01' OR "StartsAt" >= '2026-09-01';
