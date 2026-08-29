using Domain.Enums;
using Domain.Model;
using Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public class Parent : User
    {
        // свойства родителя 
        public string? AdditionalPhone { get; private set; } // Второй телефон
        public string? Address { get; private set; }
        public string? WorkPlace { get; private set; }

        // Внешние ключи
        // Связь с игроками (один родитель может иметь несколько детей в академии)
        public ICollection<Player> Children { get; private set; } = new List<Player>();
        public ICollection<Payment> Payments { get; private set; } = new List<Payment>();
        public Guid SubscriptionId { get; private set; }
        public Subscription Subscription { get; private set; }
        public Guid? UserId { get; private set; }
        public User User { get; private set; }


        // EF Core
        private Parent() { }

        public Parent(Email email,FullName fullName, PhoneNumber? phone = null,string? additionalPhone = null, string? address = null,string? workPlace = null) : base(email, fullName, UserRole.Parent, phone)
        {
            AdditionalPhone = additionalPhone;
            Address = address;
            WorkPlace = workPlace;
        }

        //Методы
        // Добавить ребенка
        public void AddChild(Player child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));

            if (Children.Any(c => c.Id == child.Id))
                throw new DomainException("Этот ребенок уже добавлен");

            Children.Add(child);
            UpdateTimestamp();
        }

        // Удалить ребенка
        public void RemoveChild(Guid childId)
        {
            var child = Children.FirstOrDefault(c => c.Id == childId);
            if (child == null)
                throw new DomainException("Ребенок не найден");

            Children.Remove(child);
            UpdateTimestamp();
        }
    }
}
