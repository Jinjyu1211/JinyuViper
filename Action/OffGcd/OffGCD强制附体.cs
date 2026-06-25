using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class OffGCD强制附体 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!PromeSettings.Instance.GetQt("附体"))
            return new CheckResult(false, "Qt(附体)关闭");
        if (!VPRApi.有目标()) return new CheckResult(false, "无目标");
        if (VPRApi.处于附体状态()) return new CheckResult(false, "已附体");
        if (VPRApi.等级() < 90) return new CheckResult(false, "等级<90");
        if (!VPRApi.有Buff(ViperBuff.祖灵预备))
            return new CheckResult(false, "无祖灵预备");
        if (VPRApi.Buff剩余ms(ViperBuff.祖灵预备) > 5000f)
            return new CheckResult(false, $"祖灵预备{VPRApi.Buff剩余ms(ViperBuff.祖灵预备):F0}ms>5000ms");
        if (VPRApi.处于蛇剑连击()) return new CheckResult(false, "蛇剑连击中");

        return new CheckResult(true, $"强制附体 祖灵预备剩余{VPRApi.Buff剩余ms(ViperBuff.祖灵预备):F0}ms");
    }
    public PAction GetAction()
    {
        return new PAction(ViperSkill.祖灵降临, ActionType.Gcd, ActionTargetType.Target);
    }
}
