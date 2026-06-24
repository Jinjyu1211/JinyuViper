using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class GCDAoe基础 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!PromeSettings.Instance.GetQt("AOE"))
            return new CheckResult(false, "Qt(AOE)关闭");
        if (!VPRApi.有目标()) return new CheckResult(false, "无目标");
        if (!VPRApi.目标在近战范围()) return new CheckResult(false, "超出近战范围");
        if (VPRApi.附近敌人数() < 2) return new CheckResult(false, "敌人数<2");
        if (VPRApi.处于蛇剑连击()) return new CheckResult(false, "蛇剑连击中");
        if (VPRApi.处于附体状态()) return new CheckResult(false, "附体状态中");
        if (VPRApi.续剑阶段() != 0) return new CheckResult(false, "续剑阶段≠0");

        uint nextSkill = VPRApi.Next基础GCD();
        if (VPRApi.附近敌人数() >= 3 && VPRApi.等级() >= 40)
            nextSkill = ViperSkill.疾裂穿牙;
        if (!VPRApi.技能CD好(nextSkill))
            return new CheckResult(false, $"AOE基础CD中 {VPRApi.技能CD毫秒(nextSkill):F0}ms");

        return new CheckResult(true, $"AOE基础 敌人数={VPRApi.附近敌人数()}");
    }
    public PAction GetAction()
    {
        uint skill = VPRApi.Next基础GCD();
        if (VPRApi.附近敌人数() >= 3 && VPRApi.等级() >= 40)
            skill = ViperSkill.疾裂穿牙;
        return new PAction(skill, ActionType.Gcd, ActionTargetType.Target);
    }
}
