using Core.Entity;
using System.Security.Claims;

namespace UI.Services.Options
{
    public interface ITokenService
    {
        Task<TokenPair> IssueAsync(AppUser user);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken);
    }
}
