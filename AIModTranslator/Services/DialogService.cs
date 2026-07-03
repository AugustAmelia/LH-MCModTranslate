using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AIModTranslator.Services.Interfaces;
using Avalonia.Layout;
using Avalonia.Media;

namespace AIModTranslator.Services;

public class DialogService : IDialogService
{
    public Task ShowMessageAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource();
        var dialog = new Window
        {
            Title = title,
            MinWidth = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent = SizeToContent.WidthAndHeight,
            ExtendClientAreaToDecorationsHint = true,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            CanResize = false,
            ShowInTaskbar = false
        };
        
        var panel = new StackPanel { Margin = new Thickness(30), VerticalAlignment = VerticalAlignment.Center };
        
        var titleText = new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(0, 0, 0, 15), Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")) };
        panel.Children.Add(titleText);
        
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 30), Foreground = new SolidColorBrush(Color.Parse("#A0A0A0")), FontSize = 14 });
        
        var okButton = new Button 
        { 
            Content = "OK", 
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 100,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Background = new SolidColorBrush(Color.Parse("#7E57C2")),
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(6),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        okButton.Click += (s, e) => { dialog.Close(); tcs.SetResult(); };
        panel.Children.Add(okButton);
        
        dialog.Content = panel;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            dialog.ShowDialog(desktop.MainWindow);
        }
        else
        {
            dialog.Show();
        }
        
        return tcs.Task;
    }

    public Task<bool> ShowConfirmAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        var dialog = new Window
        {
            Title = title,
            MinWidth = 450,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent = SizeToContent.WidthAndHeight,
            ExtendClientAreaToDecorationsHint = true,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            CanResize = false,
            ShowInTaskbar = false
        };
        
        var panel = new StackPanel { Margin = new Thickness(30), VerticalAlignment = VerticalAlignment.Center };
        
        var titleText = new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 18, Margin = new Thickness(0, 0, 0, 15), Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")) };
        panel.Children.Add(titleText);
        
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 30), Foreground = new SolidColorBrush(Color.Parse("#A0A0A0")), FontSize = 14 });
        
        var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 15 };
        
        var cancelButton = new Button 
        { 
            Content = "Отмена", 
            Width = 100, 
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(6),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        cancelButton.Click += (s, e) => { dialog.Close(); tcs.SetResult(false); };
        
        var yesButton = new Button 
        { 
            Content = "Да", 
            Width = 100, 
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Background = new SolidColorBrush(Color.Parse("#7E57C2")),
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(6),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        yesButton.Click += (s, e) => { dialog.Close(); tcs.SetResult(true); };
        
        buttonsPanel.Children.Add(cancelButton);
        buttonsPanel.Children.Add(yesButton);
        panel.Children.Add(buttonsPanel);
        
        dialog.Content = panel;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            dialog.ShowDialog(desktop.MainWindow);
        }
        else
        {
            dialog.Show();
        }
        
        return tcs.Task;
    }
}
