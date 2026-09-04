using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using UI.Models.ViewModels.Schedule;
using UI.Services.Interfaces;

namespace UI.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IUoW _data;
        private readonly INotificationService _notify;
        private static readonly TimeZoneInfo Tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"); // вынесите в конфиг

        public ScheduleService(IUoW ctx, INotificationService notify) { _data = ctx; _notify = notify; }

        // ---------- чтение для FullCalendar ----------

        public async Task<List<CalendarEventDto>> GetEventsAsync(DateTime from, DateTime to, Guid? groupId, Guid? venueId, Guid? forUserId, CancellationToken ct)
        {
            var q = _data.Trainings.Query().AsNoTracking().Where(t => t.StartsAt < to && t.EndsAt > from);
            if (groupId is not null) q = q.Where(t => t.GroupId == groupId || t.OpponentGroupId == groupId);
            if (venueId is not null) q = q.Where(t => t.VenueId == venueId);
            if (forUserId is Guid uid)   // родитель / игрок / тренер видят только свои группы
            {
                var myGroups = await _data.Players.Query().Where(p => (p.ParentId == uid || p.UserId == uid) && p.GroupId != null).Select(p => p.GroupId!.Value)
                    .Union(_data.Groups.Query().Where(g => g.Coach.UserId == uid).Select(g => g.Id)).ToListAsync(ct);
                q = q.Where(t => myGroups.Contains(t.GroupId) || (t.OpponentGroupId != null && myGroups.Contains(t.OpponentGroupId.Value)));
            }

            var rows = await q.Select(t => new
            {
                t.Id,
                t.Kind,
                t.StartsAt,
                t.EndsAt,
                t.Status,
                t.Note,
                t.SeriesId,
                t.CancelReason,
                t.GroupId,
                GroupName = t.Group.Name,
                t.Group.Color,
                CoachName = t.Group.Coach.User.LastName,
                t.OpponentGroupId,
                OpponentName = t.OpponentGroup != null ? t.OpponentGroup.Name : null,
                t.VenueId,
                VenueName = t.Venue.Name
            }).ToListAsync(ct);

            return rows.Select(r =>
            {
                var isMatch = r.Kind == EventKind.Match;
                var title = isMatch ? $"⚽ {r.GroupName} — {r.OpponentName}" : $"{r.GroupName} · {r.VenueName}";
                if (r.Status == TrainingStatus.Cancelled) title = "✕ " + title;
                var color = r.Status == TrainingStatus.Cancelled ? "#adb5bd" : isMatch ? "#dc3545" : r.Color ?? "#3788d8";
                return new CalendarEventDto(r.Id, title, r.StartsAt, r.EndsAt, color, null, new
                {
                    kind = r.Kind.ToString(),
                    status = r.Status.ToString(),
                    groupId = r.GroupId,
                    groupName = r.GroupName,
                    opponentGroupId = r.OpponentGroupId,
                    opponentName = r.OpponentName,
                    venueId = r.VenueId,
                    venueName = r.VenueName,
                    coachName = r.CoachName,
                    note = r.Note,
                    seriesId = r.SeriesId,
                    cancelReason = r.CancelReason
                });
            }).ToList();
        }

        // ---------- пересечения ----------

        public async Task<List<ConflictDto>> CheckAsync(Guid? selfId, EventKind kind, Guid groupId, Guid? opponentId, Guid venueId, DateTime start, DateTime end, CancellationToken ct)
        {
            var groups = new List<Guid> { groupId }; if (opponentId is Guid o) groups.Add(o);
            var coachIds = await _data.Groups.Query().Where(g => groups.Contains(g.Id)).Select(g => g.CoachId).ToListAsync(ct);

            var overlapping = await _data.Trainings.Query().AsNoTracking()
                .Where(t => t.Id != selfId && t.Status == TrainingStatus.Planned && t.StartsAt < end && t.EndsAt > start)
                .Where(t => t.VenueId == venueId
                         || groups.Contains(t.GroupId) || (t.OpponentGroupId != null && groups.Contains(t.OpponentGroupId.Value))
                         || coachIds.Contains(t.Group.CoachId) || (t.OpponentGroup != null && coachIds.Contains(t.OpponentGroup.CoachId)))
                .Select(t => new {
                    t.Id,
                    t.StartsAt,
                    t.EndsAt,
                    t.VenueId,
                    VenueName = t.Venue.Name,
                    t.GroupId,
                    GroupName = t.Group.Name,
                    t.OpponentGroupId,
                    t.Group.CoachId,
                    OppCoachId = t.OpponentGroup != null ? t.OpponentGroup.CoachId : (Guid?)null
                })
                .ToListAsync(ct);

            return overlapping.Select(t =>
            {
                string what = t.VenueId == venueId ? $"Место «{t.VenueName}» занято"
                    : groups.Contains(t.GroupId) || (t.OpponentGroupId != null && groups.Contains(t.OpponentGroupId.Value)) ? $"Группа {t.GroupName} уже занята"
                    : "Тренер занят в другой группе";
                return new ConflictDto(t.Id, what, t.StartsAt, t.EndsAt, $"{t.GroupName}, {t.VenueName}");
            }).ToList();
        }

        // ---------- создание (одиночное и серия) ----------

        public async Task<(CreateResult?, string?, List<ConflictDto>?)> CreateAsync(EventEditDto dto, CancellationToken ct)
        {
            if (await ValidateAsync(dto, ct) is { } e) return (null, e, null);

            var slots = Expand(dto);
            if (slots.Count == 0) return (null, "Не получилось ни одного занятия: проверьте дни недели и дату окончания", null);
            if (slots.Count > 200) return (null, "Слишком много занятий в серии (макс. 200)", null);

            var skipped = new List<ConflictDto>();
            var toCreate = new List<(DateTime s, DateTime e)>();
            foreach (var (s, en) in slots)
            {
                var c = await CheckAsync(null, dto.Kind, dto.GroupId, dto.OpponentGroupId, dto.VenueId, s, en, ct);
                if (c.Count == 0) toCreate.Add((s, en)); else skipped.AddRange(c);
            }
            if (skipped.Count > 0 && !dto.SkipConflicts) return (null, null, skipped);

            var seriesId = dto.Recurrence is null ? (Guid?)null : Guid.NewGuid();
            var entities = toCreate.Select(x => new Training
            {
                Kind = dto.Kind,
                GroupId = dto.GroupId,
                OpponentGroupId = dto.Kind == EventKind.Match ? dto.OpponentGroupId : null,
                VenueId = dto.VenueId,
                StartsAt = x.s,
                EndsAt = x.e,
                Note = dto.Note?.Trim(),
                SeriesId = seriesId
            }).ToList();
            await _data.Trainings.AddRangeAsync(entities, ct);
            await _data.SaveChangesAsync(ct);

            if (dto.NotifyParticipants && entities.Count > 0)
            {
                var first = entities.OrderBy(x => x.StartsAt).First();
                var msg = entities.Count == 1
                    ? $"{KindName(first)} {Local(first.StartsAt)}, {await VenueName(first.VenueId, ct)}"
                    : $"Добавлено {entities.Count} занятий с {Local(first.StartsAt):dd.MM} по {Local(entities.Max(x => x.StartsAt)):dd.MM}";
                await _notify.NotifyEventAsync(first, "Новое событие в расписании", msg, ct);
            }
            return (new CreateResult(entities.Count, skipped), null, null);
        }

        private static List<(DateTime, DateTime)> Expand(EventEditDto dto)
        {
            var s = dto.Start.UtcDateTime; var e = dto.End.UtcDateTime;
            if (dto.Recurrence is null) return [(s, e)];

            var days = dto.Recurrence.Weekdays.Select(d => (DayOfWeek)d).ToHashSet();
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(s, Tz);
            var duration = e - s;
            var result = new List<(DateTime, DateTime)>();
            for (var day = localStart.Date; day <= dto.Recurrence.Until.ToDateTime(TimeOnly.MinValue); day = day.AddDays(1))
            {
                if (!days.Contains(day.DayOfWeek)) continue;
                var ls = day + localStart.TimeOfDay;
                var us = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(ls, DateTimeKind.Unspecified), Tz);
                result.Add((us, us + duration));
            }
            return result;
        }

        // ---------- редактирование ----------

        public async Task<(string?, List<ConflictDto>?)> UpdateAsync(Guid id, EventEditDto dto, CancellationToken ct)
        {
            var t = await _data.Trainings.Query().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null) return ("Событие не найдено", null);
            if (t.Status != TrainingStatus.Planned) return ("Можно менять только запланированные события", null);
            if (await ValidateAsync(dto, ct) is { } e) return (e, null);

            var s = dto.Start.UtcDateTime; var en = dto.End.UtcDateTime;
            var conflicts = await CheckAsync(id, dto.Kind, dto.GroupId, dto.OpponentGroupId, dto.VenueId, s, en, ct);
            if (conflicts.Count > 0) return (null, conflicts);

            var changed = t.StartsAt != s || t.EndsAt != en || t.VenueId != dto.VenueId;
            var oldDesc = $"{Local(t.StartsAt)}, {await VenueName(t.VenueId, ct)}";

            t.Kind = dto.Kind; t.GroupId = dto.GroupId; t.OpponentGroupId = dto.Kind == EventKind.Match ? dto.OpponentGroupId : null;
            t.VenueId = dto.VenueId; t.StartsAt = s; t.EndsAt = en; t.Note = dto.Note?.Trim();
            await _data.SaveChangesAsync(ct);

            if (changed && dto.NotifyParticipants)
                await _notify.NotifyEventAsync(t, $"{KindName(t)} перенесена", $"Было: {oldDesc}. Стало: {Local(s)}, {await VenueName(t.VenueId, ct)}", ct);
            return (null, null);
        }

        /// <summary>Drag-and-drop в календаре. ApplyToSeries сдвигает все будущие события серии на тот же интервал.</summary>
        public async Task<(string?, List<ConflictDto>?)> MoveAsync(Guid id, MoveDto dto, CancellationToken ct)
        {
            var t = await _data.Trainings.Query().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null) return ("Событие не найдено", null);
            if (t.Status != TrainingStatus.Planned) return ("Отменённое событие нельзя перенести", null);

            var delta = dto.Start.UtcDateTime - t.StartsAt;
            var newDuration = dto.End.UtcDateTime - dto.Start.UtcDateTime;
            var targets = dto.ApplyToSeries && t.SeriesId is not null
                ? await _data.Trainings.Query().Where(x => x.SeriesId == t.SeriesId && x.StartsAt >= t.StartsAt && x.Status == TrainingStatus.Planned).ToListAsync(ct)
                : [t];

            var allConflicts = new List<ConflictDto>();
            foreach (var x in targets)
                allConflicts.AddRange(await CheckAsync(x.Id, x.Kind, x.GroupId, x.OpponentGroupId, x.VenueId, x.StartsAt + delta, x.StartsAt + delta + newDuration, ct));
            // пересечения между самими переносимыми событиями невозможны — они не пересекались и сдвигаются одинаково
            allConflicts = allConflicts.Where(c => targets.All(x => x.Id != c.EventId)).ToList();
            if (allConflicts.Count > 0) return (null, allConflicts);

            var old = Local(t.StartsAt);
            foreach (var x in targets) { x.StartsAt += delta; x.EndsAt = x.StartsAt + newDuration; }
            await _data.SaveChangesAsync(ct);

            await _notify.NotifyEventAsync(t, targets.Count > 1 ? "Перенесена серия занятий" : $"{KindName(t)} перенесена",
                $"{old} → {Local(t.StartsAt)}" + (targets.Count > 1 ? $" (и ещё {targets.Count - 1})" : ""), ct);
            return (null, null);
        }

        public async Task<string?> CancelAsync(Guid id, CancelDto dto, CancellationToken ct)
        {
            var t = await _data.Trainings.Query().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null) return "Событие не найдено";
            var targets = dto.ApplyToSeries && t.SeriesId is not null
                ? await _data.Trainings.Query().Where(x => x.SeriesId == t.SeriesId && x.StartsAt >= t.StartsAt && x.Status == TrainingStatus.Planned).ToListAsync(ct)
                : [t];
            foreach (var x in targets) { x.Status = TrainingStatus.Cancelled; x.CancelReason = dto.Reason?.Trim(); }
            await _data.SaveChangesAsync(ct);

            var reason = string.IsNullOrWhiteSpace(dto.Reason) ? "" : $" Причина: {dto.Reason}";
            await _notify.NotifyEventAsync(t, targets.Count > 1 ? "Отменена серия занятий" : $"{KindName(t)} отменена",
                $"{Local(t.StartsAt)}, {await VenueName(t.VenueId, ct)}." + (targets.Count > 1 ? $" Всего отменено: {targets.Count}." : "") + reason, ct);
            return null;
        }

        // ---------- helpers ----------

        private async Task<string?> ValidateAsync(EventEditDto d, CancellationToken ct)
        {
            if (d.End <= d.Start) return "Время окончания должно быть позже начала";
            if (d.End - d.Start > TimeSpan.FromHours(6)) return "Событие длиннее 6 часов";
            var g = await _data.Groups.Query().FirstOrDefaultAsync(x => x.Id == d.GroupId, ct);
            if (g is null || g.IsArchived) return "Группа не найдена или в архиве";
            if (!await _data.Venues.AnyAsync(v => v.Id == d.VenueId && v.IsActive, ct)) return "Место не найдено или неактивно";
            if (d.Kind == EventKind.Match)
            {
                if (d.OpponentGroupId is null) return "Для матча укажите вторую группу";
                if (d.OpponentGroupId == d.GroupId) return "Группа не может играть сама с собой";
                if (!await _data.Groups.AnyAsync(x => x.Id == d.OpponentGroupId && !x.IsArchived, ct)) return "Группа соперника не найдена";
                if (d.Recurrence is not null) return "Матчи не создаются серией";
            }
            if (d.Recurrence is not null && (d.Recurrence.Weekdays.Length == 0 || d.Recurrence.Weekdays.Any(w => w is < 0 or > 6))) return "Выберите дни недели";
            return null;
        }

        private static string KindName(Training t) => t.Kind == EventKind.Match ? "Матч" : "Тренировка";
        private static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, Tz);
        private Task<string> VenueName(Guid id, CancellationToken ct) => _data.Venues.Query().Where(v => v.Id == id).Select(v => v.Name).FirstAsync(ct);
    }
}
