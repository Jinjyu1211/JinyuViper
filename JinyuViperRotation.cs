using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;
using PromeRotation.TargetSelector;
using PromeRotation.Timeline;

namespace JinyuViper;

[RotationMetadata(41u, "蝰蛇剑士", "JinyuViper", "1.0.1")]
public class JinyuViperRotation : IRotation
{
    public string RotationName => "蝰蛇剑士";
    public uint JobId => 41u;

    private readonly IRotationEventHandler _eventHandler = new ViperRotationEventHandler();
    public IRotationEventHandler GetEventHandler() => _eventHandler;

    private readonly List<IDecisionResolver> _gcdResolvers  = new();
    private readonly List<IDecisionResolver> _offGcdResolvers = new();

    public static IReadOnlyDictionary<string, bool> QtList { get; } =
        new Dictionary<string, bool>
        {
            ["停手"]     = false,
            ["AOE"]     = true,
            ["智能AOE"]  = false,
            ["蛇灵气"]   = true,
            ["附体"]     = true,
            ["蛇剑"]     = true,
            ["真北"]     = true,
            ["浴血"]     = true,
            ["内丹"]     = true,
            ["120对齐"]  = false,
            ["对齐蛇气"]  = false,
            ["起手爆发"]  = true,
            ["爆发药"]   = true,
            ["倾泻爆发"]  = false,
            ["优先飞蛇"]  = false,
            ["团辅补飞蛇"] = true,
            ["身位指示"]  = true,
            ["日随模式"]  = false,
        };

    public static HashSet<string> HiddenQts { get; } = new() { "优化GCD偏移" };

    public static IReadOnlyDictionary<string, Type> Openers { get; } =
        new Dictionary<string, Type>
        {
            ["标准起手"] = typeof(ViperOpenerStandard),
            ["空白起手"] = typeof(ViperOpenerBlank),
            ["齿剑快速起手"] = typeof(ViperOpenerQuickFang),
        };

    public static int SelectedOpenerIndex { get; set; } = 0;

    public JinyuViperRotation()
    {
        _gcdResolvers.Add(new GCD飞蛇之尾());
        _gcdResolvers.Add(new GCD附体());
        _gcdResolvers.Add(new GCD附体连());
        _gcdResolvers.Add(new GCD蛇剑());
        _gcdResolvers.Add(new GCD蛇剑连());
        _gcdResolvers.Add(new GCDAoe基础());
        _gcdResolvers.Add(new GCD基础());
        _gcdResolvers.Add(new GCD飞蛇之牙());

        _offGcdResolvers.Add(new OffGCD自动真北());
        _offGcdResolvers.Add(new OffGCD自动内丹());
        _offGcdResolvers.Add(new OffGCD自动浴血());
        _offGcdResolvers.Add(new OffGCD蛇剑连续剑());
        _offGcdResolvers.Add(new OffGCD附体续剑());
        _offGcdResolvers.Add(new OffGCD蛇灵气());
        _offGcdResolvers.Add(new OffGCD连击3续剑());
        _offGcdResolvers.Add(new OffGCD飞蛇续剑());
        _offGcdResolvers.Add(new OffGCD爆发药());
        _offGcdResolvers.Add(new OffGCD强制附体());

        foreach (var kvp in QtList)
            PromeSettings.Instance.AddQt(kvp.Key, kvp.Value);

        ViperConfig.Load();
        try { ViperHotkeyManager.Setup(); } catch { }
        ViperOpenerStateMachine.BuildStandardSteps();
    }

    public IOpener? GetOpener() => null;

    public static bool UseOpener => !PromeSettings.Instance.GetQt("日随模式");

    public PAction? NextGcd()
    {
        if (UseOpener && ViperOpenerStateMachine.IsActive)
        {
            var action = ViperOpenerStateMachine.Consume(ActionType.Gcd);
            if (action != null) return action;
            if (ViperOpenerStateMachine.StepIndex < ViperOpenerStateMachine.StepCount) return null;
        }
        foreach (var resolver in _gcdResolvers)
        {
            if (resolver.Check().Success) return resolver.GetAction();
        }
        return null;
    }

    public PAction? NextOffGcd()
    {
        if (UseOpener && ViperOpenerStateMachine.IsActive)
        {
            var action = ViperOpenerStateMachine.Consume(ActionType.OffGcd);
            if (action != null) return action;
        }
        foreach (var resolver in _offGcdResolvers)
        {
            if (resolver.Check().Success) return resolver.GetAction();
        }
        return null;
    }

    // ProcessAlwaysDecision不受HasActiveCommand限制，确保起手OffGCD不被阻塞
    public PAction? NextAlways()
    {
        if (!UseOpener || !ViperOpenerStateMachine.IsActive) return null;
        if (ViperOpenerStateMachine.HasPendingOffGcd)
            return ViperOpenerStateMachine.Consume(ActionType.OffGcd);
        return null;
    }

    public void UpdateDebugStatus()
    {
        RotationManager.GcdSolverStatus.Clear();
        RotationManager.OffGcdSolverStatus.Clear();

        if (ViperOpenerStateMachine.IsActive)
        {
            RotationManager.GcdSolverStatus.Add(new SolverStatus
            {
                Name = "起手状态机",
                Success = true,
                Message = $"步骤 {ViperOpenerStateMachine.StepIndex}/{ViperOpenerStateMachine.StepCount}"
            });
        }

        foreach (var resolver in _gcdResolvers)
        {
            var result = resolver.Check();
            RotationManager.GcdSolverStatus.Add(new SolverStatus
            {
                Name    = resolver.GetType().Name,
                Success = result.Success,
                Message = result.Message
            });
        }

        foreach (var resolver in _offGcdResolvers)
        {
            var result = resolver.Check();
            RotationManager.OffGcdSolverStatus.Add(new SolverStatus
            {
                Name    = resolver.GetType().Name,
                Success = result.Success,
                Message = result.Message
            });
        }
    }

    public void DrawQTs()
    {
        var s = PromeSettings.Instance;

        // 日随模式 + 起手爆发 同时开启 → 强制关闭起手爆发 + 重置状态机
        if (s.GetQt("起手爆发") && s.GetQt("日随模式"))
        {
            s.SetQt("起手爆发", false);
            if (ViperOpenerStateMachine.IsActive) ViperOpenerStateMachine.Reset();
        }

        // 倾泻爆发 ↔ 优先飞蛇 互斥
        if (s.GetQt("倾泻爆发") && s.GetQt("优先飞蛇"))
            s.SetQt("优先飞蛇", false);

        // 智能AOE：开启时设置目标选择模式为3米内最高HP（敌人最密集），关闭时恢复None
        if (s.GetQt("智能AOE") && s.GetQt("AOE"))
        {
            if (s.TargetSelectorMode != SelectorModeType.HighestHpIn3R)
                s.TargetSelectorMode = SelectorModeType.HighestHpIn3R;
        }
        else
        {
            if (s.TargetSelectorMode == SelectorModeType.HighestHpIn3R)
                s.TargetSelectorMode = SelectorModeType.None;
        }

        // DrawQTs每帧调用，确保Hotkey面板在当前Rotation激活时可见
        ViperHotkeyManager.EnsureVisible();
    }

    public void DrawSettings() => ViperSettingsUI.Draw();
}
