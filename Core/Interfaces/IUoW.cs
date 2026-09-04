using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    //public interface IUoW : IDisposable, IAsyncDisposable
    public interface IUoW 
    {
        IRepo<AbsenceNotice> AbsenceNotices{ get; }        
        IRepo<Attendance> Attendances{ get; }        
        IRepo<Coach> Coaches{ get; }
        IRepo<TrainingGroup> Groups { get; }
        IRepo<Notification> Notifications{ get; }
        IRepo<Payment> Payments{ get; }
        IRepo<Player> Players { get; }
        IRepo<Skill> Skills { get; }
        IRepo<SkillAssessment> SkillAssessments { get; }
        IRepo<SkillScore> SkillScores { get; }
        IRepo<Subscription> Subscriptions{ get; }
        IRepo<Training> Trainings{ get; }        
        IRepo<Venue> Venues{ get; }

        //IRepo<AppUser> Users{ get; }
        //IRepo<AppRole> Role{ get; }

        //IRepo<Player> Players{ get; }
        //IRepo<Group> Groups { get; }
        //IRepo<Manager> Managers{ get; }
        //IRepo<Parent> Parents { get; }

        // Основные методы
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
    }
}
