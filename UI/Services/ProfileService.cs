using Core.Entity;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UI.Models.ViewModels.Profile;
using UI.Services.Interfaces;

namespace UI.Services
{
    public class ProfileService : IProfileService
    {
        private static readonly string[] AllowedExt = [".jpg", ".jpeg", ".png", ".webp"];
        private const long MaxAvatarBytes = 2 * 1024 * 1024;

        private readonly IUoW _data;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ProfileService(IUoW data, UserManager<AppUser> userManager, IWebHostEnvironment env)
        { _data = data; _userManager = userManager; _env = env; }

        public async Task<ProfileDto?> GetAsync(Guid userId, CancellationToken ct)
        {
            var u = await _userManager.FindByIdAsync(userId.ToString());
            if (u is null) return null;
            var role = (await _userManager.GetRolesAsync(u)).FirstOrDefault() ?? "";

            var dto = new ProfileDto
            {
                Email = u.Email!,
                UserName = u.UserName!,
                Role = role,
                FirstName = u.FirstName,
                LastName = u.LastName,
                MiddleName = u.MiddleName,
                Phone = u.PhoneNumber,
                BirthDate = u.BirthDate,
                City = u.City,
                About = u.About,
                AvatarUrl = u.AvatarPath,
                NotifyByEmail = u.NotifyByEmail,
                CreatedAt = u.CreatedAt
            };

            switch (role)
            {
                case "Coach":
                    dto.Coach = await _data.Coaches.Query().AsNoTracking().Where(c => c.UserId == userId).Select(c => new CoachProfileDto
                    {
                        Qualification = c.Qualification,
                        ExperienceYears = c.ExperienceYears,
                        Bio = c.Bio,
                        Achievements = c.Achievements,
                        HiredAt = c.HiredAt,
                        Groups = c.Groups.Where(g => !g.IsArchived).OrderBy(g => g.Name).Select(g => g.Name).ToList()
                    }).FirstOrDefaultAsync(ct) ?? new CoachProfileDto();
                    break;
                case "Parent":
                    dto.Parent = await _data.ParentProfiles.Query().AsNoTracking().Where(p => p.UserId == userId).Select(p => new ParentProfileDto
                    {
                        SecondPhone = p.SecondPhone,
                        EmergencyContactName = p.EmergencyContactName,
                        EmergencyContactPhone = p.EmergencyContactPhone,
                        Address = p.Address,
                        Notes = p.Notes
                    }).FirstOrDefaultAsync(ct) ?? new ParentProfileDto();
                    break;
                case "Player":
                    dto.Player = await _data.Players.Query().AsNoTracking().Where(p => p.UserId == userId).Select(p => new PlayerProfileDto
                    {
                        GroupName = p.Group != null ? p.Group.Name : null,
                        CoachName = p.Group != null ? p.Group.Coach.User.FirstName + " " + p.Group.Coach.User.LastName : null,
                        ParentName = p.Parent.FirstName + " " + p.Parent.LastName,
                        MedicalUntil = p.MedicalCertificateUntil
                    }).FirstOrDefaultAsync(ct);
                    dto.BirthDate ??= await _data.Players.Query().Where(p => p.UserId == userId).Select(p => (DateOnly?)p.BirthDate).FirstOrDefaultAsync(ct);
                    break;
            }
            return dto;
        }

        public async Task<string?> UpdateAsync(Guid userId, UpdateProfileRequest r, CancellationToken ct)
        {
            var u = await _userManager.FindByIdAsync(userId.ToString());
            if (u is null) return "Пользователь не найден";
            if (string.IsNullOrWhiteSpace(r.FirstName) || string.IsNullOrWhiteSpace(r.LastName)) return "Имя и фамилия обязательны";
            if (r.BirthDate is not null && (r.BirthDate > DateOnly.FromDateTime(DateTime.UtcNow) || r.BirthDate < new DateOnly(1930, 1, 1))) return "Некорректная дата рождения";
            if (r.About?.Length > 1000) return "Поле «О себе» — до 1000 символов";

            var role = (await _userManager.GetRolesAsync(u)).FirstOrDefault();
            var isPlayer = role == "Player";

            // Ребёнок не меняет ФИО и дату рождения — это делает родитель в карточке ученика
            if (!isPlayer)
            {
                u.FirstName = r.FirstName.Trim(); u.LastName = r.LastName.Trim(); u.MiddleName = r.MiddleName?.Trim();
                u.BirthDate = r.BirthDate;
            }
            u.PhoneNumber = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim();
            u.City = r.City?.Trim(); u.About = r.About?.Trim(); u.NotifyByEmail = r.NotifyByEmail;
            var res = await _userManager.UpdateAsync(u);
            if (!res.Succeeded) return string.Join("; ", res.Errors.Select(e => e.Description));

            if (role == "Coach" && r.Coach is not null)
            {
                var c = await _data.Coaches.Query().FirstOrDefaultAsync(x => x.UserId == userId, ct) ?? await _data.Coaches.AddAsync(new Coach { UserId = userId });
                c.Qualification = r.Coach.Qualification?.Trim(); c.ExperienceYears = r.Coach.ExperienceYears is < 0 or > 60 ? null : r.Coach.ExperienceYears;
                c.Bio = r.Coach.Bio?.Trim(); c.Achievements = r.Coach.Achievements?.Trim();
            }
            if (role == "Parent" && r.Parent is not null)
            {
                var p = await _data.ParentProfiles.Query().FirstOrDefaultAsync(x => x.UserId == userId, ct) ?? await _data.ParentProfiles.AddAsync(new ParentProfile { UserId = userId });
                p.SecondPhone = r.Parent.SecondPhone?.Trim(); p.EmergencyContactName = r.Parent.EmergencyContactName?.Trim();
                p.EmergencyContactPhone = r.Parent.EmergencyContactPhone?.Trim(); p.Address = r.Parent.Address?.Trim(); p.Notes = r.Parent.Notes?.Trim();
            }
            await _data.SaveChangesAsync(ct);
            return null;
        }

        public async Task<(string?, string?)> SetAvatarAsync(Guid userId, IFormFile file, CancellationToken ct)
        {
            if (file.Length == 0) return (null, "Файл пуст");
            if (file.Length > MaxAvatarBytes) return (null, "Файл больше 2 МБ");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext)) return (null, "Допустимы JPG, PNG, WEBP");
            if (!file.ContentType.StartsWith("image/")) return (null, "Файл не является изображением");

            var dir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(dir);
            var name = $"{userId}{ext}";
            // удаляем старые с другим расширением
            foreach (var old in Directory.GetFiles(dir, $"{userId}.*")) File.Delete(old);
            await using (var fs = File.Create(Path.Combine(dir, name))) await file.CopyToAsync(fs, ct);

            var u = await _userManager.FindByIdAsync(userId.ToString());
            u!.AvatarPath = $"/uploads/avatars/{name}?v={DateTime.UtcNow.Ticks}";
            await _userManager.UpdateAsync(u);
            return (u.AvatarPath, null);
        }

        public async Task<string?> RemoveAvatarAsync(Guid userId, CancellationToken ct)
        {
            var u = await _userManager.FindByIdAsync(userId.ToString());
            if (u is null) return "Пользователь не найден";
            var dir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            if (Directory.Exists(dir)) foreach (var f in Directory.GetFiles(dir, $"{userId}.*")) File.Delete(f);
            u.AvatarPath = null; await _userManager.UpdateAsync(u);
            return null;
        }

        public async Task<string?> ChangePasswordAsync(Guid userId, ChangePasswordRequest r, CancellationToken ct)
        {
            var u = await _userManager.FindByIdAsync(userId.ToString());
            if (u is null) return "Пользователь не найден";
            var res = await _userManager.ChangePasswordAsync(u, r.CurrentPassword, r.NewPassword);
            if (!res.Succeeded) return string.Join(" ", res.Errors.Select(e => e.Description));
            u.MustChangePassword = false; await _userManager.UpdateAsync(u);
            return null;
        }
    }
}
