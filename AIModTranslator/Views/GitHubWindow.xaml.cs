using Avalonia.Controls;
using Avalonia.Interactivity;
using AIModTranslator.ViewModels;

namespace AIModTranslator.Views;

public partial class GitHubWindow : Window
{
    public GitHubWindow()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }

    private void OnDownloadClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GitHubViewModel vm)
        {
            if (vm.DownloadCommand.CanExecute(null))
            {
                vm.DownloadCommand.Execute(null);
            }
        }
        Close();
    }
}
