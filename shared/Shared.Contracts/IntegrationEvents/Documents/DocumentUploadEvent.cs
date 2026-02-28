
namespace Shared.Contracts.IntegrationEvents.Documents;

public record DocumentUploadEvent
{
    public Guid EventId {get;init;} =Guid.NewGuid();
    public DateTime OccuredOn{get;init;} =DateTime.UtcNow;

    public Guid TenantId {get;init;}

    public Guid DocumentId {get;init;}

    public Guid UploadedByUserId {get;init;}

    public string FileName {get;init;} =string.Empty;
    
    public string ContentType {get;init;} =string.Empty;

    public long FileSizeBytes {get;init;}

    public string StoragePath {get;init;} =string.Empty;

    public Guid? WorkflowTemplateId {get;init;}
    
}