using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    //оценки тренера за дату
    public class SkillAssessment : BaseEntity
    {
        public Guid PlayerId { get; set; }
        public Player Player{ get; set; } = null!;
        public Guid CoachId { get; set; }
        public Coach Coach { get; set; } = null!;
        public DateOnly Date { get; set; }
        public string? Season { get; set; }              // "2026/2027" — берётся из группы на момент оценки
        public string? Comment { get; set; }
        public ICollection<SkillScore> Scores { get; set; } = [];
    }
}
