using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class OffGCD蛇剑连续剑 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!VPRApi.有目标()) return new CheckResult(false, "无目标");
        int s = VPRApi.续剑阶段();
        if (s != 7 && s != 8) return new CheckResult(false, $"续剑阶段={s}≠7/8");
        if (!VPRApi.技能CD好(ViperSkill.蛇剑连续剑CD1) && !VPRApi.技能CD好(ViperSkill.蛇剑连续剑CD2))
            return new CheckResult(false, "蛇剑连续剑CD中");
        return new CheckResult(true, $"蛇剑连续剑 续剑阶段={s}");
    }
    public PAction GetAction()
    {
        uint skill = VPRApi.Next蛇剑连续剑();
        return new PAction(skill, ActionType.OffGcd, ActionTargetType.Target);
    }
}
