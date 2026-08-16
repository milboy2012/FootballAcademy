using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    // Связь игрока с достижением (когда разблокировано)
    public class PlayerAchievement : BaseEntity
    {
        public Guid PlayerId { get; private set; }
        public Player Player { get; private set; } = null!;

        public Guid AchievementId { get; private set; }
        public Achievement Achievement { get; private set; } = null!;

        public DateTime UnlockedAt { get; private set; }

        // EF Core
        private PlayerAchievement() { } 

        public PlayerAchievement(Guid playerId, Guid achievementId)
        {
            PlayerId = playerId;
            AchievementId = achievementId;
            UnlockedAt = DateTime.UtcNow;
        }
    }
}
