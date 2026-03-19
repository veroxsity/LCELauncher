using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class ServerManager
{
    public void Normalize(LauncherConfig config)
    {
        if (config.FirstBridgePort < 1024)
        {
            config.FirstBridgePort = 25570;
        }

        var usedPorts = new HashSet<int>();
        foreach (var server in config.Servers)
        {
            if (string.IsNullOrWhiteSpace(server.Id))
            {
                server.Id = Guid.NewGuid().ToString("N");
            }

            server.DisplayName = string.IsNullOrWhiteSpace(server.DisplayName) ? "Unnamed Server" : server.DisplayName.Trim();
            server.RemoteAddress = server.RemoteAddress.Trim();
            server.RemotePort = server.RemotePort <= 0 ? 25565 : server.RemotePort;

            if (server.Type == ServerType.JavaBridge)
            {
                if (server.LocalBridgePort is null || server.LocalBridgePort < 1024 || !usedPorts.Add(server.LocalBridgePort.Value))
                {
                    server.LocalBridgePort = AllocateNextBridgePort(config, usedPorts);
                }
                else
                {
                    usedPorts.Add(server.LocalBridgePort.Value);
                }
            }
            else
            {
                server.LocalBridgePort = null;
            }
        }

        if (!config.Servers.Any(server => server.Id == config.SelectedServerId))
        {
            config.SelectedServerId = config.Servers.FirstOrDefault()?.Id;
        }
    }

    public int AllocateNextBridgePort(LauncherConfig config)
    {
        var usedPorts = config.Servers
            .Where(server => server.Type == ServerType.JavaBridge && server.LocalBridgePort.HasValue)
            .Select(server => server.LocalBridgePort!.Value)
            .ToHashSet();

        return AllocateNextBridgePort(config, usedPorts);
    }

    public IReadOnlyList<ClientServerEntry> BuildClientServerEntries(LauncherConfig config)
    {
        Normalize(config);

        return config.Servers
            .Where(server => !string.IsNullOrWhiteSpace(server.RemoteAddress))
            .Select(server =>
            {
                if (server.Type == ServerType.JavaBridge)
                {
                    return new ClientServerEntry("127.0.0.1", (ushort)server.LocalBridgePort!.Value, server.DisplayName);
                }

                return new ClientServerEntry(server.RemoteAddress, (ushort)server.RemotePort, server.DisplayName);
            })
            .ToList();
    }

    private static int AllocateNextBridgePort(LauncherConfig config, ISet<int> usedPorts)
    {
        var port = config.FirstBridgePort;
        while (usedPorts.Contains(port))
        {
            port++;
        }

        usedPorts.Add(port);
        return port;
    }
}
