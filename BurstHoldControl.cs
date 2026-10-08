using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Extensions;
using Lumina.Excel.Sheets;
using A = ApiHelper.ApiHelper;

namespace Jinyu;

internal static class BurstHoldControl
{
    private sealed record Sample(uint Hp, uint MaxHp, long Time);
    private static readonly Dictionary<ulong, Sample> samples = new();
    private static long checkedAt;
    private static bool holding;
    public static void Reset() { samples.Clear(); checkedAt=0; holding=false; }

    public static bool ShouldHoldForDyingTrash()
    {
        if (!A.战斗中 || A.玩家 == null) { Reset(); return false; }
        var now=Environment.TickCount64;
        if(now-checkedAt<250)return holding;
        checkedAt=now;
        var enemies=Svc.Objects.OfType<IBattleNpc>().Where(x=>x.CurrentHp>0 && x.MaxHp>0 && x.IsEnemy()
            && Vector3.Distance(A.玩家.Position,x.Position)-A.玩家.HitboxRadius-x.HitboxRadius<=25).ToArray();
        // Treat ranked enemies conservatively as bosses/elites instead of holding on them.
        var trash=enemies.Where(x=>Svc.Data.GetExcelSheet<BNpcBase>().GetRow(x.BaseId).Rank==0).ToArray();
        Span<double> predictions=stackalloc double[32];
        var count=0;
        foreach(var enemy in trash)
        {
            if(samples.TryGetValue(enemy.GameObjectId,out var sample))
            {
                var elapsed=now-sample.Time;
                if(sample.MaxHp!=enemy.MaxHp || enemy.CurrentHp>sample.Hp)
                    samples[enemy.GameObjectId]=new(enemy.CurrentHp,enemy.MaxHp,now);
                else if(elapsed>=2000)
                {
                    if(sample.Hp>enemy.CurrentHp && count<predictions.Length)
                        predictions[count++]=(double)enemy.CurrentHp*elapsed/(sample.Hp-enemy.CurrentHp);
                    if(elapsed>=5000)samples[enemy.GameObjectId]=new(enemy.CurrentHp,enemy.MaxHp,now);
                }
            }
            else samples[enemy.GameObjectId]=new(enemy.CurrentHp,enemy.MaxHp,now);
        }
        var ids=trash.Select(x=>x.GameObjectId).ToHashSet();
        foreach(var id in samples.Keys.Where(x=>!ids.Contains(x)).ToArray())samples.Remove(id);
#pragma warning disable PR_EXPERIMENTAL
        var excluded=Svc.Party.Length==8 || GameData.IsInBossRoom() || GameData.TerritoryId==1048
            || BurstAlignment.Elapsed<=10 || trash.Length==0;
#pragma warning restore PR_EXPERIMENTAL
        if(excluded)return holding=false;
        var hp=trash.Sum(x=>(double)x.CurrentHp)/trash.Sum(x=>(double)x.MaxHp);
        var ttl=double.MaxValue;
        if(count>0)
        {
            predictions[..count].Sort();
            var start=count>4?1:0;var end=count>4?count-1:count;
            var sum=0d;for(var i=start;i<end;i++)sum+=predictions[i];
            ttl=sum/(end-start);
        }
        return holding=hp<Math.Clamp(JinyuSettings.Current.TrashHoldHpPercent,0,100)/100d || ttl<15000;
    }
}
