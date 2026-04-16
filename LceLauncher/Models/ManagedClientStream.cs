namespace LceLauncher.Models;

public enum ManagedClientStream
{
    Release = 0,
    Debug = 1,
}

public static class ManagedClientStreamExtensions
{
    public static string GetKey(this ManagedClientStream stream) => stream switch
    {
        ManagedClientStream.Release => "release",
        ManagedClientStream.Debug => "debug",
        _ => "release",
    };

    public static string GetDisplayName(this ManagedClientStream stream) => stream switch
    {
        ManagedClientStream.Release => "Release",
        ManagedClientStream.Debug => "Debug",
        _ => "Release",
    };

    public static string GetInstallDirectoryName(this ManagedClientStream stream) => stream switch
    {
        ManagedClientStream.Release => "nightly",
        ManagedClientStream.Debug => "debug-nightly",
        _ => "nightly",
    };
}
