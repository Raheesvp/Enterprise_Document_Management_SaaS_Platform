using DocumentService.Application.Interfaces;
using DocumentService.Domain.Repositories;
using DocumentService.Infrastructure.Persistence;
using DocumentService.Infrastructure.Persistence.Interceptors;
using DocumentService.Infrastructure.Repositories;
using DocumentService.Infrastructure.Services;
using DocumentService.Infrastructure.Stubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Common;

namespace DocumentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // HTTP context accessor
        services.AddHttpContextAccessor();

        // Tenant context — scoped per request
        services.AddScoped<ITenantContext, HttpTenantContext>();

        // Interceptors
        services.AddScoped<DomainEventInterceptor>();
        services.AddScoped<TenantDbCommandInterceptor>();

        // PostgreSQL DbContext
        services.AddDbContext<DocumentDbContext>(
            (serviceProvider, options) =>
            {
                var domainEventInterceptor = serviceProvider
                    .GetRequiredService<DomainEventInterceptor>();

                var tenantInterceptor = serviceProvider
                    .GetRequiredService<TenantDbCommandInterceptor>();

                options
                    .UseNpgsql(
                        configuration
                            .GetConnectionString("DocumentDb"),
                        npgsqlOptions =>
                        {
                            npgsqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(30),
                                errorCodesToAdd: null);
                        })
                    .AddInterceptors(
                        domainEventInterceptor,
                        tenantInterceptor);
            });

        // Real repositories — replacing stubs from Day 15
        services.AddScoped<IDocumentRepository,
            DocumentRepository>();
        services.AddScoped<IDocumentReadRepository,
            DocumentReadRepository>();

        // Storage still stub — replaced Day 18
        services.AddScoped<IStorageService,
            StubStorageService>();

        return services;
    }
}
