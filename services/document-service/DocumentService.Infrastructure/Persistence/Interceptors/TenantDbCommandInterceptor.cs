using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.Domain.Common;
using System.Data.Common;

namespace DocumentService.Infrastructure.Persistence.Interceptors;

public sealed class TenantDbCommandInterceptor : DbCommandInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantDbCommandInterceptor> _logger;

    public TenantDbCommandInterceptor(
        ITenantContext tenantContext,
        ILogger<TenantDbCommandInterceptor> logger)
    {
        _tenantContext = tenantContext;
        _logger        = logger;
    }

    public override async ValueTask<InterceptionResult<DbDataReader>>
        ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
    {
        await SetTenantAsync(command, cancellationToken);
        return await base.ReaderExecutingAsync(
            command, eventData, result, cancellationToken);
    }

    public override async ValueTask<InterceptionResult<int>>
        NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
    {
        await SetTenantAsync(command, cancellationToken);
        return await base.NonQueryExecutingAsync(
            command, eventData, result, cancellationToken);
    }

    public override async ValueTask<InterceptionResult<object>>
        ScalarExecutingAsync(
            DbCommand command,  //represent actual sql statements 
            CommandEventData eventData,//it provide which dbcontext is running the query and it allow us to where the call is coming from.
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
    {
        await SetTenantAsync(command, cancellationToken);
        return await base.ScalarExecutingAsync(
            command, eventData, result, cancellationToken);
    }

    private async Task SetTenantAsync(
        DbCommand command,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved)
            return;

        try
        {
            using var tenantCommand = command.Connection!.CreateCommand();
            tenantCommand.Transaction = command.Transaction;
            tenantCommand.CommandText =

            //set tenant ensures that only tenants with the correct tenant_id can access their data
                $"SET LOCAL app.current_tenant_id = " +
                $"'{_tenantContext.TenantId}'";

            await tenantCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to set tenant context for query");
        }
    }
}
