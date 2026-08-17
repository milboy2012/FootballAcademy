using Domain.Enums;
using Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public class User : BaseEntity
    {
        // Identity-данные
        public Email Email { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public PhoneNumber? Phone { get; set; }
        public FullName FullName { get; set; }

        // Статус
        public bool IsEmailConfirmed { get; set; }
        public bool IsPhoneConfirmed { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }

        // Роль 
        public UserRole Role { get; protected set; }

        // Навигационные свойства
        //public ICollection<Notification> Notifications { get; private set; } = new List<Notification>();
        //public ICollection<AuditLog> AuditLogs { get; private set; } = new List<AuditLog>();

        // EF Core
        protected User() { }

        protected User(
        Email email,
        FullName fullName,
        UserRole role,
        PhoneNumber? phone = null)
        {
            Email = email ?? throw new ArgumentNullException(nameof(email));
            FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
            Role = role;
            Phone = phone;
            IsActive = true;
            IsEmailConfirmed = false;
            IsPhoneConfirmed = false;
        }

        // методы
        public void SetPassword(string passwordHash)
        {
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        }

        public void ConfirmEmail()
        {
            IsEmailConfirmed = true;
            UpdateTimestamp();
        }

        public void ConfirmPhone()
        {
            IsPhoneConfirmed = true;
            UpdateTimestamp();
        }

        public void SetRefreshToken(string token, DateTime expiry)
        {
            RefreshToken = token;
            RefreshTokenExpiry = expiry;
            UpdateTimestamp();
        }

        public void ClearRefreshToken()
        {
            RefreshToken = null;
            RefreshTokenExpiry = null;
            UpdateTimestamp();
        }

        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void Block()
        {
            IsActive = false;
            UpdateTimestamp();
        }

        public void Unblock()
        {
            IsActive = true;
            UpdateTimestamp();
        }

        public void UpdateProfile(FullName fullName, PhoneNumber? phone)
        {
            FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
            Phone = phone;
            UpdateTimestamp();
        }
    }
}
