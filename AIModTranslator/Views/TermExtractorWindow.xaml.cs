using Avalonia.Controls;
using Avalonia.Interactivity;
using AIModTranslator.ViewModels;

namespace AIModTranslator.Views;

public partial class TermExtractorWindow : Window
{
    public TermExtractorWindow()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }

    private void OnAddClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TermExtractorViewModel vm)
        {
            if (vm.AddToGlossaryCommand.CanExecute(null))
            {
                vm.AddToGlossaryCommand.Execute(null);
            }
        }
        Close();
    }
}
