using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Configurations
{
    public class SkillScoreConfiguration : IEntityTypeConfiguration<SkillScore>
    {
        public void Configure(EntityTypeBuilder<SkillScore> b)
        {
            b.ToTable("SkillScores");

            //b.HasCheckConstraint("CK_SkillScores_Value_Range", "Value >= 1 AND Value <= 10");
            b.HasIndex(t => new { t.AssessmentId, t.SkillId });
        }
    }
}
