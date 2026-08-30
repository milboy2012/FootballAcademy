using Domain.Entity;
using Domain.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public class ScoreRepository : GenericRepository<Score>, IScoreRepository
    {
        public ScoreRepository(Context context) : base(context)
        {
        }
    }
}
