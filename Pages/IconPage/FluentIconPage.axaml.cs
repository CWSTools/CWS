using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaFluentUI.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Gallery.Controls;
using Gallery.Messages.IconViewMessages;

namespace Gallery.Pages.IconPage;

public partial class FluentIconPage : UserControl
{
    private const int BatchSize = 20;
    private readonly int _iconWidth = 92;
    private readonly int _iconHeight = 92;

    private CheckedBorder? _currentItem;
    private readonly List<CheckedBorder> _allCards = new();
    private CancellationTokenSource? _loadCancellation;
    private bool _iconsLoaded;
    
    public FluentIconPage()
    {
        InitializeComponent();
        
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_iconsLoaded)
        {
            return;
        }

        _loadCancellation = new CancellationTokenSource();
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(100, _loadCancellation.Token);
            await LoadIconsAsync(_loadCancellation.Token);
        }, DispatcherPriority.Loaded);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = null;

        foreach (var card in _allCards)
        {
            card.PointerReleased -= OnIconCardPointerReleased;
            card.ContextMenu = null;
            card.Child = null;
        }

        _allCards.Clear();
        UniformGrid.Children.Clear();
        _currentItem = null;
        _iconsLoaded = false;

        base.OnDetachedFromVisualTree(e);
    }
    
    private async Task LoadIconsAsync(CancellationToken cancellationToken)
    {
        if (_iconsLoaded)
        {
            return;
        }

        var allIcons = GetAllIcons();
        var iconList = allIcons.ToList();

        foreach (var chunk in Chunk(iconList, BatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var (name, path) in chunk)
            {
                var iconCard = CreateIconCard(name, path);
                UniformGrid.Children.Add(iconCard);
                _allCards.Add(iconCard);
            }

            await Task.Delay(1, cancellationToken);
        }

        _iconsLoaded = true;
    }

    private static IEnumerable<List<KeyValuePair<string, Geometry>>> Chunk(List<KeyValuePair<string, Geometry>> source, int batchSize)
    {
        for (int i = 0; i < source.Count; i += batchSize)
            yield return source.GetRange(i, Math.Min(batchSize, source.Count - i));
    }

    private CheckedBorder CreateIconCard(string name, Geometry data)
    {
        var iconCard = new CheckedBorder
        {
            Classes = { "IconCard" },
            Width = _iconWidth,
            Height = _iconHeight,
            Child = new StackPanel
            {
                Children =
                {
                    new PathIcon { Name = "PART_PathIcon", Tag = name, Data = data },
                    new TextBlock { Name = "PART_Name", Text = name }
                }
            }
        };

        iconCard.PointerReleased += OnIconCardPointerReleased;

        return iconCard;
    }

    private void OnIconCardPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        if (_currentItem != null) _currentItem.IsChecked = false;

        if (sender is CheckedBorder border)
        {
            var icon = border.FindLogicalDescendantOfType<PathIcon>();
            if (icon == null) return;

            _currentItem = border;
            WeakReferenceMessenger.Default.Send(new CheckedIconChangedMessage((string)icon.Tag!, icon.Data!));
        }

        e.Handled = true;
    }

    private Dictionary<string, Geometry> GetAllIcons()
    {
        return typeof(AvaloniaFluentUI.Icons.FluentIcon)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsStatic && f.FieldType == typeof(Geometry))
            .ToDictionary(f => f.Name, f => (Geometry)f.GetValue(null)!);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        var columns = (int)UniformGrid.Bounds.Width / (_iconWidth + (int)UniformGrid.ColumnSpacing);
        UniformGrid.Columns = columns;
    }

    private void ApplyFilter(string searchText)
    {
        foreach (var card in _allCards)
        {
            if (card.FindLogicalDescendantOfType<TextBlock>() is { Name: "PART_Name" } tb)
            {
                card.IsVisible = tb.Text?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;
            }
            else
            {
                card.IsVisible = false;
            }
        }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is SearchTextBox tb)
        {
            ApplyFilter(tb.Text ?? string.Empty);
        }
    }
}

