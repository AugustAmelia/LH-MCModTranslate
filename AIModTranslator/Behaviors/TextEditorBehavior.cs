using Avalonia;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using System;

namespace AIModTranslator.Behaviors;

public class TextEditorBehavior : Behavior<TextEditor>
{
    public static readonly StyledProperty<string> BoundTextProperty =
        AvaloniaProperty.Register<TextEditorBehavior, string>(nameof(BoundText), string.Empty, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public string BoundText
    {
        get => GetValue(BoundTextProperty);
        set => SetValue(BoundTextProperty, value);
    }

    private bool _isUpdating;

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            if (AssociatedObject.Document == null)
            {
                AssociatedObject.Document = new AvaloniaEdit.Document.TextDocument();
            }
            AssociatedObject.TextChanged += OnTextChanged;
            AssociatedObject.Text = BoundText ?? string.Empty;
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.TextChanged -= OnTextChanged;
        }
        base.OnDetaching();
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (_isUpdating || AssociatedObject == null) return;
        
        _isUpdating = true;
        SetCurrentValue(BoundTextProperty, AssociatedObject.Text);
        _isUpdating = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == BoundTextProperty)
        {
            if (_isUpdating || AssociatedObject == null) return;
            
            _isUpdating = true;
            if (AssociatedObject.Text != BoundText)
            {
                AssociatedObject.Text = BoundText ?? string.Empty;
            }
            _isUpdating = false;
        }
    }
}
