using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(
        DbContextOptions<NotificationDbContext> options)
        : base(options) { }

    public DbSet<Notification> Notifications
        => Set<Notification>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id)
                .HasColumnName("id");
            e.Property(x => x.TenantId)
                .HasColumnName("tenant_id");
            e.Property(x => x.UserId)
                .HasColumnName("user_id");
            e.Property(x => x.Title)
                .HasColumnName("title")
                .HasMaxLength(255);
            e.Property(x => x.Message)
                .HasColumnName("message");
            e.Property(x => x.Type)
                .HasColumnName("type")
                .HasConversion<string>();
            e.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>();
            e.Property(x => x.ReferenceId)
                .HasColumnName("reference_id");
            e.Property(x => x.ReferenceType)
                .HasColumnName("reference_type")
                .HasMaxLength(100);
            e.Property(x => x.CreatedAt)
                .HasColumnName("created_at");
            e.Property(x => x.ReadAt)
                .HasColumnName("read_at");

            e.Ignore(x => x.DomainEvents);

            e.HasIndex(x => x.UserId)
                .HasDatabaseName("ix_notifications_user_id");
            e.HasIndex(x => new { x.TenantId, x.UserId })
                .HasDatabaseName("ix_notifications_tenant_user");
        });
    }
}
