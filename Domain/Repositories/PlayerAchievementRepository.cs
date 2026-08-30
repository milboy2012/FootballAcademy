using Domain.Entity;
using Domain.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public class PlayerAchievementRepository : GenericRepository<PlayerAchievement>, IPlayerAchievementRepository
    {
        public PlayerAchievementRepository(Context context) : base(context)
        {
        }
    }
}
