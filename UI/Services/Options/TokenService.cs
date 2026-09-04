using Core.Entity;
using Core.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Core.Options;


namespace UI.Services.Options
{
    public record TokenPair(string AccessToken, DateTime AccessExpiresAt, string RefreshToken);
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwt;
        private readonly UserManager<AppUser> _userManager;

        public TokenService(IOptions<JwtSettings> jwt, UserManager<AppUser> userManager)
        {
            _jwt = jwt.Value;
            _userManager = userManager;
        }

        public async Task<TokenPair> IssueAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new("fullName", $"{user.FirstName} {user.LastName}"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            user.RefreshToken = refresh;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);
            await _userManager.UpdateAsync(user);

            return new TokenPair(new JwtSecurityTokenHandler().WriteToken(token), expires, refresh);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken)
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
                ValidateLifetime = false // токен уже истёк — это нормально для refresh
            };

            try
            {
                var principal = new JwtSecurityTokenHandler()
                    .ValidateToken(accessToken, parameters, out var validated);
                return validated is JwtSecurityToken jwt &&
                       jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase)
                    ? principal : null;
            }
            catch { return null; }
        }
    }
}
