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
    public class SkillAssessmentConfiguration : IEntityTypeConfiguration<SkillAssessment>
    {
        public void Configure(EntityTypeBuilder<SkillAssessment> b)
        {
            b.ToTable("SkillAssessments");
        }
    }
}
