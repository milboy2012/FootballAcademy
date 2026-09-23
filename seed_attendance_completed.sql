-- ============================================================================
--  FootballAcademy — ВАРИАНТ 1: «прошедшие» тренировки августа 2026
--
--  Что делает:
--    1) удаляет ранее проставленную посещаемость августовских тренировок;
--    2) переводит все августовские тренировки в статус Completed (1);
--    3) заполняет посещаемость по всем детям каждой команды;
--    4) синхронизирует Subscriptions.TrainingsUsed = числу отметок Present.
--
--  Применять ПОСЛЕ seed_test_data.sql.
--  Скрипт идемпотентен — можно запускать повторно.
--
--  Применение:
--    psql -h localhost -U postgres -d footbalAcademyAuth -f seed_attendance_completed.sql
-- ============================================================================
BEGIN;

-- ---------- 1. Сброс предыдущей разметки ----------
DELETE FROM "Attendances"
WHERE "TrainingId" IN (
    SELECT "Id" FROM "Trainings"
    WHERE "StartsAt" >= '2026-08-01 00:00:00+00'
      AND "StartsAt" <  '2026-09-01 00:00:00+00'
);

-- ---------- 2. Все августовские тренировки — проведённые ----------
UPDATE "Trainings"
SET "Status"      = 1,                       -- TrainingStatus.Completed
    "CompletedAt" = "EndsAt" + interval '10 minutes',
    "Summary"     = CASE ("Kind"::int)
                        WHEN 0 THEN 'Разминка, работа с мячом, двусторонка'
                        ELSE 'Товарищеский матч'
                    END,
    "Highlights"  = CASE ("Kind"::int)
                        WHEN 0 THEN 'Лучшие по объёму работы — капитаны команд'
                        ELSE 'Разбор игры на следующей тренировке'
                    END,
    "UpdatedAt"   = now()
WHERE "StartsAt" >= '2026-08-01 00:00:00+00'
  AND "StartsAt" <  '2026-09-01 00:00:00+00';

-- ---------- 3. Посещаемость ----------
--  В каждой тренировке ровно один ребёнок из шести отсутствует, поэтому
--  за 6 тренировок у каждого ребёнка ровно 5 посещений и 1 пропуск.
--  Причина пропуска циклически меняется: 1=болезнь, 2=предупредил, 3=опоздание.
INSERT INTO "Attendances" (
    "Id", "TrainingId", "PlayerId", "SubscriptionId",
    "Present", "Reason", "Comment", "CreatedAt", "IsDeleted")
SELECT
    gen_random_uuid(),
    d."TrainingId",
    d."PlayerId",
    CASE WHEN d."AbsentNumber" = d."PlayerNumber" THEN NULL ELSE d."SubscriptionId" END,
    CASE WHEN d."AbsentNumber" = d."PlayerNumber" THEN false ELSE true END,
    CASE WHEN d."AbsentNumber" = d."PlayerNumber"
         THEN ((d."SessionNumber" - 1) % 3 + 1)::int      -- Sick / Excused / Late
         ELSE NULL
    END,
    CASE WHEN d."AbsentNumber" = d."PlayerNumber"
         THEN (ARRAY['Заболел, предупредили заранее',
                     'Отпросился по семейным обстоятельствам',
                     'Опоздал, отпустили раньше'])[(d."SessionNumber" - 1) % 3 + 1]
         ELSE NULL
    END,
    now(),
    false
FROM (
    SELECT
        tr."Id"         AS "TrainingId",
        p."Id"          AS "PlayerId",
        sub."Id"        AS "SubscriptionId",
        tr."SessionNumber",
        ((tr."SessionNumber" - 1) % 6) + 1 AS "AbsentNumber",
        row_number() OVER (PARTITION BY tr."Id" ORDER BY p."LastName", p."FirstName") AS "PlayerNumber"
    FROM (
        SELECT
            t."Id",
            t."GroupId",
            row_number() OVER (PARTITION BY t."GroupId" ORDER BY t."StartsAt") AS "SessionNumber"
        FROM "Trainings" t
        WHERE t."StartsAt" >= '2026-08-01 00:00:00+00'
          AND t."StartsAt" <  '2026-09-01 00:00:00+00'
          AND t."IsDeleted" = false
    ) tr
    JOIN "Players" p
      ON p."GroupId" = tr."GroupId"
     AND p."IsDeleted" = false
    LEFT JOIN LATERAL (
        SELECT s."Id"
        FROM "Subscriptions" s
        WHERE s."PlayerId" = p."Id"
          AND s."IsDeleted" = false
        ORDER BY s."To" DESC
        LIMIT 1
    ) sub ON true
) d;

-- ---------- 4. Синхронизация израсходованных занятий ----------
UPDATE "Subscriptions" s
SET "TrainingsUsed" = cnt."Used",
    "UpdatedAt"     = now()
FROM (
    SELECT p."Id" AS "PlayerId", count(a."Id") AS "Used"
    FROM "Players" p
    LEFT JOIN "Attendances" a
           ON a."PlayerId" = p."Id"
          AND a."Present" = true
          AND a."IsDeleted" = false
    WHERE p."IsDeleted" = false
    GROUP BY p."Id"
) cnt
WHERE s."PlayerId" = cnt."PlayerId"
  AND s."IsDeleted" = false;

COMMIT;

-- ============================================================================
--  КОНТРОЛЬНЫЕ ЗАПРОСЫ
-- ============================================================================
-- Всего отметок (ожидается 24 тренировки x 6 детей = 144):
--   SELECT count(*) FROM "Attendances" WHERE "IsDeleted" = false;
--
-- Статусы августовских тренировок (ожидается 24 в статусе 1 = Completed):
--   SELECT "Status", count(*) FROM "Trainings"
--   WHERE "StartsAt" >= '2026-08-01' AND "StartsAt" < '2026-09-01'
--   GROUP BY "Status";
--
-- Посещаемость по командам (ожидается 6 детей, ~83% посещаемости):
--   SELECT g."Name",
--          count(*) FILTER (WHERE a."Present") AS "был",
--          count(*) FILTER (WHERE NOT a."Present") AS "пропустил",
--          round(100.0 * count(*) FILTER (WHERE a."Present") / count(*), 1) AS "процент"
--   FROM "Attendances" a
--   JOIN "Trainings" t ON t."Id" = a."TrainingId"
--   JOIN "Groups" g ON g."Id" = t."GroupId"
--   WHERE t."StartsAt" >= '2026-08-01' AND t."StartsAt" < '2026-09-01'
--   GROUP BY g."Name" ORDER BY g."Name";
--
-- Израсходовано занятий по абонементам (ожидается 5 из 16 у каждого ребёнка):
--   SELECT p."LastName", p."FirstName", s."TrainingsUsed", s."TrainingsLimit"
--   FROM "Subscriptions" s JOIN "Players" p ON p."Id" = s."PlayerId"
--   ORDER BY p."LastName", p."FirstName";
