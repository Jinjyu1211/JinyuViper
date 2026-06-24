using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class GCD飞蛇之尾 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手")) return new CheckResult(false, "停手");
        if (!VPRApi.有目标()) return new CheckResult(false, "无目标");
        if (!VPRApi.目标在远程范围()) return new CheckResult(false, "超出远程范围");
        if (VPRApi.飞蛇层数() < 1) return new CheckResult(false, "飞蛇层数<1");
        if (VPRApi.等级() < 82) return new CheckResult(false, "等级<82");
        
        if (!VPRApi.技能CD好(ViperSkill.飞蛇之尾))
            return new CheckResult(false, $"飞蛇之尾CD中 {VPRApi.技能CD毫秒(ViperSkill.飞蛇之尾):F0}ms");

        // 通用规则：本体没有buff时优先补疾速（远程兜底除外）
        if (!VPRApi.有Buff(ViperBuff.急速) && !VPRApi.有Buff(ViperBuff.猛袭)
            && VPRApi.目标在近战范围())
            return new CheckResult(false, "本体无buff 优先补疾速");

        // 倾泻爆发：不在附体/蛇剑连中就打
        if (PromeSettings.Instance.GetQt("倾泻爆发"))
        {
            if (!VPRApi.处于附体状态() && !VPRApi.处于蛇剑连击())
                return new CheckResult(true, $"倾泻爆发 飞蛇之尾 飞蛇层数={VPRApi.飞蛇层数()}");
        }

        // 优先飞蛇：有飞蛇之魂就优先打飞蛇之尾
        if (PromeSettings.Instance.GetQt("优先飞蛇"))
        {
            if (!VPRApi.处于附体状态() && !VPRApi.处于蛇剑连击())
                return new CheckResult(true, $"优先飞蛇 飞蛇之尾 飞蛇层数={VPRApi.飞蛇层数()}");
        }

        // 日随模式：与副本模式相同逻辑，仅移除120团辅检测
        if (PromeSettings.Instance.GetQt("日随模式"))
        {
            if (!VPRApi.处于附体状态() && !VPRApi.处于蛇剑连击())
                return new CheckResult(true, $"日随模式 飞蛇之尾 飞蛇层数={VPRApi.飞蛇层数()}");
        }

        if (!VPRApi.目标在近战范围())
            return new CheckResult(true, $"飞蛇之尾 远程兜底 飞蛇层数={VPRApi.飞蛇层数()}");

        if (VPRApi.等级() < 92) return new CheckResult(false, "等级<92");
        if (!VPRApi.双BUFF齐全() && VPRApi.飞蛇层数() != 3)
            return new CheckResult(false, "双BUFF不齐且飞蛇层数≠3");
        if (VPRApi.处于附体状态()) return new CheckResult(false, "附体状态中");
        if (VPRApi.处于蛇剑连击()) return new CheckResult(false, "蛇剑连击中");

        // 120团辅期+Level≥92直接打飞蛇之尾（跳过飞蛇之魂检查）
        if (VPRApi.IsInViper120() && VPRApi.等级() >= 92)
            return new CheckResult(true, $"120团辅期 飞蛇之尾 飞蛇层数={VPRApi.飞蛇层数()}");

        // 团辅预备：蛇灵气即将就绪时，飞蛇之魂≥2提前消耗，避免团辅期强碎灵蛇+蛇灵气产生2层导致溢出
        float 蛇灵气CD预备 = VPRApi.蛇灵气CD毫秒();
        float gcdMs = VPRApi.GCD总时长ms();
        if (VPRApi.飞蛇层数() >= 2 && VPRApi.等级() >= 86
            && 蛇灵气CD预备 < 2f * gcdMs + VPRApi.GCD剩余ms()
            && !VPRApi.处于附体状态() && !VPRApi.处于蛇剑连击())
            return new CheckResult(true, $"团辅预备 飞蛇≥2 蛇灵气即将就绪 飞蛇层数={VPRApi.飞蛇层数()}");

        // 团辅补飞蛇：120团辅期内蛇灵气CD≤40s时跳过飞蛇之魂检查
        if (PromeSettings.Instance.GetQt("团辅补飞蛇") && VPRApi.IsInViper120())
        {
            float 蛇灵气CDs = VPRApi.蛇灵气CD毫秒() / 1000f;
            if (蛇灵气CDs <= 40f)
                return new CheckResult(true, $"团辅补飞蛇 蛇灵气CD={蛇灵气CDs:F1}s≤40s");
        }

        float comboTime = VPRApi.连击剩余ms();
        if (comboTime > 0 && comboTime - 600f <= VPRApi.GCD剩余ms() + VPRApi.飞蛇复唱时间())
            return new CheckResult(false, "连击时间不足");

        if (VPRApi.续剑阶段() == 9) return new CheckResult(false, "续剑=9");

        float 短BUFF = Math.Min(VPRApi.Buff剩余ms(ViperBuff.急速), VPRApi.Buff剩余ms(ViperBuff.猛袭));
        float 蛇灵气CD = VPRApi.蛇灵气CD毫秒();

        if (VPRApi.飞蛇层数() == 3)
        {
            if (VPRApi.有蛇剑(1f) && PromeSettings.Instance.GetQt("蛇剑"))
            {
                if (VPRApi.普攻续短buff时间() + VPRApi.飞蛇复唱时间() < 短BUFF)
                    return new CheckResult(true, "3层 有蛇剑 普攻续短buff+飞蛇复唱<短BUFF");
                return new CheckResult(false, "3层 有蛇剑 BUFF充足");
            }
            if (VPRApi.有蛇剑(2f) && PromeSettings.Instance.GetQt("蛇剑")
                && VPRApi.Buff剩余ms(ViperBuff.祖灵预备) <= 0)
                return new CheckResult(true, "3层 有蛇剑(2GCD) 无祖灵预备");
            if (蛇灵气CD < 7f * gcdMs && PromeSettings.Instance.GetQt("蛇灵气"))
                return new CheckResult(true, "3层 蛇灵气CD在7GCD内");
        }

        if (VPRApi.飞蛇层数() >= 2 && !PromeSettings.Instance.GetQt("120对齐")
            && 蛇灵气CD < 2f * gcdMs)
            return new CheckResult(true, "2层 非120对齐 蛇灵气CD在2GCD内");

        if (PromeSettings.Instance.GetQt("120对齐") && PromeSettings.Instance.GetQt("蛇灵气"))
        {
            if (蛇灵气CD < 3f * gcdMs && 蛇灵气CD >= VPRApi.GCD剩余ms() - 600f
                && VPRApi.灵力值() > 90 && VPRApi.Next基础连招阶段() == 3)
                return new CheckResult(true, "120对齐 蛇灵气3GCD内 灵力>90 基础阶段3");

            if (蛇灵气CD < 6f * gcdMs && VPRApi.灵力值() >= 50)
            {
                float 普攻复唱 = VPRApi.普攻复唱时间();
                float mod = (蛇灵气CD - VPRApi.GCD剩余ms()) % 普攻复唱;
                if (mod >= 普攻复唱 - 600f || mod <= 240f)
                    return new CheckResult(true, "120对齐 蛇灵气6GCD内 灵力>=50 CD对齐");
            }
        }

        return new CheckResult(false, "默认不打");
    }

    public PAction GetAction()
    {
        return new PAction(ViperSkill.飞蛇之尾, ActionType.Gcd, ActionTargetType.Target);
    }
}
