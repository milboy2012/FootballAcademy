using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    // Фактическая тренировка (привязана к расписанию)
    public class TrainingSession : BaseEntity
    {
        public DateOnly Date { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }
        public string? Location { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsCancelled { get; private set; }
        public string? CancellationReason { get; private set; }

        // Заметки тренера о тренировке
        public string? CoachNotes { get; private set; }

        // Связи
        public Guid GroupId { get; private set; }
        public Group Group { get; private set; } = null!;

        public Guid CoachId { get; private set; }
        public Coach Coach { get; private set; } = null!;

        public Guid? ScheduleId { get; private set; } // Ссылка на расписание (если есть)
        public Schedule? Schedule { get; private set; }

        public ICollection<Attendance> Attendances { get; private set; } = new List<Attendance>();

        // EF Core
        private TrainingSession() { }

        public TrainingSession(DateOnly date,TimeOnly startTime,TimeOnly endTime,Group group,Coach coach,string? location = null,Guid? scheduleId = null)
        {
            if (startTime >= endTime)
                throw new ArgumentException("Время начала должно быть раньше времени окончания");
            if (group == null) throw new ArgumentNullException(nameof(group));
            if (coach == null) throw new ArgumentNullException(nameof(coach));

            Date = date;
            StartTime = startTime;
            EndTime = endTime;
            GroupId = group.Id;
            Group = group;
            CoachId = coach.Id;
            Coach = coach;
            Location = location;
            ScheduleId = scheduleId;
            IsCompleted = false;
            IsCancelled = false;
        }

        // Методы
        public void Complete()
        {
            IsCompleted = true;
            UpdateTimestamp();
        }

        public void Cancel(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Причина отмены обязательна");

            IsCancelled = true;
            CancellationReason = reason;
            UpdateTimestamp();
        }

        public void AddCoachNotes(string notes)
        {
            CoachNotes = notes;
            UpdateTimestamp();
        }
    }
}
