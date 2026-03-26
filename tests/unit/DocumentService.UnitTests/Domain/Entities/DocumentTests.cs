using DocumentService.Domain.Entities;
using DocumentService.Domain.Enums;
using DocumentService.Domain.Events;
using DocumentService.Domain.ValueObjects;
using FluentAssertions;

namespace DocumentService.UnitTests.Domain.Entities;

public class DocumentTests
{
    private static Document CreateDocument(
        Guid? tenantId = null,
        string title = "Invoice.pdf",
        string mimeType = "application/pdf",
        long sizeBytes = 1024 * 1024)
    {
        return Document.Create(tenantId ?? Guid.NewGuid(), Guid.NewGuid(), "Test User", DocumentTitle.Create(title), ContentType.Create(mimeType), StoragePath.Create(Guid.NewGuid(), Guid.NewGuid(), title),
            FileSize.FromBytes(sizeBytes));
    }

    [Fact]
    public void Create_ValidInputs_SetsStatusToUploading()
    {
        var doc = CreateDocument();
        doc.Status.Should().Be(DocumentStatus.Uploading);
    }

    [Fact]
    public void Create_ValidInputs_CreatesFirstVersionAutomatically()
    {
        var doc = CreateDocument();

        doc.Versions.Should().HaveCount(1);
        doc.CurrentVersion.Should().NotBeNull();
        doc.CurrentVersion!.VersionNumber.Should().Be(1);
        doc.CurrentVersion.IsCurrentVersion.Should().BeTrue();
    }

    [Fact]
    public void Create_ValidInputs_RaisesDocumentCreatedDomainEvent()
    {
        var doc = CreateDocument();

        doc.DomainEvents.Should().HaveCount(1);
        doc.DomainEvents.First().Should().BeOfType<DocumentCreatedEvent>();
    }

    [Fact]
    public void Create_TenantIdStoredCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var doc = CreateDocument(tenantId: tenantId);

        doc.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void MarkAsProcessing_FromUploading_Succeeds()
    {
        var doc = CreateDocument();
        doc.MarkAsProcessing();
        doc.Status.Should().Be(DocumentStatus.Processing);
    }

    [Fact]
    public void MarkAsActive_FromProcessing_Succeeds()
    {
        var doc = CreateDocument();
        doc.MarkAsProcessing();
        doc.MarkAsActive();

        doc.Status.Should().Be(DocumentStatus.Active);
    }

    [Fact]
    public void MarkAsActive_FromUploading_ThrowsException()
    {
        var doc = CreateDocument();
        var act = () => doc.MarkAsActive();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Processing*");
    }

    [Fact]
    public void SubmitForReview_FromActive_ChangesStatus()
    {
        var doc = CreateDocument();
        doc.MarkAsProcessing();
        doc.MarkAsActive();
        doc.SubmitForReview();

        doc.Status.Should().Be(DocumentStatus.UnderReview);
    }

    [Fact]
    public void Approve_FromUnderReview_ChangesStatus()
    {
        var doc = CreateDocument();
        doc.MarkAsProcessing();
        doc.MarkAsActive();
        doc.SubmitForReview();
        doc.Approve();

        doc.Status.Should().Be(DocumentStatus.Approved);
    }

    [Fact]
    public void Reject_FromUnderReview_ChangesStatus()
    {
        var doc = CreateDocument();
        doc.MarkAsProcessing();
        doc.MarkAsActive();
        doc.SubmitForReview();
        doc.Reject();

        doc.Status.Should().Be(DocumentStatus.Rejected);
    }

    [Fact]
    public void Approve_FromActive_ThrowsException()
    {
        var doc = CreateDocument();
        doc.MarkAsProcessing();
        doc.MarkAsActive();

        var act = () => doc.Approve();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Archive_FromAnyActiveStatus_Succeeds()
    {
        var doc = CreateDocument();
        doc.MarkAsProcessing();
        doc.MarkAsActive();
        doc.Archive();

        doc.Status.Should().Be(DocumentStatus.Archived);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ThrowsException()
    {
        var doc = CreateDocument();
        doc.Archive();

        var act = () => doc.Archive();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already archived*");
    }

    [Fact]
    public void MarkAsProcessing_RaisesDocumentStatusChangedEvent()
    {
        var doc = CreateDocument();
        doc.ClearDomainEvents();
        doc.MarkAsProcessing();

        doc.DomainEvents.Should().HaveCount(1);
        doc.DomainEvents.First().Should().BeOfType<DocumentStatusChangedEvent>();
    }

    [Fact]
    public void AddVersion_WhenActive_AddsNewVersionAndSetsAsCurrent()
    {
        var doc = CreateDocument();
        doc.MarkAsProcessing();
        doc.MarkAsActive();

        doc.AddVersion(
            StoragePath.Create(doc.TenantId, doc.Id, "invoice-v2.pdf"),
            FileSize.FromBytes(2 * 1024 * 1024),
            Guid.NewGuid());

        doc.Versions.Should().HaveCount(2);
        doc.CurrentVersion!.VersionNumber.Should().Be(2);
        doc.CurrentVersion.IsCurrentVersion.Should().BeTrue();
        doc.Versions.First().IsCurrentVersion.Should().BeFalse();
    }

    [Fact]
    public void AddVersion_WhenNotActive_ThrowsException()
    {
        var doc = CreateDocument();
        var act = () => doc.AddVersion(
            StoragePath.Create(Guid.NewGuid(), Guid.NewGuid(), "v2.pdf"),
            FileSize.FromBytes(1024),
            Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddVersion_RaisesDocumentVersionAddedEvent()
    {
        var doc = CreateDocument();
        doc.MarkAsProcessing();
        doc.MarkAsActive();
        doc.ClearDomainEvents();

        doc.AddVersion(
            StoragePath.Create(doc.TenantId, doc.Id, "v2.pdf"),
            FileSize.FromBytes(1024),
            Guid.NewGuid());

        doc.DomainEvents.Should().HaveCount(1);
        doc.DomainEvents.First().Should().BeOfType<DocumentVersionAddedEvent>();
    }

    [Fact]
    public void UpdateTitle_ChangesTitle()
    {
        var doc = CreateDocument(title: "Old Title.pdf");
        doc.UpdateTitle(DocumentTitle.Create("New Title.pdf"));

        doc.Title.Value.Should().Be("New Title.pdf");
    }

    [Fact]
    public void UpdateTitle_UpdatesUpdatedAt()
    {
        var doc = CreateDocument();
        var before = doc.UpdatedAt;

        Thread.Sleep(10);
        doc.UpdateTitle(DocumentTitle.Create("Updated.pdf"));

        doc.UpdatedAt.Should().BeAfter(before);
    }
}

