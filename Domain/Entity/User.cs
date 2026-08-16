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
        public Email Email { get; private set; }
        public string PasswordHash { get; private set; } = string.Empty;
        public PhoneNumber? Phone { get; private set; }
        public FullName FullName { get; private set; }

        // Статус
        public bool IsEmailConfirmed { get; private set; }
        public bool IsPhoneConfirmed { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime? LastLoginAt { get; private set; }
        public string? RefreshToken { get; private set; }
        public DateTime? RefreshTokenExpiry { get; private set; }

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
