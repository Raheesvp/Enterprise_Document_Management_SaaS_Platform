using Dapper;
using DocumentService.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DocumentService.Infrastructure.Repositories;

// DocumentReadRepository — Dapper read-side repository
//
// Uses raw SQL for maximum read performance
// No EF Core change tracking overhead
// Returns lightweight read models — not full aggregates
//
// Every query filters by tenant_id — application level isolation
// PostgreSQL RLS adds database level isolation on top
public sealed class DocumentReadRepository : IDocumentReadRepository
{
    private readonly string _connectionString;

    public DocumentReadRepository(IConfiguration configuration)
    {
        _connectionString = configuration
            .GetConnectionString("DocumentDb")!;
    }

    // Creates a new Npgsql connection for each query
    // Connection pooling handles performance automatically
    private NpgsqlConnection CreateConnection()
        => new(_connectionString);

    public async Task<DocumentSummary?> GetSummaryByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                d.id                    AS Id,
                d.tenant_id             AS TenantId,
                d.title                 AS Title,
                d.status                AS Status,
                d.document_type         AS DocumentType,
                d.mime_type             AS MimeType,
                dv.file_size_bytes      AS FileSizeBytes,
                COUNT(dv2.id)           AS VersionCount,
                d.uploaded_by_user_id   AS UploadedByUserId,
                d.created_at            AS CreatedAt,
                d.updated_at            AS UpdatedAt,
                d.description           AS Description,
                d.tags                  AS Tags
            FROM documents d
            LEFT JOIN document_versions dv
                ON dv.document_id = d.id
                AND dv.is_current_version = true
            LEFT JOIN document_versions dv2
                ON dv2.document_id = d.id
            WHERE d.id = @Id
              AND d.tenant_id = @TenantId
            GROUP BY
                d.id, d.tenant_id, d.title, d.status,
                d.document_type, d.mime_type,
                dv.file_size_bytes, d.uploaded_by_user_id,
                d.created_at, d.updated_at,
                d.description, d.tags";

        await using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<DocumentSummary>(
            new CommandDefinition(
                sql,
                new { Id = id, TenantId = tenantId },
                cancellationToken: ct));
    }

    public async Task<PagedResult<DocumentSummary>> GetPagedAsync(
        Guid tenantId,
        DocumentQueryFilter filter,
        CancellationToken ct = default)
    {
        // Build dynamic WHERE clause based on filters provided
        var conditions = new List<string>
        {
            "d.tenant_id = @TenantId",
            "d.status != 'Archived'"  // Never show archived by default
        };

        var parameters = new DynamicParameters();
        parameters.Add("TenantId", tenantId);
        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);
        parameters.Add("PageSize", filter.PageSize);

        // Add optional filters only if provided
        if (filter.Status.HasValue)
        {
            conditions.Add("d.status = @Status");
            parameters.Add("Status", filter.Status.Value.ToString());
        }

        if (filter.Type.HasValue)
        {
            conditions.Add("d.document_type = @DocumentType");
            parameters.Add("DocumentType", filter.Type.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            conditions.Add("d.title ILIKE @SearchTerm");
            parameters.Add("SearchTerm", $"%{filter.SearchTerm}%");
        }

        if (filter.FromDate.HasValue)
        {
            conditions.Add("d.created_at >= @FromDate");
            parameters.Add("FromDate", filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            conditions.Add("d.created_at <= @ToDate");
            parameters.Add("ToDate", filter.ToDate.Value);
        }

        var whereClause = string.Join(" AND ", conditions);

        // Count query — total records for pagination
        var countSql = $@"
            SELECT COUNT(*)
            FROM documents d
            WHERE {whereClause}";

        // Data query — paginated results
        var dataSql = $@"
            SELECT
                d.id                    AS Id,
                d.tenant_id             AS TenantId,
                d.title                 AS Title,
                d.status                AS Status,
                d.document_type         AS DocumentType,
                d.mime_type             AS MimeType,
                COALESCE(dv.file_size_bytes, 0) AS FileSizeBytes,
                COUNT(dv2.id)           AS VersionCount,
                d.uploaded_by_user_id   AS UploadedByUserId,
                d.created_at            AS CreatedAt,
                d.updated_at            AS UpdatedAt,
                d.description           AS Description,
                d.tags                  AS Tags
            FROM documents d
            LEFT JOIN document_versions dv
                ON dv.document_id = d.id
                AND dv.is_current_version = true
            LEFT JOIN document_versions dv2
                ON dv2.document_id = d.id
            WHERE {whereClause}
            GROUP BY
                d.id, d.tenant_id, d.title, d.status,
                d.document_type, d.mime_type,
                dv.file_size_bytes, d.uploaded_by_user_id,
                d.created_at, d.updated_at,
                d.description, d.tags
            ORDER BY d.created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        await using var connection = CreateConnection();

        // Run both queries
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters,
                cancellationToken: ct));

        var items = await connection.QueryAsync<DocumentSummary>(
            new CommandDefinition(dataSql, parameters,
                cancellationToken: ct));

        return new PagedResult<DocumentSummary>(
            items.ToList().AsReadOnly(),
            totalCount,
            filter.Page,
            filter.PageSize);
    }

    public async Task<IReadOnlyList<DocumentVersionSummary>>
        GetVersionsAsync(
            Guid documentId,
            Guid tenantId,
            CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                dv.id                   AS Id,
                dv.version_number       AS VersionNumber,
                dv.file_size_bytes      AS FileSizeBytes,
                dv.storage_path         AS StoragePath,
                dv.is_current_version   AS IsCurrentVersion,
                dv.uploaded_by_user_id  AS UploadedByUserId,
                dv.created_at           AS CreatedAt,
                dv.extracted_text       AS ExtractedText,
                dv.page_count           AS PageCount
            FROM document_versions dv
            INNER JOIN documents d ON d.id = dv.document_id
            WHERE dv.document_id = @DocumentId
              AND d.tenant_id = @TenantId
            ORDER BY dv.version_number ASC";

        await using var connection = CreateConnection();

        var versions = await connection.QueryAsync<DocumentVersionSummary>(
            new CommandDefinition(
                sql,
                new { DocumentId = documentId, TenantId = tenantId },
                cancellationToken: ct));

        return versions.ToList().AsReadOnly();
    }
}
