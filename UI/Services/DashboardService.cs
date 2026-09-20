using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using UI.Models.ViewModels.Dashboard;
using UI.Services.Interfaces;

namespace UI.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUoW _data;

        public DashboardService(IUoW data) => _data = data;
        public async Task<DashboardDto> GetAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow; 
            var today = DateOnly.FromDateTime(now);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var prevMonthStart = monthStart.AddMonths(-1);
            var weekStart = now.Date.AddDays(-(int)(now.DayOfWeek == 0 ? 6 : (int)now.DayOfWeek - 1)); 
            var weekEnd = weekStart.AddDays(7);

            var players = _data.Players.Query().Where(p => p.IsActive);
            var activePlayers = await players.CountAsync(ct);
            var newMonth = await players.CountAsync(p => p.CreatedAt >= monthStart, ct);
            var groups = await _data.Groups.Query().CountAsync(g => !g.IsArchived, ct);
            var coaches = await _data.Coaches.Query().CountAsync(c => c.User.IsActive, ct);
            var parents = await players.Select(p => p.ParentId).Distinct().CountAsync(ct);

            var subs = _data.Subscriptions.Query();
            var pending = await subs.CountAsync(s => s.Status == SubscriptionStatus.PendingPayment, ct);
            var active = await subs.CountAsync(s => s.Status == SubscriptionStatus.Active && s.To >= today, ct);
            var expiring = await subs.CountAsync(s => s.Status == SubscriptionStatus.Active && s.To >= today && s.To <= today.AddDays(7), ct);
            var withSub = await subs.Where(s => s.Status == SubscriptionStatus.Active && s.From <= today && s.To >= today).Select(s => s.PlayerId).Distinct().CountAsync(ct);
            var medicalExpired = await players.CountAsync(p => p.MedicalCertificateUntil == null || p.MedicalCertificateUntil < today, ct);

            var revenueMonth = await _data.Payments.Query().Where(p => p.PaidAt >= monthStart).SumAsync(p => p.Amount, ct);
            var revenuePrev = await _data.Payments.Query().Where(p => p.PaidAt >= prevMonthStart && p.PaidAt < monthStart).SumAsync(p => p.Amount, ct);

            var tr = _data.Trainings.Query();
            var trainingsWeek = await tr.CountAsync(t => t.StartsAt >= weekStart && t.StartsAt < weekEnd && t.Status != TrainingStatus.Cancelled, ct);
            var completedMonth = await tr.CountAsync(t => t.StartsAt >= monthStart && t.Status == TrainingStatus.Completed, ct);
            var cancelledMonth = await tr.CountAsync(t => t.StartsAt >= monthStart && t.Status == TrainingStatus.Cancelled, ct);
            var attMonth = await _data.Attendances.Query().Where(a => a.Training.StartsAt >= monthStart && a.Training.Status == TrainingStatus.Completed)
                .GroupBy(_ => 1).Select(g => new { Total = g.Count(), Present = g.Count(a => a.Present) }).FirstOrDefaultAsync(ct);
            var attPercent = attMonth is null || attMonth.Total == 0 ? 0 : attMonth.Present * 100 / attMonth.Total;

            // --- ряды ---
            var from6 = monthStart.AddMonths(-5);
            var revRaw = await _data.Payments.Query().Where(p => p.PaidAt >= from6).GroupBy(p => new { p.PaidAt.Year, p.PaidAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Sum = g.Sum(p => p.Amount) }).ToListAsync(ct);
            var revenueByMonth = Enumerable.Range(0, 6).Select(i => from6.AddMonths(i)).Select(m =>
                new Point(m.ToString("MMM yy", new System.Globalization.CultureInfo("ru-RU")), revRaw.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)?.Sum ?? 0)).ToList();

            var from8w = weekStart.AddDays(-7 * 7);
            var attRaw = await _data.Attendances.Query().Where(a => a.Training.StartsAt >= from8w && a.Training.Status == TrainingStatus.Completed)
                .Select(a => new { a.Training.StartsAt, a.Present }).ToListAsync(ct);
            var attendanceByWeek = Enumerable.Range(0, 8).Select(i => from8w.AddDays(7 * i)).Select(w =>
            {
                var wk = attRaw.Where(a => a.StartsAt >= w && a.StartsAt < w.AddDays(7)).ToList();
                return new Point(w.ToString("dd.MM"), wk.Count == 0 ? 0 : wk.Count(a => a.Present) * 100 / wk.Count);
            }).ToList();

            var groupFill = await _data.Groups.Query().Where(g => !g.IsArchived).OrderBy(g => g.Name)
                .Select(g => new NamedPoint(g.Name, g.Players.Count(p => p.IsActive), g.MaxPlayers, g.Color)).ToListAsync(ct);

            var attByGroupRaw = await _data.Attendances.Query().Where(a => a.Training.StartsAt >= monthStart && a.Training.Status == TrainingStatus.Completed)
                .GroupBy(a => a.Training.Group.Name).Select(g => new { g.Key, Total = g.Count(), Present = g.Count(a => a.Present) }).ToListAsync(ct);
            var attendanceByGroup = attByGroupRaw.OrderBy(x => x.Key).Select(x => new NamedPoint(x.Key, x.Total == 0 ? 0 : x.Present * 100 / x.Total)).ToList();

            var reasons = await _data.Attendances.Query().Where(a => !a.Present && a.Training.StartsAt >= monthStart)
                .GroupBy(a => a.Reason ?? AbsenceReason.Unknown).Select(g => new { g.Key, N = g.Count() }).ToListAsync(ct);
            var absenceReasons = reasons.Select(r => new NamedPoint(r.Key switch { AbsenceReason.Sick => "Болезнь", AbsenceReason.Excused => "Предупредили", AbsenceReason.Late => "Опоздание", _ => "Без причины" }, r.N)).ToList();

            //var venueLoad = await _data.Venues.Query().Where(v => v.IsActive).Select(v => new NamedPoint(v.Name, v.Trainings.Where(t => t.StartsAt >= monthStart && t.Status != TrainingStatus.Cancelled).Sum(t => (decimal)EF.Functions.DateDiffMinute(t.StartsAt, t.EndsAt)) / 60m)).ToListAsync(ct);

            //var venueLoad = (await _data.Venues.Query()
            //    .Where(v => v.IsActive)
            //    .Select(v => new
            //    {
            //        v.Name,
            //        Hours = v.Trainings
            //                    .Where(t => t.StartsAt >= monthStart && t.Status != TrainingStatus.Cancelled)
            //                    .Sum(t => (decimal)EF.Functions.DateDiffMinute(t.StartsAt, t.EndsAt)) / 60m
            //    })
            //    .ToListAsync(ct))
            //    .Select(x => new NamedPoint(x.Name, x.Hours))
            //    .ToList();

            var venueLoad = new List<NamedPoint>();

            //var skillAverages = await _data.SkillScores.Query().Where(s => s.Assessment.Date >= today.AddDays(-30)).GroupBy(s => s.Skill.Name).Select(g => new NamedPoint(g.Key, Math.Round((decimal)g.Average(s => s.Value), 1))).ToListAsync(ct);          
            //var skillAv = await _data.SkillScores.Query().Where(s => s.Assessment.Date >= today.AddDays(-30)).GroupBy(s => s.Skill.Name).ToListAsync();
            var skillAverages = new List<NamedPoint>();
            //foreach (var skill in skillAv)
            //{
            //    NamedPoint namedPoint = new NamedPoint(skill.Key, Math.Round((decimal)skill.Average(s => s.Value), 1));
            //    skillAverages.Add(namedPoint);
            //}
                


            // --- сигналы ---
            var alerts = new List<AlertDto>();
            if (pending > 0) alerts.Add(new("warning", $"Заявок на оплату ожидают подтверждения: {pending}", "/Subscriptions"));
            if (medicalExpired > 0) alerts.Add(new("danger", $"Игроков без действующей медсправки: {medicalExpired}", "/Players"));
            var noGroup = await players.CountAsync(p => p.GroupId == null, ct);
            if (noGroup > 0) alerts.Add(new("info", $"Игроков без группы: {noGroup}", "/Players"));
            var unmarked = await tr.CountAsync(t => t.EndsAt < now.AddHours(-24) && t.Status == TrainingStatus.Planned, ct);
            if (unmarked > 0) alerts.Add(new("warning", $"Прошедших тренировок без отметки посещаемости: {unmarked}", "/Schedule"));
            var fullGroups = groupFill.Where(g => g.Value >= g.Max).Select(g => g.Name).ToList();
            if (fullGroups.Count > 0) alerts.Add(new("info", $"Заполнены полностью: {string.Join(", ", fullGroups)}", "/Groups"));
            var lowAtt = attendanceByGroup.Where(g => g.Value < 60).Select(g => $"{g.Name} ({g.Value}%)").ToList();
            if (lowAtt.Count > 0) alerts.Add(new("danger", $"Низкая посещаемость: {string.Join(", ", lowAtt)}", "/Groups"));

            return new DashboardDto(activePlayers, newMonth, groups, coaches, parents, pending, active, expiring, activePlayers - withSub, medicalExpired,
                revenueMonth, revenuePrev, trainingsWeek, completedMonth, cancelledMonth, attPercent,
                revenueByMonth, attendanceByWeek, groupFill, attendanceByGroup, absenceReasons, venueLoad, skillAverages, alerts);
        }
    }
}
