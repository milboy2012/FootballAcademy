using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    // Достижение(ачивка) для игроков
    public class Achievement : BaseEntity
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public AchievementType Type { get; private set; }
        // Иконка достижения
        public string IconUrl { get; private set; }
        // Опыт за достижение
        public int ExperienceReward { get; private set; }
        // Монетки за достижение
        public int CoinReward { get; private set; }
        // Требуемое значение (например, 10 голов)
        public int RequiredValue { get; private set; } 
        public bool IsActive { get; private set; }

        // Связи
        public ICollection<PlayerAchievement> Players { get; private set; } = new List<PlayerAchievement>();

        // EF Core
        private Achievement() { } 

        public Achievement(string name, string description, AchievementType type, int experienceReward, int coinReward, int requiredValue = 0, string? iconUrl = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название достижения обязательно");
            if (experienceReward < 0 || coinReward < 0)
                throw new ArgumentException("Награды не могут быть отрицательными");

            Name = name;
            Description = description;
            Type = type;
            ExperienceReward = experienceReward;
            CoinReward = coinReward;
            RequiredValue = requiredValue;
            IconUrl = iconUrl ?? "/icons/default-achievement.png";
            IsActive = true;
        }
    }
}
