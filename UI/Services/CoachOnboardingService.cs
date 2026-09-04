using Core.Entity;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using UI.Services.Interfaces;
using UI.Services.Model;

namespace UI.Services
{
    public class CoachOnboardingService : ICoachOnboardingService
    {
        private readonly IUoW _data;
        private readonly UserManager<AppUser> _userManager;

        public CoachOnboardingService(IUoW data, UserManager<AppUser> userManager)
        {
            _data = data;
            _userManager = userManager;
        }

        public async Task<(CoachCreatedDto? Result, string? Error)> CreateAsync(string email, string? firstName, string? lastName, CancellationToken ct)
        {
            email = email.Trim().ToLowerInvariant();
            if (await _userManager.FindByEmailAsync(email) is not null)
                return (null, "Пользователь с таким email уже существует");

            var password = GenerateTemporaryPassword();
            var user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = string.IsNullOrWhiteSpace(firstName) ? "Тренер" : firstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(lastName) ? "" : lastName.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                MustChangePassword = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return (null, string.Join("; ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, "Coach");

            var coach = new Coach { UserId = user.Id, HiredAt = DateOnly.FromDateTime(DateTime.UtcNow) };
            await _data.Coaches.AddAsync(coach, ct);
            await _data.SaveChangesAsync(ct);

            return (new CoachCreatedDto(coach.Id, email, password), null);
        }

        public async Task<(string? Password, string? Error)> ResetTemporaryPasswordAsync(Guid coachId, CancellationToken ct)
        {
            var userId = await _data.Coaches.Query().Where(c => c.Id == coachId).Select(c => c.UserId).FirstOrDefaultAsync(ct);
            var user = userId == default ? null : await _userManager.FindByIdAsync(userId.ToString());
            if (user is null) return (null, "Тренер не найден");

            var password = GenerateTemporaryPassword();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, password);
            if (!result.Succeeded) return (null, string.Join("; ", result.Errors.Select(e => e.Description)));

            user.MustChangePassword = true;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user); // завершить активные сессии
            return (password, null);
        }

        //генерация первоначального пароля для тренера
        private string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", lower = "abcdefghijkmnpqrstuvwxyz",
                     digits = "23456789", special = "!@#$%&*";
            var all = upper + lower + digits + special;

            var chars = new List<char>
            {
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                special[RandomNumberGenerator.GetInt32(special.Length)]
            };
            while (chars.Count < 12) chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

            // перемешать
            for (var i = chars.Count - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars.ToArray());
        }
    }
}
