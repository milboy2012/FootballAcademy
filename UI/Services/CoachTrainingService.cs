using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using UI.Models.DataModels;
using UI.Models.ViewModels.Coach;
using UI.Models.ViewModels.Parent;
using UI.Models.ViewModels.Subscription;
using UI.Services.Interfaces;

namespace UI.Services
{
    public class CoachTrainingService : ICoachTrainingService
    {
        private readonly IUoW _data;
        ISubscriptionService _subs;
        public CoachTrainingService(IUoW data, ISubscriptionService subs)
        {
            _data = data;
            _subs = subs;
        }

        public Task<Guid?> GetCoachIdAsync(Guid userId, CancellationToken ct)
            => _data.Coaches.Query().Where(c => c.UserId == userId).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct);

        public Task<List<CoachGroupDto>> GetGroupsAsync(Guid coachId, CancellationToken ct)
            => _data.Groups.Query().AsNoTracking().Where(g => g.CoachId == coachId && !g.IsArchived).OrderBy(g => g.Name)
                .Select(g => new CoachGroupDto(g.Id, g.Name, g.Players.Count(p => p.IsActive), g.Color)).ToListAsync(ct);

        /// <summary>Ближайшие занятия + недавние без отметки посещаемости (чтобы не забыть заполнить).</summary>
        public async Task<List<UpcomingDto>> GetUpcomingAsync(Guid coachId, int days, CancellationToken ct)
        {
            var from = DateTime.UtcNow.AddDays(-7); var to = DateTime.UtcNow.AddDays(days);
            return await MyTrainings(coachId)
                .Where(t => t.StartsAt >= from && t.StartsAt <= to && t.Status != TrainingStatus.Cancelled)
                .OrderBy(t => t.StartsAt)
                .Select(t => new UpcomingDto(t.Id, t.StartsAt, t.EndsAt, t.Group.Name, t.Venue.Name, t.Kind, t.Status, t.Attendances.Any()))
                .ToListAsync(ct);
        }

        public async Task<(TrainingDetailsDto?, string?)> GetTrainingAsync(Guid trainingId, Guid coachId, CancellationToken ct)
        {
            var t = await MyTrainings(coachId).AsNoTracking()
                .Where(x => x.Id == trainingId)
                .Select(x => new
                {
                    x.Id,
                    x.Kind,
                    x.StartsAt,
                    x.EndsAt,
                    x.Status,
                    x.GroupId,
                    GroupName = x.Group.Name,
                    OpponentName = x.OpponentGroup != null ? x.OpponentGroup.Name : null,
                    VenueName = x.Venue.Name,
                    x.Note,
                    x.Summary,
                    x.Highlights,
                    x.CompletedAt
                }).FirstOrDefaultAsync(ct);
            if (t is null) return (null, "Тренировка не найдена или принадлежит другой группе");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var marks = await _data.Attendances.Query().AsNoTracking().Where(a => a.TrainingId == trainingId).ToDictionaryAsync(a => a.PlayerId, ct);

            var players = await _data.Players.Query().AsNoTracking()
                .Where(p => p.GroupId == t.GroupId && p.IsActive)
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new
                {
                    p.Id,
                    p.LastName,
                    p.FirstName,
                    p.BirthDate,
                    p.MedicalCertificateUntil,
                    HasSub = p.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active && s.To >= today),
                    Total = p.Attendances.Count(a => a.Training.GroupId == t.GroupId && a.TrainingId != trainingId),
                    Present = p.Attendances.Count(a => a.Training.GroupId == t.GroupId && a.TrainingId != trainingId && a.Present)
                }).ToListAsync(ct);

            var rows = new List<AttendanceRowDto>();
            foreach (var p in players)
            {
                marks.TryGetValue(p.Id, out var m);
                var age = today.Year - p.BirthDate.Year;
                if (p.BirthDate > today.AddYears(-age)) age--;

                var status = await _subs.GetStatusAsync(p.Id, ct);

                rows.Add(new AttendanceRowDto(p.Id, p.LastName, p.FirstName, age,
                    p.MedicalCertificateUntil is not null && p.MedicalCertificateUntil >= today,
                    status,
                    m?.Present, m?.Reason, m?.Comment,
                    p.Total == 0 ? 0 : p.Present * 100 / p.Total, ""));
            }
            //rows = players.Select(async p =>
            //{
            //    marks.TryGetValue(p.Id, out var m);
            //    var age = today.Year - p.BirthDate.Year; if (p.BirthDate > today.AddYears(-age)) age--;
            //    return new AttendanceRowDto(p.Id, p.LastName, p.FirstName, age,
            //        p.MedicalCertificateUntil is not null && p.MedicalCertificateUntil >= today, await _subs.GetStatusAsync(p.Id, ct),
            //        m?.Present, m?.Reason, m?.Comment, p.Total == 0 ? 0 : p.Present * 100 / p.Total, "");
            //}).ToList();

            // игроки, которые были отмечены, но уже покинули группу — тоже показываем
            var gone = marks.Keys.Except(players.Select(p => p.Id)).ToList();
            if (gone.Count > 0)
            {
                var extra = await _data.Players.Query().AsNoTracking().Where(p => gone.Contains(p.Id))
                    .Select(p => new { p.Id, p.LastName, p.FirstName }).ToListAsync(ct);
                
                foreach(var p in extra)
                {
                    var m = marks[p.Id];
                    var sts = await _subs.GetStatusAsync(p.Id, ct);
                    rows.Add(new AttendanceRowDto(p.Id, p.LastName, p.FirstName + " (выбыл)", 0, true, sts, m.Present, m.Reason, m.Comment, 0, ""));
                }                
            }

            return (new TrainingDetailsDto(t.Id, t.Kind, t.StartsAt, t.EndsAt, t.Status, t.GroupId, t.GroupName, t.OpponentName,
                t.VenueName, t.Note, t.Summary, t.Highlights, t.CompletedAt, rows), null);
        }

        public async Task<string?> ConductAsync(Guid trainingId, Guid coachId, ConductDto dto, CancellationToken ct)
        {
            var t = await MyTrainings(coachId).Include(x => x.Attendances).FirstOrDefaultAsync(x => x.Id == trainingId, ct);
            if (t is null) return "Тренировка не найдена или принадлежит другой группе";
            if (t.Status == TrainingStatus.Cancelled) return "Тренировка отменена";
            if (t.StartsAt > DateTime.UtcNow.AddMinutes(30)) return "Тренировка ещё не началась — отметить посещаемость можно за 30 минут до начала";

            var allowed = await _data.Players.Query().Where(p => p.GroupId == t.GroupId).Select(p => p.Id).ToHashSetAsync(ct);
            foreach (var id in t.Attendances.Select(a => a.PlayerId)) allowed.Add(id);
            if (dto.Attendance.Any(a => !allowed.Contains(a.PlayerId))) return "В списке есть игрок не из этой группы";
            if (dto.Attendance.GroupBy(a => a.PlayerId).Any(g => g.Count() > 1)) return "Игрок указан дважды";

            var date = DateOnly.FromDateTime(t.StartsAt);
            var blocked = new List<string>();

            foreach (var item in dto.Attendance)
            {
                var a = t.Attendances.FirstOrDefault(x => x.PlayerId == item.PlayerId);
                var wasPresent = a?.Present == true;

                if (item.Present && !wasPresent)
                {
                    var (sub, err) = await _subs.TryConsumeAsync(item.PlayerId, date, ct);
                    if (sub is null)
                    {
                        var name = await _data.Players.Query()
                            .Where(p => p.Id == item.PlayerId).Select(p => p.LastName + " " + p.FirstName).FirstAsync(ct);
                        blocked.Add($"{name} — {err}");
                        continue;                                   // отметку не сохраняем
                    }
                    a ??= NewRow(t, item.PlayerId);
                    a.SubscriptionId = sub.Id;
                }
                else if (!item.Present && wasPresent)
                {
                    await _subs.RefundAsync(a!.SubscriptionId, ct);  // сняли присутствие — вернули занятие
                    a.SubscriptionId = null;
                }
                a ??= NewRow(t, item.PlayerId);
                a.Present = item.Present;
                a.Reason = item.Present ? null : item.Reason ?? AbsenceReason.Unknown;
                a.Comment = string.IsNullOrWhiteSpace(item.Comment) ? null : item.Comment.Trim();
            }

            if (blocked.Count > 0)
            {
                //_data.ChangeTracker.Clear();     // ничего не сохраняем частично
                return "Нельзя отметить присутствие (абонемент недействителен):\n" + string.Join("\n", blocked) + "\nОтметьте их как отсутствующих или попросите родителей продлить абонемент.";
            }

            //foreach (var item in dto.Attendance)
            //{
            //    var a = t.Attendances.FirstOrDefault(x => x.PlayerId == item.PlayerId);
            //    if (a is null) { a = new Attendance { TrainingId = t.Id, PlayerId = item.PlayerId }; t.Attendances.Add(a); }
            //    a.Present = item.Present;
            //    a.Reason = item.Present ? null : item.Reason ?? AbsenceReason.Unknown;
            //    a.Comment = string.IsNullOrWhiteSpace(item.Comment) ? null : item.Comment.Trim();
            //}

            t.Summary = dto.Summary?.Trim();
            t.Highlights = dto.Highlights?.Trim();
            if (dto.Complete)
            {
                var unmarked = allowed.Count(id => _data.Players.Query().Any(p => p.Id == id && p.IsActive && p.GroupId == t.GroupId) && dto.Attendance.All(a => a.PlayerId != id));
                if (unmarked > 0) return $"Не отмечено игроков: {unmarked}. Отметьте всех, чтобы завершить тренировку";
                t.Status = TrainingStatus.Completed;
                t.CompletedAt = DateTime.UtcNow;
            }
            await _data.SaveChangesAsync(ct);
            return null;
        }
        //-----------------------------------Оценки за тренировку
        public async Task<(TrainingAssessmentsDto? Dto, string? Error)> GetAssessmentsAsync(Guid trainingId, Guid coachId, CancellationToken ct)
        {
            var t = await MyTrainings(coachId).AsNoTracking().Where(x => x.Id == trainingId)
                .Select(x => new { x.Id, x.GroupId, x.Status, x.StartsAt }).FirstOrDefaultAsync(ct);
            if (t is null) return (null, "Тренировка не найдена");

            var skills = await _data.Skills.Query().AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.SortOrder)
                .Select(s => new SkillDto(s.Id, s.Name, s.Description)).ToListAsync(ct);

            var present = await _data.Attendances.Query().AsNoTracking().Where(a => a.TrainingId == trainingId && a.Present).Select(a => a.PlayerId).ToListAsync(ct);
            //var existing = await _data.SkillAssessments.Query().AsNoTracking().Where(a => a.TrainingId == trainingId)
            //    .Select(a => new { a.PlayerId, a.Comment, Scores = a.Scores.ToDictionary(s => s.SkillId, s => s.Value) }).ToDictionaryAsync(a => a.PlayerId, ct);

            var rws = await _data.SkillAssessments.Query()
                        .AsNoTracking()
                        .Where(a => a.TrainingId == trainingId)
                        .Select(a => new
                        {
                            a.PlayerId,
                            a.Comment,
                            Scores = a.Scores.Select(s => new { s.SkillId, s.Value }).ToList()
                        })
                        .ToListAsync(ct);

            var existing = rws.ToDictionary(
                a => a.PlayerId,
                a => new
                {
                    a.PlayerId,
                    a.Comment,
                    Scores = a.Scores.ToDictionary(s => s.SkillId, s => s.Value)
                });

            var players = await _data.Players.Query().AsNoTracking().Where(p => p.GroupId == t.GroupId && p.IsActive)
                .OrderBy(p => p.LastName).Select(p => new { p.Id, Name = p.LastName + " " + p.FirstName }).ToListAsync(ct);

            // среднее по последним 5 оценённым тренировкам (до текущей)
            var ids = players.Select(p => p.Id).ToList();
            var recent = await _data.SkillScores.Query().AsNoTracking()
                .Where(s => ids.Contains(s.Assessment.PlayerId) && s.Assessment.TrainingId != null && s.Assessment.Training!.StartsAt < t.StartsAt)
                .Select(s => new { s.Assessment.PlayerId, s.SkillId, s.Value, s.Assessment.Training!.StartsAt }).ToListAsync(ct);
            var avg = recent.GroupBy(r => r.PlayerId).ToDictionary(g => g.Key, g =>
                g.GroupBy(r => r.SkillId).ToDictionary(sg => sg.Key, sg => Math.Round(sg.OrderByDescending(r => r.StartsAt).Take(5).Average(r => r.Value), 1)));

            var rows = players.Select(p =>
            {
                existing.TryGetValue(p.Id, out var e);
                return new TrainingAssessmentRowDto(p.Id, p.Name, present.Contains(p.Id), e?.Scores, e?.Comment, avg.GetValueOrDefault(p.Id) ?? []);
            }).ToList();

            return (new TrainingAssessmentsDto(skills, rows, t.Status == TrainingStatus.Completed), null);
        }

        public async Task<string?> SaveAssessmentsAsync(Guid trainingId, Guid coachId, SaveAssessmentsDto dto, CancellationToken ct)
        {
            var t = await MyTrainings(coachId).Include(x => x.Group).FirstOrDefaultAsync(x => x.Id == trainingId, ct);
            if (t is null) return "Тренировка не найдена";
            if (t.Status == TrainingStatus.Cancelled) return "Тренировка отменена";
            if (t.StartsAt > DateTime.UtcNow) return "Оценки выставляются после начала тренировки";

            var skillIds = await _data.Skills.Query().Where(s => s.IsActive).Select(s => s.Id).ToHashSetAsync(ct);
            var present = await _data.Attendances.Query().Where(a => a.TrainingId == trainingId && a.Present).Select(a => a.PlayerId).ToHashSetAsync(ct);
            var existing = await _data.SkillAssessments.Query().Include(a => a.Scores).Where(a => a.TrainingId == trainingId).ToListAsync(ct);
            var parents = await _data.Players.Query().Where(p => p.GroupId == t.GroupId).Select(p => new { p.Id, p.ParentId, p.FirstName }).ToDictionaryAsync(p => p.Id, ct);
            var date = DateOnly.FromDateTime(t.StartsAt);

            foreach (var p in dto.Players)
            {
                if (!parents.ContainsKey(p.PlayerId)) return "Игрок не из этой группы";
                var scores = p.Scores.Where(s => skillIds.Contains(s.Key)).ToDictionary(s => s.Key, s => s.Value);
                if (scores.Values.Any(v => v is < 1 or > 10)) return "Оценки от 1 до 10";
                var a = existing.FirstOrDefault(x => x.PlayerId == p.PlayerId);

                if (scores.Count == 0 && string.IsNullOrWhiteSpace(p.Comment))
                { if (a is not null) _data.SkillAssessments.Delete(a); continue; }           // очистили — удалить
                if (!present.Contains(p.PlayerId)) return $"{parents[p.PlayerId].FirstName}: нельзя оценить отсутствующего";

                var isNew = a is null;
                if (a is null) { 
                    a = new SkillAssessment { PlayerId = p.PlayerId, CoachId = coachId, TrainingId = trainingId, Date = date, Season = t.Group.Season }; 
                    await _data.SkillAssessments.AddAsync(a); 
                }

                a.Comment = p.Comment?.Trim();
                // синхронизируем набор оценок
                foreach (var s in a.Scores.Where(s => !scores.ContainsKey(s.SkillId)).ToList()) _data.SkillScores.Delete(s);
                foreach (var (skillId, value) in scores)
                {
                    var sc = a.Scores.FirstOrDefault(x => x.SkillId == skillId);
                    if (sc is null) a.Scores.Add(new SkillScore { SkillId = skillId, Value = value }); else sc.Value = value;
                }
                if (isNew) await _data.Notifications.AddAsync(new Notification
                {
                    UserId = parents[p.PlayerId].ParentId,
                    Title = "Новые оценки",
                    Message = $"Тренер оценил {parents[p.PlayerId].FirstName} за тренировку {TimeZoneInfo.ConvertTimeFromUtc(t.StartsAt, AppTime.Tz()):dd.MM}",
                    Link = $"/Parent/Progress/{p.PlayerId}"
                });
            }
            await _data.SaveChangesAsync(ct);
            return null;
        }

        private Attendance? NewRow(Training t, Guid playerId)
        {
            Attendance attendance = new Attendance();
            attendance.PlayerId = playerId;
            t.Attendances.Add(attendance);
            return attendance;
        }

        private IQueryable<Training> MyTrainings(Guid coachId) => _data.Trainings.Query().Where(t => t.Group.CoachId == coachId || (t.OpponentGroup != null && t.OpponentGroup.CoachId == coachId));
    }
}
