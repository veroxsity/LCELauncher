namespace LceLauncher.Models;

public sealed class OnlineAccountProfile
{
    public string HomeAccountId { get; init; } = string.Empty;
    public string MicrosoftUsername { get; init; } = string.Empty;
    public string MinecraftProfileId { get; init; } = string.Empty;
    public string MinecraftUsername { get; init; } = string.Empty;
    public DateTimeOffset LastAuthenticatedAtUtc { get; init; }
    /// <summary>Crafatar face URL — populated after sign-in, null for local-auth accounts.</summary>
    public string? AvatarUrl { get; init; }
}
