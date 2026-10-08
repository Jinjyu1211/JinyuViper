using System.Text.Json;

namespace Jinyu;

public sealed class JinyuSettings
{
    private static readonly string PathName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncherCN", "pluginConfigs", "PromeRotation", "ACR", "Jinyu", "Jinyu.settings.json");
    public static JinyuSettings Current { get; } = Load();
    public int Opener { get; set; }
    public bool UseTimelineOpener { get; set; }
    public bool Animate { get; set; } = true;
    public float BackgroundOpacity { get; set; } = 0.72f;
    public float TrashHoldHpPercent { get; set; } = 30;
    public uint PotionId { get; set; }
    public bool PreferHq { get; set; } = true;
    public Dictionary<string, int> Hotkeys { get; set; } = new();
    public static string? SaveError { get; private set; }
    public bool Align120 { get; set; } = true;
    public bool Potion { get; set; }
    public float TrueNorthWindow { get; set; } = 4;
    public float BloodbathHp { get; set; } = 60;
    public float SecondWindHp { get; set; } = 40;
    public float BurstOffset { get; set; } = 7;
    public float MaxBuffWait { get; set; } = 5;
    private static JinyuSettings Load()
    {
        try { return File.Exists(PathName) ? JsonSerializer.Deserialize<JinyuSettings>(File.ReadAllText(PathName)) ?? new() : new(); }
        catch (Exception e) { ApiHelper.ApiHelper.警告日志($"Jinyu settings: {e.Message}"); return new(); }
    }
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
            var temporary = PathName + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, PathName, true);
            SaveError = null;
        }
        catch (Exception e) { SaveError = e.Message; ApiHelper.ApiHelper.警告日志($"Jinyu settings save: {e.Message}"); }
    }
}
