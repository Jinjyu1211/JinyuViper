using System;
using PromeRotation.Core;
using PromeRotation.Helpers;
using PromeRotation.Extensions;
using PromeRotation.Data;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace JinyuViper
{
    public static class ViperSkill
    {
        public const uint 钢牙   = 34606;
        public const uint 穿裂钢牙 = 34607;
        public const uint 猎牙   = 34608;
        public const uint 滑牙   = 34609;
        public const uint 锐牙左   = 34610;
        public const uint 锐牙右   = 34611;
        public const uint 侧击獠牙   = 34610;
        public const uint 侧裂獠牙   = 34611;
        public const uint 背锐牙左 = 34612;
        public const uint 背锐牙右 = 34613;
        public const uint 背击獠牙   = 34612;
        public const uint 背裂獠牙   = 34613;

        public const uint 疾裂钢牙  = 34614;
        public const uint 疾裂穿牙  = 34615;
        public const uint 疾裂猎牙  = 34616;
        public const uint 疾裂滑牙  = 34617;
        public const uint 疾裂锐牙  = 34618;
        public const uint 疾裂背锐牙 = 34619;

        public const uint 强碎灵蛇     = 34620;
        public const uint 猛袭盘蛇     = 34621;  // 侧身位
        public const uint 疾速盘蛇     = 34622;  // 背身位
        public const uint 强碎灵蛇AOE  = 34623;
        public const uint 穿裂灵蛇     = 34624;
        public const uint 崩裂灵蛇     = 34625;

        public const uint 祖灵降临    = 34626;
        public const uint 祖灵之牙一式 = 34627;
        public const uint 祖灵之牙二式 = 34628;
        public const uint 祖灵之牙三式 = 34629;
        public const uint 祖灵之牙四式 = 34630;
        public const uint 祖灵之牙五式 = 34631;

        public const uint 飞蛇之牙 = 34632;
        public const uint 飞蛇之尾 = 34633;

        public const uint 双牙连击     = 34636;
        public const uint 双牙乱击     = 34637;
        public const uint 双牙连击AOE  = 34638;
        public const uint 双牙乱击AOE  = 34639;

        public const uint 祖灵续剑一 = 34640;
        public const uint 祖灵续剑二 = 34641;
        public const uint 祖灵续剑三 = 34642;
        public const uint 祖灵续剑四 = 34643;

        public const uint 蛇剑连续剑CD1 = 35921;
        public const uint 蛇剑连续剑CD2 = 35922;

        public const uint 飞蛇连尾击 = 34644;
        public const uint 飞蛇乱尾击 = 34645;
        public const uint 蛇行       = 34646;

        public const uint 真北       = 7546;
        public const uint 内丹       = 7541;
        public const uint 浴血       = 7542;

        public const uint 蛇尾术 = 35920;

        public const uint 蛇灵气 = 34647;
        public const uint 突进 = 34646;
    }

    public static class ViperBuff
    {
        public const uint 连牙标记 = 3672;
        public const uint 乱牙标记 = 3772;

        public const uint 侧绿左 = 3645;
        public const uint 侧绿右 = 3646;
        public const uint 背红左 = 3647;
        public const uint 背红右 = 3648;

        public const uint 急速 = 3669;
        public const uint 猛袭 = 3668;

        public const uint 祖灵附体 = 3670;
        public const uint 祖灵预备 = 3671;

        public const uint 真北       = 1250;

        public const uint 飞蛇连尾标记 = 3665;
        public const uint 飞蛇乱尾标记 = 3666;

        public const uint 双牙连击标记     = 3657;
        public const uint 双牙乱击标记     = 3658;
        public const uint 双牙连击AOE标记  = 3659;
        public const uint 双牙乱击AOE标记  = 3660;
    }

    public static class VPRApi
    {
        public static byte 飞蛇层数() => JobGaugeHelper.VPR.飞蛇之魂层数;
        public static byte 灵力值() => JobGaugeHelper.VPR.灵力值;
        public static byte 祖灵力档数() => JobGaugeHelper.VPR.祖灵力档数;
        public static uint 祖灵层数()
        {
            uint n = (uint)(灵力值() / 50);
            if (有Buff(ViperBuff.祖灵预备)) n++;
            return n;
        }
        public static int 蛇剑连阶段() => (int)JobGaugeHelper.VPR.蛇剑连状态;
        public static int 续剑阶段() => (int)JobGaugeHelper.VPR.蛇尾术状态;

        public static bool 处于蛇剑连击()
        {
            int s = 蛇剑连阶段();
            return s >= 1 && s <= 6;
        }
        public static bool 处于附体状态() => 祖灵力档数() >= 1;

        public static uint 等级() => Core.Me?.Level ?? 100;
        public static bool 战斗中() => GameData.IsInCombat();

        public static bool 有目标()
        {
            try { return Core.Me != null && Core.Target != null && Core.Target.EntityId != Core.Me.EntityId; } catch { return false; }
        }
        public static bool 目标在近战范围()
        {
            try
            {
                if (!有目标()) return false;
                return Core.Target!.DistanceToMe() <= GameData.GetCurrentMeleeRange();
            }
            catch { return true; }
        }
        public static bool 目标在距离内(float range)
        {
            try
            {
                if (!有目标()) return false;
                return Core.Target!.DistanceToMe() <= range;
            }
            catch { return true; }
        }
        public static bool 目标在远程范围() => 目标在距离内(25f);
        public static bool 目标在背后()
        {
            try { return TargetHelper.GetTargetPositional() == Positional.Rear; }
            catch { return false; }
        }
        public static uint 附近敌人数(float range = 5f)
        {
            try { return TargetHelper.EnemyInRange(range); } catch { return 1; }
        }
        public static uint 目标周围敌人数(float range = 5f)
        {
            try { return TargetHelper.EnemyInRangeTarget(Core.Target, range); } catch { return 1; }
        }

        public static float GCD剩余ms() => ActionHelper.GetGcdRemain() * 1000f;
        public static float GCD总时长ms() => ActionHelper.GetGcdTotal() * 1000f;
        public static float 技能CD毫秒(uint id)
        {
            try
            {
                var adjusted = ActionHelper.GetAdjustedActionId(id);
                float cd = 0f;
                if (adjusted != 0) cd = ActionHelper.GetActionCooldown(adjusted) * 1000f;
                if (cd == 0f && id != 0) cd = ActionHelper.GetActionCooldown(id) * 1000f;
                return cd;
            }
            catch { return 0f; }
        }
        public static bool 技能CD好(uint id)
        {
            try
            {
                var adjusted = ActionHelper.GetAdjustedActionId(id);
                if (adjusted != 0 && ActionHelper.IsReady(adjusted)) return true;
                return ActionHelper.IsReady(id);
            }
            catch { return false; }
        }
        public static float 技能充能(uint id)
        {
            try
            {
                var adjusted = ActionHelper.GetAdjustedActionId(id);
                var adjustedCharges = adjusted == 0 ? 0 : ActionHelper.GetActionCharges(adjusted);
                var rawCharges = id == 0 ? 0 : ActionHelper.GetActionCharges(id);
                return Math.Max(adjustedCharges, rawCharges);
            }
            catch { return 0f; }
        }
        public static float 强碎灵蛇充能() => 技能充能(ViperSkill.强碎灵蛇);
        public static int 技能最大充能(uint id)
        {
            try
            {
                var adjusted = ActionHelper.GetAdjustedActionId(id);
                if (adjusted != 0) return ActionHelper.GetMaxCharges(adjusted);
                return id != 0 ? ActionHelper.GetMaxCharges(id) : 0;
            }
            catch { return 0; }
        }
        public static uint 上一个连击ID()
        {
            try { return ActionHelper.GetLastComboID(); } catch { return 0; }
        }
        public static bool 能力技不卡GCD() => GCD剩余ms() <= 400f;
        public static bool 后半G窗口() => GCD剩余ms() < GCD总时长ms() / 2f;
        public static float 蛇灵气CD毫秒() => 技能CD毫秒(ViperSkill.蛇灵气);
        public static bool 最近用过(uint id, float 秒) => ActionHelper.RecentlyUsed(id, (int)(秒 * 1000f));

        public static bool 处于蛇剑连窗口()
        {
            uint last = 上一个连击ID();
            return (last == 34621 || last == 34622) || (last == 34624 || last == 34625);
        }
        public static bool 处于附体连窗口()
        {
            uint last = 上一个连击ID();
            return last >= 34627 && last <= 34631;
        }
        public static bool 处于无法使用其他能力技窗口() => 处于蛇剑连窗口() || 处于附体连窗口();

        public static bool 处于强碎灵窗口()
        {
            int stage = 蛇剑连阶段();
            return stage == 1 || stage == 2 || stage == 3;
        }

        public static bool 有Buff(uint id)
        {
            try { return Core.Me?.HasStatus(id) == true; } catch { return false; }
        }
        public static float Buff剩余ms(uint id)
        {
            try { return Core.Me != null ? Core.Me.GetStatusLeftTime(id) * 1000f : 0f; } catch { return 0f; }
        }
        public static bool 双BUFF齐全() => 有Buff(ViperBuff.急速) && 有Buff(ViperBuff.猛袭);

        public static bool Is锐牙buff快到期()
        {
            uint[] buffs = { ViperBuff.连牙标记, ViperBuff.乱牙标记, ViperBuff.侧绿左, ViperBuff.侧绿右, ViperBuff.背红左, ViperBuff.背红右 };
            foreach (uint id in buffs)
            {
                float t = Buff剩余ms(id);
                if (t > 0f && t < 8000f) return true;
            }
            return false;
        }

        public static bool 团辅激活() => GameData.IsInPure120();

        /// <summary>
        /// 120团辅期判定：仅依赖实际团辅buff检测。
        /// 不再使用蛇灵气CD≥98s推测，避免4人本/日常/木人训练等无团辅场景误触发。
        /// 团辅到达前的预备期由 Qt("120对齐") + 蛇灵气让位/续buff窗口 等独立逻辑处理。
        /// </summary>
        public static bool IsInViper120() => GameData.IsInPure120();
        public static bool 正在喝爆发药()
        {
            uint[] 爆发药buff = { 2678, 2679, 4945, 4946 };
            foreach (uint id in 爆发药buff)
            {
                if (有Buff(id)) return true;
            }
            return false;
        }

        // === 2.12技速下的实际复唱时间(通过GCD总时长比例换算) ===
        // 强碎灵蛇=2.55s(固定) | 盘蛇=3.00s→疾速下2.12s | 祖灵降临=1.87s
        // 祖灵之牙=1.70s | 祖灵大龊牙=2.55s
        // 飞蛇之尾=3.50s(无疾速)→2.97s(疾速下)
        public static float 蛇剑复唱时间() => GCD总时长ms() * 3000f / 2500f;       // 3.00s@2.5速 → 2.54s@2.12速
        public static float 飞蛇复唱时间() => GCD总时长ms() * 3500f / 2500f;       // 3.50s@无疾速 → 2.97s@疾速
        public static float 附体复唱时间() => GCD总时长ms() * 2200f / 2500f;       // 1.87s@2.12速 ✓
        public static float 附体连击复唱时间() => GCD总时长ms() * 2000f / 2500f;   // 1.70s@2.12速 ✓
        public static float 附体团契复唱时间() => GCD总时长ms() * 3000f / 2500f;   // 2.55s@2.12速 ✓
        public static float 普攻复唱时间() => GCD总时长ms();                        // 2.12s@2.12速

        // 双附体总时间: lv≥96=11.22s, lv<96=8.67s (buff=30s, 余量充足)
        public static float 附体时间()
        {
            if (等级() >= 96)
                return 附体复唱时间() + 4f * 附体连击复唱时间() + 附体团契复唱时间();
            return 附体复唱时间() + 4f * 附体连击复唱时间();
        }

        // 装备要求：2.12技速GCD
        public const float 要求GCD = 2.12f;
        public static bool 技速达标() => GCD总时长ms() <= 要求GCD * 1000f + 50f; // 容差50ms

        public static int 蛇灵气前能打的灵气()
        {
            return 10 * CountThirds() + 10 * 蛇灵气前蛇剑数量();
        }

        public static int CountThirds()
        {
            double num = 普攻复唱时间();
            double num2 = 蛇灵气CD毫秒() - 附体时间() - 蛇灵气前蛇剑数量() * 10400f - 飞蛇复唱时间() - GCD剩余ms();
            int num3 = Next基础连招阶段();
            if (num <= 0.0) return 0;
            if (num2 < 0.0) return 0;
            if (num3 < 1 || num3 > 3) return 0;
            long num4 = (long)Math.Floor(num2 / num) + 1;
            int num5 = ((num3 == 1) ? 3 : (num3 - 1)) switch
            {
                3 => 1,
                2 => 2,
                1 => 3,
                _ => throw new InvalidOperationException(),
            };
            if (num5 > num4) return 0;
            return (int)((num4 - num5) / 3) + 1;
        }

        public static int 下一个蛇剑ms()
        {
            float cd = 技能CD毫秒(ViperSkill.强碎灵蛇);
            return (int)((cd > 40000f) ? (cd - 40000f) : cd);
        }

        public static int 蛇灵气前蛇剑数量()
        {
            double num = 蛇灵气CD毫秒();
            int num2 = 下一个蛇剑ms();
            int num3 = (int)强碎灵蛇充能();
            int num4 = 0;
            while (num > 0.0)
            {
                int num5 = (num3 < 2) ? num2 : int.MaxValue;
                int val = (num3 >= 1 && num >= 7500.0) ? 7500 : int.MaxValue;
                int num6 = Math.Min(num5, val);
                if (num6 == int.MaxValue || (double)num6 > num) break;
                if (num6 == num5)
                {
                    num -= num6;
                    num3++;
                    num2 = (num3 >= 2) ? int.MaxValue : 40000;
                    continue;
                }
                num -= 7500.0;
                num4++;
                num3--;
                if (num3 < 2)
                {
                    num2 -= 7500;
                    while (num2 <= 0 && num3 < 2)
                    {
                        num3++;
                        num2 += 40000;
                        if (num3 >= 2) { num2 = int.MaxValue; break; }
                    }
                }
                else
                {
                    num2 = int.MaxValue;
                }
                if (num3 < 2 && num2 == int.MaxValue) num2 = 40000;
            }
            return num4;
        }

        public static float 连击剩余ms()
        {
            try { return ActionHelper.GetComboLeftTime() * 1000f; } catch { return 0f; }
        }

        public static bool 目标残血小怪()
        {
            try
            {
                if (Core.Target == null) return false;
                float hp = PartyHelper.GetHpPercent(Core.Target);
                return hp > 0 && hp < 5f;
            }
            catch { return false; }
        }

        public static float 自身血量百分比()
        {
            try { return Core.Me != null ? PartyHelper.GetHpPercent(Core.Me) : 100f; }
            catch { return 100f; }
        }

        public static bool BUFF型为蛇剑型()
        {
            return Math.Abs(Buff剩余ms(ViperBuff.急速) - Buff剩余ms(ViperBuff.猛袭)) < 3000f;
        }

        public static float 普攻续加速BUFF时间()
        {
            int num;
            switch (Next基础GCD())
            {
                case ViperSkill.背锐牙左:
                case ViperSkill.背锐牙右:
                    num = 5; break;
                case ViperSkill.钢牙:
                    num = 4; break;
                case ViperSkill.猎牙:
                    num = 3; break;
                case ViperSkill.锐牙左:
                case ViperSkill.锐牙右:
                    num = 2; break;
                case ViperSkill.穿裂钢牙:
                    num = 1; break;
                default:
                    num = 0; break;
            }
            return GCD剩余ms() + num * 普攻复唱时间();
        }

        public static float 普攻续加攻BUFF时间()
        {
            int num;
            switch (Next基础GCD())
            {
                case ViperSkill.背锐牙左:
                case ViperSkill.背锐牙右:
                    num = 2; break;
                case ViperSkill.钢牙:
                    num = 1; break;
                case ViperSkill.猎牙:
                    num = 0; break;
                case ViperSkill.锐牙左:
                case ViperSkill.锐牙右:
                    num = 5; break;
                case ViperSkill.穿裂钢牙:
                    num = 4; break;
                case ViperSkill.滑牙:
                    num = 3; break;
                default:
                    num = 0; break;
            }
            return GCD剩余ms() + num * 普攻复唱时间();
        }

        public static float 普攻续短buff时间()
        {
            if (Buff剩余ms(ViperBuff.急速) >= Buff剩余ms(ViperBuff.猛袭))
                return 普攻续加攻BUFF时间();
            return 普攻续加速BUFF时间();
        }

        public static float 蛇剑续短buff时间()
        {
            if (等级() > 92 && 飞蛇层数() == 3)
                return 蛇剑复唱时间() + 飞蛇复唱时间();
            return 蛇剑复唱时间();
        }

        public static bool 使用蛇剑续BUFF()
        {
            if (!BUFF型为蛇剑型() && 普攻续短buff时间() < 蛇剑续短buff时间())
                return false;
            return true;
        }

        public static bool 有蛇剑(float gcd需求)
        {
            if (处于蛇剑连击()) return false;
            if (祖灵力档数() != 0) return false;
            // 附体让路
            if (PromeSettings.Instance.GetQt("附体") && !处于附体状态()
                && 等级() >= 90 && (灵力值() >= 90 || 有Buff(ViperBuff.祖灵预备))
                && 技能CD毫秒(ViperSkill.祖灵降临) <= gcd需求 * 普攻复唱时间())
                return false;
            float 充能 = 强碎灵蛇充能();
            if (充能 >= 1f) return true;
            float cd = 技能CD毫秒(ViperSkill.强碎灵蛇);
            float window = GCD剩余ms() + 100f + (gcd需求 - 1f) * 普攻复唱时间();
            if (cd < window) return true;
            return false;
        }

        public static bool 技能高亮(uint id)
        {
            try { return id.IsActionHighlighted(); } catch { return false; }
        }
        public static uint 调整技能ID(uint id)
        {
            try { return ActionHelper.GetAdjustedActionId(id); } catch { return id; }
        }

        public static int Next基础连招阶段()
        {
            uint last = 上一个连击ID();
            if ((last == ViperSkill.钢牙 || last == ViperSkill.穿裂钢牙) && 等级() >= 5)
                return 2;
            if ((last == ViperSkill.猎牙 || last == ViperSkill.滑牙) && 等级() >= 30)
                return 3;
            return 1;
        }

        public static uint Next基础GCD()
        {
            switch (Next基础连招阶段())
            {
                case 1:
                    if (!有Buff(ViperBuff.连牙标记)) return ViperSkill.穿裂钢牙;
                    return ViperSkill.钢牙;

                case 2:
                    if (等级() < 30) return ViperSkill.猎牙;
                    // 有buff标记时按标记走（维持侧背交替顺序）
                    if (有Buff(ViperBuff.侧绿左) || 有Buff(ViperBuff.侧绿右)) return ViperSkill.猎牙;
                    if (有Buff(ViperBuff.背红左) || 有Buff(ViperBuff.背红右)) return ViperSkill.滑牙;
                    // 无标记时：无急速→滑牙(背)补急速，有急速→猎牙(侧)补猛袭
                    if (!有Buff(ViperBuff.急速)) return ViperSkill.滑牙;
                    return ViperSkill.猎牙;

                case 3:
                    if (技能高亮(ViperSkill.背锐牙左)) return ViperSkill.背锐牙左;
                    if (技能高亮(ViperSkill.背锐牙右)) return ViperSkill.背锐牙右;
                    if (技能高亮(ViperSkill.锐牙左))   return ViperSkill.锐牙左;
                    if (技能高亮(ViperSkill.锐牙右))   return ViperSkill.锐牙右;
                    if (有Buff(ViperBuff.背红左)) return ViperSkill.背锐牙左;
                    if (有Buff(ViperBuff.背红右)) return ViperSkill.背锐牙右;
                    if (有Buff(ViperBuff.侧绿左)) return ViperSkill.锐牙左;
                    if (有Buff(ViperBuff.侧绿右)) return ViperSkill.锐牙右;
                    uint last = 上一个连击ID();
                    if (last == ViperSkill.猎牙) return ViperSkill.锐牙右;
                    return ViperSkill.背锐牙右;

                default:
                    return ViperSkill.钢牙;
            }
        }

        public static uint Next蛇剑连()
        {
            int stage = 蛇剑连阶段();
            // stage 2/3/5/6 固定映射，维持背侧交替顺序（与ahxq一致）
            if (stage == 2) return ViperSkill.疾速盘蛇;  // 34622 背身位(急速)
            if (stage == 3) return ViperSkill.猛袭盘蛇;  // 34621 侧身位(猛袭)
            if (stage == 5) return ViperSkill.崩裂灵蛇;  // 背
            if (stage == 6) return ViperSkill.穿裂灵蛇;  // 侧
            // stage 1/4 续短buff
            if (stage == 1)
            {
                if (双BUFF齐全() && Buff剩余ms(ViperBuff.急速) > 10000f && Buff剩余ms(ViperBuff.猛袭) > 10000f)
                {
                    if (!目标在背后()) return ViperSkill.猛袭盘蛇;
                    return ViperSkill.疾速盘蛇;
                }
                if (Buff剩余ms(ViperBuff.急速) <= Buff剩余ms(ViperBuff.猛袭))
                    return ViperSkill.疾速盘蛇;
                return ViperSkill.猛袭盘蛇;
            }
            if (stage == 4)
            {
                if (Buff剩余ms(ViperBuff.急速) <= Buff剩余ms(ViperBuff.猛袭))
                    return ViperSkill.崩裂灵蛇;
                return ViperSkill.穿裂灵蛇;
            }
            return ViperSkill.强碎灵蛇;
        }

        public static uint Next附体连()
        {
            byte n = 祖灵力档数();
            if (等级() >= 96)
            {
                switch (n)
                {
                    case 5: return ViperSkill.祖灵之牙一式;
                    case 4: return ViperSkill.祖灵之牙二式;
                    case 3: return ViperSkill.祖灵之牙三式;
                    case 2: return ViperSkill.祖灵之牙四式;
                    case 1: return ViperSkill.祖灵之牙五式;
                }
            }
            else
            {
                switch (n)
                {
                    case 4: return ViperSkill.祖灵之牙一式;
                    case 3: return ViperSkill.祖灵之牙二式;
                    case 2: return ViperSkill.祖灵之牙三式;
                    case 1: return ViperSkill.祖灵之牙四式;
                }
            }
            return ViperSkill.祖灵之牙一式;
        }

        public static uint Next蛇剑连续剑()
        {
            int s = 续剑阶段();
            if (s == 7)
            {
                if (有Buff(ViperBuff.双牙连击标记)) return ViperSkill.双牙连击;
                if (有Buff(ViperBuff.双牙乱击标记)) return ViperSkill.双牙乱击;
                if (!最近用过(ViperSkill.双牙连击, 1.2f)) return ViperSkill.双牙连击;
                return ViperSkill.双牙乱击;
            }
            if (s == 8)
            {
                if (有Buff(ViperBuff.双牙连击AOE标记)) return ViperSkill.双牙连击AOE;
                if (有Buff(ViperBuff.双牙乱击AOE标记)) return ViperSkill.双牙乱击AOE;
                if (!最近用过(ViperSkill.双牙连击AOE, 1.2f)) return ViperSkill.双牙连击AOE;
                return ViperSkill.双牙乱击AOE;
            }
            return ViperSkill.双牙连击;
        }

        public static uint Next飞蛇续剑()
        {
            if (有Buff(ViperBuff.飞蛇连尾标记)) return ViperSkill.飞蛇连尾击;
            if (有Buff(ViperBuff.飞蛇乱尾标记)) return ViperSkill.飞蛇乱尾击;
            if (!最近用过(ViperSkill.飞蛇连尾击, 1.2f)) return ViperSkill.飞蛇连尾击;
            return ViperSkill.飞蛇乱尾击;
        }

        public static bool 有蛇剑充能() => 强碎灵蛇充能() >= 1f;

        public static uint 获取爆发药ID()
        {
            try { return GameData.GetBestPotionId(); } catch { return 0u; }
        }

        public static float 物品CD毫秒(uint itemId)
        {
            try
            {
                unsafe
                {
                    var ptr = ActionManager.Instance();
                    if (ptr == null) return 0f;
                    uint id = itemId > 1000000 ? itemId - 1000000 : itemId;
                    float recast = ptr->GetRecastTime((FFXIVClientStructs.FFXIV.Client.Game.ActionType)2, id);
                    float elapsed = ptr->GetRecastTimeElapsed((FFXIVClientStructs.FFXIV.Client.Game.ActionType)2, id);
                    return Math.Max(0f, (recast - elapsed) * 1000f);
                }
            }
            catch { return 0f; }
        }

        // ==================== 预设配置 ====================

        /// <summary>应用QT预设（一键批量设置）</summary>
        public static void ApplyPreset(int presetIndex)
        {
            var s = PromeSettings.Instance;
            switch (presetIndex)
            {
                case 0: // 副本模式
                    s.SetQt("日随模式", false);
                    s.SetQt("120对齐", true);
                    s.SetQt("对齐蛇气", true);
                    s.SetQt("智能AOE", false);
                    s.SetQt("AOE", true);
                    s.SetQt("起手爆发", true);
                    s.SetQt("倾泻爆发", false);
                    s.SetQt("优先飞蛇", false);
                    break;

                case 1: // 日随模式
                    s.SetQt("日随模式", true);
                    s.SetQt("120对齐", false);
                    s.SetQt("对齐蛇气", false);
                    s.SetQt("智能AOE", true);
                    s.SetQt("AOE", true);
                    s.SetQt("起手爆发", false);
                    s.SetQt("倾泻爆发", false);
                    s.SetQt("优先飞蛇", false);
                    break;

                case 2: // 倾泻模式
                    s.SetQt("日随模式", false);
                    s.SetQt("120对齐", false);
                    s.SetQt("对齐蛇气", false);
                    s.SetQt("智能AOE", false);
                    s.SetQt("AOE", true);
                    s.SetQt("起手爆发", true);
                    s.SetQt("倾泻爆发", true);
                    s.SetQt("优先飞蛇", false);
                    break;

                case 3: // 全开调试
                    foreach (var kvp in JinyuViperRotation.QtList)
                        if (kvp.Key != "停手") s.SetQt(kvp.Key, true);
                    break;
            }
        }

        /// <summary>预设名称列表</summary>
        public static readonly string[] PresetNames =
        {
            "副本模式",   // 120对齐+对齐蛇气+标准循环
            "日随模式",   // 简化循环+智能AOE
            "倾泻模式",   // 不保留资源+不起手
            "全开调试",   // 除停手外全部开启
        };
    }
}
