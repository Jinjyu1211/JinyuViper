using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class OffGCD附体续剑 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!VPRApi.有目标()) return new CheckResult(false, "无目标");
        int s = VPRApi.续剑阶段();
        if (s < 3 || s > 6) return new CheckResult(false, $"续剑阶段={s} 不在3~6");
        if (!VPRApi.技能CD好(ViperSkill.蛇尾术))
            return new CheckResult(false, "蛇尾术CD中");
        return new CheckResult(true, $"附体续剑 续剑阶段={s}");
    }
    public PAction GetAction()
    {
        return new PAction(ViperSkill.蛇尾术, ActionType.OffGcd, ActionTargetType.Target);
    }
}
