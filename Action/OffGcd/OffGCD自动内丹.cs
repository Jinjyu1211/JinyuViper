using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace JinyuViper;

internal class OffGCD自动内丹 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!PromeSettings.Instance.GetQt("日随模式"))
            return new CheckResult(false, "非日随模式");
        if (!PromeSettings.Instance.GetQt("内丹"))
            return new CheckResult(false, "内丹QT关闭");
        if (VPRApi.技能CD毫秒(ViperSkill.内丹) > 0f)
            return new CheckResult(false, "内丹CD中");
        if (VPRApi.GCD剩余ms() < 300f)
            return new CheckResult(false, "GCD窗口不足");
        float hp = VPRApi.自身血量百分比();
        if (hp > ViperConfig.Current.内丹血量阈值)
            return new CheckResult(false, $"血量={hp:F1}% > {ViperConfig.Current.内丹血量阈值}%");
        return new CheckResult(true, $"血量={hp:F1}% < {ViperConfig.Current.内丹血量阈值}%，自动内丹");
    }

    public PAction GetAction()
    {
        return new PAction(ViperSkill.内丹, ActionType.OffGcd, ActionTargetType.Self);
    }
}
