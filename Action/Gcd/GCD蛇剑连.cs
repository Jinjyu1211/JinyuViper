using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class GCD蛇剑连 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!VPRApi.有目标()) return new CheckResult(false, "无目标");
        if (!VPRApi.目标在近战范围()) return new CheckResult(false, "超出近战范围");
        if (!VPRApi.处于蛇剑连击()) return new CheckResult(false, "非蛇剑连击中");
        
        uint nextSkill = VPRApi.Next蛇剑连();
        if (!VPRApi.技能CD好(nextSkill))
            return new CheckResult(false, $"蛇剑连CD中 {VPRApi.技能CD毫秒(nextSkill):F0}ms");
        
        return new CheckResult(true, "蛇剑连击续行");
    }
    public PAction GetAction()
    {
        uint skill = VPRApi.Next蛇剑连();
        return new PAction(skill, ActionType.Gcd, ActionTargetType.Target);
    }
}
