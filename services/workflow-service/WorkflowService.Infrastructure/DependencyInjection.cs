using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowService.Application.Interfaces;
using WorkflowService.Infrastructure.Jobs;
using WorkflowService.Infrastructure.Persistence;
using WorkflowService.Infrastructure.Repositories;

namespace WorkflowService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL DbContext
        services.AddDbContext<WorkflowDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("WorkflowDb"),
                npgsqlOptions =>
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null)));

        // Repositories
        services.AddScoped<IWorkflowRepository,
            WorkflowRepository>();

        // SLA Job
        services.AddScoped<SlaCheckerJob>();

        // Hangfire — background job processing
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(
                CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                configuration.GetConnectionString("WorkflowDb")));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
            options.Queues      = new[] { "sla", "default" };
        });

        return services;
    }
}
