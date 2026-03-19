namespace LceLauncher.Models;

public sealed class ServerEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string DisplayName { get; set; } = "New Server";

    public ServerType Type { get; set; } = ServerType.NativeLce;

    public string RemoteAddress { get; set; } = string.Empty;

    public int RemotePort { get; set; } = 25565;

    public int? LocalBridgePort { get; set; }

    public bool RequiresOnlineAuth { get; set; }

    public override string ToString() => DisplayName;
}
