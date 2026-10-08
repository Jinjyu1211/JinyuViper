using A = ApiHelper.ApiHelper;
using Dalamud.Bindings.ImGui;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace Jinyu;

[RotationMetadata(41, "Jinyu", "Jinyu", "1.0.0.0")]
public sealed class JinyuRotation : IRotation, IRotationMeta, IRotationLifecycle, IDisposable
{
    private readonly IRotationEventHandler events = new JinyuEventHandler();
    public static IReadOnlyDictionary<string, bool> QtList { get; } = new Dictionary<string, bool>
    {
        ["AOE"] = true, ["爆发"] = true, ["飞蛇"] = true,
        ["真北"] = true, ["自动内丹"] = true, ["自动浴血"] = false, ["停手"] = false
    };
    public static IReadOnlyDictionary<string, Type> Openers { get; } = new Dictionary<string, Type>
    {
        ["Jinyu_通用蛇剑起手"] = typeof(SelectedSerpentOpener),
        ["Jinyu_齿剑快速起手"] = typeof(SelectedFangOpener)
    };
    public JinyuRotation() { foreach (var q in QtList) A.添加QT(q.Key, q.Value); }
    public PAction? NextAlways() => null;
    public PAction? NextGcd() => ViperLogic.Action(ViperLogic.Gcd(), ActionType.Gcd);
    public PAction? NextOffGcd() => ViperLogic.PotionAction() ?? ViperLogic.Action(ViperLogic.Ogcd(), ActionType.OffGcd);
    public IOpener? GetOpener()
    {
        var selected = JinyuSettings.Current.UseTimelineOpener ? A.时间轴起手名称 ?? A.元数据起手名称 : null;
        if (!string.IsNullOrWhiteSpace(selected) && Openers.TryGetValue(selected, out var type)) return (IOpener?)Activator.CreateInstance(type);
        return JinyuSettings.Current.Opener == 1 ? new FastFangOpener() : new SerpentOpener();
    }
    public IRotationEventHandler GetEventHandler() => events;
    public void OnEnterAcr() => PelaUi.Attach();
    public void OnExitAcr() => PelaUi.Detach();
    public void Dispose() => PelaUi.Detach();
    public void UpdateDebugStatus()
    {
        A.清除GCD调试状态(); A.清除oGCD调试状态();
        if (A.玩家 == null || A.玩家的职业 != 41) return;
        var g = ViperLogic.Gcd(); var o = ViperLogic.Ogcd();
        A.添加GCD调试状态("VPR", g != 0, $"技能 {g} / 灵力 {A.VPR.灵力} / 飞蛇 {A.VPR.飞蛇} / 祖灵 {A.VPR.祖灵}");
        A.添加oGCD调试状态("VPR", o != 0, $"技能 {o} / 团辅 {BurstAlignment.RaidRemaining:F1}s / 双附体窗口满足 {BurstAlignment.CanDouble}");
    }
    public void DrawSettings()
    {
        if (ImGui.Button("打开佩拉控制台")) PelaUi.Open();
    }
    internal static void DrawConfiguration()
    {
        var s = JinyuSettings.Current;
        var changed = false;
        var opener = s.Opener;
        if (ImGui.Combo("起手", ref opener, (IReadOnlyList<string>)new[] { "通用蛇剑起手", "齿剑快速起手" })) { s.Opener = opener; changed = true; }
        var timeline = s.UseTimelineOpener; if (ImGui.Checkbox("使用时间轴起手", ref timeline)) { s.UseTimelineOpener = timeline; changed = true; }
        var align = s.Align120; if (ImGui.Checkbox("120对齐", ref align)) { s.Align120 = align; changed = true; }
        var potion = s.Potion; if (ImGui.Checkbox("自动爆发药", ref potion)) { s.Potion = potion; changed = true; }
        var north = s.TrueNorthWindow; if (ImGui.SliderFloat("真北提前窗口（秒）", ref north, 0.7f, 10)) { s.TrueNorthWindow = north; changed = true; }
        var bath = s.BloodbathHp; if (ImGui.SliderFloat("浴血血量阈值", ref bath, 1, 100)) { s.BloodbathHp = bath; changed = true; }
        var wind = s.SecondWindHp; if (ImGui.SliderFloat("内丹血量阈值", ref wind, 1, 100)) { s.SecondWindHp = wind; changed = true; }
        var offset = s.BurstOffset; if (ImGui.SliderFloat("首轮爆发偏移（秒）", ref offset, 0, 20)) { s.BurstOffset = offset; changed = true; }
        var wait = s.MaxBuffWait; if (ImGui.SliderFloat("等待团辅上限（秒）", ref wait, 0, 10)) { s.MaxBuffWait = wait; changed = true; }
        var trashHp=s.TrashHoldHpPercent;
        if(ImGui.SliderFloat("小怪保留血量阈值（%）",ref trashHp,0,100,"%.0f")) {s.TrashHoldHpPercent=trashHp;changed=true;}
        ImGui.TextWrapped("低于血量阈值或预计15秒内击杀时保留爆发；0仅关闭血量判断。");
        if (changed) s.Save();
        if (A.玩家 != null)
            ImGui.TextUnformatted($"团辅剩余 {BurstAlignment.RaidRemaining:F1}s / 双附体需求 {BurstAlignment.DoubleSpan(A.GCD总时长):F1}s");
        ImGui.TextWrapped("起手选择在下一次起手生效；已执行的起手不会中途重排。外部自定义时间轴起手仍可能由框架优先执行。");
        if (JinyuSettings.SaveError != null) ImGui.TextWrapped($"保存失败：{JinyuSettings.SaveError}");
    }
    public void DrawQTs()
    {
        if (ImGui.Button("佩拉 · QT / Hotkey / 设置")) PelaUi.Open();
    }
}

public sealed class JinyuEventHandler : IRotationEventHandler
{
    public void OnUpdate() => ViperLogic.UpdateTargetRecovery();
    public void OnOutOfBattleUpdate() { }
    public void OnBattleStarted() => BurstAlignment.Start();
    public void OnBattleUpdate() => ViperLogic.UpdateTargetRecovery();
    public void OnNoTarget() => ViperLogic.UpdateTargetRecovery();
    public void OnBattleEnded() => BurstAlignment.Reset();
    public void OnTerritoryChanged(ushort id) => BurstAlignment.Reset();
}
