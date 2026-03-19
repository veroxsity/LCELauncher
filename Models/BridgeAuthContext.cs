namespace LceLauncher.Models;

public sealed class BridgeAuthContext
{
    public string MinecraftProfileId { get; init; } = string.Empty;

    public string MinecraftUsername { get; init; } = string.Empty;

    public string MinecraftAccessToken { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; init; }
}
