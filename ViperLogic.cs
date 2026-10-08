using A = ApiHelper.ApiHelper;
using Dalamud.Game.ClientState.JobGauge.Enums;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;

namespace Jinyu;

internal static class ViperLogic
{
    private static long targetUnavailableSince;
    public static void UpdateTargetRecovery()
    {
        if(!A.战斗中 || A.玩家的职业!=41) {targetUnavailableSince=0;return;}
        if(!A.可攻击)
        {
            if(targetUnavailableSince==0)targetUnavailableSince=Environment.TickCount64;
            return;
        }
        if(targetUnavailableSince==0)return;
        if(Environment.TickCount64-targetUnavailableSince>=3000)
        {
            ActionQueueManager.ClearNormalQueues();
            A.警告日志("Jinyu: 目标恢复可攻击，清理普通旧动作队列并重新求解。");
        }
        targetUnavailableSince=0;
    }
    private static bool Valid => A.玩家 != null && A.玩家的职业 == 41 && A.活着 && A.战斗中
        && A.可攻击 && !A.读条中 && !A.获取QT("停手");
    private static float Buff(uint id) => A.状态剩余(A.玩家, id);
    private static bool Unlocked(uint id) => id != 0 && A.技能已解锁(id);
    private static uint Adjust(uint id) => A.调整技能ID(id);
    private static bool Melee => A.目标距离 <= A.攻击距离(3);
    private static bool Aoe => A.获取QT("AOE") && A.五米敌人 >= 3 && Unlocked(34614);

    public static PAction? Action(uint id, ActionType type)
    {
        if (id == 0) return null;
        ActionHelper.TryResolveActionTargetType(id, out var target);
        return new(id, type, target) { RequiresVerification = true,
            ExecutionGuard = () => Valid && InRange(id)
                && (id is not (34626 or 34647) || !BurstHoldControl.ShouldHoldForDyingTrash()) };
    }

    private static bool InRange(uint id)
    {
        if (!ActionHelper.TryGetActionRange(id, out var range)) return false;
        if (range == 0) return id is 34647 or 7546 or 7541 or 7542 || A.五米敌人 > 0;
        return A.目标距离 <= A.攻击距离(range);
    }

    private static uint Allowed(uint id) => Unlocked(id) && InRange(id) ? id : 0;

    // Transformed buttons determine which follow-ups still exist after manual input.
    private static uint[] FollowUps() => new[] { Adjust(35920), Adjust(35921), Adjust(35922) }
        .Where(id => id is >= 34634 and <= 34645).Where(Unlocked)
        .Where(id => A.技能可用(id) && !A.最近用过(id,600)).Distinct().ToArray();

    public static uint Gcd()
    {
        UpdateTargetRecovery();
        if (!Valid) return 0;
        if (ActionQueueManager.HasActionsInGcdQueue()) return 0;
        var followUps = FollowUps();
        if (followUps.Length > 0 && followUps.All(InRange)) return 0;
        var tribute = A.VPR.祖灵;
        if (tribute > 0)
        {
            var count = Unlocked(34631) ? 5 : 4;
            var next = tribute == 1 && count == 5 ? 34631u : (uint)(34627 + count - tribute);
            return Allowed(Adjust(next));
        }

        if (!Melee)
        {
            if (A.获取QT("飞蛇") && A.VPR.飞蛇 > 0 && Unlocked(34633)) return Allowed(34633);
            return Allowed(34632);
        }

        var dread = JobGaugeHelper.VPR.蛇剑连状态;
        if ((int)dread != 0)
        {
            var leftFirst = Buff(3668) <= Buff(3669) || !Unlocked(34622);
            var next = dread switch
            {
                DreadCombo.Dreadwinder => leftFirst ? 34621u : 34622u,
                DreadCombo.HuntersCoil => 34622u,
                DreadCombo.SwiftskinsCoil => 34621u,
                DreadCombo.PitOfDread => leftFirst ? 34624u : 34625u,
                DreadCombo.HuntersDen => 34625u,
                DreadCombo.SwiftskinsDen => 34624u,
                _ => 0u
            };
            return Allowed(next);
        }

        var snake = Aoe && Unlocked(34623) ? 34623u : 34620u;
        var coils = A.VPR.飞蛇;
        var cap = A.玩家的等级 >= 88 ? 3 : 2;
        var ireSoon = Unlocked(34647) && A.获取QT("爆发") && A.技能冷却(34647) < 5;
        if (A.获取QT("飞蛇") && coils > 0 && (coils >= cap || ireSoon && coils >= cap - 1)) return Allowed(34633);

        var requiredBuff = A.玩家有状态(3671) && A.VPR.灵力 >= 50
            ? BurstAlignment.DoubleSpan(A.GCD总时长) + A.GCD总时长 : BurstAlignment.SingleDuration;
        var buffReady = Buff(3668) > requiredBuff && Buff(3669) > requiredBuff;
        var ready = A.玩家有状态(3671);
        var energy = A.VPR.灵力;
        var align = JinyuSettings.Current.Align120 && A.获取QT("爆发") && !Aoe;
        var overflow = energy + BurstAlignment.PredictedOffering >= 100;
        var spend = !align || BurstAlignment.Window || overflow || ready && Buff(3671) < BurstAlignment.SingleDuration;
        if (!BurstHoldControl.ShouldHoldForDyingTrash() && Unlocked(34626) && buffReady && spend && (ready || energy >= 50))
            return Allowed(34626);

        var holdSnake = align && (ready || A.技能冷却(34647) <= A.GCD总时长 * 3)
            && Buff(3668) > BurstAlignment.DoubleSpan(A.GCD总时长) && Buff(3669) > BurstAlignment.DoubleSpan(A.GCD总时长);
        if (Unlocked(snake) && A.技能充能(snake) >= 1 && !holdSnake && (coils < cap || !Unlocked(34633))) return Allowed(snake);
        return BaseCombo(Aoe);
    }

    private static uint BaseCombo(bool aoe)
    {
        var first = aoe ? 34614u : 34606u;
        var second = aoe ? 34615u : 34607u;
        var left = Adjust(first); var right = Adjust(second);
        // Preserve an existing single-target/AOE chain before changing modes.
        var otherLeft = Adjust(aoe ? 34606u : 34614u);
        var otherRight = Adjust(aoe ? 34607u : 34615u);
        if (otherLeft is >= 34608 and <= 34613 || otherLeft is >= 34616 and <= 34619)
        { left = otherLeft; right = otherRight; }

        uint choice;
        if (left is 34608 or 34616)
        {
            var hunter = Buff(3668); var swift = Buff(3669);
            var needRear = A.玩家有状态(3647) || A.玩家有状态(3648);
            var needFlank = A.玩家有状态(3645) || A.玩家有状态(3646);
            choice = hunter < 8 ? left : swift < 8 && Unlocked(right) ? right
                : needRear && Unlocked(right) ? right : needFlank ? left
                : hunter <= swift || !Unlocked(right) ? left : right;
        }
        else if (left is >= 34610 and <= 34613 || left is 34618)
        {
            var leftBuff = left switch { 34610 => 3645u, 34611 => 3646u, 34612 => 3647u, 34613 => 3648u, _ => 3649u };
            var rightBuff = right switch { 34610 => 3645u, 34611 => 3646u, 34612 => 3647u, 34613 => 3648u, _ => 3650u };
            choice = A.玩家有状态(rightBuff) && !A.玩家有状态(leftBuff) && Unlocked(right) ? right : left;
        }
        else choice = A.玩家有状态(3772) && Unlocked(right) ? right : left;
        return Allowed(choice);
    }

    public static uint Ogcd()
    {
        if (!Valid || A.动画锁定 > 0 || A.GCD剩余 < 0.7f) return 0;
        var followUps = FollowUps();
        var ordered = followUps.OrderBy(id => id switch
        {
            34636 when A.玩家有状态(3657) => 0,
            34637 when A.玩家有状态(3658) => 0,
            34638 when A.玩家有状态(3659) => 0,
            34639 when A.玩家有状态(3660) => 0,
            34644 => 0,
            _ => 1
        });
        foreach (var id in ordered)
            if (A.技能可用(id) && !A.最近用过(id, 600) && InRange(id)) return id;
        if (followUps.Length > 0) return 0;

        if (!BurstHoldControl.ShouldHoldForDyingTrash() && A.获取QT("爆发") && A.VPR.祖灵 == 0 && (int)JobGaugeHelper.VPR.蛇剑连状态 == 0
            && Buff(3668) > 0 && Buff(3669) > 0 && A.VPR.飞蛇 < (A.玩家的等级 >= 88 ? 3 : 2)
            && !A.玩家有状态(3671) && A.技能可用(34647)) return 34647;
        if (ActionQueueManager.HasActionsInOffGcdQueue()) return 0;
        var next = Gcd();
        var positional = next switch { 34610 or 34611 or 34621 => Positional.Flank, 34612 or 34613 or 34622 => Positional.Rear, _ => Positional.None };
        if (A.获取QT("真北") && A.GCD剩余 <= JinyuSettings.Current.TrueNorthWindow && positional != Positional.None && A.目标有身位 && A.目标身位 != positional
            && !A.玩家有状态(1250) && A.技能可用(7546)) return 7546;
        if (A.获取QT("自动浴血") && A.玩家的血量 < JinyuSettings.Current.BloodbathHp && A.技能冷却(34647) > 5 && A.技能可用(7542)) return 7542;
        if (A.获取QT("自动内丹") && A.玩家的血量 < JinyuSettings.Current.SecondWindHp && A.技能可用(7541)) return 7541;
        return 0;
    }

    public static PAction? PotionAction()
    {
        if (!Valid || BurstHoldControl.ShouldHoldForDyingTrash() || !JinyuSettings.Current.Potion || !A.获取QT("爆发") || FollowUps().Length > 0
            || A.动画锁定 > 0 || A.GCD剩余 < 0.8f || A.VPR.祖灵 > 0
            || A.VPR.灵力 < 50 || !A.玩家有状态(3671) || !BurstAlignment.Window
            || Ogcd() == 34647) return null;
        var item = PotionCatalog.SelectedId;
        if (item == 0 || ActionHelper.IsItemOnCooldown(item)) return null;
        var id = JinyuSettings.Current.PreferHq && A.有物品(item, true) ? item + 1000000
            : A.有物品(item) ? item : A.有物品(item, true) ? item + 1000000 : 0;
        if (id == 0) return null;
        return new(id, ActionType.Item, ActionTargetType.Self) { ExecutionGuard = () => Valid && !BurstHoldControl.ShouldHoldForDyingTrash() && !ActionHelper.IsItemOnCooldown(item) };
    }
}
