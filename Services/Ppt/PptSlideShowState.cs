namespace Gallery.Services.Ppt;

public sealed record PptSlideShowState
{
    public bool IsConnected { get; init; }
    public bool IsInSlideShow { get; init; }
    public int CurrentSlide { get; init; }
    public int TotalSlides { get; init; }
    public string PresentationName { get; init; } = string.Empty;
    public string PresentationPath { get; init; } = string.Empty;
    public string ConnectionKind { get; init; } = string.Empty;
    public string StatusMessage { get; init; } = string.Empty;

    public static PptSlideShowState Disconnected(string message = "") => new()
    {
        StatusMessage = message
    };
}
