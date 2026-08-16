using Domain.Enums;
using Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public class Coach : User
    {
        // Тренерский опыт в годах
        public int ExperienceYears { get; private set; }
        // Специализация: вратари, защита, полузащита, нападение
        public string? Specialization { get; private set; }
        // UEFA C, B, A, PRO
        public string? LicenseLevel { get; private set; } 
        public string? Biography { get; private set; }
        public string? PhotoUrl { get; private set; }

        // Рейтинг тренера (отзывы родителей)
        public double AverageRating { get; private set; }
        public int TotalReviews { get; private set; }

        // Связи
        public ICollection<Group> Groups { get; private set; } = new List<Group>();
        public ICollection<Player> Players { get; private set; } = new List<Player>();

        // EF Core
        private Coach() { }
        public Coach(Email email,FullName fullName,int experienceYears,PhoneNumber? phone = null,string? specialization = null,string? licenseLevel = null) : base(email, fullName, UserRole.Coach, phone)
        {
            ExperienceYears = experienceYears >= 0 ? experienceYears : throw new ArgumentException("Опыт не может быть отрицательным");
            Specialization = specialization;
            LicenseLevel = licenseLevel;
            AverageRating = 0;
            TotalReviews = 0;
        }

        //методы
        public void UpdateProfessionalInfo(
        int experienceYears,
        string? specialization,
        string? licenseLevel,
        string? biography,
        string? photoUrl)
        {
            ExperienceYears = experienceYears >= 0 ? experienceYears : throw new ArgumentException("Опыт не может быть отрицательным");
            Specialization = specialization;
            LicenseLevel = licenseLevel;
            Biography = biography;
            PhotoUrl = photoUrl;
            UpdateTimestamp();
        }

        // Добавить отзыв
        public void AddReview(int rating)
        {
            if (rating < 1 || rating > 5)
                throw new ArgumentOutOfRangeException(nameof(rating), "Оценка должна быть от 1 до 5");

            var totalScore = AverageRating * TotalReviews + rating;
            TotalReviews++;
            AverageRating = Math.Round(totalScore / TotalReviews, 1);
            UpdateTimestamp();
        }

        // Назначить на группу
        public void AssignToGroup(Group group)
        {
            if (group == null) throw new ArgumentNullException(nameof(group));

            if (Groups.Any(g => g.Id == group.Id))
                throw new DomainException("Тренер уже назначен на эту группу");

            Groups.Add(group);
            UpdateTimestamp();
        }

        // Снять с группы
        public void RemoveFromGroup(Guid groupId)
        {
            var group = Groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null)
                throw new DomainException("Группа не найдена");

            Groups.Remove(group);
            UpdateTimestamp();
        }
    }
}
