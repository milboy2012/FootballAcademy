using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entity
{
    // Группа игроков по возрасту и уровню
    public class Group : BaseEntity
    {
        // U-7, U-9, и т.д.
        public string Name { get; private set; }

        // Минимальный возраст
        public int AgeMin { get; private set; }

        // Максимальный возраст
        public int AgeMax { get; private set; }

        // Начинающие, продвинутые, элита
        public GroupLevel Level { get; private set; }

        // Максимум игроков (12-15)
        public int MaxPlayers { get; private set; }       

        //Описание
        public string? Description { get; private set; }

        // Дни недели (ПН, ВТ, СР, ЧТ, ПТ, СБ)
        public string? TrainingDays { get; private set; }

        // Длительность тренировки (мин)
        public int DurationMinutes { get; private set; }

        // внешние ключи
        public ICollection<TrainingSession> TrainingSessions { get; private set; } = new List<TrainingSession>();
        public Guid? PlayerId { get; private set; }
        public Player Player { get; private set; }
        public Guid? CoachId { get; private set; }
        public Coach Coach { get; private set; }
        public ICollection<Schedule> Schedules { get; private set; } = new List<Schedule>();        

        // EF Core
        private Group() { }

        public Group(string name, int ageMin, int ageMax, GroupLevel level, int maxPlayers = 12, int durationMinutes = 90, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название группы обязательно", nameof(name));
            if (ageMin < 0 || ageMax < ageMin)
                throw new ArgumentException("Некорректный возрастной диапазон");
            if (maxPlayers < 5 || maxPlayers > 20)
                throw new ArgumentException("Количество игроков должно быть от 5 до 20");

            Name = name;
            AgeMin = ageMin;
            AgeMax = ageMax;
            Level = level;
            MaxPlayers = maxPlayers;
            DurationMinutes = durationMinutes;
            Description = description;
        }

        // Методы
        public void AssignCoach(Coach coach)
        {
            Coach = coach ?? throw new ArgumentNullException(nameof(coach));
            CoachId = coach.Id;
            UpdateTimestamp();
        }

        public bool CanAddPlayer(Player player)
        {
            if (player == null) return false;

            // Проверяем возраст
            var age = player.GetAge(); // нужно добавить метод в Player
            if (age < AgeMin || age > AgeMax)
                return false;

            // Проверяем количество игроков
            if (Players.Count >= MaxPlayers)
                return false;

            return true;
        }

        public void UpdateDetails(
            string name,
            int ageMin,
            int ageMax,
            GroupLevel level,
            int maxPlayers,
            string? description)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            AgeMin = ageMin >= 0 ? ageMin : throw new ArgumentException("Возраст не может быть отрицательным");
            AgeMax = ageMax >= AgeMin ? ageMax : throw new ArgumentException("Максимальный возраст должен быть больше минимального");
            Level = level;
            MaxPlayers = maxPlayers >= 5 && maxPlayers <= 20 ? maxPlayers : throw new ArgumentException("Количество игроков должно быть от 5 до 20");
            Description = description;
            UpdateTimestamp();
        }

        public int GetAvailableSlots()
        {
            return Math.Max(0, MaxPlayers - Players.Count);
        }

        public string GetAgeRangeDisplay()
        {
            return $"{AgeMin}-{AgeMax} лет";
        }

    }
}
