-- ============================================================================
--  FootballAcademy — ВАРИАНТ 2: тренировки НЕ проведены, посещаемость не отмечена
--
--  Что делает:
--    1) удаляет ранее проставленную посещаемость августовских тренировок;
--    2) возвращает всем августовским тренировкам статус Planned (0)
--       и очищает CompletedAt / Summary / Highlights;
--    3) создаёт пустые строки посещаемости на весь состав команды
--       (Present = false, Reason = 0 «Неизвестно») — чтобы тренер сам отметил;
--    4) обнуляет Subscriptions.TrainingsUsed.
--
--  Это состояние «как перед первым занятием»: заходите тренером,
--  открываете тренировку, отмечаете посещаемость и нажимаете «Завершить».
--  Занятия спишутся из абонементов автоматически (по одному за Present).
--
--  Применять ПОСЛЕ seed_test_data.sql.
--  Скрипт идемпотентен — можно запускать повторно.
--
--  Применение:
--    psql -h localhost -U postgres -d footbalAcademyAuth -f seed_attendance_reset_for_coach.sql
-- ============================================================================
BEGIN;

-- ---------- 1. Сброс предыдущей разметки ----------
DELETE FROM "Attendances"
WHERE "TrainingId" IN (
    SELECT "Id" FROM "Trainings"
    WHERE "StartsAt" >= '2026-08-01 00:00:00+00'
      AND "StartsAt" <  '2026-09-01 00:00:00+00'
);

-- ---------- 2. Августовские тренировки — снова запланированные ----------
UPDATE "Trainings"
SET "Status"      = 0,                       -- TrainingStatus.Planned
    "CompletedAt" = NULL,
    "Summary"     = NULL,
    "Highlights"  = NULL,
    "UpdatedAt"   = now()
WHERE "StartsAt" >= '2026-08-01 00:00:00+00'
  AND "StartsAt" <  '2026-09-01 00:00:00+00';

-- ---------- 3. Пустые строки посещаемости на весь состав ----------
--  Нужны, чтобы в форме тренера сразу были все дети команды.
--  Все отмечены как отсутствующие с причиной «Неизвестно» — тренер правит вручную.
INSERT INTO "Attendances" (
    "Id", "TrainingId", "PlayerId", "SubscriptionId",
    "Present", "Reason", "Comment", "CreatedAt", "IsDeleted")
SELECT
    gen_random_uuid(),
    tr."Id",
    p."Id",
    NULL,
    false,
    0,                                       -- AbsenceReason.Unknown
    NULL,
    now(),
    false
FROM (
    SELECT "Id", "GroupId"
    FROM "Trainings"
    WHERE "StartsAt" >= '2026-08-01 00:00:00+00'
      AND "StartsAt" <  '2026-09-01 00:00:00+00'
      AND "IsDeleted" = false
) tr
JOIN "Players" p
  ON p."GroupId" = tr."GroupId"
 AND p."IsDeleted" = false;

-- ---------- 4. Обнуление израсходованных занятий ----------
UPDATE "Subscriptions"
SET "TrainingsUsed" = 0,
    "UpdatedAt"     = now()
WHERE "IsDeleted" = false;

COMMIT;

-- ============================================================================
--  КОНТРОЛЬНЫЕ ЗАПРОСЫ
-- ============================================================================
-- Всего отметок (ожидается 24 тренировки x 6 детей = 144, все Present = false):
--   SELECT "Present", count(*) FROM "Attendances" WHERE "IsDeleted" = false GROUP BY "Present";
--
-- Статусы августовских тренировок (ожидается 24 в статусе 0 = Planned):
--   SELECT "Status", count(*) FROM "Trainings"
--   WHERE "StartsAt" >= '2026-08-01' AND "StartsAt" < '2026-09-01'
--   GROUP BY "Status";
--
-- Остаток занятий по абонементам (ожидается 0 израсходовано, 16 доступно):
--   SELECT p."LastName", p."FirstName", s."TrainingsUsed", s."TrainingsLimit"
--   FROM "Subscriptions" s JOIN "Players" p ON p."Id" = s."PlayerId"
--   ORDER BY p."LastName", p."FirstName";
--
-- Проверка, что тренировка ещё не завершена (CompletedAt пустой):
--   SELECT count(*) FROM "Trainings"
--   WHERE "StartsAt" >= '2026-08-01' AND "StartsAt" < '2026-09-01'
--     AND "CompletedAt" IS NOT NULL;
