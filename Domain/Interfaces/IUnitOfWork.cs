using Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        // Репозитории
        IGenericRepository<Achievement> Achievements { get; }
        IGenericRepository<Attendance> Attendances { get; }
        IGenericRepository<AuditLog> AuditLogs { get; }
        IGenericRepository<Coach> Coachs { get; }
        IGenericRepository<Group> Groups { get; }
        IGenericRepository<Message> Messages { get; }
        IGenericRepository<Notification> Notifications { get; }
        IGenericRepository<Parent> Parents { get; }
        IGenericRepository<Payment> Payments { get; }
        IGenericRepository<Player> Players { get; }
        IGenericRepository<PlayerAchievement> PlayerAchievements{ get; }
        IGenericRepository<Schedule> Shedules { get; }
        IGenericRepository<Score> Scores { get; }
        IGenericRepository<Subscription> Subscriptions { get; }
        IGenericRepository<TrainingSession> TrainingSessions { get; }
        IGenericRepository<User> Users { get; }

        // Основные методы
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);



    }
}
