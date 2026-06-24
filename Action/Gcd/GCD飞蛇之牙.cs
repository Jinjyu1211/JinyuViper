using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class GCD飞蛇之牙 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!VPRApi.有目标()) return new CheckResult(false, "无目标");
        if (VPRApi.处于蛇剑连击()) return new CheckResult(false, "蛇剑连击中");
        if (VPRApi.处于附体状态()) return new CheckResult(false, "附体状态中");
        if (VPRApi.续剑阶段() != 0) return new CheckResult(false, "续剑阶段≠0");
        if (!VPRApi.目标在远程范围()) return new CheckResult(false, "超出远程范围");
        if (VPRApi.等级() < 30) return new CheckResult(false, "等级<30");
        
        if (!VPRApi.技能CD好(ViperSkill.飞蛇之牙))
            return new CheckResult(false, $"飞蛇之牙CD中 {VPRApi.技能CD毫秒(ViperSkill.飞蛇之牙):F0}ms");
        
        return new CheckResult(true, "飞蛇之牙 远程填充");
    }
    public PAction GetAction()
    {
        return new PAction(ViperSkill.飞蛇之牙, ActionType.Gcd, ActionTargetType.Target);
    }
}
