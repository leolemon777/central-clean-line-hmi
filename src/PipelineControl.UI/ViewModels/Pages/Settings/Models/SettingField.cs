using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;

namespace PipelineControl.UI.ViewModels.Pages.Settings.Models;

public sealed partial class SettingField : ObservableObject
{
    private bool updatingOptions;

    public SettingField(
        string key,
        string categoryKey,
        string label,
        string description,
        SettingFieldKind kind,
        string value,
        string defaultValue,
        string unit = "",
        int? min = null,
        int? max = null,
        bool isCritical = false,
        bool isRestartRequired = false,
        IEnumerable<(string Label, string Value)>? options = null)
    {
        Key = key;
        CategoryKey = categoryKey;
        Label = label;
        Description = description;
        Kind = kind;
        Unit = unit;
        DefaultValue = defaultValue;
        OriginalValue = value;
        Min = min;
        Max = max;
        IsCritical = isCritical;
        IsRestartRequired = isRestartRequired;
        Options = new ObservableCollection<SettingOption>();

        if (options is not null)
        {
            foreach (var option in options)
            {
                Options.Add(new SettingOption(option.Label, option.Value, OnOptionSelectionChanged));
            }
        }

        this.value = value;
        Validate();
        SyncOptions();
    }

    public string Key { get; }

    public string CategoryKey { get; }

    public string Label { get; }

    public string Description { get; }

    public SettingFieldKind Kind { get; }

    public string Unit { get; }

    public string DefaultValue { get; }

    public string OriginalValue { get; private set; }

    public int? Min { get; }

    public int? Max { get; }

    public bool IsCritical { get; }

    public bool IsRestartRequired { get; }

    public ObservableCollection<SettingOption> Options { get; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

    public bool IsDirty => !string.Equals(Value, OriginalValue, StringComparison.Ordinal);

    public bool BooleanValue
    {
        get => bool.TryParse(Value, out var parsed) && parsed;
        set => Value = value.ToString();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    [NotifyPropertyChangedFor(nameof(BooleanValue))]
    private string value = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorText = string.Empty;

    partial void OnValueChanged(string? oldValue, string newValue)
    {
        Validate();
        SyncOptions();
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(BooleanValue));
    }

    public void AcceptChanges()
    {
        OriginalValue = Value;
        OnPropertyChanged(nameof(IsDirty));
    }

    public void Discard()
    {
        Value = OriginalValue;
    }

    public void ResetToDefault()
    {
        Value = DefaultValue;
    }

    public int ToInt()
    {
        return int.TryParse(Value, out var parsed) ? parsed : 0;
    }

    public bool ToBool()
    {
        return bool.TryParse(Value, out var parsed) && parsed;
    }

    private void Validate()
    {
        ErrorText = Kind switch
        {
            SettingFieldKind.IpAddress => ValidateIp(Value),
            SettingFieldKind.Numeric => ValidateNumber(Value),
            SettingFieldKind.Text when string.IsNullOrWhiteSpace(Value) => "不能为空。",
            _ => string.Empty
        };
    }

    private string ValidateIp(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return string.Empty;
        }

        if (!IPAddress.TryParse(candidate, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return "请输入正确的 IPv4 地址。";
        }

        var parts = candidate.Split('.');
        if (parts.Length != 4 || parts[0] is "0" or "127" or "255")
        {
            return "IP 网段不适合作为板卡通讯地址。";
        }

        return string.Empty;
    }

    private string ValidateNumber(string candidate)
    {
        if (!int.TryParse(candidate, out var number))
        {
            return "请输入整数。";
        }

        if (Min is not null && number < Min.Value)
        {
            return $"不能小于 {Min.Value}。";
        }

        if (Max is not null && number > Max.Value)
        {
            return $"不能大于 {Max.Value}。";
        }

        return string.Empty;
    }

    private void SyncOptions()
    {
        if (updatingOptions)
        {
            return;
        }

        updatingOptions = true;
        foreach (var option in Options)
        {
            option.IsSelected = string.Equals(option.Value, Value, StringComparison.Ordinal);
        }
        updatingOptions = false;
    }

    private void OnOptionSelectionChanged(SettingOption option, bool isSelected)
    {
        if (updatingOptions || !isSelected)
        {
            return;
        }

        Value = option.Value;
    }
}
