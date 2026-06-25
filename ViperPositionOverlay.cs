using System;
using Dalamud.Game.ClientState.Objects.Types;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;

namespace JinyuViper;

internal static class ViperPositionOverlay
{
    private static readonly ViperMDrawClient mDraw = new();
    public static bool IsMDrawReady => mDraw.IsReady;

    // 侧身位技能：猛袭盘蛇, 侧击獠牙, 侧裂獠牙, 穿裂灵蛇
    private static readonly uint[] 侧身技能 = { 34621, 34610, 34611, 34624 };
    // 背身位技能：疾速盘蛇, 背击獠牙, 背裂獠牙, 崩裂灵蛇
    private static readonly uint[] 背身技能 = { 34622, 34612, 34613, 34625 };

    public static void Update()
    {
        if (!PromeSettings.Instance.GetQt("身位指示"))
        {
            mDraw.Clean();
            return;
        }

        var target = Core.Target;
        if (!IsLiveTarget(target))
        {
            mDraw.Clean();
            return;
        }

        try
        {
            var go = (IGameObject)target!;
            var pos = go.Position;
            float radius = go.HitboxRadius + GameData.GetCurrentMeleeRange() + 0.5f;
            float rot = go.Rotation;

            var nextPos = GetNextPositional();
            if (nextPos == PositionalType.None)
            {
                mDraw.DrawTargetRing(pos, radius, ViperMDrawColors.TargetRing);
                return;
            }

            if (nextPos == PositionalType.Rear)
                mDraw.DrawRear(pos, radius, rot, ViperMDrawColors.CorrectPosition, ViperMDrawColors.InactivePosition);
            else
                mDraw.DrawFlank(pos, radius, rot, ViperMDrawColors.CorrectPosition, ViperMDrawColors.InactivePosition);
        }
        catch
        {
            mDraw.Clean();
        }
    }

    public static void Clean() => mDraw.Clean();

    private static PositionalType GetNextPositional()
    {
        // 附体期间无身位要求，跳过
        if (VPRApi.处于附体状态())
            return PositionalType.None;

        // 蛇剑连中：用Next蛇剑连()获取实际技能ID判断身位
        if (VPRApi.处于蛇剑连击())
        {
            uint next = VPRApi.Next蛇剑连();
            if (IsFlank(next)) return PositionalType.Flank;
            if (IsRear(next)) return PositionalType.Rear;
        }

        // 蛇剑连续剑（双牙连击/乱击）
        int followUp = VPRApi.续剑阶段();
        if (followUp == 7)
        {
            if (VPRApi.有Buff(ViperBuff.双牙连击标记)) return PositionalType.Flank;
            if (VPRApi.有Buff(ViperBuff.双牙乱击标记)) return PositionalType.Rear;
        }
        if (followUp == 8) return PositionalType.None; // AOE版无身位要求

        // 基础GCD：用Next基础GCD()获取实际技能ID判断身位
        uint nextBase = VPRApi.Next基础GCD();
        if (IsFlank(nextBase)) return PositionalType.Flank;
        if (IsRear(nextBase)) return PositionalType.Rear;

        return PositionalType.None;
    }

    private static bool IsFlank(uint id)
    {
        foreach (uint s in 侧身技能)
            if (id == s) return true;
        return false;
    }

    private static bool IsRear(uint id)
    {
        foreach (uint s in 背身技能)
            if (id == s) return true;
        return false;
    }

    private static bool IsLiveTarget(IBattleChara? target)
    {
        if (target == null) return false;
        try
        {
            var go = (IGameObject)target;
            return go.IsTargetable && !go.IsDead;
        }
        catch { return false; }
    }

    private enum PositionalType { None, Rear, Flank }
}
