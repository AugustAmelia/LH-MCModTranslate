using Avalonia.Controls;
using AIModTranslator.ViewModels;

namespace AIModTranslator.Views;

public partial class TranslationMemoryWindow : Window
{
    public TranslationMemoryWindow()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }

    public TranslationMemoryWindow(TranslationMemoryViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
