using Avalonia.Controls;
using AIModTranslator.ViewModels;

namespace AIModTranslator.Views;

public partial class GlossaryWindow : Window
{
    public GlossaryWindow()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }

    public GlossaryWindow(GlossaryViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
