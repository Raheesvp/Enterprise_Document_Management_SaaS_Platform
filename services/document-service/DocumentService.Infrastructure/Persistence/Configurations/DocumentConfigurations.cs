using DocumentService.Domain.Entities;
using DocumentService.Domain.Enums;
using DocumentService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentService.Infrastructure.Persistence.Configurations;

// DocumentConfiguration — maps Document aggregate to PostgreSQL table
//
// Key decisions:
// 1. Value Objects are owned entities — stored as columns, not tables
// 2. Status stored as string — readable in DB, no join needed
// 3. TenantId indexed — every query filters by tenant
// 4. Snake_case naming — PostgreSQL convention
public sealed class DocumentConfiguration
    : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        // Table name — PostgreSQL snake_case convention
        builder.ToTable("documents");

        // Primary key
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // We generate Guids in domain

        // Tenant isolation — indexed for fast filtering
        builder.Property(d => d.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.HasIndex(d => d.TenantId)
            .HasDatabaseName("ix_documents_tenant_id");

        // Composite index — most common query pattern
        // WHERE tenant_id = ? AND status = ?
        builder.HasIndex(d => new { d.TenantId, d.Status })
            .HasDatabaseName("ix_documents_tenant_status");

        builder.Property(d => d.UploadedByUserId)
            .HasColumnName("uploaded_by_user_id")
            .IsRequired();

        builder.Property(d => d.UploadedByName)
            .HasColumnName("uploaded_by_name")
            .HasMaxLength(200)
            .IsRequired();

        // ── Value Object Mappings ──────────────────────────
        // DocumentTitle — owned, stored as single column
        builder.OwnsOne(d => d.Title, titleBuilder =>
        {
            titleBuilder.Property(t => t.Value)
                .HasColumnName("title")
                .HasMaxLength(255)
                .IsRequired();
        });

        // ContentType — owned, stored as two columns
        builder.OwnsOne(d => d.ContentType, ctBuilder =>
        {
            ctBuilder.Property(ct => ct.MimeType)
                .HasColumnName("mime_type")
                .HasMaxLength(100)
                .IsRequired();

            ctBuilder.Property(ct => ct.DocumentType)
                .HasColumnName("document_type")
                .HasConversion<string>() // stored as "Pdf", "Word" etc
                .HasMaxLength(50)
                .IsRequired();
        });

        // DocumentStatus — stored as string for readability
        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Optional metadata fields
        builder.Property(d => d.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(d => d.Tags)
            .HasColumnName("tags")
            .HasMaxLength(500);

        // Timestamps
        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // ── Versions Relationship ──────────────────────────
        // One Document → many DocumentVersions
        // Cascade delete — archive removes versions too
        builder.HasMany(d => d.Versions)
            .WithOne()
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Ignore Domain Events ───────────────────────────
        // Domain events are in-memory only — not persisted
        builder.Ignore(d => d.DomainEvents);
    }
}
