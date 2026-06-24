using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Managers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class ViperRotationEventHandler : IRotationEventHandler
{
    private static uint? _savedTargetId;      // 停手前保存的目标ID
    private static bool _wasStopActive;       // 上一帧停手状态
    private static bool _targetRestored;      // 目标是否已恢复（防止每帧重复查找）

    public void OnUpdate()
    {
        Handle停手();
        ViperPositionOverlay.Update();
    }

    public void OnOutOfBattleUpdate()
    {
        Handle停手();
        ViperPositionOverlay.Update();
    }

    public void OnBattleStarted() => ViperOpenerStateMachine.Start();
    public void OnBattleUpdate() { }
    public void OnNoTarget() => ViperPositionOverlay.Clean();

    public void OnBattleEnded()
    {
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
        ViperOpenerStateMachine.Reset();
        ViperPositionOverlay.Clean();
        Reset停手状态();
    }

    public void OnTerritoryChanged(ushort territoryId)
    {
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
        ViperOpenerStateMachine.Reset();
        ViperPositionOverlay.Clean();
        Reset停手状态();
    }

    /// <summary>
    /// 停手QT逻辑：
    /// 开启时：保存当前目标，取消选中（停止自动攻击）
    /// 关闭时：恢复之前的目标，若阵亡则按AOE设置选择新目标
    /// </summary>
    private static void Handle停手()
    {
        bool stopActive = PromeSettings.Instance.GetQt("停手");

        if (stopActive)
        {
            // 停手刚开启：保存当前目标并取消选中
            if (!_wasStopActive)
            {
                var current = Core.Target;
                if (current != null)
                    _savedTargetId = (uint)((IGameObject)current).GameObjectId;
                else
                    _savedTargetId = null;

                Core.SetTarget(null);  // 取消选中，停止自动攻击
                _targetRestored = false;
            }
            // 停手持续中：确保无目标
            else if (Core.Target != null)
            {
                Core.SetTarget(null);
            }

            _wasStopActive = true;
            return;
        }

        // 停手刚关闭：恢复目标
        if (_wasStopActive && !_targetRestored)
        {
            RestoreTarget();
            _targetRestored = true;
        }

        // 停手已关闭超过1帧，重置状态让框架自动选目标
        if (!stopActive && !_wasStopActive)
        {
            _savedTargetId = null;
            _targetRestored = false;
        }

        _wasStopActive = stopActive;
    }

    private static void RestoreTarget()
    {
        // 尝试恢复之前的目标
        if (_savedTargetId.HasValue)
        {
            var obj = Svc.Objects.SearchById(_savedTargetId.Value);
            if (obj is IBattleChara bc && bc.IsTargetable && !bc.IsDead)
            {
                Core.SetTarget(bc);
                return;
            }
        }

        // 目标不可用，选择新目标
        var newTarget = FindNewTarget();
        if (newTarget != null)
            Core.SetTarget(newTarget);
    }

    private static IBattleChara? FindNewTarget()
    {
        IBattleChara? best = null;
        float bestDist = float.MaxValue;
        float bestHp = 0f;
        bool useAoe = PromeSettings.Instance.GetQt("智能AOE");
        var me = Core.Me;
        if (me == null) return null;

        for (int i = 0; i < Svc.Objects.Length; i++)
        {
            var obj = Svc.Objects[i];
            if (obj is not IBattleChara bc) continue;
            if (!bc.IsTargetable || bc.IsDead) continue;
            if (!((ICharacter)bc).StatusFlags.HasFlag(StatusFlags.Hostile)) continue;

            float dist = Vector3.Distance(me.Position, obj.Position) - (me.HitboxRadius + obj.HitboxRadius);
            if (dist > 50f) continue;

            if (useAoe)
            {
                // 智能AOE：优先3米内最高HP（敌人最密集处）
                float hpPercent = bc.CurrentHp / Math.Max(1f, bc.MaxHp);
                if (dist <= 3f && hpPercent > bestHp)
                {
                    best = bc;
                    bestHp = hpPercent;
                    bestDist = dist;
                }
            }
            else
            {
                // 非AOE：最近目标
                if (dist < bestDist)
                {
                    best = bc;
                    bestDist = dist;
                }
            }
        }

        return best;
    }

    private static void Reset停手状态()
    {
        _savedTargetId = null;
        _wasStopActive = false;
        _targetRestored = false;
    }
}
