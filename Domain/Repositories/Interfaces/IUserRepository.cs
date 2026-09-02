using Domain.Entity;
using Domain.Interfaces;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<bool> UserHasRoleAsync(Guid userId, string roleName);
        Task<bool> RegisterAsync(User user, string password);
        //public Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken);
        //public Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken);
        //public void Dispose();
        //public Task<User> FindByIdAsync(string userId, CancellationToken cancellationToken);
        //public Task<User> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken);
        //public Task<string> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken);
        //public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken);
        //public Task<string> GetUserNameAsync(User user, CancellationToken cancellationToken);
        //public Task SetNormalizedUserNameAsync(User user, string normalizedName, CancellationToken cancellationToken);
        //public Task SetUserNameAsync(User user, string userName, CancellationToken cancellationToken);

        //public Task<string> GetEmailAsync(User user, CancellationToken cancellationToken);
        //public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken);
        //public Task<string> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken);
        //public Task SetEmailAsync(User user, string email, CancellationToken cancellationToken);
        //public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken);
        //public Task SetNormalizedEmailAsync(User user, string normalizedEmail, CancellationToken cancellationToken);
        //public Task<User> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

        //public Task<string> GetPhoneNumberAsync(User user, CancellationToken cancellationToken);
        //public Task<bool> GetPhoneNumberConfirmedAsync(User user, CancellationToken cancellationToken);
        //public Task SetPhoneNumberAsync(User user, string phoneNumber, CancellationToken cancellationToken);
        //public Task SetPhoneNumberConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken);
        //public Task<string> GetPasswordHashAsync(User user, CancellationToken cancellationToken);
        //public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken);
        //public Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken);

        //public Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken);
        //public Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken);
        //public Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken);
        //public Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken);
        //public Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken);

        //public Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken);
        //public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken);
        //public Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken);
        //public Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken cancellationToken);
        //public Task ResetAccessFailedCountAsync(User user, CancellationToken cancellationToken);
        //public Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken cancellationToken);
        //public Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken);

        //public Task<bool> GetTwoFactorEnabledAsync(User user, CancellationToken cancellationToken);
        //public Task SetTwoFactorEnabledAsync(User user, bool enabled, CancellationToken cancellationToken);

        //public Task<string> GetAuthenticatorKeyAsync(User user, CancellationToken cancellationToken);
        //public Task SetAuthenticatorKeyAsync(User user, string key, CancellationToken cancellationToken);
        //public Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken);

    }
}
