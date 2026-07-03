using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using AIModTranslator.ViewModels;
using Avalonia.Platform.Storage;

namespace AIModTranslator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        AddHandler(DragDrop.DropEvent, OnFileDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private async void OnFileDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var files = e.Data.GetFiles();
        var path = files?.FirstOrDefault()?.TryGetLocalPath();
        
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (System.IO.Directory.Exists(path))
        {
            await vm.LoadDroppedFolderAsync(path);
        }
        else if (path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
        {
            await vm.LoadDroppedJarAsync(path);
        }
        else
        {
            await vm.LoadDroppedFileAsync(path);
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.GetFiles() != null && e.Data.GetFiles().Any()
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }
}
