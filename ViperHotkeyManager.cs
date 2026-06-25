using PromeRotation;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.UI.HotKey;

namespace JinyuViper;

internal static class ViperHotkeyManager
{
    private static HotkeyPanel? _panel;

    public static int Count => _panel?.Count ?? 0;
    public static bool IsVisible => _panel?.IsOpen ?? false;

    /// <summary>确保面板已注册并可见（每帧由DrawQTs调用）</summary>
    public static void EnsureVisible()
    {
        if (_panel == null) Setup();
        if (_panel != null && !_panel.IsOpen) _panel.IsOpen = true;
    }

    public static void Setup()
    {
        if (_panel != null)
        {
            try { HotkeyManager.Instance.RemoveHotkeyPanel(_panel); } catch { }
            _panel = null;
        }

        _panel = new HotkeyPanel(ViperConfig.HotkeyColumns, 45f, 5f, "蝰蛇快捷键");
        AddAllHotkeys(_panel);

        try { HotkeyManager.Instance.AddHotkeyPanel(_panel); } catch { }
    }

    public static void Rebuild() => Setup();

    public static void SetVisible(bool visible)
    {
        if (_panel != null) _panel.IsOpen = visible;
    }

    private static void AddAllHotkeys(HotkeyPanel panel)
    {
        panel.AddHotkey("亲疏自行", new PAction(7548u, ActionType.OffGcd, ActionTargetType.Self));
        panel.AddHotkey("内丹",     new PAction(7541u, ActionType.OffGcd, ActionTargetType.Self));
        panel.AddHotkey("浴血",     new PAction(7542u, ActionType.OffGcd, ActionTargetType.Self));
        panel.AddHotkey("牵制",     new PAction(7549u, ActionType.OffGcd, ActionTargetType.Target));
        panel.AddHotkey("真北",     new PAction(7546u, ActionType.OffGcd, ActionTargetType.Self));
        panel.AddHotkey("蛇行",     new PAction(34646u, ActionType.OffGcd, ActionTargetType.Target));
        panel.AddHotkey("冲刺",     new PAction(3u,     ActionType.OffGcd, ActionTargetType.Self));

        panel.AddHotkey("附体", new ReawakenLogic(), 34626u);

        // 极限技：用技能ID 3（冲刺）作为图标
        panel.AddHotkey("极限技", new LimitBreakLogic(), 3u);

        // 清空队列：执行逻辑 + 自定义红底白X图标
        panel.AddHotkey("清空队列", new ExecuteLogic(ClearQueueAction), 0u, Path.Combine(ViperConfig.BaseDir, "Resources", "Clear.png"));
    }

    private static void ClearQueueAction()
    {
        try { HotkeyQueueManager.ClearAll(); } catch { }
        try { ActionQueueManager.ClearAllQueues(); } catch { }
        ViperOpenerStateMachine.Reset();
    }

    /// <summary>附体自定义逻辑：检查目标/灵力值/附体状态</summary>
    private class ReawakenLogic : IHotkeyLogic
    {
        public bool IsActive() => false;
        public void OnClick()
        {
            if (!VPRApi.有目标()) return;
            if (VPRApi.处于附体状态()) return;
            if (VPRApi.灵力值() < 50 && !VPRApi.有Buff(ViperBuff.祖灵预备)) return;
            HotkeyQueueManager.TryEnqueue(new PAction(34626u, ActionType.Gcd, ActionTargetType.Target));
        }
    }

    /// <summary>极限技自定义逻辑：动态获取当前极限技ID</summary>
    private class LimitBreakLogic : IHotkeyLogic
    {
        public bool IsActive() => false;
        public void OnClick()
        {
            uint id = LimitBreakHelper.GetLimitBreakActionId();
            if (id == 0) return;
            HotkeyQueueManager.TryEnqueue(new PAction(id, ActionType.LimitBreak, ActionTargetType.Target));
        }
    }
}
