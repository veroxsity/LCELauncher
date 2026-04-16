namespace LceLauncher.ViewModels;

public sealed class SidebarNavItem
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? Subtitle { get; init; }

    /// <summary>Direct content for items that have no tabs (e.g. HOME, ABOUT).</summary>
    public ViewModelBase? DirectContent { get; init; }

    /// <summary>Tabs shown in the content header when this sidebar item is selected.</summary>
    public IReadOnlyList<TabNavItem>? Tabs { get; init; }
}
