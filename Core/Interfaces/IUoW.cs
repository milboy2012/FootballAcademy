using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IUoW : IDisposable, IAsyncDisposable
    {
        IRepo<Coach> Coaches{ get; }
        IRepo<Player> Players{ get; }
        IRepo<Group> Groups { get; }
        IRepo<Manager> Managers{ get; }
        IRepo<Parent> Parents { get; }

        // Основные методы
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
    }
}
