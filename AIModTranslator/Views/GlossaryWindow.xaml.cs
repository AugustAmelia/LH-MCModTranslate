using Avalonia.Controls;
using AIModTranslator.ViewModels;

namespace AIModTranslator.Views;

public partial class GlossaryWindow : Window
{
    public GlossaryWindow(GlossaryViewModel viewModel)
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        DataContext = viewModel;
    }
}
