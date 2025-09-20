using Avalonia;
using Avalonia.Controls;
using IconPacks.Avalonia.Material;

namespace FileCompass.Desktop.Views;

public partial class EmptyStateView : UserControl
{
    public static readonly StyledProperty<PackIconMaterialKind> IconProperty =
        AvaloniaProperty.Register<EmptyStateView, PackIconMaterialKind>(nameof(Icon));

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<EmptyStateView, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<EmptyStateView, string>(nameof(Message), string.Empty);

    public PackIconMaterialKind Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public EmptyStateView()
    {
        InitializeComponent();
    }
}
