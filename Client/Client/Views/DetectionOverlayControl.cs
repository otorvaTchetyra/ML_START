using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Client.Models;
using System;
using System.Collections.Generic;

namespace Client;

public class DetectionOverlayControl : Control
{
    private List<OverlayDetection> _detections = new();

    private static readonly IPen BoxPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 50, 50)), 3);
    private static readonly IBrush BoxFill = new SolidColorBrush(Color.FromArgb(80, 255, 50, 50));

    public void Update(IReadOnlyList<OverlayDetection> detections)
    {
        _detections = new List<OverlayDetection>(detections);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        foreach (var d in _detections)
        {
            var w = Math.Max(d.Width, 16);
            var h = Math.Max(d.Height, 16);
            var rect = new Rect(
                d.Left - (w - d.Width) / 2.0,
                d.Top - (h - d.Height) / 2.0,
                w, h);
            context.DrawRectangle(BoxFill, BoxPen, rect);
        }
    }
}
