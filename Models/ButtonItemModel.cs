using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Gallery.Models;

public class ButtonItemModel : IDisposable
{
    private Bitmap? _image;
    private readonly string _imageName;
    private bool _disposed;

    public string Title { get; }
    public string Content { get; }
    public string Page { get; }

    public Bitmap? Image
    {
        get
        {
            if (_disposed) return null;
            if (_image == null)
            {
                try
                {
                    using var stream = AssetLoader.Open(new Uri($"avares://CWSTool/Assets/Controls/{_imageName}.png"));
                    _image = Bitmap.DecodeToHeight(stream, 72);
                }
                catch (FileNotFoundException)
                {
#if DEBUG
                    Debug.WriteLine($"Missing control asset: {_imageName}.png");
#endif
                    return null;
                }
            }
            return _image;
        }
    }

    public ButtonItemModel(string imageName, string title, string tag, string content)
    {
        _imageName = imageName;
        Title = title;
        Page = tag;
        Content = content;
    }
    
    public static List<ButtonItemModel> CreateList(params (string imageName, string title, string page, string content)[] items)
    {
        var list = new List<ButtonItemModel>(items.Length);
        foreach (var (imageName, title, page, content) in items)
        {
            list.Add(new ButtonItemModel(imageName, title,  page, content));
        }
        return list;
    }

    public void ReleaseImage()
    {
        if (_image != null)
        {
            _image.Dispose();
            _image = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ReleaseImage();
        GC.SuppressFinalize(this);
    }
}
