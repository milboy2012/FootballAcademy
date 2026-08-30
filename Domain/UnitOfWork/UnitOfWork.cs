using Domain.Entity;
using Domain.Interfaces;
using Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly Context _context;
        private bool _disposed;

        //Репозитории
        private IGenericRepository<Achievement> _achievement;
        private IGenericRepository<Attendance> _attendance;
        private IGenericRepository<AuditLog> _auditLog;
        private IGenericRepository<Coach> _coach;  
        private IGenericRepository<Group> _group;
        private IGenericRepository<Message> _message;
        private IGenericRepository<Notification> _notification;
        private IGenericRepository<Parent> _parent;
        private IGenericRepository<Payment> _payment;
        private IGenericRepository<Player> _player;
        private IGenericRepository<PlayerAchievement> _playerAchievement;
        private IGenericRepository<Schedule> _schedule;
        private IGenericRepository<Score> _score;
        private IGenericRepository<Subscription> _subscription;
        private IGenericRepository<TrainingSession> _trainingSession;
        private IGenericRepository<User> _user;


        public UnitOfWork(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IGenericRepository<Achievement> Achievements
        {
            get { return _achievement ??= new GenericRepository<Achievement>(_context); }
        }
        public IGenericRepository<Attendance> Attendances { 
            get {  return _attendance ??= new GenericRepository<Attendance>(_context);}
        }

        public IGenericRepository<AuditLog> AuditLogs
        {
            get { return _auditLog ??= new GenericRepository<AuditLog>(_context); }
        }

        public IGenericRepository<Coach> Coachs
        {
            get { return _coach ??= new GenericRepository<Coach>(_context); }
        }

        public IGenericRepository<Group> Groups
        {
            get { return _group  ??= new GenericRepository<Group>(_context);}
        }

        public IGenericRepository<Message> Messages
        {
            get { return _message ??= new GenericRepository<Message>(_context);}
        }

        public IGenericRepository<Notification> Notifications
        {
            get {  return _notification ??= new GenericRepository<Notification>(_context);}
        }

        public IGenericRepository<Parent> Parents
        {
            get { return _parent ??= new GenericRepository<Parent>(_context);}
        }

        public IGenericRepository<Payment> Payments
        {
            get { return _payment ??= new GenericRepository<Payment>(_context);}
        }

        public IGenericRepository<Player> Players
        {
            get { return _player ??= new GenericRepository<Player>(_context);}
        }

        public IGenericRepository<PlayerAchievement> PlayerAchievements
        {
            get { return _playerAchievement ??= new GenericRepository<PlayerAchievement>(_context); }
        }

        public IGenericRepository<Schedule> Shedules
        {
            get { return _schedule ??= new GenericRepository<Schedule>(_context);}
        }

        public IGenericRepository<Score> Scores
        {
            get { return _score ??= new GenericRepository<Score>(_context);}
        }

        public IGenericRepository<Subscription> Subscriptions
        {
            get { return _subscription ??= new GenericRepository<Subscription>(_context);}
        }

        public IGenericRepository<TrainingSession> TrainingSessions
        {
            get { return _trainingSession ??= new GenericRepository<TrainingSession>(_context);}
        }

        public IGenericRepository<User> Users
        {
            get { return _user ??= new GenericRepository<User>(_context);}
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }
        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!_disposed && disposing)
            {
                await _context.DisposeAsync();
            }
            _disposed = true;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            return await SaveChangesAsync(cancellationToken) > 0;
        }
    }
}
