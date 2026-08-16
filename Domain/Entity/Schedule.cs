using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    // Расписание тренировки (повторяющееся событие)
    public class Schedule : BaseEntity
    {
        public DayOfWeek DayOfWeek { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }

        // Место проведения
        public string? Location { get; private set; } 
        public bool IsActive { get; private set; }
        public string? Notes { get; private set; }

        // Связи
        public Guid GroupId { get; private set; }
        public Group Group { get; private set; } = null!;

        public Guid CoachId { get; private set; }
        public Coach Coach { get; private set; } = null!;

        // EF Core
        private Schedule() { } 

        public Schedule(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, Group group, Coach coach, string? location = null, string? notes = null)
        {
            if (startTime >= endTime)
                throw new ArgumentException("Время начала должно быть раньше времени окончания");
            if (group == null) throw new ArgumentNullException(nameof(group));
            if (coach == null) throw new ArgumentNullException(nameof(coach));

            DayOfWeek = dayOfWeek;
            StartTime = startTime;
            EndTime = endTime;
            GroupId = group.Id;
            Group = group;
            CoachId = coach.Id;
            Coach = coach;
            Location = location;
            Notes = notes;
            IsActive = true;
        }

        // Методы
        public void UpdateTime(TimeOnly start, TimeOnly end)
        {
            if (start >= end)
                throw new ArgumentException("Время начала должно быть раньше времени окончания");

            StartTime = start;
            EndTime = end;
            UpdateTimestamp();
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        public int GetDurationMinutes()
        {
            return (int)(EndTime - StartTime).TotalMinutes;
        }
    }
}
