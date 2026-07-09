using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PipelineControl.UI.Services.Settings;
using PipelineControl.UI.ViewModels.Pages.Settings.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PipelineControl.UI.ViewModels.Pages.Settings;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService settingsService;
    private Func<Task>? pendingConfirmedAction;
    private SystemSettings currentSettings = SystemSettings.CreateDefaults();
    private SystemSettings defaultSettings = SystemSettings.CreateDefaults();

    public SettingsViewModel(ISettingsService settingsService)
    {
        this.settingsService = settingsService;
        Fields = new ObservableCollection<SettingField>();
        InitialLoadTask = LoadAsync();
    }

    public Task InitialLoadTask { get; }

    public ObservableCollection<SettingField> Fields { get; }

    public IReadOnlyList<SettingField> AllFields => Fields;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDirtyChanges))]
    [NotifyPropertyChangedFor(nameof(DirtyBannerText))]
    private int dirtyCount;

    [ObservableProperty]
    private string statusText = "参数已加载";

    [ObservableProperty]
    private string restartNoticeText = string.Empty;

    [ObservableProperty]
    private string confirmationTitle = string.Empty;

    [ObservableProperty]
    private string confirmationText = string.Empty;

    [ObservableProperty]
    private bool isConfirmationOpen;

    public bool HasDirtyChanges => DirtyCount > 0;

    public bool HasValidationErrors => Fields.Any(field => field.HasError);

    public bool CanSave => DirtyCount > 0 && !HasValidationErrors;

    public bool CanDiscard => DirtyCount > 0;

    public string DirtyBannerText => $"{DirtyCount} 项未保存";

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        OpenConfirmation(
            "保存参数",
            "保存后重启生效。确认保存？",
            SaveConfirmedAsync);
        await Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanDiscard))]
    private void Discard()
    {
        foreach (var field in Fields)
        {
            field.Discard();
        }

        RestartNoticeText = string.Empty;
        StatusText = "已撤销";
        RefreshState();
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        foreach (var field in Fields)
        {
            field.ResetToDefault();
        }

        RefreshState();
        OpenConfirmation(
            "恢复默认",
            "恢复并写入本机配置。确认继续？",
            SaveConfirmedAsync);
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ConfirmPendingAsync()
    {
        var action = pendingConfirmedAction;
        IsConfirmationOpen = false;
        pendingConfirmedAction = null;

        if (action is not null)
        {
            await action().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private void CancelPending()
    {
        IsConfirmationOpen = false;
        pendingConfirmedAction = null;
        StatusText = "已取消";
    }

    private async Task LoadAsync()
    {
        defaultSettings = await settingsService.LoadDefaultsAsync().ConfigureAwait(true);
        currentSettings = await settingsService.LoadAsync().ConfigureAwait(true);
        BuildFields(currentSettings, defaultSettings);
        RefreshState();
    }

    private async Task SaveConfirmedAsync()
    {
        if (HasValidationErrors)
        {
            StatusText = "参数格式错误";
            return;
        }

        currentSettings.CardComm.PcIp = GetField("CardComm.PcIp").Value;
        currentSettings.CardComm.MainCardIp = GetField("CardComm.MainCardIp").Value;
        currentSettings.CardComm.ExtensionCardCount = GetField("CardComm.ExtensionCardCount").ToInt();
        currentSettings.CardComm.ScanCycleMs = GetField("CardComm.ScanCycleMs").ToInt();
        currentSettings.CardComm.HeartbeatMs = GetField("CardComm.HeartbeatMs").ToInt();
        currentSettings.Advanced.SimulationMode = GetField("Advanced.SimulationMode").ToBool();
        currentSettings.ServoComm.GatewayIp = GetField("ServoComm.GatewayIp").Value;
        currentSettings.ServoComm.GatewayPort = GetField("ServoComm.GatewayPort").ToInt();
        currentSettings.ServoComm.Axis1Station = GetField("ServoComm.Axis1Station").ToInt();
        currentSettings.ServoComm.Axis2Station = GetField("ServoComm.Axis2Station").ToInt();
        currentSettings.ServoComm.Axis3Station = GetField("ServoComm.Axis3Station").ToInt();
        currentSettings.ServoComm.Axis4Station = GetField("ServoComm.Axis4Station").ToInt();
        currentSettings.ServoComm.ScanCycleMs = GetField("ServoComm.ScanCycleMs").ToInt();
        currentSettings.ServoComm.HeartbeatCycleMs = GetField("ServoComm.HeartbeatCycleMs").ToInt();
        currentSettings.ServoComm.DefaultSpeedRpm = GetField("ServoComm.DefaultSpeedRpm").ToInt();
        currentSettings.ServoComm.MaxSpeedRpm = GetField("ServoComm.MaxSpeedRpm").ToInt();

        await settingsService.SaveLocalAsync(currentSettings).ConfigureAwait(true);

        foreach (var field in Fields)
        {
            field.AcceptChanges();
        }

        RestartNoticeText = "需重启生效";
        StatusText = "已保存";
        RefreshState();
    }

    private void BuildFields(SystemSettings settings, SystemSettings defaults)
    {
        foreach (var field in Fields)
        {
            field.PropertyChanged -= OnFieldChanged;
        }

        Fields.Clear();
        Add(new SettingField("CardComm.PcIp", "CardComm", "本机 IP", "PC 网口地址", SettingFieldKind.IpAddress, settings.CardComm.PcIp, defaults.CardComm.PcIp, isCritical: true, isRestartRequired: true));
        Add(new SettingField("CardComm.MainCardIp", "CardComm", "主卡 IP", "控制卡地址", SettingFieldKind.IpAddress, settings.CardComm.MainCardIp, defaults.CardComm.MainCardIp, isCritical: true, isRestartRequired: true));
        Add(new SettingField("CardComm.ExtensionCardCount", "CardComm", "扩展卡", "扩展模块数量", SettingFieldKind.Numeric, settings.CardComm.ExtensionCardCount.ToString(), defaults.CardComm.ExtensionCardCount.ToString(), "块", 0, 8, isCritical: true, isRestartRequired: true));
        Add(new SettingField("CardComm.ScanCycleMs", "CardComm", "扫描周期", "IO 轮询间隔", SettingFieldKind.Numeric, settings.CardComm.ScanCycleMs.ToString(), defaults.CardComm.ScanCycleMs.ToString(), "ms", 0, 1000, isCritical: true, isRestartRequired: true));
        Add(new SettingField("CardComm.HeartbeatMs", "CardComm", "心跳", "通讯检测间隔", SettingFieldKind.Numeric, settings.CardComm.HeartbeatMs.ToString(), defaults.CardComm.HeartbeatMs.ToString(), "ms", 0, 5000, isCritical: true, isRestartRequired: true));
        Add(new SettingField("Advanced.SimulationMode", "Advanced", "仿真", "无硬件调试", SettingFieldKind.Toggle, settings.Advanced.SimulationMode.ToString(), defaults.Advanced.SimulationMode.ToString(), isCritical: true, isRestartRequired: true));
        AddServoCommFields(settings, defaults);
    }

    private void AddServoCommFields(SystemSettings settings, SystemSettings defaults)
    {
        Add(new SettingField("ServoComm.GatewayIp", "ServoComm", "伺服网关 IP", "RS485 串口服务器地址", SettingFieldKind.IpAddress, settings.ServoComm.GatewayIp, defaults.ServoComm.GatewayIp, isCritical: true, isRestartRequired: true));
        Add(new SettingField("ServoComm.GatewayPort", "ServoComm", "网关端口", "Modbus TCP 端口", SettingFieldKind.Numeric, settings.ServoComm.GatewayPort.ToString(), defaults.ServoComm.GatewayPort.ToString(), string.Empty, 1, 65535, isCritical: true, isRestartRequired: true));
        Add(new SettingField("ServoComm.Axis1Station", "ServoComm", "1#站号", "轴1 Modbus 站号", SettingFieldKind.Numeric, settings.ServoComm.Axis1Station.ToString(), defaults.ServoComm.Axis1Station.ToString(), string.Empty, 1, 247, isCritical: true, isRestartRequired: true));
        Add(new SettingField("ServoComm.Axis2Station", "ServoComm", "2#站号", "轴2 Modbus 站号", SettingFieldKind.Numeric, settings.ServoComm.Axis2Station.ToString(), defaults.ServoComm.Axis2Station.ToString(), string.Empty, 1, 247, isCritical: true, isRestartRequired: true));
        Add(new SettingField("ServoComm.Axis3Station", "ServoComm", "3#站号", "轴3 Modbus 站号", SettingFieldKind.Numeric, settings.ServoComm.Axis3Station.ToString(), defaults.ServoComm.Axis3Station.ToString(), string.Empty, 1, 247, isCritical: true, isRestartRequired: true));
        Add(new SettingField("ServoComm.Axis4Station", "ServoComm", "4#站号", "轴4 Modbus 站号", SettingFieldKind.Numeric, settings.ServoComm.Axis4Station.ToString(), defaults.ServoComm.Axis4Station.ToString(), string.Empty, 1, 247, isCritical: true, isRestartRequired: true));
        Add(new SettingField("ServoComm.ScanCycleMs", "ServoComm", "扫描周期", "伺服轮询间隔", SettingFieldKind.Numeric, settings.ServoComm.ScanCycleMs.ToString(), defaults.ServoComm.ScanCycleMs.ToString(), "ms", 50, 2000, isCritical: true, isRestartRequired: true));
        Add(new SettingField("ServoComm.HeartbeatCycleMs", "ServoComm", "心跳周期", "使能续写间隔（≤5s）", SettingFieldKind.Numeric, settings.ServoComm.HeartbeatCycleMs.ToString(), defaults.ServoComm.HeartbeatCycleMs.ToString(), "ms", 500, 5000, isCritical: true, isRestartRequired: true));
        Add(new SettingField("ServoComm.DefaultSpeedRpm", "ServoComm", "默认转速", "使能后初始转速", SettingFieldKind.Numeric, settings.ServoComm.DefaultSpeedRpm.ToString(), defaults.ServoComm.DefaultSpeedRpm.ToString(), "rpm", -9000, 9000));
        Add(new SettingField("ServoComm.MaxSpeedRpm", "ServoComm", "最大转速", "转速限幅保护", SettingFieldKind.Numeric, settings.ServoComm.MaxSpeedRpm.ToString(), defaults.ServoComm.MaxSpeedRpm.ToString(), "rpm", 1, 9000, isCritical: true));
    }

    private void Add(SettingField field)
    {
        Fields.Add(field);
        field.PropertyChanged += OnFieldChanged;
    }

    private void OnFieldChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingField.Value)
            or nameof(SettingField.IsDirty)
            or nameof(SettingField.HasError)
            or nameof(SettingField.ErrorText))
        {
            RefreshState();
        }
    }

    private void RefreshState()
    {
        DirtyCount = Fields.Count(field => field.IsDirty);
        OnPropertyChanged(nameof(HasValidationErrors));
        SaveCommand.NotifyCanExecuteChanged();
        DiscardCommand.NotifyCanExecuteChanged();
    }

    private void OpenConfirmation(string title, string text, Func<Task> confirmedAction)
    {
        ConfirmationTitle = title;
        ConfirmationText = text;
        pendingConfirmedAction = confirmedAction;
        IsConfirmationOpen = true;
    }

    private SettingField GetField(string key)
    {
        return Fields.Single(field => string.Equals(field.Key, key, StringComparison.Ordinal));
    }
}
