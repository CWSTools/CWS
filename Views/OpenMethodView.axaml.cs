using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaFluentUI.Locale;
using AvaloniaFluentUI.Controls;
using Gallery.Models;
using Gallery.Services;
using Gallery.ViewModels;

namespace Gallery.Views;

public partial class OpenMethodView : UserControl
{
    private bool _isRevertingSelection;

    public OpenMethodView()
    {
        InitializeComponent();
    }

    private void OnTargetSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isRevertingSelection ||
            sender is not ComboBox comboBox ||
            comboBox.DataContext is not OpenMethodEntryModel entry ||
            !entry.HasPendingChange)
        {
            return;
        }
    }

    private async void OnShowCustomDialog(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.DataContext is not OpenMethodEntryModel entry ||
            DataContext is not OpenMethodViewModel viewModel ||
            !entry.HasPendingChange)
        {
            return;
        }

        Dialog.Title = viewModel.ConfirmDialogTitle;
        Dialog.PrimaryButtonText = viewModel.ConfirmApplyButton;
        Dialog.CloseButtonText = viewModel.ConfirmCancelButton;
        Dialog.Content = viewModel.BuildConfirmMessage(entry);
        var result = await Dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            viewModel.ApplySelection(entry);
            AppLoggerService.Info("open-method", $"Confirmed selection change for {entry.Key}.");
        }
        else
        {
            _isRevertingSelection = true;
            entry.RevertSelection();
            _isRevertingSelection = false;
            AppLoggerService.Info("open-method", $"Canceled selection change for {entry.Key}.");
        }
    }

    private void OnRefreshCurrentClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is OpenMethodViewModel viewModel)
        {
            viewModel.RefreshDetectedCurrentTargets();
            viewModel.RefreshSystemEntryStatus();
            AppLoggerService.Info("open-method", "Manual refresh of current defaults triggered.");
        }
    }

    private void OnRegisterSystemEntryClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is OpenMethodViewModel viewModel)
        {
            viewModel.RegisterSystemEntry();
            AppLoggerService.Info("open-method", "Register system entry triggered.");
        }
    }

    private async void OnTestOpenClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.DataContext is not OpenMethodEntryModel entry ||
            DataContext is not OpenMethodViewModel viewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            entry.SetStatus(LocalizationService.Instance.GetString("OM_ResultStorageUnavailable"));
            return;
        }

        var patterns = viewModel.GetExtensionsForEntry(entry.Key)
            .Select(extension => $"*{extension}")
            .ToArray();

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = $"{entry.Title} - {viewModel.TestOpenButton}",
            FileTypeFilter =
            [
                new FilePickerFileType(entry.Title)
                {
                    Patterns = patterns
                }
            ]
        });

        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        var localPath = file.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(localPath))
        {
            entry.SetStatus(LocalizationService.Instance.GetString("OM_ResultLocalFileOnly"));
            return;
        }

        viewModel.OpenFile(entry, localPath);
    }
}
