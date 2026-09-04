using Core.Entity;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UI.Models.ViewModels.Player;
using UI.Models.ViewModels.Users;
using UI.Services.Interfaces;
using UI.Services.Model;

namespace UI.Services
{
    public class UserAdminService : IUserAdminService
    {
        public static readonly string[] AssignableRoles = ["Manager", "Coach", "Parent", "Player"];

        private readonly IUoW _data;
        private readonly ContextAuth _ctx;
        private readonly UserManager<AppUser> _userManager;

        public UserAdminService(IUoW data, UserManager<AppUser> userManager, ContextAuth ctx)
        {
            _data = data;
            _userManager = userManager;
            _ctx = ctx;

        }

        // ---------- чтение ----------

        public async Task<TabulatorPage<UserListItemDto>> GetPageAsync(UsersQuery q, CancellationToken ct)
        {
            // Пользователь -> его роли (одним запросом через join к UserRoles/Roles)
            //var query =
            //    from u in _ctx.Users.AsNoTracking()
            //    select new
            //    {
            //        User = u,
            //        Roles = _data.Role.Where(ur => ur.UserId == u.Id)
            //            .Join(_data.Role, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!).ToList()
            //    };

            var users = _ctx.Set<AppUser>().AsNoTracking();
            var userRoles = _ctx.Set<IdentityUserRole<Guid>>();
            var roles = _ctx.Set<AppRole>();

            var query =
                from u in users
                    select new
                        {
                            User = u,
                            Roles = userRoles.Where(ur => ur.UserId == u.Id)
                            .Join(roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!).ToList()
                         };

            if (!string.IsNullOrWhiteSpace(q.Role))
                query = query.Where(x => x.Roles.Contains(q.Role));
            if (q.IsActive is not null)
                query = query.Where(x => x.User.IsActive == q.IsActive);
            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = $"%{q.Search.Trim()}%";
                query = query.Where(x =>
                    EF.Functions.ILike(x.User.LastName, s) ||
                    EF.Functions.ILike(x.User.FirstName, s) ||
                    EF.Functions.ILike(x.User.Email!, s) ||
                    (x.User.PhoneNumber != null && EF.Functions.ILike(x.User.PhoneNumber, s)));
            }

            var desc = string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
            query = q.SortField switch
            {
                "email" => desc ? query.OrderByDescending(x => x.User.Email) : query.OrderBy(x => x.User.Email),
                "createdAt" => desc ? query.OrderByDescending(x => x.User.CreatedAt) : query.OrderBy(x => x.User.CreatedAt),
                "isActive" => desc ? query.OrderByDescending(x => x.User.IsActive) : query.OrderBy(x => x.User.IsActive),
                _ => desc ? query.OrderByDescending(x => x.User.LastName).ThenByDescending(x => x.User.FirstName)
                          : query.OrderBy(x => x.User.LastName).ThenBy(x => x.User.FirstName)
            };

            var size = Math.Clamp(q.Size, 1, 200);
            var page = Math.Max(q.Page, 1);
            var total = await query.CountAsync(ct);
            var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(ct);

            var data = items.Select(x => new UserListItemDto(
                x.User.Id, x.User.LastName, x.User.FirstName, x.User.Email!, x.User.PhoneNumber,
                x.Roles.FirstOrDefault() ?? "—", x.User.IsActive, x.User.MustChangePassword, x.User.CreatedAt));

            return new TabulatorPage<UserListItemDto>(data, Math.Max(1, (int)Math.Ceiling(total / (double)size)), total);
        }

        public async Task<UserDetailsDto?> GetAsync(Guid id, CancellationToken ct)
        {
            var u = await _userManager.FindByIdAsync(id.ToString());
            if (u is null) return null;
            var roles = await _userManager.GetRolesAsync(u);

            var childrenCount = await _data.Players.Query().CountAsync(p => p.ParentId == id, ct);
            var groupsCount = await _data.Groups.Query().CountAsync(g => g.Coach.UserId == id, ct);
            var linkedPlayer = await _data.Players.Query().Where(p => p.UserId == id)
                .Select(p => p.LastName + " " + p.FirstName).FirstOrDefaultAsync(ct);

            return new UserDetailsDto(u.Id, u.LastName, u.FirstName, u.Email!, u.PhoneNumber,
                roles.FirstOrDefault() ?? "", u.IsActive, u.MustChangePassword, u.CreatedAt,
                childrenCount, groupsCount, linkedPlayer);
        }

        // ---------- создание менеджера ----------

        public async Task<(string?, string?)> CreateManagerAsync(CreateManagerDto dto, CancellationToken ct)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            if (await _userManager.FindByEmailAsync(email) is not null)
                return (null, "Пользователь с таким email уже существует");

            var password = PasswordGenerator.Generate();
            var user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                PhoneNumber = dto.Phone?.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                MustChangePassword = true
            };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded) return (null, Join(result));
            await _userManager.AddToRoleAsync(user, "Manager");
            return (password, null);
        }

        // ---------- профиль ----------

        public async Task<string?> UpdateProfileAsync(Guid id, UpdateProfileDto dto, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return "Пользователь не найден";
            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
                return "Имя и фамилия обязательны";

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return Join(result);

            // Синхронизируем ФИО в профиле ученика, если пользователь — ребёнок
            var player = await _data.Players.Query().FirstOrDefaultAsync(p => p.UserId == id, ct);
            if (player is not null)
            {
                player.FirstName = user.FirstName;
                player.LastName = user.LastName;
                await _data.SaveChangesAsync(ct);
            }
            return null;
        }

        // ---------- блокировка ----------

        public async Task<string?> SetBlockedAsync(Guid id, bool blocked, string? reason, Guid actorId, CancellationToken ct)
        {
            if (id == actorId) return "Нельзя заблокировать самого себя";
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return "Пользователь не найден";

            if (blocked && await _userManager.IsInRoleAsync(user, "Manager"))
            {
                var activeManagers = (await _userManager.GetUsersInRoleAsync("Manager")).Count(m => m.IsActive && m.Id != id);
                if (activeManagers == 0) return "Нельзя заблокировать последнего активного менеджера";
            }

            user.IsActive = !blocked;
            // LockoutEnd — стандартный механизм Identity, PasswordSignIn вернёт IsLockedOut
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, blocked ? DateTimeOffset.MaxValue : null);
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user); // завершает текущие сессии

            // Тренер уходит — его группы остаются без тренера, в UI подсветим
            // (жёсткое каскадное действие здесь не делаем — решает менеджер)
            return null;
        }

        // ---------- смена роли ----------

        public async Task<string?> ChangeRoleAsync(Guid id, string newRole, Guid actorId, CancellationToken ct)
        {
            if (!AssignableRoles.Contains(newRole)) return "Недопустимая роль";
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return "Пользователь не найден";

            var current = await _userManager.GetRolesAsync(user);
            if (current.Contains("Admin")) return "Роль администратора изменить нельзя";
            if (current.Contains(newRole)) return null;

            if (id == actorId && current.Contains("Manager")) return "Нельзя снять роль менеджера с самого себя";

            // --- проверки при снятии старой роли ---
            if (current.Contains("Coach"))
            {
                var groups = await _data.Groups.Query().CountAsync(g => g.Coach.UserId == id, ct);
                if (groups > 0) return $"У тренера {groups} групп(ы). Сначала переназначьте их другому тренеру";
            }
            if (current.Contains("Parent") && newRole != "Parent")
            {
                var children = await _data.Players.Query().CountAsync(p => p.ParentId == id, ct);
                if (children > 0) return $"К пользователю привязано {children} детей. Роль родителя снять нельзя";
            }
            if (newRole == "Player" && !await _data.Players.AnyAsync(p => p.UserId == id, ct))
                return "Роль «Игрок» можно назначить только учётной записи, привязанной к ученику";

            // --- миграция профилей ---
            //await using var tx = await _data.Database.BeginTransactionAsync(ct);

            if (current.Contains("Player") && newRole != "Player")
            {
                var player = await _data.Players.Query().FirstOrDefaultAsync(p => p.UserId == id, ct);
                if (player is not null) player.UserId = null;       // ученик остаётся в базе, аккаунт отвязывается
            }
            if (newRole == "Coach" && !await _data.Coaches.AnyAsync(c => c.UserId == id, ct))
            {
                _data.Coaches.AddAsync(new Coach { UserId = id, HiredAt = DateOnly.FromDateTime(DateTime.UtcNow) }, ct);
            }
            await _data.SaveChangesAsync(ct);

            //var rm = await _userManager.RemoveFromRolesAsync(user, current);
            //if (!rm.Succeeded) { await tx.RollbackAsync(ct); return Join(rm); }
            //var add = await _userManager.AddToRoleAsync(user, newRole);
            //if (!add.Succeeded) { await tx.RollbackAsync(ct); return Join(add); }

            //await _userManager.UpdateSecurityStampAsync(user); // пользователь перелогинится с новыми правами
            //await tx.CommitAsync(ct);
            return null;
        }

        public async Task<(string?, string?)> ResetPasswordAsync(Guid id, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return (null, "Пользователь не найден");
            var password = PasswordGenerator.Generate();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, password);
            if (!result.Succeeded) return (null, Join(result));
            user.MustChangePassword = true;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            return (password, null);
        }

        private static string Join(IdentityResult r) => string.Join("; ", r.Errors.Select(e => e.Description));
    }
}
