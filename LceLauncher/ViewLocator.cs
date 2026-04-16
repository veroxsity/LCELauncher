using Avalonia.Controls;
using Avalonia.Controls.Templates;
using LceLauncher.ViewModels;

namespace LceLauncher;

public sealed class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null) return null;

        var name = param.GetType().FullName!
            .Replace("ViewModel", "View", StringComparison.Ordinal)
            .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal);

        var type = Type.GetType(name);
        if (type is not null)
            return (Control)Activator.CreateInstance(type)!;

        return new TextBlock
        {
            Text = $"View not found: {name}",
            Foreground = Avalonia.Media.Brushes.OrangeRed,
        };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
