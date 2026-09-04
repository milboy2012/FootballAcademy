using Core.Entity;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using UI.Services.Interfaces;
using UI.Services.Model;

namespace UI.Services
{
    public class PlayerAccountService : IPlayerAccountService
    {
        private const string EmailDomain = "player.local";
        private readonly IUoW _data;
        private readonly UserManager<AppUser> _userManager;

        public PlayerAccountService(UserManager<AppUser> userManager, IUoW data)
        {
            _userManager = userManager;
            _data = data;
        }

        public async Task<(PlayerAccountInfo? Info, string? Error)> CreateAsync(Guid playerId, string? password, CancellationToken ct)
        {
            var player = await _data.Players.GetByIdAsync(playerId, ct);
            if (player is null) return (null, "Ученик не найден");
            if (player.UserId is not null) return (null, "У ученика уже есть учётная запись");

            var login = await GenerateLoginAsync(player.FirstName, player.LastName);
            password ??= GeneratePassword();

            var user = new AppUser
            {
                UserName = login,
                Email = $"{login}@{EmailDomain}",
                EmailConfirmed = true,
                FirstName = player.FirstName,
                LastName = player.LastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return (null, string.Join("; ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, "Player");

            player.UserId = user.Id;
            await _data.SaveChangesAsync(ct);

            return (new PlayerAccountInfo(login, password, true), null);
        }

        public async Task<(string? NewPassword, string? Error)> ResetPasswordAsync(Guid playerId, CancellationToken ct)
        {
            var user = await GetUserAsync(playerId, ct);
            if (user is null) return (null, "Учётная запись не найдена");

            var newPassword = GeneratePassword();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded
                ? (newPassword, null)
                : (null, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        public async Task<string?> SetActiveAsync(Guid playerId, bool active, CancellationToken ct)
        {
            var user = await GetUserAsync(playerId, ct);
            if (user is null) return "Учётная запись не найдена";
            user.IsActive = active;
            await _userManager.UpdateAsync(user);
            if (!active) await _userManager.UpdateSecurityStampAsync(user); // сбросит активные cookie
            return null;
        }


        //private методы
        private async Task<AppUser?> GetUserAsync(Guid playerId, CancellationToken ct)
        {
            var userId = await _data.Players.Query().Where(p => p.Id == playerId).Select(p => p.UserId).FirstOrDefaultAsync(ct);
            return userId is null ? null : await _userManager.FindByIdAsync(userId.ToString()!);
        }
        private async Task<string> GenerateLoginAsync(string first, string last)
        {
            var baseLogin = $"{Translit(first)}.{Translit(last)}";
            var login = baseLogin;
            var i = 1;
            while (await _userManager.FindByNameAsync(login) is not null) login = $"{baseLogin}{++i}";
            return login;
        }

        /// <summary>Пароль вида «Мяч-4821» — ребёнку легко запомнить, соответствует политике Identity.</summary>
        private static string GeneratePassword()
        {
            string[] words = ["Gol", "Pas", "Myach", "Udar", "Kubok", "Match", "Finish", "Start"];
            var w = words[Random.Shared.Next(words.Length)];
            return $"{w}-{Random.Shared.Next(1000, 9999)}a";
        }

        private static string Translit(string s)
        {
            var map = new Dictionary<char, string>
            {
                ['а'] = "a",
                ['б'] = "b",
                ['в'] = "v",
                ['г'] = "g",
                ['д'] = "d",
                ['е'] = "e",
                ['ё'] = "e",
                ['ж'] = "zh",
                ['з'] = "z",
                ['и'] = "i",
                ['й'] = "y",
                ['к'] = "k",
                ['л'] = "l",
                ['м'] = "m",
                ['н'] = "n",
                ['о'] = "o",
                ['п'] = "p",
                ['р'] = "r",
                ['с'] = "s",
                ['т'] = "t",
                ['у'] = "u",
                ['ф'] = "f",
                ['х'] = "h",
                ['ц'] = "c",
                ['ч'] = "ch",
                ['ш'] = "sh",
                ['щ'] = "sch",
                ['ъ'] = "",
                ['ы'] = "y",
                ['ь'] = "",
                ['э'] = "e",
                ['ю'] = "yu",
                ['я'] = "ya"
            };
            var sb = new StringBuilder();
            foreach (var ch in s.Trim().ToLowerInvariant())
            {
                if (map.TryGetValue(ch, out var v)) sb.Append(v);
                else if (char.IsAsciiLetterOrDigit(ch)) sb.Append(ch);
            }
            return sb.Length == 0 ? "player" : sb.ToString();
        }
    }
}
