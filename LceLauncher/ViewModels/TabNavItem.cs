namespace LceLauncher.ViewModels;

public sealed class TabNavItem
{
    public required string DisplayName { get; init; }
    public required ViewModelBase Content { get; init; }
}
