using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Gallery.Models;
using Gallery.Services;
using Gallery.ViewModels;

namespace Gallery.Views;

public partial class PptSlideShowView : UserControl
{
    public PptSlideShowView()
    {
        InitializeComponent();
    }

    private void OnOpenFloatingWindowClicked(object? sender, RoutedEventArgs e)
    {
        PptSlideShowFloatingWindowCoordinator.ShowManual(RuntimeConfigService.Current);
    }

    private async void OnOpenPresentationClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PptSlideShowViewModel viewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = viewModel.OpenButton,
            FileTypeFilter =
            [
                new FilePickerFileType(viewModel.Title)
                {
                    Patterns =
                    [
                        "*.ppt",
                        "*.pptx",
                        "*.pptm",
                        "*.pps",
                        "*.ppsx",
                        "*.ppsm",
                        "*.pot",
                        "*.potx",
                        "*.potm",
                        "*.dps",
                        "*.dpt"
                    ]
                }
            ]
        });

        var file = files.FirstOrDefault();
        var localPath = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(localPath))
        {
            return;
        }

        await viewModel.OpenPresentationAsync(localPath);
    }
}
