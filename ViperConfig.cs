using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using ECommons.DalamudServices;
using ECommons.Logging;

namespace JinyuViper;

public static class ViperConfig
{
    private static readonly string ConfigPath = Path.Combine(
        Svc.PluginInterface.ConfigDirectory.FullName, "JinyuViper", "settings.json");
    private static readonly JsonSerializerOptions JsonOptions = new()
    { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private static ViperSettings _config = new();

    public static bool HotkeyVisible => _config.HotkeyVisible;
    public static int  HotkeyColumns => _config.HotkeyColumns;
    public static ViperSettings Current => _config;

    /// <summary>本ACR DLL所在目录的绝对路径</summary>
    public static readonly string BaseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

    public static void Load()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            if (File.Exists(ConfigPath))
                _config = JsonSerializer.Deserialize<ViperSettings>(File.ReadAllText(ConfigPath), JsonOptions) ?? new();
            JinyuViperRotation.SelectedOpenerIndex = Math.Clamp(_config.SelectedOpenerIndex, 0, 2);
            JinyuViperRotation.IsDailyMode = _config.DailyMode;
            PluginLog.Information($"[VPR] 配置已加载: visible={_config.HotkeyVisible}, columns={_config.HotkeyColumns}, opener={_config.SelectedOpenerIndex}, daily={_config.DailyMode}");
        }
        catch (Exception ex) { PluginLog.Error($"[VPR] 配置加载失败: {ex.Message}"); _config = new(); }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            _config.SelectedOpenerIndex = JinyuViperRotation.SelectedOpenerIndex;
            _config.DailyMode = JinyuViperRotation.IsDailyMode;
            string tmp = ConfigPath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_config, JsonOptions));
            File.Move(tmp, ConfigPath, overwrite: true);
            PluginLog.Information("[VPR] 设置已保存");
        }
        catch (Exception ex) { PluginLog.Error($"[VPR] 保存设置失败: {ex.Message}"); }
    }

    public static void SetHotkeyColumns(int cols)    => _config.HotkeyColumns = cols;
}
