using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyGraph.Domain.Entities;
namespace StudyGraph.Infrastructure.Persistence.Configurations;

public sealed class StudyTaskConfiguration : IEntityTypeConfiguration<StudyTask>
{
    public void Configure(EntityTypeBuilder<StudyTask> builder)
    {
        builder.ToTable("study_tasks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(5000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.DueAt });
        builder.HasIndex(x => new { x.UserId, x.Status, x.Priority });
    }
}
