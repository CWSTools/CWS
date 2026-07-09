using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Gallery.Models;
using Gallery.Services;

namespace Gallery.Views;

public partial class MiniWhiteboardWindow : Window
{
    public MiniWhiteboardWindow()
        : this(RuntimeConfigService.Current)
    {
    }

    public MiniWhiteboardWindow(AppConfig config)
    {
        InitializeComponent();

        Width = Math.Max(260, config.Iccce.MiniWhiteboard.DefaultWidth);
        Height = Math.Max(180, config.Iccce.MiniWhiteboard.DefaultHeight);
        Opacity = Math.Clamp(config.Iccce.MiniWhiteboard.DefaultOpacity, 0.35, 1.0);
        RootBorder.Background = Brush.Parse(config.Iccce.MiniWhiteboard.BackgroundColor);
        InkCanvas.PenBrush = Brush.Parse(config.Iccce.MiniWhiteboard.PenColor);
        InkCanvas.PenWidth = Math.Clamp(config.Iccce.MiniWhiteboard.PenWidth, 1, 24);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnClearClicked(object? sender, RoutedEventArgs e)
    {
        InkCanvas.Clear();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

public sealed class MiniWhiteboardInkCanvas : Control
{
    private readonly List<InkStroke> _strokes = [];
    private InkStroke? _activeStroke;

    public IBrush PenBrush { get; set; } = Brushes.White;

    public double PenWidth { get; set; } = 3;

    public MiniWhiteboardInkCanvas()
    {
        ClipToBounds = true;
        Background = Brushes.Transparent;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += OnPointerCaptureLost;
    }

    public IBrush Background { get; set; }

    public void Clear()
    {
        _strokes.Clear();
        _activeStroke = null;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(Background, Bounds);

        foreach (var stroke in _strokes)
        {
            DrawStroke(context, stroke);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _activeStroke = new InkStroke(PenBrush, PenWidth);
        _activeStroke.Points.Add(e.GetPosition(this));
        _strokes.Add(_activeStroke);
        e.Pointer.Capture(this);
        InvalidateVisual();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_activeStroke == null)
        {
            return;
        }

        _activeStroke.Points.Add(e.GetPosition(this));
        InvalidateVisual();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        FinishStroke(e.Pointer);
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _activeStroke = null;
    }

    private void FinishStroke(IPointer pointer)
    {
        _activeStroke = null;
        pointer.Capture(null);
        InvalidateVisual();
    }

    private static void DrawStroke(DrawingContext context, InkStroke stroke)
    {
        if (stroke.Points.Count == 0)
        {
            return;
        }

        var pen = new Pen(stroke.Brush, stroke.Width, lineCap: PenLineCap.Round);
        if (stroke.Points.Count == 1)
        {
            var point = stroke.Points[0];
            context.DrawEllipse(stroke.Brush, null, point, stroke.Width / 2, stroke.Width / 2);
            return;
        }

        for (var i = 1; i < stroke.Points.Count; i++)
        {
            context.DrawLine(pen, stroke.Points[i - 1], stroke.Points[i]);
        }
    }

    private sealed class InkStroke(IBrush brush, double width)
    {
        public IBrush Brush { get; } = brush;
        public double Width { get; } = width;
        public List<Point> Points { get; } = [];
    }
}
