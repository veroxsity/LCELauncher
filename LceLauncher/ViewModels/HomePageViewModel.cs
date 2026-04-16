namespace LceLauncher.ViewModels;

public sealed class HomePageViewModel : ViewModelBase
{
    public string WelcomeTitle => "Minecraft: Legacy Console Edition";
    public string WelcomeSubtitle => "Community-driven preservation project";
    public string Description =>
        "LCE recreates the classic console Minecraft experience on PC, " +
        "bringing back split-screen, console-specific world generation, and the original UI. " +
        "Use the Downloads tab to install the latest nightly build, manage servers in the Servers tab, " +
        "and hit Play when you're ready.";
}
