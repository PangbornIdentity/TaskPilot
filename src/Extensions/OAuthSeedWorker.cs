using OpenIddict.Abstractions;
using TaskPilot.Constants;

namespace TaskPilot.Extensions;

/// <summary>
/// Hosted service that seeds the OAuth "mcp" scope into the OpenIddict store on every startup.
/// The operation is idempotent — if the scope already exists it is a no-op.
/// </summary>
public sealed class OAuthSeedWorker(IServiceProvider serviceProvider, ILogger<OAuthSeedWorker> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        if (await scopeManager.FindByNameAsync(AuthConstants.McpScope, cancellationToken) is null)
        {
            logger.LogInformation("Seeding OAuth scope '{Scope}'.", AuthConstants.McpScope);
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = AuthConstants.McpScope,
                DisplayName = "TaskPilot MCP Access",
                Description = "Access TaskPilot tasks via MCP.",
                Resources = { AuthConstants.McpResourceIndicator }
            }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
