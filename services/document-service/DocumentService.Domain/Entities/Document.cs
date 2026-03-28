using Shared.Domain.Primitives;
using DocumentService.Domain.Enums;
using DocumentService.Domain.Events;
using DocumentService.Domain.ValueObjects;

namespace DocumentService.Domain.Entities;

public sealed class Document : AggregateRoot<Guid>
{
    private readonly List<DocumentVersion> _versions = new();

    private Document() { }

    private Document(
        Guid id,
        Guid tenantId,
        Guid uploadedByUserId,
        string uploadedByName,
        DocumentTitle title,
        ContentType contentType,
        StoragePath storagePath,
        FileSize fileSize) : base(id)
    {
        TenantId         = tenantId;
        UploadedByUserId = uploadedByUserId;
        UploadedByName   = uploadedByName;
        Title            = title;
        ContentType      = contentType;
        Status           = DocumentStatus.Uploading;
        CreatedAt        = DateTime.UtcNow;
        UpdatedAt        = DateTime.UtcNow;

        var firstVersion = new DocumentVersion(
            id, 1, storagePath, fileSize,
            uploadedByUserId.ToString());
        firstVersion.IsCurrentVersion = true;
        _versions.Add(firstVersion);
    }

    public Guid           TenantId         { get; private set; }
    public Guid           UploadedByUserId { get; private set; }
    public string         UploadedByName   { get; private set; } = string.Empty;
    public DocumentTitle  Title            { get; private set; } = null!;
    public ContentType    ContentType      { get; private set; } = null!;
    public DocumentStatus Status           { get; private set; }
    public DateTime       CreatedAt        { get; private set; }
    public DateTime       UpdatedAt        { get; private set; }
    public string?        Description      { get; private set; }
    public string?        Tags             { get; private set; }

    public IReadOnlyCollection<DocumentVersion> Versions
        => _versions.AsReadOnly();

    public DocumentVersion? CurrentVersion
        => _versions.FirstOrDefault(v => v.IsCurrentVersion);

    public static Document Create(
        Guid tenantId,
        Guid uploadedByUserId,
        string uploadedByName,
        DocumentTitle title,
        ContentType contentType,
        StoragePath storagePath,
        FileSize fileSize)
    {
        var document = new Document(
            Guid.NewGuid(),
            tenantId,
            uploadedByUserId,
            uploadedByName,
            title,
            contentType,
            storagePath,
            fileSize);

        var occurredOn = DateTime.UtcNow;
        document.RaiseDomainEvent(new DocumentCreatedEvent(
            Guid.NewGuid(),
            document.Id,
            tenantId,
            uploadedByUserId,
            title.Value,
            contentType.MimeType,
            storagePath.Value,
            occurredOn,
            document.CreatedAt));

        return document;
    }

    public void MarkAsProcessing()
    {
        EnsureStatus(DocumentStatus.Uploading);
        ChangeStatus(DocumentStatus.Processing);
    }

    public void MarkAsActive()
    {
        EnsureStatus(DocumentStatus.Processing);
        ChangeStatus(DocumentStatus.Active);
    }

    public void SubmitForReview()
    {
        EnsureStatus(DocumentStatus.Active);
        ChangeStatus(DocumentStatus.UnderReview);
    }

    public void Approve()
    {
        if (Status != DocumentStatus.UnderReview && Status != DocumentStatus.Active)
            throw new InvalidOperationException($"Cannot approve document in {Status} state");
            
        ChangeStatus(DocumentStatus.Approved);
    }

    public void Reject()
    {
        if (Status != DocumentStatus.UnderReview && Status != DocumentStatus.Active)
            throw new InvalidOperationException($"Cannot reject document in {Status} state");

        ChangeStatus(DocumentStatus.Rejected);
    }

    public void Archive()
    {
        if (Status == DocumentStatus.Archived)
            throw new InvalidOperationException(
                "Document is already archived");
        ChangeStatus(DocumentStatus.Archived);
    }

    public void AddVersion(
        StoragePath storagePath,
        FileSize fileSize,
        Guid uploadedByUserId)
    {
        if (Status != DocumentStatus.Active)
            throw new InvalidOperationException(
                $"Cannot add version. Status: {Status}");

        foreach (var v in _versions)
            v.IsCurrentVersion = false;

        var newVersion = new DocumentVersion(
            Id, _versions.Count + 1,
            storagePath, fileSize,
            uploadedByUserId.ToString());

        newVersion.IsCurrentVersion = true;
        _versions.Add(newVersion);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DocumentVersionAddedEvent(
            Guid.NewGuid(), Id, TenantId,
            uploadedByUserId,
            newVersion.VersionNumber,
            storagePath.Value,
            DateTime.UtcNow, DateTime.UtcNow));
    }

    public void UpdateTitle(DocumentTitle newTitle)
    {
        Title     = newTitle;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt   = DateTime.UtcNow;
    }

    public void UpdateTags(string? tags)
    {
        Tags      = tags;
        UpdatedAt = DateTime.UtcNow;
    }

    private void ChangeStatus(DocumentStatus newStatus)
    {
        var oldStatus = Status;
        Status    = newStatus;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DocumentStatusChangedEvent(
            Guid.NewGuid(), Id, TenantId,
            oldStatus, newStatus,
            UpdatedAt, UpdatedAt));
    }

    private void EnsureStatus(DocumentStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException(
                $"Document must be {expected}. Current: {Status}");
    }
}
