using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace JinyuViper;

internal class OffGCD自动真北 : IDecisionResolver
{
    private static readonly uint[] 侧身GCD =
    {
        ViperSkill.猛袭盘蛇,
        ViperSkill.穿裂灵蛇,
        ViperSkill.侧击獠牙,
        ViperSkill.侧裂獠牙,
        ViperSkill.猎牙,
    };

    private static readonly uint[] 背身GCD =
    {
        ViperSkill.疾速盘蛇,
        ViperSkill.崩裂灵蛇,
        ViperSkill.背击獠牙,
        ViperSkill.背裂獠牙,
        ViperSkill.滑牙,
    };

    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!JinyuViperRotation.IsDailyMode)
            return new CheckResult(false, "非日随模式");
        if (!PromeSettings.Instance.GetQt("真北"))
            return new CheckResult(false, "真北QT关闭");
        if (VPRApi.等级() < 50)
            return new CheckResult(false, "等级<50");
        if (!VPRApi.有目标())
            return new CheckResult(false, "无目标");
        if (!VPRApi.目标在近战范围())
            return new CheckResult(false, "超出近战范围");
        if (VPRApi.有Buff(ViperBuff.真北))
            return new CheckResult(false, "已有真北");
        if (VPRApi.技能CD毫秒(ViperSkill.真北) > 0f)
            return new CheckResult(false, "真北CD中");
        if (VPRApi.最近用过(ViperSkill.真北, 2.5f))
            return new CheckResult(false, "2.5秒内已使用");

        if (VPRApi.GCD剩余ms() < 300f)
            return new CheckResult(false, "GCD窗口不足");

        float gcdTotal = ActionHelper.GetGcdTotal();
        int threshold = ViperConfig.Current.真北GCD百分比;
        if (threshold > 0 && gcdTotal > 0f && ActionHelper.GetGcdElapsed() / gcdTotal * 100f <= threshold)
            return new CheckResult(false, $"GCD进度不够({threshold}%)");

        if (!TargetHelper.HasPositionalRequirement(Core.Target))
            return new CheckResult(false, "目标无身位需求");

        bool needFlank = false, needRear = false;

        uint nextSnake = VPRApi.Next蛇剑连();
        foreach (var id in 侧身GCD) { if (nextSnake == id) { needFlank = true; break; } }
        foreach (var id in 背身GCD) { if (nextSnake == id) { needRear = true; break; } }

        if (!needFlank && !needRear)
        {
            uint nextBase = VPRApi.Next基础GCD();
            foreach (var id in 侧身GCD) { if (nextBase == id) { needFlank = true; break; } }
            foreach (var id in 背身GCD) { if (nextBase == id) { needRear = true; break; } }
        }

        if (!needFlank && !needRear)
            return new CheckResult(false, $"下个GCD(蛇={nextSnake} 基={VPRApi.Next基础GCD()}) 无身位要求");

        var pos = TargetHelper.GetTargetPositional();
        if (needFlank && pos != Positional.Flank)
            return new CheckResult(true, $"需要侧身但位置={pos}，自动真北");
        if (needRear && pos != Positional.Rear)
            return new CheckResult(true, $"需要背后但位置={pos}，自动真北");

        return new CheckResult(false, "身位正确，不需要真北");
    }

    public PAction GetAction()
    {
        return new PAction(ViperSkill.真北, ActionType.OffGcd, ActionTargetType.Self);
    }
}
