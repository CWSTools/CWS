using System;
using System.ComponentModel;
using Avalonia.Controls;
using AvaloniaFluentUI.Locale;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallery.Services;

namespace Gallery.ViewModels;

public abstract partial class ViewModelBase : ObservableObject, IDisposable
{
    public virtual string Title => String.Empty;

    public ViewModelBase()
    {
        LocalizationService.Instance.PropertyChanged += OnLanguageChanged;
    }

    protected virtual void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Title));
    }

    public virtual void Dispose()
    {
        LocalizationService.Instance.PropertyChanged -= OnLanguageChanged;
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void Goto(object value)
    {
        if (value is Button button)
        {
            JumpService.GotoControl(button);
        }
    }
}
