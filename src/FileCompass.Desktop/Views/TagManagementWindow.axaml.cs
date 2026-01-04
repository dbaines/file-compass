using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using FileCompass.Core.Models;
using FileCompass.Desktop.Services;
using FileCompass.Desktop.ViewModels;
using FileCompass.Translations;

namespace FileCompass.Desktop.Views;

public partial class TagManagementWindow : Window
{
    private TagManagementViewModel ViewModel => (TagManagementViewModel)DataContext!;

    public TagManagementWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnNewColourClick(object? sender, RoutedEventArgs e)
    {
        ShowColourPickerPopup(NewColourButton, ViewModel.NewTagColour, colour =>
        {
            ViewModel.NewTagColour = colour;
        });
    }

    private void OnEditTagClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Tag tag)
        {
            ShowEditDialog(tag);
        }
    }

    private async void ShowEditDialog(Tag tag)
    {
        var editName = tag.Name;
        var editColour = tag.Colour;

        var nameTextBox = new TextBox
        {
            Text = editName,
            Watermark = Strings.TagsNamePlaceholder,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var colourButton = new Button
        {
            Width = 36,
            Height = 36,
            Content = new Avalonia.Controls.Shapes.Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(Color.Parse(editColour))
            }
        };

        colourButton.Click += (s, args) =>
        {
            ShowColourPickerPopup(colourButton, editColour, colour =>
            {
                editColour = colour;
                if (colourButton.Content is Avalonia.Controls.Shapes.Ellipse ellipse)
                {
                    ellipse.Fill = new SolidColorBrush(Color.Parse(colour));
                }
            });
        };

        var inputPanel = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        Grid.SetColumn(nameTextBox, 0);
        Grid.SetColumn(colourButton, 1);
        inputPanel.Children.Add(nameTextBox);
        inputPanel.Children.Add(colourButton);

        var saveButton = new Button
        {
            Content = Strings.ButtonSave,
            Width = 80,
            Tag = "save"
        };

        var cancelButton = new Button
        {
            Content = Strings.ButtonCancel,
            Width = 80,
            Margin = new Thickness(8, 0, 0, 0),
            Tag = "cancel"
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(cancelButton);

        var dialog = new Window
        {
            Title = Strings.ButtonEdit,
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Children =
                {
                    inputPanel,
                    buttonPanel
                }
            }
        };

        var saved = false;

        saveButton.Click += (s, args) =>
        {
            saved = true;
            dialog.Close();
        };

        cancelButton.Click += (s, args) =>
        {
            dialog.Close();
        };

        await dialog.ShowDialog(this);

        if (saved && !string.IsNullOrWhiteSpace(nameTextBox.Text))
        {
            tag.Name = nameTextBox.Text.Trim();
            tag.Colour = editColour;
            await ServiceLocator.TagRepository.UpdateAsync(tag);
            await ViewModel.LoadTagsAsync();
        }
    }

    private async void OnDeleteTagClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Tag tag)
        {
            var usageCount = await TagManagementViewModel.GetTagUsageCountAsync(tag.Id);

            string message;
            if (usageCount > 0)
            {
                message = string.Format(CultureInfo.CurrentCulture, Strings.DialogDeleteTagMessage, usageCount);
            }
            else
            {
                message = string.Format(CultureInfo.CurrentCulture, Strings.DialogDeleteTagConfirmMessage, tag.Name);
            }

            var confirmed = await ShowConfirmationDialogAsync(Strings.DialogDeleteTagTitle, message);

            if (confirmed)
            {
                await ViewModel.DeleteTagAsync(tag.Id);
            }
        }
    }

    private void ShowColourPickerPopup(Control target, string currentColour, Action<string> onColourSelected)
    {
        var colorView = new Avalonia.Controls.ColorView
        {
            Color = Color.Parse(currentColour),
            ColorModel = Avalonia.Controls.ColorModel.Hsva,
            ColorSpectrumShape = Avalonia.Controls.ColorSpectrumShape.Ring,
            IsColorPaletteVisible = true,
            IsColorComponentsVisible = false,
            IsHexInputVisible = true,
            IsAlphaVisible = false,
            IsColorSpectrumSliderVisible = false,
            IsAccentColorsVisible = false,
            PaletteColors = GetFluentPalette()
        };

        // Select palette tab by default (delayed to ensure control is fully loaded)
        colorView.AttachedToVisualTree += async (s, args) =>
        {
            await Task.Yield();
            var tabControl = colorView.FindDescendantOfType<TabControl>();
            if (tabControl != null && tabControl.ItemCount > 1)
            {
                tabControl.SelectedIndex = 1; // Palette is second tab
            }
        };

        var selectButton = new Button
        {
            Content = Strings.ButtonOk,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var popup = new Popup
        {
            PlacementTarget = target,
            Placement = PlacementMode.Bottom,
            IsLightDismissEnabled = true,
            Child = new Border
            {
                Background = Background,
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Child = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        colorView,
                        selectButton
                    }
                }
            }
        };

        selectButton.Click += (s, args) =>
        {
            var color = colorView.Color;
            var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            onColourSelected(hex);
            popup.IsOpen = false;
        };

        // Add popup to the visual tree via OverlayLayer
        var overlayLayer = OverlayLayer.GetOverlayLayer(this);
        if (overlayLayer != null)
        {
            overlayLayer.Children.Add(popup);
            popup.Closed += (s, args) => overlayLayer.Children.Remove(popup);
        }

        popup.IsOpen = true;
    }

    private static IEnumerable<Color> GetFluentPalette()
    {
        return
        [
            Color.Parse("#FFB900"), Color.Parse("#FF8C00"), Color.Parse("#F7630C"), Color.Parse("#CA5010"),
            Color.Parse("#DA3B01"), Color.Parse("#EF6950"), Color.Parse("#D13438"), Color.Parse("#FF4343"),
            Color.Parse("#E74856"), Color.Parse("#E81123"), Color.Parse("#EA005E"), Color.Parse("#C30052"),
            Color.Parse("#E3008C"), Color.Parse("#BF0077"), Color.Parse("#C239B3"), Color.Parse("#9A0089"),
            Color.Parse("#0078D7"), Color.Parse("#0063B1"), Color.Parse("#8E8CD8"), Color.Parse("#6B69D6"),
            Color.Parse("#8764B8"), Color.Parse("#744DA9"), Color.Parse("#B146C2"), Color.Parse("#881798"),
            Color.Parse("#0099BC"), Color.Parse("#2D7D9A"), Color.Parse("#00B7C3"), Color.Parse("#038387"),
            Color.Parse("#00B294"), Color.Parse("#018574"), Color.Parse("#00CC6A"), Color.Parse("#10893E"),
            Color.Parse("#7A7574"), Color.Parse("#5D5A58"), Color.Parse("#68768A"), Color.Parse("#515C6B"),
            Color.Parse("#567C73"), Color.Parse("#486860"), Color.Parse("#498205"), Color.Parse("#107C10"),
            Color.Parse("#767676"), Color.Parse("#4C4A48"), Color.Parse("#69797E"), Color.Parse("#4A5459"),
            Color.Parse("#647C64"), Color.Parse("#525E54"), Color.Parse("#847545"), Color.Parse("#7E735F")
        ];
    }

    private async Task<bool> ShowConfirmationDialogAsync(string title, string message)
    {
        var result = false;

        var yesButton = new Button
        {
            Content = Strings.ButtonDelete,
            Width = 80,
            Tag = "yes"
        };

        var noButton = new Button
        {
            Content = Strings.ButtonCancel,
            Width = 80,
            Margin = new Thickness(8, 0, 0, 0),
            Tag = "no"
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        buttonPanel.Children.Add(yesButton);
        buttonPanel.Children.Add(noButton);

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap
                    },
                    buttonPanel
                }
            }
        };

        yesButton.Click += (s, args) =>
        {
            result = true;
            dialog.Close();
        };

        noButton.Click += (s, args) =>
        {
            dialog.Close();
        };

        await dialog.ShowDialog(this);

        return result;
    }
}
