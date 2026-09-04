using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class SkillScore : BaseEntity
    {
        public Guid AssessmentId { get; set; }
        public SkillAssessment Assessment { get; set; } = null!;
        public Guid SkillId { get; set; }
        public Skill Skill { get; set; } = null!;
        public int Value { get; set; }                   // 1..10
    }
}
