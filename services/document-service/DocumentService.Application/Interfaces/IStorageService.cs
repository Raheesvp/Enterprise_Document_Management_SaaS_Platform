namespace DocumentService.Application.Interfaces;

// IStorageService — Application defines the contract
// Infrastructure implements using MinIO (local dev)
// Same interface works for AWS S3 or Azure Blob in production
// Zero code change needed to swap storage provider
//
// This is the Dependency Inversion Principle in practice:
// Application depends on abstraction — never on MinIO directly
public interface IStorageService
{
    // Upload file — returns the final storage path
    Task<string> UploadAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken ct = default);

    // Download file — returns stream for reading
    Task<Stream> DownloadAsync(
        string path,
        CancellationToken ct = default);

    // Delete file from storage
    Task DeleteAsync(
        string path,
        CancellationToken ct = default);

    // Check if file exists
    Task<bool> ExistsAsync(
        string path,
        CancellationToken ct = default);

    // Get pre-signed URL for direct browser access (short lived)
    Task<string> GetPresignedUrlAsync(
        string path, 
        string? contentType = null, 
        int expiryMinutes = 30);
}