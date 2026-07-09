using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Gallery.Models;
using Gallery.Services;
using Gallery.ViewModels;

namespace Gallery.Views;

public partial class PptSlideShowFloatingWindow : Window
{
    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;

    private readonly DispatcherTimer _topmostTimer;
    private INotifyPropertyChanged? _viewModelNotifier;
    private object? _lastDown;

    public PptSlideShowFloatingWindow()
    {
        InitializeComponent();
        _topmostTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(750)
        };
        _topmostTimer.Tick += (_, _) => KeepAboveSlideShow();
        Opened += OnOpened;
        Closed += OnClosed;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        ApplyLayout();
        Dispatcher.UIThread.Post(PositionFloatingWindow, DispatcherPriority.Background);

        if (ShouldKeepTopmost())
        {
            KeepAboveSlideShow();
            _topmostTimer.Start();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _topmostTimer.Stop();
        DetachViewModelNotifier();
        DataContextChanged -= OnDataContextChanged;

        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        DataContext = null;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        DetachViewModelNotifier();

        if (DataContext is INotifyPropertyChanged notifier)
        {
            _viewModelNotifier = notifier;
            _viewModelNotifier.PropertyChanged += OnViewModelPropertyChanged;
        }

        ApplyLayout();
        Dispatcher.UIThread.Post(PositionFloatingWindow, DispatcherPriority.Background);
    }

    private void DetachViewModelNotifier()
    {
        if (_viewModelNotifier != null)
        {
            _viewModelNotifier.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModelNotifier = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PptSlideShowViewModel.IsPreviewExpanded)
            or nameof(PptSlideShowViewModel.FloatingWindowWidth)
            or nameof(PptSlideShowViewModel.FloatingWindowHeight)
            or nameof(PptSlideShowViewModel.CurrentSlide)
            or nameof(PptSlideShowViewModel.TotalSlides)
            or nameof(PptSlideShowViewModel.IsConnected)
            or nameof(PptSlideShowViewModel.IsInSlideShow)
            or nameof(PptSlideShowViewModel.CanStartSlideShow)
            or nameof(PptSlideShowViewModel.CanControlSlideShow))
        {
            Dispatcher.UIThread.Post(() =>
            {
                ApplyLayout();
                PositionFloatingWindow();
                SyncPreviewSelection();
            }, DispatcherPriority.Background);
        }
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (DataContext is PptSlideShowViewModel { IsPreviewExpanded: true } viewModel &&
            !IsInsideElement(e.Source, PreviewList) &&
            !IsInsideElement(e.Source, PageButtonBorder))
        {
            viewModel.IsPreviewExpanded = false;
        }

        if (IsInsideInteractiveElement(e.Source))
        {
            return;
        }

        BeginMoveDrag(e);
    }

    private static bool IsInsideElement(object? source, Visual target)
    {
        var current = source as Visual;
        while (current != null)
        {
            if (ReferenceEquals(current, target))
            {
                return true;
            }

            current = current.GetVisualParent();
        }

        return false;
    }

    private static bool IsInsideInteractiveElement(object? source)
    {
        var current = source as Visual;
        while (current != null)
        {
            if (current is Border border &&
                border.Name is "RefreshButtonBorder" or "StartEndButtonBorder" or "PreviousButtonBorder"
                    or "PageButtonBorder" or "NextButtonBorder" or "WhiteboardButtonBorder")
            {
                return true;
            }

            if (current is ListBox or ListBoxItem)
            {
                return true;
            }

            current = current.GetVisualParent();
        }

        return false;
    }

    private void OnPreviousPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _lastDown = sender;
        SetFeedback(PreviousButtonFeedbackBorder, 0.15);

        if (DataContext is PptSlideShowViewModel viewModel)
        {
            viewModel.BeginLongPress(next: false);
        }
    }

    private void OnRefreshPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _lastDown = sender;
        SetFeedback(RefreshButtonFeedbackBorder, 0.15);
    }

    private void OnRefreshPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        SetFeedback(RefreshButtonFeedbackBorder, 0);

        if (!ReferenceEquals(_lastDown, sender))
        {
            return;
        }

        _lastDown = null;
        if (DataContext is PptSlideShowViewModel viewModel)
        {
            viewModel.RefreshCommand.Execute(null);
        }
    }

    private void OnRefreshPointerExited(object? sender, PointerEventArgs e)
    {
        SetFeedback(RefreshButtonFeedbackBorder, 0);
        _lastDown = null;
    }

    private void OnStartEndPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _lastDown = sender;
        SetFeedback(StartEndButtonFeedbackBorder, 0.15);
    }

    private void OnStartEndPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        SetFeedback(StartEndButtonFeedbackBorder, 0);

        if (!ReferenceEquals(_lastDown, sender))
        {
            return;
        }

        _lastDown = null;
        if (DataContext is not PptSlideShowViewModel viewModel)
        {
            return;
        }

        if (viewModel.CanStartSlideShow)
        {
            viewModel.StartSlideShowCommand.Execute(null);
        }
        else if (viewModel.CanControlSlideShow)
        {
            viewModel.EndSlideShowCommand.Execute(null);
        }
    }

    private void OnStartEndPointerExited(object? sender, PointerEventArgs e)
    {
        SetFeedback(StartEndButtonFeedbackBorder, 0);
        _lastDown = null;
    }

    private void OnPreviousPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        SetFeedback(PreviousButtonFeedbackBorder, 0);
        var wasRepeating = EndLongPress();

        if (!ReferenceEquals(_lastDown, sender))
        {
            return;
        }

        _lastDown = null;
        if (!wasRepeating && DataContext is PptSlideShowViewModel viewModel)
        {
            viewModel.PreviousCommand.Execute(null);
        }
    }

    private void OnPreviousPointerExited(object? sender, PointerEventArgs e)
    {
        SetFeedback(PreviousButtonFeedbackBorder, 0);
        _lastDown = null;
        EndLongPress();
    }

    private void OnNextPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _lastDown = sender;
        SetFeedback(NextButtonFeedbackBorder, 0.15);

        if (DataContext is PptSlideShowViewModel viewModel)
        {
            viewModel.BeginLongPress(next: true);
        }
    }

    private void OnNextPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        SetFeedback(NextButtonFeedbackBorder, 0);
        var wasRepeating = EndLongPress();

        if (!ReferenceEquals(_lastDown, sender))
        {
            return;
        }

        _lastDown = null;
        if (!wasRepeating && DataContext is PptSlideShowViewModel viewModel)
        {
            viewModel.NextCommand.Execute(null);
        }
    }

    private void OnNextPointerExited(object? sender, PointerEventArgs e)
    {
        SetFeedback(NextButtonFeedbackBorder, 0);
        _lastDown = null;
        EndLongPress();
    }

    private void OnWhiteboardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _lastDown = sender;
        SetFeedback(WhiteboardButtonFeedbackBorder, 0.15);
    }

    private void OnWhiteboardPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        SetFeedback(WhiteboardButtonFeedbackBorder, 0);

        if (!ReferenceEquals(_lastDown, sender))
        {
            return;
        }

        _lastDown = null;
        if (RuntimeConfigService.Current.Iccce.MiniWhiteboard.IsEnabled)
        {
            MiniWhiteboardWindowCoordinator.Show(RuntimeConfigService.Current);
        }
    }

    private void OnWhiteboardPointerExited(object? sender, PointerEventArgs e)
    {
        SetFeedback(WhiteboardButtonFeedbackBorder, 0);
        _lastDown = null;
    }

    private void OnPagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _lastDown = sender;
        SetFeedback(PageButtonFeedbackBorder, 0.15);
    }

    private void OnPagePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        SetFeedback(PageButtonFeedbackBorder, 0);

        if (!ReferenceEquals(_lastDown, sender))
        {
            return;
        }

        _lastDown = null;
        if (DataContext is not PptSlideShowViewModel viewModel)
        {
            return;
        }

        if (viewModel.CanStartSlideShow)
        {
            viewModel.StartSlideShowCommand.Execute(null);
            return;
        }

        viewModel.TogglePreviewCommand.Execute(null);
    }

    private void OnPagePointerExited(object? sender, PointerEventArgs e)
    {
        SetFeedback(PageButtonFeedbackBorder, 0);
        _lastDown = null;
    }

    private void OnPreviewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox ||
            listBox.SelectedItem is not PptSlidePreviewItemModel item ||
            DataContext is not PptSlideShowViewModel viewModel)
        {
            return;
        }

        listBox.SelectedItem = null;
        viewModel.SelectPreviewSlideCommand.Execute(item);
    }

    private bool EndLongPress()
    {
        if (DataContext is PptSlideShowViewModel viewModel)
        {
            viewModel.EndLongPress();
            return viewModel.ConsumeLongPressRepeating();
        }

        return false;
    }

    private void ApplyLayout()
    {
        if (DataContext is not PptSlideShowViewModel viewModel)
        {
            return;
        }

        var isSide = viewModel.IsFloatingSideLayout;
        ButtonRow.Orientation = isSide ? Orientation.Vertical : Orientation.Horizontal;
        PreviousButtonIcon.Data = Geometry.Parse(isSide
            ? "M11.0357,3.3994C11.5682,2.86687 12.4316,2.86687 12.9641,3.3994L19.5096,9.94485C20.0421,10.4774 20.0421,11.3408 19.5096,11.8733C18.9771,12.4059 18.1137,12.4059 17.5811,11.8733L13.3635,7.65575V19.6364C13.3635,20.3895 12.753,21 11.9999,21C11.2468,21 10.6363,20.3895 10.6363,19.6364V7.65575L6.41869,11.8733C5.88616,12.4059 5.02275,12.4059 4.49022,11.8733C3.95769,11.3408 3.95769,10.4774 4.49022,9.94485L11.0357,3.3994Z"
            : "M19.5 5.5 9 16l10.5 10.5-2.1 2.1L4.8 16 17.4 3.4z");
        NextButtonIcon.Data = Geometry.Parse(isSide
            ? "M11.0357,20.6006C11.5682,21.1331 12.4316,21.1331 12.9641,20.6006L19.5096,14.0551C20.0421,13.5226 20.0421,12.6592 19.5096,12.1267C18.9771,11.5941 18.1137,11.5941 17.5811,12.1267L13.3635,16.3443V4.36364C13.3635,3.61052 12.753,3 11.9999,3C11.2468,3 10.6363,3.61052 10.6363,4.36364V16.3443L6.41869,12.1267C5.88616,11.5941 5.02275,11.5941 4.49022,12.1267C3.95769,12.6592 3.95769,13.5226 4.49022,14.0551L11.0357,20.6006Z"
            : "M12.6 3.4 25.2 16 12.6 28.6l-2.1-2.1L21 16 10.5 5.5z");
        StartEndButtonIcon.Data = Geometry.Parse(viewModel.CanControlSlideShow
            ? "M7 7h18v18H7z"
            : "M10 6l16 10-16 10z");

        PageButtonBorder.IsVisible = viewModel.IsPageNumberVisible;
        StartEndButtonBorder.Opacity = viewModel.CanStartSlideShow || viewModel.CanControlSlideShow ? 1.0 : 0.38;
        WhiteboardButtonBorder.Opacity = RuntimeConfigService.Current.Iccce.MiniWhiteboard.IsEnabled ? 1.0 : 0.38;

        switch (viewModel.FloatingWindowPosition)
        {
            case "LeftBottom":
                DockPanel.SetDock(PreviewList, Dock.Top);
                DockPanel.SetDock(ButtonRow, Dock.Bottom);
                PreviewList.HorizontalAlignment = HorizontalAlignment.Left;
                ButtonRow.HorizontalAlignment = HorizontalAlignment.Left;
                break;
            case "LeftSide":
                DockPanel.SetDock(PreviewList, Dock.Right);
                DockPanel.SetDock(ButtonRow, Dock.Left);
                PreviewList.HorizontalAlignment = HorizontalAlignment.Left;
                ButtonRow.HorizontalAlignment = HorizontalAlignment.Left;
                break;
            case "RightSide":
                DockPanel.SetDock(PreviewList, Dock.Left);
                DockPanel.SetDock(ButtonRow, Dock.Right);
                PreviewList.HorizontalAlignment = HorizontalAlignment.Right;
                ButtonRow.HorizontalAlignment = HorizontalAlignment.Right;
                break;
            default:
                DockPanel.SetDock(PreviewList, Dock.Top);
                DockPanel.SetDock(ButtonRow, Dock.Bottom);
                PreviewList.HorizontalAlignment = HorizontalAlignment.Right;
                ButtonRow.HorizontalAlignment = HorizontalAlignment.Right;
                break;
        }
    }

    private void PositionFloatingWindow()
    {
        var workingArea = Screens.Primary?.WorkingArea;
        if (workingArea is not { } area)
        {
            return;
        }

        var width = Math.Max(1, (int)Math.Ceiling(Bounds.Width > 0 ? Bounds.Width : ClientSize.Width));
        var height = Math.Max(1, (int)Math.Ceiling(Bounds.Height > 0 ? Bounds.Height : ClientSize.Height));
        Position = new PixelPoint(
            area.X + (area.Width - width) / 2,
            area.Y + area.Height - height - 36);
    }

    private void SyncPreviewSelection()
    {
        if (DataContext is not PptSlideShowViewModel viewModel || viewModel.CurrentSlide <= 0)
        {
            return;
        }

        foreach (var item in PreviewList.Items)
        {
            if (item is PptSlidePreviewItemModel preview && preview.SlideNumber == viewModel.CurrentSlide)
            {
                PreviewList.SelectedItem = preview;
                PreviewList.ScrollIntoView(preview);
                return;
            }
        }
    }

    private static void SetFeedback(Control? feedback, double opacity)
    {
        if (feedback != null)
        {
            feedback.Opacity = opacity;
        }
    }

    private void KeepAboveSlideShow()
    {
        if (!ShouldKeepTopmost())
        {
            Topmost = false;
            return;
        }

        Topmost = true;

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        SetWindowPos(
            handle,
            HwndTopmost,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
    }

    private bool ShouldKeepTopmost() =>
        DataContext is not PptSlideShowViewModel ||
        RuntimeConfigService.Current.Iccce.Ppt.KeepFloatingWindowTopmost;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}
