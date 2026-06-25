using System;
using System.Collections.Generic;
using ECommons.Logging;
using PromeRotation.Data;
using PromeRotation.Managers;
using PromeRotation.Rotation;

namespace JinyuViper;

internal static class ViperOpenerStateMachine
{
    private struct OpenerStep
    {
        public ActionType Type;
        public Func<uint>? GetDynamicId;
        public uint FixedId;
        public ActionTargetType Target;
        public bool RequiresVerification;
        public Func<bool>? Condition;
        public string Label;
    }

    private static List<OpenerStep> _steps = null!;
    private static int _index = 0;
    private static bool _active = false;
    private static DateTime _lastStepTime = DateTime.MinValue;
    private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(30);

    public static bool IsActive => _active;
    public static int  StepIndex => _index;
    public static int  StepCount => _steps?.Count ?? 0;

    public static void BuildStandardSteps()
    {
        _steps = new List<OpenerStep>
        {
            new() { Type = ActionType.Gcd, FixedId = ViperSkill.强碎灵蛇, Target = ActionTargetType.Target, RequiresVerification = true, Label = "强碎灵蛇" },

            new()
            {
                Type = ActionType.OffGcd, FixedId = ViperSkill.蛇灵气, Target = ActionTargetType.Self,
                Condition = () => PromeSettings.Instance.GetQt("蛇灵气") && VPRApi.等级() >= 86,
                Label = "蛇灵气"
            },

            new() { Type = ActionType.Gcd, Target = ActionTargetType.Target, RequiresVerification = true, GetDynamicId = () => VPRApi.Next蛇剑连(), Label = "盘蛇#1" },
            new() { Type = ActionType.Gcd, Target = ActionTargetType.Target, RequiresVerification = true, GetDynamicId = () => VPRApi.Next蛇剑连(), Label = "盘蛇#2" },

            new()
            {
                Type = ActionType.Gcd, FixedId = ViperSkill.祖灵降临, Target = ActionTargetType.Target, RequiresVerification = true,
                Condition = () => PromeSettings.Instance.GetQt("附体"),
                Label = "祖灵降临"
            },

            new()
            {
                Type = ActionType.Gcd, FixedId = ViperSkill.祖灵之牙一式, Target = ActionTargetType.Target, RequiresVerification = true,
                Condition = () => PromeSettings.Instance.GetQt("附体"),
                Label = "祖灵一式"
            },

            new()
            {
                Type = ActionType.Gcd, FixedId = ViperSkill.祖灵之牙二式, Target = ActionTargetType.Target, RequiresVerification = true,
                Condition = () => PromeSettings.Instance.GetQt("附体"),
                Label = "祖灵二式"
            },

            new()
            {
                Type = ActionType.Gcd, FixedId = ViperSkill.祖灵之牙三式, Target = ActionTargetType.Target, RequiresVerification = true,
                Condition = () => PromeSettings.Instance.GetQt("附体"),
                Label = "祖灵三式"
            },

            new()
            {
                Type = ActionType.Gcd, FixedId = ViperSkill.祖灵之牙四式, Target = ActionTargetType.Target, RequiresVerification = true,
                Condition = () => PromeSettings.Instance.GetQt("附体"),
                Label = "祖灵四式"
            },

            new()
            {
                Type = ActionType.Gcd, FixedId = ViperSkill.祖灵之牙五式, Target = ActionTargetType.Target, RequiresVerification = true,
                Condition = () => PromeSettings.Instance.GetQt("附体"),
                Label = "祖灵五式"
            },

            // 第11步：强碎灵蛇#2（GCD）- 收尾
            new() { Type = ActionType.Gcd, FixedId = ViperSkill.强碎灵蛇, Target = ActionTargetType.Target, RequiresVerification = true, Label = "强碎灵蛇#2" },
        };
    }

    public static void BuildQuickFangSteps()
    {
        _steps = new List<OpenerStep>
        {
            // 第1步：基础GCD开场
            new() { Type = ActionType.Gcd, FixedId = ViperSkill.钢牙, Target = ActionTargetType.Target, RequiresVerification = true, Label = "钢牙" },

            // 第2步：蛇灵气穿插
            new()
            {
                Type = ActionType.OffGcd, FixedId = ViperSkill.蛇灵气, Target = ActionTargetType.Self,
                Condition = () => PromeSettings.Instance.GetQt("蛇灵气") && VPRApi.等级() >= 86,
                Label = "蛇灵气"
            },

            // 第3步：基础GCD#2（拿急速buff）
            new() { Type = ActionType.Gcd, Target = ActionTargetType.Target, RequiresVerification = true, GetDynamicId = () => VPRApi.Next基础GCD(), Label = "基础GCD#2" },

            // 第4步：爆发药
            new()
            {
                Type = ActionType.OffGcd, Target = ActionTargetType.Self,
                Condition = () => PromeSettings.Instance.GetQt("爆发药") && VPRApi.获取爆发药ID() != 0,
                GetDynamicId = () => VPRApi.获取爆发药ID(),
                Label = "爆发药"
            },

            // 第5步：强碎灵蛇（有急速buff后开蛇剑连）
            new() { Type = ActionType.Gcd, FixedId = ViperSkill.强碎灵蛇, Target = ActionTargetType.Target, RequiresVerification = true, Label = "强碎灵蛇" },

            // 第6-7步：蛇剑连（先侧身后背身，补猛袭buff）
            new() { Type = ActionType.Gcd, Target = ActionTargetType.Target, RequiresVerification = true, GetDynamicId = () => VPRApi.Next蛇剑连(), Label = "盘蛇#1(侧身)" },
            new() { Type = ActionType.Gcd, Target = ActionTargetType.Target, RequiresVerification = true, GetDynamicId = () => VPRApi.Next蛇剑连(), Label = "盘蛇#2(背身)" },

            // 第8步：祖灵降临
            new()
            {
                Type = ActionType.Gcd, FixedId = ViperSkill.祖灵降临, Target = ActionTargetType.Target, RequiresVerification = true,
                Condition = () => PromeSettings.Instance.GetQt("附体"),
                Label = "祖灵降临"
            },

            // 第9-12步：附体连×4
            new()
            {
                Type = ActionType.Gcd, FixedId = ViperSkill.祖灵之牙一式, Target = ActionTargetType.Target, RequiresVerification = true,
                Condition = () => PromeSettings.Instance.GetQt("附体"),
                Label = "祖灵一式"
            },
            new()
            {
                Type = ActionType.Gcd, FixedId = ViperSkill.祖灵之牙二式, Target = ActionTargetType.Target, RequiresVerification = true,
                Condition = () => PromeSettings.Instance.GetQt("附体"),
                Label = "祖灵二式"
            },
            new()
            {
                Type = ActionType.Gcd, FixedId = ViperSkill.祖灵之牙三式, Target = ActionTargetType.Target, RequiresVerification = true,
                Condition = () => PromeSettings.Instance.GetQt("附体"),
                Label = "祖灵三式"
            },
            new()
            {
                Type = ActionType.Gcd, FixedId = ViperSkill.祖灵之牙四式, Target = ActionTargetType.Target, RequiresVerification = true,
                Condition = () => PromeSettings.Instance.GetQt("附体"),
                Label = "祖灵四式"
            },
        };
    }

    public static void Reset()
    {
        _index = 0;
        _active = false;
    }

    public static void Start()
    {
        if (!PromeSettings.Instance.GetQt("起手爆发")) return;
        if (!JinyuViperRotation.UseOpener) return;
        Reset();

        switch (JinyuViperRotation.SelectedOpenerIndex)
        {
            case 0: BuildStandardSteps(); break;   // 标准起手
            case 2: BuildQuickFangSteps(); break;   // 齿剑快速起手
            default: return;                       // 空白起手(1)等不激活
        }

        _active = true;
        PluginLog.Information($"[VPR] 起手状态机启动, 共{_steps.Count}步");
    }

    public static PAction? Consume(ActionType type)
    {
        while (_index < _steps.Count)
        {
            var step = _steps[_index];
            if (step.Type != type)
                return null;

            if (step.Condition != null && !step.Condition())
            {
                PluginLog.Information($"[VPR] 起手步骤[{_index}] {step.Label} 条件不满足, 跳过");
                _index++;
                continue;
            }

            uint actionId = step.GetDynamicId != null ? step.GetDynamicId() : step.FixedId;
            if (actionId == 0)
            {
                PluginLog.Information($"[VPR] 起手步骤[{_index}] {step.Label} ID=0, 跳过");
                _index++;
                continue;
            }

            if (type == ActionType.Gcd && VPRApi.GCD剩余ms() > 0f)
            {
                if (_lastStepTime != DateTime.MinValue && DateTime.Now - _lastStepTime > StepTimeout)
                {
                    PluginLog.Warning($"[VPR] 起手GCD等待超时({StepTimeout.TotalSeconds:F0}s), 强制重置");
                    _active = false;
                }
                return null;
            }

            var action = new PAction(actionId, step.Type, step.Target)
            {
                RequiresVerification = step.RequiresVerification
            };
            PluginLog.Information($"[VPR] 起手步骤[{_index}] {step.Label} → {actionId}");
            _index++;
            _lastStepTime = DateTime.Now;
            return action;
        }

        if (_active)
        {
            _active = false;
            PluginLog.Information("[VPR] 起手完成, 切换到主循环");
        }
        return null;
    }

    public static bool HasPendingOffGcd =>
        _active && _index < _steps.Count && _steps[_index].Type == ActionType.OffGcd;
}
