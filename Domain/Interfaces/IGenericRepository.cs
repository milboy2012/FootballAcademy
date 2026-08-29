using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IGenericRepository<T> : IRepository<T> where T : class
    {
        // Дополнительные методы для специфичных запросов
        //Task<IReadOnlyList<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        //Task<IReadOnlyList<T>> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
        //Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    }
}
