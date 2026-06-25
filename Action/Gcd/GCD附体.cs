using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class GCD附体 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!PromeSettings.Instance.GetQt("附体"))
            return new CheckResult(false, "Qt(附体)关闭");
        if (!VPRApi.有目标()) return new CheckResult(false, "无目标");
        if (!VPRApi.目标在近战范围()) return new CheckResult(false, "超出近战范围");
        if (VPRApi.处于蛇剑连击()) return new CheckResult(false, "蛇剑连击中");
        if (VPRApi.处于附体状态()) return new CheckResult(false, "已附体");
        if (VPRApi.等级() < 90) return new CheckResult(false, "等级<90");

        if (!VPRApi.技能CD好(ViperSkill.祖灵降临))
            return new CheckResult(false, $"祖灵降临CD中 {VPRApi.技能CD毫秒(ViperSkill.祖灵降临):F0}ms");

        if (VPRApi.祖灵层数() < 1)
            return new CheckResult(false, $"祖灵层数={VPRApi.祖灵层数()}<1 灵力={VPRApi.灵力值()}");

        // 通用规则：本体没有buff时优先补疾速
        if (!VPRApi.有Buff(ViperBuff.急速) && !VPRApi.有Buff(ViperBuff.猛袭))
            return new CheckResult(false, "本体无buff 优先补疾速");

        // 倾泻爆发：跳过所有保留检查
        if (PromeSettings.Instance.GetQt("倾泻爆发"))
            return new CheckResult(true, $"倾泻爆发 开附体 灵力={VPRApi.灵力值()}");

        if (JinyuViperRotation.IsDailyMode)
            return new CheckResult(true, $"日随模式 开附体 灵力={VPRApi.灵力值()}");

        float comboTime = VPRApi.连击剩余ms();
        if (comboTime > 0 && comboTime < 9000f)
            return new CheckResult(false, $"连击时间{comboTime:F0}ms<9000ms");

        // 120对齐：蛇剑连结束后动态延迟，用基础GCD填充等待团辅
        // 强碎灵蛇buff=30s, 双附体=26.4s(lv≥96)/20.4s(lv<96), 余量有限需控制填充量
        if (PromeSettings.Instance.GetQt("120对齐") && !VPRApi.处于蛇剑连击())
        {
            bool 刚结束蛇剑连 = VPRApi.最近用过(ViperSkill.疾速盘蛇, 8f) || VPRApi.最近用过(ViperSkill.猛袭盘蛇, 8f)
                || VPRApi.最近用过(ViperSkill.穿裂灵蛇, 8f) || VPRApi.最近用过(ViperSkill.崩裂灵蛇, 8f);
            if (刚结束蛇剑连)
            {
                float 蛇灵气剩余 = VPRApi.蛇灵气CD毫秒() / 1000f;
                float 双附体时间 = VPRApi.附体时间() * 2f / 1000f;
                float buff剩余 = Math.Min(VPRApi.Buff剩余ms(ViperBuff.急速), VPRApi.Buff剩余ms(ViperBuff.猛袭)) / 1000f;

                // 团辅未到且蛇灵气未就绪 → 继续填充基础GCD
                if (!VPRApi.IsInViper120() && 蛇灵气剩余 > 1f)
                {
                    // 检查buff是否够覆盖双附体（已过时间 + 双附体 < buff总量）
                    float 已过时间 = 8f - 蛇灵气剩余; // 从打强碎灵蛇到现在约过了多久
                    if (buff剩余 + 30f > 双附体时间 + 已过时间 + 3f)
                        return new CheckResult(false, $"等待团辅 蛇灵气{蛇灵气剩余:F1}s>1s 填充GCD");
                }
                // 团辅到了或蛇灵气就绪 → 可以开附体
            }
        }

        // 120团辅期直接开附体（跳过灵力值和蛇灵气CD检查）
        if (VPRApi.IsInViper120())
            return new CheckResult(true, $"120团辅期 开附体 灵力={VPRApi.灵力值()}");

        float 蛇灵气CD = VPRApi.蛇灵气CD毫秒() / 1000f;
        // 蛇灵气CD>40s时可以开附体
        if (蛇灵气CD > 40f)
            return new CheckResult(true, $"开附体 灵力={VPRApi.灵力值()} 蛇灵气CD={蛇灵气CD:F1}s>40s");

        // 蛇灵气CD≤40s且120对齐开启时，灵力值>90才开（避免溢出）
        if (蛇灵气CD <= 40f && PromeSettings.Instance.GetQt("120对齐"))
        {
            if (VPRApi.灵力值() <= 90)
                return new CheckResult(false, $"蛇灵气CD={蛇灵气CD:F1}s≤40s 灵力{VPRApi.灵力值()}≤90 避免溢出");
        }

        return new CheckResult(true, $"开附体 灵力={VPRApi.灵力值()} 祖灵层数={VPRApi.祖灵层数()} 蛇灵气CD={蛇灵气CD:F1}s");
    }

    public PAction GetAction()
    {
        return new PAction(ViperSkill.祖灵降临, ActionType.Gcd, ActionTargetType.Target);
    }
}
