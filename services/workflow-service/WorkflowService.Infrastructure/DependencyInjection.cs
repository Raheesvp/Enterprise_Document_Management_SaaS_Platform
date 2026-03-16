using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowService.Application.Interfaces;
using WorkflowService.Infrastructure.Persistence;
using WorkflowService.Infrastructure.Repositories;

namespace WorkflowService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<WorkflowDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("WorkflowDb"),
                npgsqlOptions =>
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null)));

        services.AddScoped<IWorkflowRepository,
            WorkflowRepository>();

        return services;
    }
}
