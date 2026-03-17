using Microsoft.EntityFrameworkCore;
using WorkflowService.Domain.Entities;

namespace WorkflowService.Infrastructure.Persistence;

public sealed class WorkflowDbContext : DbContext
{
    public WorkflowDbContext(
        DbContextOptions<WorkflowDbContext> options)
        : base(options) { }

    public DbSet<WorkflowInstance> WorkflowInstances
        => Set<WorkflowInstance>();

    public DbSet<WorkflowStage> WorkflowStages
        => Set<WorkflowStage>();

    public DbSet<WorkflowDefinition> WorkflowDefinitions
        => Set<WorkflowDefinition>();

    public DbSet<StageDefinitionEntity> StageDefinitions
        => Set<StageDefinitionEntity>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowInstance>(e =>
        {
            e.ToTable("workflow_instances");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.DocumentId).HasColumnName("document_id");
            e.Property(x => x.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
            e.Property(x => x.DocumentTitle).HasColumnName("document_title").HasMaxLength(255);
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
            e.Property(x => x.CurrentStageOrder).HasColumnName("current_stage_order");
            e.Property(x => x.InitiatedByUserId).HasColumnName("initiated_by_user_id");
            e.Property(x => x.StartedAt).HasColumnName("started_at");
            e.Property(x => x.CompletedAt).HasColumnName("completed_at");
            e.Ignore(x => x.DomainEvents);
            e.HasMany(x => x.Stages)
                .WithOne()
                .HasForeignKey(x => x.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TenantId).HasDatabaseName("ix_workflow_instances_tenant_id");
            e.HasIndex(x => x.DocumentId).HasDatabaseName("ix_workflow_instances_document_id");
        });

        modelBuilder.Entity<WorkflowStage>(e =>
        {
            e.ToTable("workflow_stages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.WorkflowInstanceId).HasColumnName("workflow_instance_id");
            e.Property(x => x.StageOrder).HasColumnName("stage_order");
            e.Property(x => x.StageName).HasColumnName("stage_name").HasMaxLength(100);
            e.Property(x => x.AssignedToUserId).HasColumnName("assigned_to_user_id");
            e.Property(x => x.AssignedToEmail).HasColumnName("assigned_to_email").HasMaxLength(255);
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
            e.Property(x => x.Comments).HasColumnName("comments");
            e.Property(x => x.SlaDeadline).HasColumnName("sla_deadline");
            e.Property(x => x.ProcessedAt).HasColumnName("processed_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<WorkflowDefinition>(e =>
        {
            e.ToTable("workflow_definitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Ignore(x => x.DomainEvents);
            e.HasMany(x => x.Stages)
                .WithOne()
                .HasForeignKey(x => x.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StageDefinitionEntity>(e =>
        {
            e.ToTable("workflow_definition_stages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
            e.Property(x => x.Order).HasColumnName("order");
            e.Property(x => x.StageName).HasColumnName("stage_name").HasMaxLength(100);
            e.Property(x => x.RoleRequired).HasColumnName("role_required").HasMaxLength(100);
            e.Property(x => x.SlaDays).HasColumnName("sla_days");
            e.Ignore(x => x.DomainEvents);
        });
    }
}
