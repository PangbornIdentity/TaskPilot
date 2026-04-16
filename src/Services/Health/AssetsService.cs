using System.Security.Cryptography;
using TaskPilot.Diagnostics;
using TaskPilot.Models.Health;

namespace TaskPilot.Services.Health;

/// <summary>
/// Computes SHA-256 fingerprints for tracked static assets and returns a manifest.
/// Results are computed lazily on first request and cached in memory.
/// </summary>
public sealed class AssetsService(IWebHostEnvironment env, ILogger<AssetsService> logger)
{
    private static readonly string[] TrackedAssets = ["/css/app.css"];

    private AssetsResponse? _cache;
    private readonly Lock _lock = new();

    /// <summary>Returns the asset fingerprint manifest, computing it on first call.</summary>
    public AssetsResponse GetManifest()
    {
        lock (_lock)
        {
            return _cache ??= BuildManifest();
        }
    }

    private AssetsResponse BuildManifest()
    {
        var assets = new Dictionary<string, string>();

        foreach (var assetPath in TrackedAssets)
        {
            var physicalPath = Path.Combine(env.WebRootPath, assetPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(physicalPath))
            {
                logger.LogWarning("Asset not found for health manifest: {AssetPath}", assetPath);
                continue;
            }

            var bytes = File.ReadAllBytes(physicalPath);
            var hash = SHA256.HashData(bytes);
            assets[assetPath] = $"sha256-{Convert.ToBase64String(hash)}";
        }

        return new AssetsResponse
        {
            Version = BuildInfo.Version,
            Assets = assets
        };
    }
}
