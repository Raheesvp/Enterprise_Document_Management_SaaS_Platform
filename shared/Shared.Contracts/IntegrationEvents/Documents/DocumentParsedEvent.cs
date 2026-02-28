namespace shared.Shared.Contracts.IntegrationEvents.Documents;

public record DocumentParsedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    public Guid TenantId { get; init; }
    public Guid DocumentId { get; init; }

 
    public string ExtractedText { get; init; } = string.Empty;
    public int PageCount { get; init; }

  
    public string? Author { get; init; }
    public string? Title { get; init; }
    public DateTime? DocumentDate { get; init; }

  
    public bool IsSuccessful { get; init; }
    public string? FailureReason { get; init; }

   
    public string? ThumbnailStoragePath { get; init; }
}

