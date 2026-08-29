using Domain.Enums;
using Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    // Платеж родителя
    public class Payment : BaseEntity
    {
        public Money Amount { get; private set; }
        public string Description { get; private set; }
        public PaymentStatus Status { get; private set; }

        // ID от платежной системы
        public string? TransactionId { get; private set; } 
        public DateTime? PaidAt { get; private set; }

        // Для счетов на оплату
        public DateTime? ExpiresAt { get; private set; }
        // Ссылка на чек
        public string? ReceiptUrl { get; private set; } 

        // Внешние ключи
        public Guid ParentId { get; private set; }
        public Parent Parent { get; private set; } = null!;

        public Guid? PlayerId { get; private set; } // Если платеж за конкретного игрока
        public Player? Player { get; private set; }

        public Guid? SubscriptionId { get; private set; } // Если это подписка
        public Subscription? Subscription { get; private set; }

        // EF Core
        private Payment() { } 


        public Payment(Guid parentId, Money amount, string description, DateTime? expiresAt = null, Guid? playerId = null, Guid? subscriptionId = null)
        {
            if (amount.Amount <= 0)
                throw new ArgumentException("Сумма платежа должна быть больше 0");
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Описание платежа обязательно");

            ParentId = parentId;
            Amount = amount;
            Description = description;
            Status = PaymentStatus.Pending;
            ExpiresAt = expiresAt;
            PlayerId = playerId;
            SubscriptionId = subscriptionId;
        }

        // Методы
        public void MarkAsPaid(string transactionId, string? receiptUrl = null)
        {
            Status = PaymentStatus.Paid;
            TransactionId = transactionId ?? throw new ArgumentNullException(nameof(transactionId));
            PaidAt = DateTime.UtcNow;
            ReceiptUrl = receiptUrl;
            UpdateTimestamp();
        }

        public void MarkAsFailed(string reason)
        {
            Status = PaymentStatus.Failed;
            UpdateTimestamp();
        }

        public void MarkAsRefunded()
        {
            Status = PaymentStatus.Refunded;
            UpdateTimestamp();
        }

        public bool IsOverdue()
        {
            return Status == PaymentStatus.Pending &&
                   ExpiresAt.HasValue &&
                   DateTime.UtcNow > ExpiresAt.Value;
        }
    }
}
