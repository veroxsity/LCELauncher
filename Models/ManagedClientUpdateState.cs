namespace LceLauncher.Models;

public sealed class ManagedClientUpdateState
{
    public DateTimeOffset? LastCheckedAtUtc { get; set; }

    public string? LastUpdateStatusText { get; set; }

    public bool LastUpdateAvailable { get; set; }

    public string? LastKnownLatestVersion { get; set; }

    public string? LastNotifiedVersion { get; set; }
}
