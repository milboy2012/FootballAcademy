using Domain.Enums;
using Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public class Player : User
    {
        // Личные данные
        //Дата рождения
        public DateOnly BirthDate { get; private set; }
        //Возраст
        public int Age => CalculateAge();
        // Аллергии, противопоказания
        public string? MedicalNotes { get; private set; } 
        //Ссылка на фото
        public string? PhotoUrl { get; private set; }

        // Спортивные данные
        // Техника (0-10)
        public decimal TechScore { get; private set; }
        // Физподготовка (0-10)
        public decimal PhysScore { get; private set; }
        // Тактика (0-10)
        public decimal TacticScore { get; private set; }
        // Психология (0-10)
        public decimal PsychologyScore { get; private set; } 

        // Рейтинг (вычисляемое поле)
        public decimal Rating { get; private set; }
        public DateTime? RatingCalculatedAt { get; private set; }

        // Игровая статистика (сезон)
        // Голы
        public int Goals { get; private set; }
        // Ассисты
        public int Assists { get; private set; }
        // Сыгранные матчи
        public int MatchesPlayed { get; private set; }
        // Лучший игрок матча
        public int ManOfMatch { get; private set; }

        // Геймификация
        // Очки опыта
        public int ExperiencePoints { get; private set; }
        // Уровень (1-100)
        public int Level { get; private set; }
        // Виртуальные монетки
        public int VirtualCoins { get; private set; }      

        // Навигационные свойства
        public Guid? GroupId { get; private set; }
        public Group? Group { get; private set; }

        public Guid? CoachId { get; private set; }
        public Coach? Coach { get; private set; }

        public Guid ParentId { get; private set; }
        public Parent? Parent { get; private set; }

        public ICollection<Attendance> Attendances { get; private set; } = new List<Attendance>();
        public ICollection<Score> Scores { get; private set; } = new List<Score>();
        public ICollection<PlayerAchievement> Achievements { get; private set; } = new List<PlayerAchievement>();
        public ICollection<Message> Messages { get; private set; } = new List<Message>();

        // EF Core
        private Player() { }

        // метод создания игрока
        public static Player Create(Email email, FullName fullName,DateOnly birthDate,Parent parent, Guid? groupId = null, PhoneNumber? phone = null, string? medicalNotes = null)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            var player = new Player
            {
                Email = email,
                FullName = fullName,
                BirthDate = birthDate,
                ParentId = parent.Id,
                Parent = parent,
                GroupId = groupId,
                Phone = phone,
                MedicalNotes = medicalNotes,
                Role = UserRole.Player,
                IsActive = true,
                ExperiencePoints = 0,
                Level = 1,
                VirtualCoins = 0
            };

            // Добавляем игрока к родителю
            parent.AddChild(player);

            return player;
        }

        // Обновление спортивных оценок (тренер)
        public void UpdateScores(decimal tech, decimal phys, decimal tactic, decimal psychology)
        {
            ValidateScore(tech);
            ValidateScore(phys);
            ValidateScore(tactic);
            ValidateScore(psychology);

            TechScore = Math.Round(tech, 1);
            PhysScore = Math.Round(phys, 1);
            TacticScore = Math.Round(tactic, 1);
            PsychologyScore = Math.Round(psychology, 1);

            UpdateTimestamp();
            RecalculateRating();
        }

        // Пересчет рейтинга игрока
        // Формула: (Техника × 0.4) + (Физо × 0.3) + (Тактика × 0.2) + (Психология × 0.1) + Бонус посещаемости
        public void RecalculateRating()
        {
            // Базовый рейтинг
            var baseRating = (TechScore * 0.4m) + (PhysScore * 0.3m) +
                             (TacticScore * 0.2m) + (PsychologyScore * 0.1m);

            // Бонус за посещаемость (макс +1)
            var attendanceBonus = CalculateAttendanceBonus();

            // Бонус за достижения (макс +0.5)
            var achievementBonus = CalculateAchievementBonus();

            Rating = Math.Min(10, Math.Round(baseRating + attendanceBonus + achievementBonus, 1));
            RatingCalculatedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        private decimal CalculateAttendanceBonus()
        {
            if (!Attendances.Any()) return 0;

            var total = Attendances.Count;
            var present = Attendances.Count(a => a.Status == AttendanceStatus.Present);
            var rate = (decimal)present / total;

            return rate switch
            {
                >= 0.9m => 1.0m,      // 90%+ посещений
                >= 0.75m => 0.5m,     // 75-89%
                _ => 0m
            };
        }

        private decimal CalculateAchievementBonus()
        {
            // Каждое достижение дает +0.05 к рейтингу (макс 0.5)
            return Math.Min(0.5m, Achievements.Count * 0.05m);
        }

        // Добавить статистику с матча
        public void AddMatchStats(int goals = 0, int assists = 0, bool manOfMatch = false)
        {
            Goals += goals;
            Assists += assists;
            MatchesPlayed++;

            if (manOfMatch)
            {
                ManOfMatch++;
                AddExperience(30); // Бонус за лучшего игрока
            }

            // Бонус за голы и ассисты
            AddExperience(goals * 10 + assists * 5);
            UpdateTimestamp();
        }

        // Добавить очки опыта (геймификация)
        public void AddExperience(int points)
        {
            if (points <= 0) return;

            ExperiencePoints += points;

            // Повышение уровня: каждые 100 XP = +1 уровень
            var newLevel = 1 + (ExperiencePoints / 100);
            if (newLevel > Level)
            {
                Level = newLevel;
                // За повышение уровня даем монетки
                VirtualCoins += Level * 5;
            }

            UpdateTimestamp();
        }

        // Отметить посещаемость
        public void MarkAttendance(AttendanceStatus status, string? comment = null)
        {
            var attendance = new Attendance(
                Id,
                DateOnly.FromDateTime(DateTime.UtcNow),
                status,
                comment
            );

            Attendances.Add(attendance);
            UpdateTimestamp();

            // Бонус за посещение
            if (status == AttendanceStatus.Present)
            {
                AddExperience(10);
                VirtualCoins += 5;
            }
        }

        // Разблокировать достижение
        public void UnlockAchievement(Achievement achievement)
        {
            if (achievement == null) throw new ArgumentNullException(nameof(achievement));

            if (Achievements.Any(a => a.AchievementId == achievement.Id))
                throw new DomainException($"Достижение '{achievement.Name}' уже разблокировано");

            var playerAchievement = new PlayerAchievement(Id, achievement.Id);
            Achievements.Add(playerAchievement);

            AddExperience(achievement.ExperienceReward);
            VirtualCoins += achievement.CoinReward;

            UpdateTimestamp();
        }

        // Проверить наличие достижения
        public bool HasAchievement(Guid achievementId)
        {
            return Achievements.Any(a => a.AchievementId == achievementId);
        }

        // Зачислить в группу
        public void EnrollToGroup(Group group, Coach coach)
        {
            if (group == null) throw new ArgumentNullException(nameof(group));
            if (coach == null) throw new ArgumentNullException(nameof(coach));

            if (GroupId != null && GroupId != group.Id)
                throw new DomainException("Игрок уже в другой группе. Сначала переведите его");

            GroupId = group.Id;
            Group = group;
            CoachId = coach.Id;
            Coach = coach;

            UpdateTimestamp();
        }

        // Перевести в другую группу
        public void TransferToGroup(Group newGroup, Coach newCoach)
        {
            if (newGroup == null) throw new ArgumentNullException(nameof(newGroup));
            if (newCoach == null) throw new ArgumentNullException(nameof(newCoach));

            // Сохраняем историю перевода (можно добавить отдельную сущность TransferHistory)
            GroupId = newGroup.Id;
            Group = newGroup;
            CoachId = newCoach.Id;
            Coach = newCoach;

            UpdateTimestamp();
        }

        // Вычислить возраст
        private int CalculateAge()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = today.Year - BirthDate.Year;
            if (BirthDate > today.AddYears(-age)) age--;
            return age;
        }

        private static void ValidateScore(decimal score)
        {
            if (score < 0 || score > 10)
                throw new ArgumentOutOfRangeException(nameof(score), "Оценка должна быть от 0 до 10");
        }

        // Получить уровень в строковом представлении
        public string GetLevelTitle()
        {
            return Level switch
            {
                <= 10 => "Новичок",
                <= 25 => "Любитель",
                <= 50 => "Продвинутый",
                <= 75 => "Профи",
                _ => "Мастер"
            };
        }
    }
}
