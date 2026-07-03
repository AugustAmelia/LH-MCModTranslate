using Avalonia.Controls;
using Avalonia.Interactivity;
using AIModTranslator.ViewModels;

namespace AIModTranslator.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            // Execute save logic
            if (vm.SaveSettingsCommand.CanExecute(null))
            {
                vm.SaveSettingsCommand.Execute(null);
            }
        }
        Close();
    }
}
