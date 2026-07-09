using Avalonia.Media.Imaging;

namespace Gallery.Models;

public sealed class PptSlidePreviewItemModel
{
    public int SlideNumber { get; init; }
    public Bitmap? Thumbnail { get; init; }
}
