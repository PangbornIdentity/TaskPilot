using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using TaskPilot.Extensions;
using TaskPilot.Constants;
using TaskPilot.Data;
using TaskPilot.Repositories;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.Validators;

namespace TaskPilot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskPilotDatabase(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (env.IsDevelopment())
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(connectionString);
        });

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
                if (context.Request.Headers.ContainsKey(AuthConstants.ApiKeyHeader))
                    return AuthConstants.ApiKeyScheme;
                return AuthConstants.CookieScheme;
            };
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            AuthConstants.ApiKeyScheme, _ => { });

        // Configure cookie auth to redirect web requests to login page, return 401 for API
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/auth/login";
            options.LogoutPath = "/auth/logout";
            options.AccessDeniedPath = "/auth/login";
            options.Events.OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };
        });

        return services;
    }

    public static IServiceCollection AddTaskPilotRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<ITaskTypeRepository, TaskTypeRepository>();
        return services;
    }

    public static IServiceCollection AddTaskPilotServices(this IServiceCollection services)
    {
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IStatsService, StatsService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<ITaskTypeService, TaskTypeService>();
        return services;
    }

    public static IServiceCollection AddTaskPilotChangelog(this IServiceCollection services, IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "app-changelog.json");
        var json = File.Exists(path) ? File.ReadAllText(path) : "{}";
        services.AddSingleton<IChangelogService>(new ChangelogService(json));
        return services;
    }

    public static IServiceCollection AddTaskPilotValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>();
        return services;
    }
}
