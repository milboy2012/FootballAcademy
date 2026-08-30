using Domain.Entity;
using Domain.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public class CoachRepository : GenericRepository<Coach>, ICoachRepository
    {
        public CoachRepository(Context context) : base(context)
        {
        }
    }
}
