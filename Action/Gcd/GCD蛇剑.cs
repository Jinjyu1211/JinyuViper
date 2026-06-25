using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class GCD蛇剑 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt("停手"))
            return new CheckResult(false, "停手");
        if (!PromeSettings.Instance.GetQt("蛇剑"))
            return new CheckResult(false, "Qt(蛇剑)关闭");
        if (!VPRApi.有目标()) return new CheckResult(false, "无目标");
        if (!VPRApi.目标在近战范围()) return new CheckResult(false, "超出近战范围");
        if (VPRApi.处于蛇剑连击()) return new CheckResult(false, "蛇剑连击中");
        if (VPRApi.处于附体状态()) return new CheckResult(false, "附体状态中");
        if (VPRApi.等级() < 65) return new CheckResult(false, "等级<65");

        if (!VPRApi.技能CD好(ViperSkill.强碎灵蛇))
            return new CheckResult(false, $"强碎灵蛇CD中 {VPRApi.技能CD毫秒(ViperSkill.强碎灵蛇):F0}ms");

        float 充能 = VPRApi.强碎灵蛇充能();
        if (充能 < 1f)
            return new CheckResult(false, $"强碎灵蛇充能={充能:F1}<1");

        float 下一层CD = VPRApi.技能CD毫秒(ViperSkill.强碎灵蛇);
        float gcdMs = VPRApi.GCD总时长ms();
        float gcdRemain = VPRApi.GCD剩余ms();

        // ===== 禁止充能溢出：充能≥2时强制打出 =====
        // 充能=2意味着CD已在空转浪费，必须立即消耗
        if (充能 >= 2f)
            return new CheckResult(true, $"强制打出 充能={充能:F1} CD空转中");

        // ===== 提前打出：下一层即将填满时 =====
        // 在充能到2之前打掉当前层，避免CD空转
        if (充能 > 1f && 下一层CD > 0f && 下一层CD <= gcdMs * 1.5f + gcdRemain)
            return new CheckResult(true, $"提前打出 充能={充能:F1} 下一层{下一层CD:F0}ms");

        // 倾泻爆发：跳过所有保留检查
        if (PromeSettings.Instance.GetQt("倾泻爆发"))
            return new CheckResult(true, $"倾泻爆发 开蛇剑连 充能={充能:F1}");

        // AOE环境检测（用于后续条件放宽）
        bool isAoe = VPRApi.附近敌人数() >= 3 && VPRApi.等级() >= 70 && PromeSettings.Instance.GetQt("AOE");

        // 通用规则：本体没有buff时优先补疾速（AOE版也补buff，不打断）
        if (!VPRApi.有Buff(ViperBuff.急速) && !VPRApi.有Buff(ViperBuff.猛袭) && !isAoe)
            return new CheckResult(false, "本体无buff 优先补疾速");

        // 非团辅期且锐牙buff快到期时跳过（AOE环境不跳过，优先补buff）
        if (!VPRApi.IsInViper120() && VPRApi.Is锐牙buff快到期() && !isAoe)
            return new CheckResult(false, "非团辅期 锐牙buff快到期");

        // 连击时间检查（AOE环境放宽到5秒）
        float comboTime = VPRApi.连击剩余ms();
        if (comboTime > 0 && comboTime < (isAoe ? 5000f : 15000f))
            return new CheckResult(false, $"连击时间{comboTime:F0}ms<{(isAoe ? 5000 : 15000)}ms");

        float 蛇灵气CD = VPRApi.蛇灵气CD毫秒() / 1000f;

        // ===== 蛇灵气CD<12s保障：确保蛇灵气好前打掉一层 =====
        // 蛇灵气就绪后给飞蛇之魂，强碎灵蛇也几乎同步好
        // 必须在蛇灵气好之前消耗一层，否则资源同时到来消化不了
        if (VPRApi.等级() >= 86 && 蛇灵气CD > 0f && 蛇灵气CD < 12f)
        {
            int 当前飞蛇 = VPRApi.飞蛇层数();
            int 蛇灵气给魂 = 1;
            int 总飞蛇 = 当前飞蛇 + 蛇灵气给魂;

            // 飞蛇之魂直接超上限 → 必须先消化飞蛇
            if (总飞蛇 > 3)
                return new CheckResult(false, $"溢出保护 飞蛇{当前飞蛇}+蛇灵{蛇灵气给魂}={总飞蛇}>3 先消化飞蛇");

            // 检查蛇剑连期间能否消化飞蛇之魂
            float 蛇剑连时间 = VPRApi.蛇剑复唱时间() + VPRApi.普攻复唱时间() * 2f;
            float 到蛇灵气时间 = Math.Max(0f, 蛇灵气CD * 1000f);
            float 剩余窗口 = 到蛇灵气时间 - 蛇剑连时间;
            int 可消化 = (int)(剩余窗口 / VPRApi.飞蛇复唱时间());

            if (总飞蛇 <= 3 + 可消化)
                return new CheckResult(true, $"蛇灵气保障 CD={蛇灵气CD:F1}s 飞蛇{当前飞蛇}+{蛇灵气给魂}={总飞蛇}≤3+可消{可消化}");

            // 消化不了 → 跳过等先消化飞蛇
            return new CheckResult(false, $"溢出保护 飞蛇{总飞蛇}>3+可消{可消化} 窗口{剩余窗口:F0}ms");
        }

        if (JinyuViperRotation.IsDailyMode)
            return new CheckResult(true, $"日随模式 开蛇剑连 充能={充能:F1}");

        if (PromeSettings.Instance.GetQt("120对齐") && VPRApi.等级() >= 86)
        {
            float 双附体时间 = VPRApi.附体时间() * 2f / 1000f;
            float 当前短buff = Math.Min(VPRApi.Buff剩余ms(ViperBuff.急速), VPRApi.Buff剩余ms(ViperBuff.猛袭)) / 1000f;
            if (当前短buff + 30f < 双附体时间 + 10f)
                return new CheckResult(false, $"buff不足覆盖双附体 需{双附体时间 + 10f:F1}s 可用{当前短buff + 30f:F1}s");

            if (蛇灵气CD >= 12f && 蛇灵气CD <= 18f)
                return new CheckResult(true, $"120对齐 续buff CD={蛇灵气CD:F1}s 双附体={双附体时间:F1}s");
        }

        // 蛇灵气让位：120团辅期内CD<5s或>115s时让位给蛇灵气
        if (VPRApi.等级() >= 86 && (蛇灵气CD < 5f || 蛇灵气CD > 115f)
            && EngageManager.GetBattleTime() > 5.0 && PromeSettings.Instance.GetQt("120对齐"))
            return new CheckResult(false, $"蛇灵气让位 CD={蛇灵气CD:F1}s");

        // 强碎灵蛇保留：蛇灵气CD≤80秒 且 Qt(蛇灵气)开启时，保障有一层可用
        // 但蛇灵气CD<12s时已在上面保障逻辑中处理，此处仅CD≥12s时保留
        if (蛇灵气CD >= 12f && 蛇灵气CD <= 80f && PromeSettings.Instance.GetQt("蛇灵气"))
        {
            if (充能 < 2f)
                return new CheckResult(false, $"蛇灵气CD={蛇灵气CD:F1}s 充能={充能:F1}<2 保留一层");
        }

        // 120团辅期前后combo覆盖检查（AOE环境放宽）
        if (VPRApi.IsInViper120() && comboTime > 0 && comboTime < (isAoe ? 5000f : 20000f))
            return new CheckResult(false, $"120团辅期 combo时间{comboTime:F0}ms<{(isAoe ? 5000 : 20000)}ms 优先基础GCD");

        return new CheckResult(true, $"开蛇剑连 充能={充能:F1} 蛇灵气CD={蛇灵气CD:F1}s");
    }

    public PAction GetAction()
    {
        uint skill = (VPRApi.附近敌人数() >= 3 && VPRApi.等级() >= 70 && PromeSettings.Instance.GetQt("AOE"))
            ? ViperSkill.强碎灵蛇AOE
            : ViperSkill.强碎灵蛇;
        return new PAction(skill, ActionType.Gcd, ActionTargetType.Target);
    }
}
