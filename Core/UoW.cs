using Core.Entity;
using Core.Interfaces;
using Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class UoW : IUoW
    {
        private readonly ContextAuth _context;
        private bool _disposed;

        //репозитории
        private IGenericRepo<AbsenceNotice> _absenceNotice;
        private IGenericRepo<Attendance> _attendance;
        private IGenericRepo<Coach> _coach;
        private IGenericRepo<Notification> _notification;
        private IGenericRepo<Payment> _payment;
        private IGenericRepo<Player> _player;
        private IGenericRepo<Skill> _skill;
        private IGenericRepo<SkillAssessment> _skillAssessment;
        private IGenericRepo<SkillScore> _skillScore;
        
        private IGenericRepo<Subscription> _subscription;        
        private IGenericRepo<Training> _trainig;
        private IGenericRepo<TrainingGroup> _trainigGroup;
        private IGenericRepo<Venue> _venue;


        public UoW(ContextAuth context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public IRepo<AbsenceNotice> AbsenceNotices
        {
            get { return _absenceNotice ??= new GenericRepo<AbsenceNotice>(_context); }
        }
        public IRepo<Attendance> Attendances
        {
            get { return _attendance ??= new GenericRepo<Attendance>(_context); }
        }
        public IRepo<Coach> Coaches
        {
            get { return _coach ??= new GenericRepo<Coach>(_context); }
        }
        public IRepo<Notification> Notifications
        {
            get { return _notification ??= new GenericRepo<Notification>(_context); }
        }
        public IRepo<Payment> Payments
        {
            get { return _payment ??= new GenericRepo<Payment>(_context); }
        }
        public IRepo<Player> Players{ 
            get { return _player ??= new GenericRepo<Player>(_context); }
        }
        public IRepo<Skill> Skills
        {
            get { return _skill ??= new GenericRepo<Skill>(_context); }
        }
        public IRepo<SkillAssessment> SkillAssessments
        {
            get { return _skillAssessment ??= new GenericRepo<SkillAssessment>(_context); }
        }
        public IRepo<SkillScore> SkillScores
        {
            get { return _skillScore ??= new GenericRepo<SkillScore>(_context); }
        }
        public IRepo<Subscription> Subscriptions
        {
            get { return _subscription ??= new GenericRepo<Subscription>(_context); }
        }
        public IRepo<Training> Trainings
        {
            get { return _trainig ??= new GenericRepo<Training>(_context); }
        }
        public IRepo<TrainingGroup> Groups
        {
            get { return _trainigGroup ??= new GenericRepo<TrainingGroup>(_context); }
        }
        public IRepo<Venue> Venues
        {
            get { return _venue ??= new GenericRepo<Venue>(_context); }
        }

        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}
        //protected virtual void Dispose(bool disposing)
        //{
        //    if (!_disposed && disposing)
        //    {
        //        _context.Dispose();
        //    }
        //    _disposed = true;
        //}

        //public async ValueTask DisposeAsync()
        //{
        //    await DisposeAsync(true);
        //    GC.SuppressFinalize(this);
        //}
        //protected virtual async ValueTask DisposeAsync(bool disposing)
        //{
        //    if (!_disposed && disposing)
        //    {
        //        await _context.DisposeAsync();
        //    }
        //    _disposed = true;
        //}

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
