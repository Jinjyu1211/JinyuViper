using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class OffGCD飞蛇续剑 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!VPRApi.有目标()) return new CheckResult(false, "无目标");
        if (VPRApi.续剑阶段() != 9) return new CheckResult(false, $"续剑阶段={VPRApi.续剑阶段()}≠9");
        return new CheckResult(true, "飞蛇续剑");
    }
    public PAction GetAction()
    {
        uint skill = VPRApi.Next飞蛇续剑();
        return new PAction(skill, ActionType.OffGcd, ActionTargetType.Target);
    }
}
