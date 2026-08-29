using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public class Attendance : BaseEntity
    {
        //посещаемость
        public DateOnly Date { get; private set; }
        public AttendanceStatus Status { get; private set; }
        // Причина пропуска и т.д.
        public string? Comment { get; private set; } 

        // Внешние ключи
        public Guid PlayerId { get; private set; }
        public Player Player { get; private set; } = null!;

        public Guid TrainingSessionId { get; private set; }
        public TrainingSession TrainingSession { get; private set; }

        //  EF Core
        private Attendance() { }

        public Attendance(Guid playerId,Guid trainingSessionId,DateOnly date,AttendanceStatus status,string? comment = null)
        {
            PlayerId = playerId;
            TrainingSessionId = trainingSessionId;
            Date = date;
            Status = status;
            Comment = comment;
        }

        // Методы
        public void UpdateStatus(AttendanceStatus newStatus, string? comment = null)
        {
            Status = newStatus;
            Comment = comment ?? Comment;
            UpdateTimestamp();
        }
    }
}
