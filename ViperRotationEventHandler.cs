using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.Logging;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Managers;
using PromeRotation.Rotation;

namespace JinyuViper;

public class ViperRotationEventHandler : IRotationEventHandler
{
    private static uint? _savedTargetId;
    private static bool _wasStopActive;
    private static bool _targetRestored;

    public void OnUpdate()
    {
        Handle停手();
        ViperPositionOverlay.Update();
    }

    public void OnOutOfBattleUpdate()
    {
        Handle停手();
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

    private static void Handle停手()
    {
        try
        {
        bool stopActive = PromeSettings.Instance.GetQt("停手");

        if (stopActive)
        {
            if (!_wasStopActive)
            {
                var current = Core.Target;
                if (current != null)
                    _savedTargetId = (uint)((IGameObject)current).GameObjectId;
                else
                    _savedTargetId = null;

                Core.SetTarget(null);
                _targetRestored = false;
            }
            else if (Core.Target != null)
            {
                Core.SetTarget(null);
            }

            _wasStopActive = true;
            return;
        }

        if (_wasStopActive && !_targetRestored)
        {
            RestoreTarget();
            _targetRestored = true;
        }

        if (!stopActive && !_wasStopActive)
        {
            _savedTargetId = null;
            _targetRestored = false;
        }

        _wasStopActive = stopActive;
        }
        catch (Exception ex) { PluginLog.Error($"[VPR] Handle停手异常: {ex.Message}"); }
    }

    private static void RestoreTarget()
    {
        if (_savedTargetId.HasValue)
        {
            var obj = Svc.Objects.SearchById(_savedTargetId.Value);
            if (obj is IBattleChara bc && bc.IsTargetable && !bc.IsDead)
            {
                Core.SetTarget(bc);
                return;
            }
        }

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
