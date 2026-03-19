using LceLauncher.Models;

namespace LceLauncher.Services;

public static class ServerListWriter
{
    public static void Write(string path, IReadOnlyList<ClientServerEntry> entries)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        writer.Write("MCSV"u8.ToArray());
        writer.Write(1U);
        writer.Write((uint)entries.Count);

        foreach (var entry in entries)
        {
            var hostBytes = System.Text.Encoding.UTF8.GetBytes(entry.Host);
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(entry.DisplayName);

            writer.Write((ushort)hostBytes.Length);
            writer.Write(hostBytes);
            writer.Write(entry.Port);
            writer.Write((ushort)nameBytes.Length);
            writer.Write(nameBytes);
        }
    }
}
