using System;
using System.Collections.Generic;
using Gallery.Services;

namespace Gallery.Services.Ppt;

public interface IPptSlideShowService : IDisposable
{
    PptSlideShowState RefreshState();
    FileOpenResult OpenPresentation(string filePath, IReadOnlyDictionary<string, string>? preferences = null);
    bool StartSlideShow();
    bool EndSlideShow();
    bool Next();
    bool Previous();
    bool GoToSlide(int slideNumber);
    IReadOnlyList<PptSlideThumbnail> ExportSlideThumbnails(int width, int height);
}
