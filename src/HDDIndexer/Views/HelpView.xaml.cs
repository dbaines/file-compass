using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;

namespace HDDIndexer.Views
{
    public sealed partial class HelpView : Page
    {
        private readonly Dictionary<string, HelpContent> _helpContent;

        public HelpView()
        {
            this.InitializeComponent();
            _helpContent = InitializeHelpContent();
        }

        private void OnHelpTopicSelected(object sender, TreeViewSelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TreeViewNode node && node.Tag is string topic)
            {
                ShowHelpContent(topic);
            }
        }

        private void OnQuickNavigate(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string topic)
            {
                ShowHelpContent(topic);
            }
        }

        private async void OnOpenVideoTutorials(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://youtube.com/playlist?list=your-tutorial-playlist-id");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void ShowHelpContent(string topic)
        {
            if (!_helpContent.TryGetValue(topic, out var content))
            {
                content = new HelpContent
                {
                    Title = "Topic Not Found",
                    Content = "The requested help topic is not available."
                };
            }

            ContentPanel.Children.Clear();

            // Title
            var titleBlock = new TextBlock
            {
                Text = content.Title,
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            ContentPanel.Children.Add(titleBlock);

            // Content sections
            foreach (var section in content.Sections)
            {
                if (!string.IsNullOrEmpty(section.Subtitle))
                {
                    var subtitleBlock = new TextBlock
                    {
                        Text = section.Subtitle,
                        FontSize = 16,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Margin = new Thickness(0, 20, 0, 10)
                    };
                    ContentPanel.Children.Add(subtitleBlock);
                }

                var contentBlock = new TextBlock
                {
                    Text = section.Text,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                    LineHeight = 20,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                ContentPanel.Children.Add(contentBlock);

                // Add code examples if present
                if (!string.IsNullOrEmpty(section.CodeExample))
                {
                    var codeBlock = new Border
                    {
                        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) { Opacity = 0.1 },
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(15),
                        Margin = new Thickness(0, 10, 0, 15),
                        Child = new TextBlock
                        {
                            Text = section.CodeExample,
                            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                            FontSize = 12,
                            TextWrapping = TextWrapping.Wrap
                        }
                    };
                    ContentPanel.Children.Add(codeBlock);
                }

                // Add screenshots if present
                if (!string.IsNullOrEmpty(section.ImagePath))
                {
                    var image = new Image
                    {
                        Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri($"ms-appx:///{section.ImagePath}")),
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                        MaxHeight = 400,
                        Margin = new Thickness(0, 10, 0, 15)
                    };
                    ContentPanel.Children.Add(image);
                }
            }

            // Scroll to top
            ContentScrollViewer.ChangeView(0, 0, null);
        }

        private Dictionary<string, HelpContent> InitializeHelpContent()
        {
            return new Dictionary<string, HelpContent>
            {
                ["installation"] = new HelpContent
                {
                    Title = "Installation",
                    Sections = new[]
                    {
                        new HelpSection
                        {
                            Text = "HDD Indexer is distributed as an MSIX package for Windows 10/11. You can install it from the Microsoft Store or sideload the package manually."
                        },
                        new HelpSection
                        {
                            Subtitle = "System Requirements",
                            Text = "• Windows 10 version 1809 (build 17763) or later\n• Windows 11 (recommended)\n• 4 GB RAM minimum, 8 GB recommended\n• 500 MB free disk space\n• .NET 6 runtime (installed automatically)"
                        },
                        new HelpSection
                        {
                            Subtitle = "Installation Steps",
                            Text = "1. Download the MSIX package from the official website\n2. Double-click the package file\n3. Click 'Install' when prompted\n4. Launch HDD Indexer from the Start menu"
                        }
                    }
                },

                ["quickstart"] = new HelpContent
                {
                    Title = "Quick Start Guide",
                    Sections = new[]
                    {
                        new HelpSection
                        {
                            Text = "Get up and running with HDD Indexer in just a few minutes."
                        },
                        new HelpSection
                        {
                            Subtitle = "Step 1: Add Your First Drive",
                            Text = "Navigate to the Drive Management tab and click 'Add Drive'. Select the drive you want to catalog and give it a descriptive name."
                        },
                        new HelpSection
                        {
                            Subtitle = "Step 2: Start Scanning",
                            Text = "Click the 'Scan' button next to your drive. The scan will run in the background and you can continue using your computer normally."
                        },
                        new HelpSection
                        {
                            Subtitle = "Step 3: Search Your Files",
                            Text = "Once scanning is complete, use the Search tab to find files across all your cataloged drives. Try searching for a file extension like '*.jpg' to see all your images."
                        }
                    }
                },

                ["shortcuts"] = new HelpContent
                {
                    Title = "Keyboard Shortcuts",
                    Sections = new[]
                    {
                        new HelpSection
                        {
                            Subtitle = "Navigation",
                            Text = "F1 - Show Help\nF5 - Refresh current view\nEsc - Cancel current operation\nCtrl+1-6 - Switch between main views"
                        },
                        new HelpSection
                        {
                            Subtitle = "File Operations",
                            Text = "Ctrl+N - Start new scan\nCtrl+O - Open drive browser\nCtrl+F - Open search\nCtrl+P - Print/Export\nCtrl+B - Create backup"
                        },
                        new HelpSection
                        {
                            Subtitle = "Advanced",
                            Text = "Ctrl+Shift+F - Advanced search\nCtrl+Shift+R - Restore backup\nCtrl+Shift+E - Export data"
                        },
                        new HelpSection
                        {
                            Subtitle = "Accessibility",
                            Text = "Alt+H - Toggle high contrast\nAlt+T - Toggle theme\nCtrl++ - Increase font size\nCtrl+- - Decrease font size"
                        }
                    }
                },

                ["advanced-search"] = new HelpContent
                {
                    Title = "Advanced Search",
                    Sections = new[]
                    {
                        new HelpSection
                        {
                            Text = "The advanced search feature allows you to find files using multiple criteria simultaneously."
                        },
                        new HelpSection
                        {
                            Subtitle = "Search Criteria",
                            Text = "• File name patterns (supports wildcards * and ?)\n• File extensions\n• File size range\n• Date range (creation or modification)\n• File type categories\n• Specific drives to search"
                        },
                        new HelpSection
                        {
                            Subtitle = "Pattern Matching",
                            Text = "Use wildcards in your searches:\n* matches any number of characters\n? matches exactly one character\nExample: photo*.jpg finds photo1.jpg, photo_vacation.jpg, etc."
                        }
                    }
                },

                ["troubleshooting"] = new HelpContent
                {
                    Title = "Troubleshooting",
                    Sections = new[]
                    {
                        new HelpSection
                        {
                            Subtitle = "Scan Issues",
                            Text = "If scanning fails:\n• Ensure the drive is connected and accessible\n• Check that you have permission to read the drive\n• Try running as administrator\n• Check available disk space for the catalog database"
                        },
                        new HelpSection
                        {
                            Subtitle = "Performance Issues",
                            Text = "To improve performance:\n• Close other applications during scanning\n• Adjust scan thread count in settings\n• Use SSD for catalog database storage\n• Schedule scans during off-peak hours"
                        },
                        new HelpSection
                        {
                            Subtitle = "Search Problems",
                            Text = "If searches are slow or incomplete:\n• Update the file statistics in settings\n• Rebuild the search index\n• Check for database corruption using validation tools"
                        }
                    }
                }
            };
        }
    }

    public class HelpContent
    {
        public string Title { get; set; } = "";
        public HelpSection[] Sections { get; set; } = Array.Empty<HelpSection>();
    }

    public class HelpSection
    {
        public string Subtitle { get; set; } = "";
        public string Text { get; set; } = "";
        public string CodeExample { get; set; } = "";
        public string ImagePath { get; set; } = "";
    }
}