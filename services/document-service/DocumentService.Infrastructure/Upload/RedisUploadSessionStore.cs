using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DocumentService.Infrastructure.Upload;

// UploadSession � tracks state of a chunked upload in Redis
//
// Stored in Redis with TTL of 24 hours
// If user does not resume within 24 hours � session expires
// Chunks are cleaned up automatically
public sealed class UploadSession
{
    public string UploadId { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public Guid UserId { get; init; }
    public string FileName { get; init; } = string.Empty;

    public Guid? DocumentId { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public long TotalSize { get; init; }
    public long BytesReceived { get; set; }
    public string TempStoragePath { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsComplete { get; set; }
}

// IUploadSessionStore � abstraction for upload session storage
// Redis implementation below � swap for SQL if needed
public interface IUploadSessionStore
{
    Task SaveAsync(UploadSession session, CancellationToken ct = default);
    Task<UploadSession?> GetAsync(string uploadId, CancellationToken ct = default);
    Task UpdateProgressAsync(string uploadId, long bytesReceived, CancellationToken ct = default);
    Task CompleteAsync(string uploadId, CancellationToken ct = default);
    Task DeleteAsync(string uploadId, CancellationToken ct = default);
}

// RedisUploadSessionStore � stores upload sessions in Redis
//
// Key pattern: upload_session:{uploadId}
// TTL: 24 hours � session expires if not resumed
// Serialized as JSON � human readable in Redis CLI
public sealed class RedisUploadSessionStore : IUploadSessionStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisUploadSessionStore> _logger;

    // Session expires after 24 hours of inactivity
    private static readonly DistributedCacheEntryOptions CacheOptions
        = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        };

    public RedisUploadSessionStore(
        IDistributedCache cache,
        ILogger<RedisUploadSessionStore> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task SaveAsync(
        UploadSession session,
        CancellationToken ct = default)
    {
        var key  = GetKey(session.UploadId);
        var json = JsonSerializer.Serialize(session);

        await _cache.SetStringAsync(key, json, CacheOptions, ct);

        _logger.LogInformation(
            "Upload session saved. UploadId: {UploadId} " +
            "TenantId: {TenantId} FileName: {FileName} " +
            "TotalSize: {TotalSize}",
            session.UploadId, session.TenantId,
            session.FileName, session.TotalSize);
    }

    public async Task<UploadSession?> GetAsync(
        string uploadId,
        CancellationToken ct = default)
    {
        var key  = GetKey(uploadId);
        var json = await _cache.GetStringAsync(key, ct);

        if (json is null)
        {
            _logger.LogWarning(
                "Upload session not found. UploadId: {UploadId}",
                uploadId);
            return null;
        }

        return JsonSerializer.Deserialize<UploadSession>(json);
    }

    public async Task UpdateProgressAsync(
        string uploadId,
        long bytesReceived,
        CancellationToken ct = default)
    {
        var session = await GetAsync(uploadId, ct);
        if (session is null) return;

        session.BytesReceived = bytesReceived;

        var key  = GetKey(uploadId);
        var json = JsonSerializer.Serialize(session);

        // Refresh TTL on every progress update
        // Upload is active � reset the 24 hour expiry
        await _cache.SetStringAsync(key, json, CacheOptions, ct);

        _logger.LogDebug(
            "Upload progress updated. UploadId: {UploadId} " +
            "BytesReceived: {BytesReceived} TotalSize: {TotalSize}",
            uploadId, bytesReceived, session.TotalSize);
    }

    public async Task CompleteAsync(
        string uploadId,
        CancellationToken ct = default)
    {
        var session = await GetAsync(uploadId, ct);
        if (session is null) return;

        session.IsComplete = true;

        var key  = GetKey(uploadId);
        var json = JsonSerializer.Serialize(session);

        // Keep completed session for 1 hour for status checks
        var completedOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };

        await _cache.SetStringAsync(key, json, completedOptions, ct);

        _logger.LogInformation(
            "Upload session completed. UploadId: {UploadId}",
            uploadId);
    }

    public async Task DeleteAsync(
        string uploadId,
        CancellationToken ct = default)
    {
        var key = GetKey(uploadId);
        await _cache.RemoveAsync(key, ct);

        _logger.LogInformation(
            "Upload session deleted. UploadId: {UploadId}",
            uploadId);
    }

    // Redis key pattern: upload_session:{uploadId}
    private static string GetKey(string uploadId)
        => $"upload_session:{uploadId}";
}
