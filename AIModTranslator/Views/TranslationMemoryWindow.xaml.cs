using Avalonia.Controls;
using AIModTranslator.ViewModels;

namespace AIModTranslator.Views;

public partial class TranslationMemoryWindow : Window
{
    public TranslationMemoryWindow(TranslationMemoryViewModel viewModel)
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        DataContext = viewModel;
    }
}
