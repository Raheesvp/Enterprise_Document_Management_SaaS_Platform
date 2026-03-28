using DocumentService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace DocumentService.Infrastructure.Storage;

public sealed class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioStorageService> _logger;
    private readonly string _bucketName;
    private readonly string _endpoint;

    public MinioStorageService(
        IMinioClient minioClient,
        IConfiguration configuration,
        ILogger<MinioStorageService> logger)
    {
        _minioClient = minioClient;
        _logger      = logger;
        _bucketName  = configuration["MinioSettings:BucketName"]
                       ?? "documents";
        _endpoint    = configuration["MinioSettings:Endpoint"]
                       ?? "localhost:9000";
    }

    public async Task<string> UploadAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        try
        {
            await EnsureBucketExistsAsync(ct);

            // MinIO requires object size — read into memory if unknown
            // Stream.Null and streams with no length need special handling
            byte[] data;
            if (content == Stream.Null || content.Length == 0)
            {
                // Empty file — create 1 byte placeholder
                data = new byte[] { 0 };
            }
            else
            {
                using var ms = new MemoryStream();
                await content.CopyToAsync(ms, ct);
                data = ms.ToArray();
            }

            using var dataStream = new MemoryStream(data);

            var args = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path)
                .WithStreamData(dataStream)
                .WithObjectSize(dataStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(args, ct);

            _logger.LogInformation(
                "File uploaded. Path: {Path} Size: {Size}",
                path, data.Length);

            return path;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to upload file. Path: {Path}", path);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(
        string path,
        CancellationToken ct = default)
    {
        try
        {
            var memoryStream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                });

            await _minioClient.GetObjectAsync(args, ct);
            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (ObjectNotFoundException)
        {
            throw new FileNotFoundException(
                $"File not found: {path}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to download file. Path: {Path}", path);
            throw;
        }
    }

    public async Task DeleteAsync(
        string path,
        CancellationToken ct = default)
    {
        try
        {
            var args = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path);

            await _minioClient.RemoveObjectAsync(args, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete file. Path: {Path}", path);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(
        string path,
        CancellationToken ct = default)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path);

            await _minioClient.StatObjectAsync(args, ct);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to check existence. Path: {Path}", path);
            return false;
        }
    }

    public async Task<string> GetPresignedUrlAsync(
        string path, 
        string? contentType = null, 
        int expiryMinutes = 30)
    {
        try
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path)
                .WithExpiry(expiryMinutes * 60);

            return await _minioClient.PresignedGetObjectAsync(args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate pre-signed URL for {Path}", path);
            return string.Empty;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        var existsArgs = new BucketExistsArgs()
            .WithBucket(_bucketName);

        var exists = await _minioClient
            .BucketExistsAsync(existsArgs, ct);

        if (!exists)
        {
            var makeArgs = new MakeBucketArgs()
                .WithBucket(_bucketName);

            await _minioClient.MakeBucketAsync(makeArgs, ct);

            _logger.LogInformation(
                "Bucket created. Name: {BucketName}", _bucketName);
        }
    }
}
