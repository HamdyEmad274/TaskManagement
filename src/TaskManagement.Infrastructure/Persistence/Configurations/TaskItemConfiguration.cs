using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.ToTable("TaskItems");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
            builder.Property(t => t.Description).IsRequired().HasMaxLength(1000);
            builder.Property(t => t.Priority).HasConversion<string>();
            builder.Property(t => t.Status).HasConversion<string>();
            builder.HasOne(t=>t.User).WithMany().HasForeignKey(t=>t.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(t => t.UserId);
            builder.HasIndex(t=> new { t.UserId , t.CreatedAt});
        }
    }
}
