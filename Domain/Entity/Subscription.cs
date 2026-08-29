using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    // Подписка родителя на абонемент
    public class Subscription : BaseEntity
    {
        public SubscriptionType Type { get; private set; }
        public decimal Price { get; private set; }
        public int DurationDays { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        // За сколько дней до окончания продлевать
        public int AutoRenewDays { get; private set; }

        // Внешние ключи
        public Guid PlayerId { get; private set; }
        public Player Player { get; private set; } = null!;

        public ICollection<Payment> Payments { get; private set; } = new List<Payment>();

        public Guid ParentId { get; private set; }
        public Parent Parent { get; private set; } = null!;

        

        

        // EF Core
        private Subscription() { }

        public Subscription(Guid parentId, Guid playerId, SubscriptionType type, decimal price, int durationDays, int autoRenewDays = 3)
        {
            ParentId = parentId;
            PlayerId = playerId;
            Type = type;
            Price = price > 0 ? price : throw new ArgumentException("Цена должна быть больше 0");
            DurationDays = durationDays > 0 ? durationDays : throw new ArgumentException("Длительность должна быть больше 0");
            AutoRenewDays = autoRenewDays;
            IsActive = true;
            StartDate = DateTime.UtcNow;
            EndDate = StartDate.AddDays(DurationDays);
        }

        // Бизнес-методы
        public void Renew()
        {
            if (!IsActive)
                throw new Exception("Подписка неактивна, продление невозможно");

            StartDate = DateTime.UtcNow;
            EndDate = StartDate.AddDays(DurationDays);
            UpdateTimestamp();
        }

        public void Cancel()
        {
            IsActive = false;
            UpdateTimestamp();
        }

        public void Activate()
        {
            IsActive = true;
            UpdateTimestamp();
        }

        public bool IsExpired()
        {
            return EndDate.HasValue && DateTime.UtcNow > EndDate.Value;
        }

        public bool ShouldAutoRenew()
        {
            return IsActive &&
                   EndDate.HasValue &&
                   DateTime.UtcNow >= EndDate.Value.AddDays(-AutoRenewDays);
        }

        public int GetRemainingDays()
        {
            if (!EndDate.HasValue) return 0;
            return Math.Max(0, (int)(EndDate.Value - DateTime.UtcNow).TotalDays);
        }

    }
}
