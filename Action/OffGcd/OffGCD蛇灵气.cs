using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class OffGCD蛇灵气 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!PromeSettings.Instance.GetQt("蛇灵气"))
            return new CheckResult(false, "Qt(蛇灵气)关闭");
        if (!VPRApi.有目标()) return new CheckResult(false, "无目标");
        if (VPRApi.等级() < 86) return new CheckResult(false, "等级<86");

        if (VPRApi.处于强碎灵窗口())
            return new CheckResult(false, "强碎灵窗口中");

        // 日随模式：与副本模式相同逻辑，仅移除120团辅检测（对齐蛇气），CD好就打
        float cd = VPRApi.蛇灵气CD毫秒();
        if (PromeSettings.Instance.GetQt("日随模式"))
        {
            if (cd > 50f)
                return new CheckResult(false, $"蛇灵气CD中 {cd:F0}ms");
            return new CheckResult(true, $"日随模式 蛇灵气 飞蛇层数={VPRApi.飞蛇层数()}");
        }

        if (VPRApi.飞蛇层数() >= 3 && VPRApi.等级() >= 92)
            return new CheckResult(false, $"飞蛇层数={VPRApi.飞蛇层数()} 满层不浪费");

        if (cd > 50f)
            return new CheckResult(false, $"蛇灵气CD中 {cd:F0}ms");

        if (PromeSettings.Instance.GetQt("对齐蛇气") && !VPRApi.团辅激活())
            return new CheckResult(false, "对齐蛇气：等待团辅");

        return new CheckResult(true, $"蛇灵气 飞蛇层数={VPRApi.飞蛇层数()}");
    }

    public PAction GetAction()
    {
        uint skill = ActionHelper.GetAdjustedActionId(ViperSkill.蛇灵气);
        return new PAction(skill, ActionType.OffGcd, ActionTargetType.Self);
    }
}
