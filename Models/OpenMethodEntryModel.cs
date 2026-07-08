using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Gallery.Models;

public partial class OpenMethodEntryModel : ObservableObject
{
    private readonly Action _onConfirmedChanged;
    private OpenMethodTargetOption _appliedOption;

    public OpenMethodEntryModel(
        string key,
        string title,
        string extensions,
        string description,
        string selectedTarget,
        IEnumerable<OpenMethodTargetOption> targetOptions,
        Action onConfirmedChanged)
    {
        Key = key;
        _title = title;
        Extensions = extensions;
        _description = description;
        _onConfirmedChanged = onConfirmedChanged;

        foreach (var option in targetOptions)
        {
            TargetOptions.Add(option);
        }

        _appliedOption = ResolveOption(selectedTarget);
        _selectedOption = _appliedOption;
    }

    public string Key { get; }

    [ObservableProperty]
    private string _title;

    public string Extensions { get; }

    [ObservableProperty]
    private string _description;

    public ObservableCollection<OpenMethodTargetOption> TargetOptions { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingChange))]
    private OpenMethodTargetOption _selectedOption;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _currentTargetLabel = string.Empty;

    public bool HasPendingChange => SelectedOption.Value != _appliedOption.Value;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public void ConfirmSelection()
    {
        if (!HasPendingChange)
        {
            return;
        }

        _appliedOption = SelectedOption;
        OnPropertyChanged(nameof(HasPendingChange));
        _onConfirmedChanged();
    }

    public void RevertSelection()
    {
        if (!HasPendingChange)
        {
            return;
        }

        SelectedOption = _appliedOption;
    }

    public void SetStatus(string message)
    {
        StatusMessage = message;
    }

    public void SetCurrentTargetLabel(string label)
    {
        CurrentTargetLabel = label;
    }

    public string GetAppliedValue() => _appliedOption.Value;

    public void RefreshLocalizedText(
        string title,
        string description,
        IEnumerable<OpenMethodTargetOption> targetOptions)
    {
        Title = title;
        Description = description;

        var selectedValue = SelectedOption.Value;
        var appliedValue = _appliedOption.Value;

        TargetOptions.Clear();
        foreach (var option in targetOptions)
        {
            TargetOptions.Add(option);
        }

        _appliedOption = ResolveOption(appliedValue);
        SelectedOption = ResolveOption(selectedValue);
        OnPropertyChanged(nameof(HasPendingChange));
    }

    private OpenMethodTargetOption ResolveOption(string target)
    {
        return TargetOptions.FirstOrDefault(option => option.Value == target) ?? TargetOptions[0];
    }
}

public sealed class OpenMethodTargetOption
{
    public required string Value { get; init; }

    public required string Label { get; init; }
}

public static class OpenMethodTargets
{
    public const string System = "System";
    public const string Office = "Office";
    public const string Wps = "WPS";
    public const string Mixed = "Mixed";
    public const string Unknown = "Unknown";
}
