using Domain.Entity;
using Domain.Enums;
using Domain.Repositories.Interfaces;
using Domain.ValueObjects;
using Microsoft.AspNet.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(Context context) : base(context)
        {
        }

        public async Task<bool> RegisterAsync(User user, string password)
        {
            // Проверка на существование пользователя
            //var existingUser = await _context.Users.FirstOrDefault(u => u.UserName == user.UserName || u.Email == user.Email);

            //if (existingUser)
            //    return false;

            //user.PasswordHash = HashPassword(password);
            //user.CreatedAt = DateTime.UtcNow;

            //// Добавляем роль User по умолчанию
            //var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            //if (defaultRole != null)
            //{
            //    user.UserRoles.Add(new UserRole { RoleId = defaultRole.Id });
            //}

            //_context.Users.Add(user);
            //await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UserHasRoleAsync(Guid userId, string roleName)
        {
            
            return await _context.Users
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.Id == userId && ur.Role.Name == roleName);
        }

        //public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        //{
        //    await _context.Users.AddAsync(user, cancellationToken);
        //    await _context.SaveChangesAsync(cancellationToken);
        //    return IdentityResult.Success;
        //}

        //public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        //{
        //    // Soft delete вместо физического удаления
        //    user.IsDeleted = true;
        //    user.DeletedAt = DateTime.UtcNow;
        //    _context.Users.Update(user);
        //    await _context.SaveChangesAsync(cancellationToken);
        //    return IdentityResult.Success;
        //}

        //public void Dispose() => _context.Dispose();

        //public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        //{
        //    Guid id = Guid.Parse(userId);
        //    return await _context.Users
        //        .IgnoreQueryFilters() // Игнорируем soft delete для поиска
        //        .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        //}

        //public async Task<User> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        //{
        //    return await _context.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
        //}

        //public Task<string> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.NormalizedUserName);

        //public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.Id.ToString());

        //public Task<string> GetUserNameAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.UserName);

        //public Task SetNormalizedUserNameAsync(User user, string normalizedName, CancellationToken cancellationToken)
        //{
        //    user.NormalizedUserName = normalizedName;
        //    return Task.CompletedTask;
        //}

        //public Task SetUserNameAsync(User user, string userName, CancellationToken cancellationToken)
        //{
        //    user.UserName = userName;
        //    return Task.CompletedTask;
        //}

        //// IUserEmailStore
        //public Task<string> GetEmailAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.Email.Value);

        //public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.IsEmailConfirmed);

        //public Task<string> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.NormalizedEmail);

        //public Task SetEmailAsync(User user, string email, CancellationToken cancellationToken)
        //{
        //    user.Email = Email.Create(email);
        //    user.NormalizedEmail = email?.ToUpperInvariant();
        //    return Task.CompletedTask;
        //}

        //public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
        //{
        //    user.IsEmailConfirmed = confirmed;
        //    return Task.CompletedTask;
        //}

        //public Task SetNormalizedEmailAsync(User user, string normalizedEmail, CancellationToken cancellationToken)
        //{
        //    user.NormalizedEmail = normalizedEmail;
        //    return Task.CompletedTask;
        //}

        //public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        //{
        //    return await _context.Users
        //        .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        //}

        //// IUserPhoneNumberStore
        //public Task<string> GetPhoneNumberAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.Phone?.Value);

        //public Task<bool> GetPhoneNumberConfirmedAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.IsPhoneConfirmed);

        //public Task SetPhoneNumberAsync(User user, string phoneNumber, CancellationToken cancellationToken)
        //{
        //    user.Phone = phoneNumber != null ? PhoneNumber.Create(phoneNumber) : null;
        //    return Task.CompletedTask;
        //}

        //public Task SetPhoneNumberConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
        //{
        //    user.IsPhoneConfirmed = confirmed;
        //    return Task.CompletedTask;
        //}

        //// IUserPasswordStore
        //public Task<string> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.PasswordHash);

        //public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

        //public Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken)
        //{
        //    user.PasswordHash = passwordHash;
        //    return Task.CompletedTask;
        //}

        //// IUserRoleStore
        //public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        //{
        //    var role = await _context.Roles
        //        .FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant(), cancellationToken);

        //    if (role != null)
        //    {
        //        user.Role = role;
        //        user.RoleId = role.Id;
        //        await _context.SaveChangesAsync(cancellationToken);
        //    }
        //}

        //public async Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
        //{
        //    var roles = new List<string>();
        //    if (user.Role != null)
        //    {
        //        roles.Add(user.Role.Name);
        //    }
        //    else if (user.RoleId.HasValue)
        //    {
        //        var role = await _context.Roles.FindAsync(new object[] { user.RoleId.Value }, cancellationToken);
        //        if (role != null)
        //        {
        //            roles.Add(role.Name);
        //        }
        //    }
        //    return roles;
        //}

        //public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        //{
        //    var role = await _context.Roles
        //        .FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant(), cancellationToken);

        //    if (role == null)
        //        return new List<User>();

        //    return await _context.Users
        //        .Where(u => u.RoleId == role.Id)
        //        .ToListAsync(cancellationToken);
        //}

        //public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        //{
        //    if (user.Role != null)
        //    {
        //        return user.Role.NormalizedName == roleName.ToUpperInvariant();
        //    }

        //    if (user.RoleId.HasValue)
        //    {
        //        var role = await _context.Roles.FindAsync(new object[] { user.RoleId.Value }, cancellationToken);
        //        return role?.NormalizedName == roleName.ToUpperInvariant();
        //    }

        //    return false;
        //}

        //public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        //{
        //    user.Role = null;
        //    user.RoleId = null;
        //    await _context.SaveChangesAsync(cancellationToken);
        //}

        //// IUserLockoutStore
        //public Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.AccessFailedCount);

        //public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.LockoutEnabled);

        //public Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.LockoutEnd);

        //public Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        //{
        //    user.AccessFailedCount++;
        //    return Task.FromResult(user.AccessFailedCount);
        //}

        //public Task ResetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        //{
        //    user.AccessFailedCount = 0;
        //    return Task.CompletedTask;
        //}

        //public Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
        //{
        //    user.LockoutEnabled = enabled;
        //    return Task.CompletedTask;
        //}

        //public Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        //{
        //    user.LockoutEnd = lockoutEnd;
        //    return Task.CompletedTask;
        //}

        //// IUserTwoFactorStore
        //public Task<bool> GetTwoFactorEnabledAsync(User user, CancellationToken cancellationToken)
        //    => Task.FromResult(user.TwoFactorEnabled);

        //public Task SetTwoFactorEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
        //{
        //    user.TwoFactorEnabled = enabled;
        //    return Task.CompletedTask;
        //}

        //// IUserAuthenticatorKeyStore
        //public Task<string> GetAuthenticatorKeyAsync(User user, CancellationToken cancellationToken)
        //{
        //    // Можно добавить поле в User для AuthenticatorKey
        //    return Task.FromResult<string>(null);
        //}

        //public Task SetAuthenticatorKeyAsync(User user, string key, CancellationToken cancellationToken)
        //{
        //    // Можно добавить поле в User для AuthenticatorKey
        //    return Task.CompletedTask;
        //}

        //public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        //{
        //    user.UpdatedAt = DateTime.UtcNow;
        //    _context.Users.Update(user);
        //    await _context.SaveChangesAsync(cancellationToken);
        //    return IdentityResult.Success;
        //}

    }
}
