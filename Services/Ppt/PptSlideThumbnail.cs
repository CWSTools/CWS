namespace Gallery.Services.Ppt;

public sealed class PptSlideThumbnail
{
    public int SlideNumber { get; init; }
    public byte[] PngBytes { get; init; } = [];
}
