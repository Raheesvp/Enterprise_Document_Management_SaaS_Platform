using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Consumers;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString(
                    "NotificationDb"),
                npgsqlOptions =>
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null)));

        // Repository
        services.AddScoped<INotificationRepository,
            NotificationRepository>();

        // Email service — singleton thread safe
        services.AddSingleton<IEmailService,
            MailKitEmailService>();

        // MassTransit + RabbitMQ
        services.AddMassTransit(x =>
        {
            x.AddConsumer<WorkflowStartedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var host     = configuration[
                               "RabbitMqSettings:Host"]
                               ?? "localhost";
                var port     = ushort.Parse(
                               configuration[
                                   "RabbitMqSettings:Port"]
                               ?? "5672");
                var username = configuration[
                               "RabbitMqSettings:Username"]
                               ?? "saasuser";
                var password = configuration[
                               "RabbitMqSettings:Password"]
                               ?? "SaaS@Rabbit2024!";
                var vhost    = configuration[
                               "RabbitMqSettings:VirtualHost"]
                               ?? "documentsaas";

                cfg.Host(host, port, vhost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.UseMessageRetry(r =>
                    r.Exponential(
                        retryLimit: 3,
                        minInterval: TimeSpan.FromSeconds(1),
                        maxInterval: TimeSpan.FromSeconds(10),
                        intervalDelta: TimeSpan.FromSeconds(2)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
