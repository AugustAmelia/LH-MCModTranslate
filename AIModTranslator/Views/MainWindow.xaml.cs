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
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        
        LoadHighlighting();
    }

    private void LoadHighlighting()
    {
        try
        {
            using var stream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://AIModTranslator/Assets/MinecraftSyntax.xshd"));
            using var reader = new System.Xml.XmlTextReader(stream);
            AvaloniaEdit.Highlighting.HighlightingManager.Instance.RegisterHighlighting("MinecraftLang", new string[] { ".lang" }, AvaloniaEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, AvaloniaEdit.Highlighting.HighlightingManager.Instance));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load highlighting: {ex.Message}");
        }
    }

    private async void OnFileDrop(object? sender, DragEventArgs e)
    {
        Console.WriteLine("Drop event fired");
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        vm.IsDragOver = false;

        var files = e.Data.GetFiles();
        var path = files?.FirstOrDefault()?.TryGetLocalPath();
        Console.WriteLine($"Dropped path: {path}");
        
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
        Console.WriteLine("DragOver event fired");
        bool hasFiles = e.Data.Contains(DataFormats.Files) || (e.Data.GetFiles()?.Any() == true);
        e.DragEffects = hasFiles ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
        
        if (DataContext is MainViewModel vm)
            vm.IsDragOver = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        Console.WriteLine("DragLeave event fired");
        if (DataContext is MainViewModel vm)
            vm.IsDragOver = false;
    }
}
