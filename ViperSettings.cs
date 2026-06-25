namespace JinyuViper;

public class ViperSettings
{
    public bool HotkeyVisible { get; set; } = true;
    public int  HotkeyColumns { get; set; } = 5;
    public int  SelectedOpenerIndex { get; set; } = 0;
    public bool DailyMode { get; set; } = false;

    public float 内丹血量阈值 { get; set; } = 40f;
    public float 浴血血量阈值 { get; set; } = 60f;
    public int   真北GCD百分比 { get; set; } = 30;
}
