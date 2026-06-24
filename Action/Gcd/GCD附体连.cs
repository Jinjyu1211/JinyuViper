using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class GCD附体连 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!VPRApi.有目标()) return new CheckResult(false, "无目标");
        if (!VPRApi.目标在近战范围()) return new CheckResult(false, "超出近战范围");
        if (!VPRApi.处于附体状态()) return new CheckResult(false, "非附体状态");
        if (VPRApi.祖灵力档数() == 0) return new CheckResult(false, "祖灵力档数=0");
        
        uint nextSkill = VPRApi.Next附体连();
        if (!VPRApi.技能CD好(nextSkill))
            return new CheckResult(false, $"附体连CD中 {VPRApi.技能CD毫秒(nextSkill):F0}ms");
        
        return new CheckResult(true, "附体连击续行");
    }
    public PAction GetAction()
    {
        uint skill = VPRApi.Next附体连();
        return new PAction(skill, ActionType.Gcd, ActionTargetType.Target);
    }
}
