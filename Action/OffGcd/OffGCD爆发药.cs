using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace JinyuViper;

internal class OffGCD爆发药 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!PromeSettings.Instance.GetQt("爆发药"))
            return new CheckResult(false, "爆发药QT关闭");

        uint potionId = VPRApi.获取爆发药ID();
        if (potionId == 0)
            return new CheckResult(false, "无可用爆发药");

        if (VPRApi.物品CD毫秒(potionId) > 0f)
            return new CheckResult(false, "爆发药CD中");

        if (!VPRApi.团辅激活())
            return new CheckResult(false, "无团辅");

        if (VPRApi.正在喝爆发药())
            return new CheckResult(false, "已在爆发药状态");

        return new CheckResult(true, $"团辅激活，使用爆发药({potionId})");
    }

    public PAction GetAction()
    {
        return new PAction(VPRApi.获取爆发药ID(), ActionType.Item, ActionTargetType.Self);
    }
}
