using Domain.Entity;
using Domain.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public class ParentRepository : GenericRepository<Parent>, IParentRepository
    {
        public ParentRepository(Context context) : base(context)
        {
        }
    }
}
