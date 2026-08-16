using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    // Оценки игрока за конкретную тренировку
    public class Score : BaseEntity
    {
        public decimal TechScore { get; private set; }
        public decimal PhysScore { get; private set; }
        public decimal TacticScore { get; private set; }
        public decimal PsychologyScore { get; private set; }
        public DateOnly Date { get; private set; }
        public string? CoachComment { get; private set; }

        // Связи
        public Guid PlayerId { get; private set; }
        public Player Player { get; private set; } = null!;

        public Guid TrainingSessionId { get; private set; }
        public TrainingSession TrainingSession { get; private set; } = null!;

        // EF Core
        private Score() { }

        public Score(Guid playerId, Guid trainingSessionId, DateOnly date, decimal techScore, decimal physScore, decimal tacticScore, decimal psychologyScore, string? coachComment = null)
        {
            ValidateScore(techScore);
            ValidateScore(physScore);
            ValidateScore(tacticScore);
            ValidateScore(psychologyScore);

            PlayerId = playerId;
            TrainingSessionId = trainingSessionId;
            Date = date;
            TechScore = Math.Round(techScore, 1);
            PhysScore = Math.Round(physScore, 1);
            TacticScore = Math.Round(tacticScore, 1);
            PsychologyScore = Math.Round(psychologyScore, 1);
            CoachComment = coachComment;
        }

        private static void ValidateScore(decimal score)
        {
            if (score < 0 || score > 10)
                throw new ArgumentOutOfRangeException(nameof(score), "Оценка должна быть от 0 до 10");
        }

        public void UpdateComment(string comment)
        {
            CoachComment = comment;
            UpdateTimestamp();
        }
    }
}
