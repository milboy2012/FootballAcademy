using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UI.Models.ViewModels.Subscription;
using UI.Services.Interfaces;

namespace UI.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUoW _data;
        private readonly UserManager<AppUser> _userManager;

        public SubscriptionService(IUoW data, UserManager<AppUser> userManager)
        {
            _data = data;
            _userManager = userManager;
        }

        public Task<List<PlanDto>> GetPlansAsync(bool onlyActive, CancellationToken ct)
        {
            var list = _data.TrainingPlans.Query().AsNoTracking().Where(p => !onlyActive || p.IsActive).OrderBy(p => p.SortOrder)
                .Select(p => new PlanDto(p.Id, p.Name, p.Type, p.Period, p.Visits, p.VisitsValidDays, p.Price, p.Description, p.IsActive, p.SortOrder)).ToListAsync(ct);
            return list;
        }


        // ---------- родитель ----------

        public async Task<(Guid?, string?)> RequestAsync(Guid parentId, RequestSubscriptionDto dto, CancellationToken ct)
        {
            var player = await _data.Players.Query().FirstOrDefaultAsync(p => p.Id == dto.PlayerId && p.ParentId == parentId, ct);
            if (player is null) return (null, "Ребёнок не найден");
            var plan = await _data.TrainingPlans.Query().FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.IsActive, ct);
            if (plan is null) return (null, "Тариф не найден");
            if (await _data.Subscriptions.AnyAsync(s => s.PlayerId == player.Id && s.Status == SubscriptionStatus.PendingPayment, ct))
                return (null, "Уже есть заявка, ожидающая подтверждения оплаты");

            // новый абонемент начинается после текущего активного (если есть), либо с указанной даты / сегодня
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var activeTo = await _data.Subscriptions.Query().Where(s => s.PlayerId == player.Id && s.Status == SubscriptionStatus.Active && s.To >= today)
                .MaxAsync(s => (DateOnly?)s.To, ct);
            var from = dto.StartFrom ?? today;
            if (activeTo is not null && from <= activeTo) from = activeTo.Value.AddDays(1);
            if (from < today) return (null, "Дата начала не может быть в прошлом");

            var to = plan.Type == PlanType.Period ? from.AddMonths((int)plan.Period!).AddDays(-1) : from.AddDays(plan.VisitsValidDays - 1);

            var sub = new Subscription
            {
                PlayerId = player.Id,
                PlanId = plan.Id,
                From = from,
                To = to,
                Price = plan.Price,
                TrainingsLimit = plan.Type == PlanType.Visits ? plan.Visits : null,
                RequestedByUserId = parentId,
                ParentComment = dto.Comment?.Trim()
            };
            await _data.Subscriptions.AddAsync(sub, ct);

            // уведомляем всех активных менеджеров
            //var managers = await _data.UserRoles.Where(ur => _data.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Manager"))
            //    .Join(_data.Users.Where(u => u.IsActive), ur => ur.UserId, u => u.Id, (ur, u) => u.Id).ToListAsync(ct);

            var managers = _userManager.GetUsersInRoleAsync("Manager").Result.ToList();


            await _data.Notifications.AddRangeAsync(managers.Select(m => new Notification
            {
                UserId = m.Id,
                Title = "Новая заявка на абонемент",
                Message = $"{player.LastName} {player.FirstName}: {plan.Name}, {plan.Price:N0} ₽. Ожидает подтверждения оплаты.",
                Link = "/Subscriptions"
            }));
            await _data.SaveChangesAsync(ct);
            return (sub.Id, null);
        }

        public async Task<string?> CancelRequestAsync(Guid parentId, Guid id, CancellationToken ct)
        {
            var s = await _data.Subscriptions.Query().FirstOrDefaultAsync(x => x.Id == id && x.RequestedByUserId == parentId, ct);
            if (s is null) return "Заявка не найдена";
            if (s.Status != SubscriptionStatus.PendingPayment) return "Отменить можно только неподтверждённую заявку";
            s.Status = SubscriptionStatus.Cancelled; await _data.SaveChangesAsync(ct);
            return null;
        }

        public Task<List<SubscriptionDto>> GetForPlayerAsync(Guid playerId, CancellationToken ct)
            => Project(_data.Subscriptions.Query().AsNoTracking().Where(s => s.PlayerId == playerId).OrderByDescending(s => s.From)).ToListAsync(ct);

        public async Task<SubscriptionStatusDto> GetStatusAsync(Guid playerId, CancellationToken ct)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var hasPending = await _data.Subscriptions.AnyAsync(s => s.PlayerId == playerId && s.Status == SubscriptionStatus.PendingPayment, ct);
            var active = await _data.Subscriptions.Query().AsNoTracking().Include(s => s.Plan)
                .Where(s => s.PlayerId == playerId && s.Status == SubscriptionStatus.Active && s.From <= today && s.To >= today)
                .OrderBy(s => s.To).FirstOrDefaultAsync(ct);

            if (active is null)
            {
                var future = await _data.Subscriptions.AnyAsync(s => s.PlayerId == playerId && s.Status == SubscriptionStatus.Active && s.From > today, ct);
                return new(false, hasPending ? "Ожидает подтверждения оплаты" : future ? "Абонемент начнётся позже" : "Абонемент не оплачен", null, null, null, hasPending);
            }
            var left = active.TrainingsLimit is null ? null : active.TrainingsLimit - active.TrainingsUsed;
            if (left is <= 0) return new(false, "Занятия закончились", active.Status, active.To, 0, hasPending);
            var text = active.TrainingsLimit is null ? $"{active.Plan.Name} до {active.To:dd.MM.yyyy}" : $"Осталось {left} занятий (до {active.To:dd.MM.yyyy})";
            return new(true, text, active.Status, active.To, left, hasPending);
        }

        // ---------- менеджер ----------

        public Task<List<SubscriptionDto>> GetForManagerAsync(SubscriptionStatus? status, string? search, CancellationToken ct)
        {
            var q = _data.Subscriptions.Query().AsNoTracking().AsQueryable();
            if (status is not null) q = q.Where(s => s.Status == status);
            if (!string.IsNullOrWhiteSpace(search)) { var p = $"%{search.Trim()}%"; q = q.Where(s => EF.Functions.ILike(s.Player.LastName, p) || EF.Functions.ILike(s.Player.Parent.LastName, p)); }
            var res = Project(q.OrderBy(s => s.Status == SubscriptionStatus.PendingPayment ? 0 : 1).ThenByDescending(s => s.CreatedAt).Take(500)).ToListAsync(ct);
            return res;
        }

        public async Task<string?> ConfirmAsync(Guid managerId, Guid id, ConfirmDto dto, CancellationToken ct)
        {
            var s = await _data.Subscriptions.Query().Include(x => x.Player).Include(x => x.Plan).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (s is null) return "Заявка не найдена";
            if (s.Status != SubscriptionStatus.PendingPayment) return "Заявка уже обработана";
            var amount = dto.Amount ?? s.Price;
            if (amount <= 0) return "Некорректная сумма";

            // если срок начала уже прошёл, пока ждали оплату — сдвигаем на сегодня, сохраняя длительность
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (s.From < today) { var len = s.To.DayNumber - s.From.DayNumber; s.From = today; s.To = today.AddDays(len); }

            s.Status = SubscriptionStatus.Active; s.ConfirmedByUserId = managerId; s.ConfirmedAt = DateTime.UtcNow; s.ManagerComment = dto.Comment?.Trim();
            s.Payments.Add(new Payment { Amount = amount, Method = dto.Method, PaidAt = DateTime.UtcNow, Comment = dto.Comment?.Trim() });

            var msg = $"Абонемент «{s.Plan.Name}» для {s.Player.FirstName} активен с {s.From:dd.MM.yyyy} по {s.To:dd.MM.yyyy}" + (s.TrainingsLimit is null ? "" : $", занятий: {s.TrainingsLimit}");
            await _data.Notifications.AddAsync(new Notification { UserId = s.Player.ParentId, Title = "Оплата подтверждена", Message = msg, Link = "/Cabinet" }, ct);
            if (s.Player.UserId is Guid pu) await _data.Notifications.AddAsync(new Notification { UserId = pu, Title = "Абонемент активен", Message = msg, Link = "/My" }, ct);
            await _data.SaveChangesAsync(ct);
            return null;
        }

        public async Task<string?> RejectAsync(Guid managerId, Guid id, string? reason, CancellationToken ct)
        {
            var s = await _data.Subscriptions.Query().Include(x => x.Player).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (s is null) return "Заявка не найдена";
            if (s.Status != SubscriptionStatus.PendingPayment) return "Заявка уже обработана";
            s.Status = SubscriptionStatus.Cancelled; s.ConfirmedByUserId = managerId; s.ConfirmedAt = DateTime.UtcNow; s.ManagerComment = reason?.Trim();
            await _data.Notifications.AddAsync(new Notification { UserId = s.Player.ParentId, Title = "Заявка на абонемент отклонена", Message = reason ?? "Обратитесь к администрации академии", Link = "/Cabinet" });
            await _data.SaveChangesAsync(ct);
            return null;
        }

        public async Task<string?> SetStatusAsync(Guid id, SubscriptionStatus status, CancellationToken ct)
        {
            var s = await _data.Subscriptions.Query().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (s is null) return "Абонемент не найден";
            if (status is not (SubscriptionStatus.Active or SubscriptionStatus.Frozen or SubscriptionStatus.Cancelled)) return "Недопустимый статус";
            if (s.Status == SubscriptionStatus.PendingPayment) return "Сначала подтвердите или отклоните оплату";
            s.Status = status; await _data.SaveChangesAsync(ct);
            return null;
        }

        // ---------- списание ----------

        public async Task<(Subscription?, string?)> TryConsumeAsync(Guid playerId, DateOnly date, CancellationToken ct)
        {
            var sub = await _data.Subscriptions.Query()
                .Where(s => s.PlayerId == playerId && s.Status == SubscriptionStatus.Active && s.From <= date && s.To >= date
                            && (s.TrainingsLimit == null || s.TrainingsUsed < s.TrainingsLimit))
                .OrderBy(s => s.To).FirstOrDefaultAsync(ct);
            if (sub is null)
            {
                var st = await GetStatusAsync(playerId, ct);
                return (null, st.Text);
            }
            if (sub.TrainingsLimit is not null) sub.TrainingsUsed++;
            return (sub, null);
        }

        public async Task RefundAsync(Guid? subscriptionId, CancellationToken ct)
        {
            if (subscriptionId is null) return;
            var s = await _data.Subscriptions.Query()
                .FirstOrDefaultAsync(x => x.Id == subscriptionId, ct);
            if (s?.TrainingsLimit is not null && s.TrainingsUsed > 0) s.TrainingsUsed--;
        }

        public async Task ExpireOutdatedAsync(CancellationToken ct)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            await _data.Subscriptions.Query()
                .Where(s => s.Status == SubscriptionStatus.Active && (s.To < today || (s.TrainingsLimit != null && s.TrainingsUsed >= s.TrainingsLimit)))
                .ExecuteUpdateAsync(u => u.SetProperty(s => s.Status, SubscriptionStatus.Expired), ct);
        }

        public async Task<string?> ExtendAsync(Guid id, DateOnly newTo, string? comment, CancellationToken ct)
        {
            var s = await _data.Subscriptions.Query()
                .Include(x => x.Player).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (s is null) return "Абонемент не найден";
            if (s.Status is SubscriptionStatus.PendingPayment or SubscriptionStatus.Cancelled) return "Нельзя продлить неоплаченный или отменённый абонемент";
            if (newTo <= s.To) return "Новая дата должна быть позже текущей";
            var old = s.To;
            s.To = newTo;
            if (s.Status == SubscriptionStatus.Expired && s.TrainingsLimit is null or > 0 && (s.TrainingsLimit == null || s.TrainingsUsed < s.TrainingsLimit))
                s.Status = SubscriptionStatus.Active;
            s.ManagerComment = string.IsNullOrWhiteSpace(comment) ? s.ManagerComment : comment.Trim();
            await _data.Notifications.AddAsync(new Notification
            {
                UserId = s.Player.ParentId,
                Title = "Абонемент продлён",
                Message = $"Срок абонемента {s.Player.FirstName} продлён с {old:dd.MM.yyyy} до {newTo:dd.MM.yyyy}" + (comment is null ? "" : $". {comment}"),
                Link = "/Cabinet"
            }, ct);
            await _data.SaveChangesAsync(ct);
            return null;
        }

        private static IQueryable<SubscriptionDto> Project(IQueryable<Subscription> q)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return q.Select(s => new SubscriptionDto(s.Id, s.PlayerId, s.Player.LastName + " " + s.Player.FirstName, s.Plan.Name, s.Plan.Type, s.Status,
                s.From, s.To, s.Price, s.TrainingsLimit, s.TrainingsUsed, s.TrainingsLimit == null ? null : s.TrainingsLimit - s.TrainingsUsed,
                s.To.DayNumber - today.DayNumber, s.ParentComment, s.ManagerComment, s.CreatedAt, s.ConfirmedAt, s.Payments.Sum(p => p.Amount)));
        }
    }
}
