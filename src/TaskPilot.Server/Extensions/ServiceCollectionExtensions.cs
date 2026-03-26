using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using TaskPilot.Server.Auth;
using TaskPilot.Server.Constants;
using TaskPilot.Server.Data;
using TaskPilot.Server.Repositories;
using TaskPilot.Server.Repositories.Interfaces;
using TaskPilot.Server.Services;
using TaskPilot.Server.Services.Interfaces;
using TaskPilot.Shared.Validators;

namespace TaskPilot.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskPilotDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
        return services;
    }

    public static IServiceCollection AddTaskPilotAuthentication(this IServiceCollection services)
    {
        const string MultiScheme = "MultiScheme";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = MultiScheme;
            options.DefaultChallengeScheme = MultiScheme;
        })
        .AddPolicyScheme(MultiScheme, "Cookie or API Key", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                // If X-Api-Key header is present, use API key scheme
                if (context.Request.Headers.ContainsKey(AuthConstants.ApiKeyHeader))
                    return AuthConstants.ApiKeyScheme;
                // Otherwise use cookie scheme
                return AuthConstants.CookieScheme;
            };
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            AuthConstants.ApiKeyScheme, _ => { });

        return services;
    }

    public static IServiceCollection AddTaskPilotRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        return services;
    }

    public static IServiceCollection AddTaskPilotServices(this IServiceCollection services)
    {
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IStatsService, StatsService>();
        services.AddScoped<IAuditService, AuditService>();
        return services;
    }

    public static IServiceCollection AddTaskPilotValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>();
        return services;
    }
}
