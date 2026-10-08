using A = ApiHelper.ApiHelper;

namespace Jinyu;

internal static class BurstAlignment
{
    // Party damage buffs on the player; Trick/Mug/Chain-style effects are on the enemy.
    private static readonly uint[] PlayerBuffs = { 1185, 1822, 1878, 1239, 786, 141, 1297, 2964, 2703, 2599, 3887 };
    private static readonly uint[] TargetBuffs = { 1221, 1825, 638, 3254, 3906 };
    private static long started;
    public static void Start() => started = Environment.TickCount64;
    public static void Reset() { started = 0; BurstHoldControl.Reset(); }
    public static float Elapsed => started == 0 ? 0 : (Environment.TickCount64 - started) / 1000f;
    public static float RaidRemaining => PlayerBuffs.Select(id => A.状态剩余(A.玩家, id))
        .Concat(TargetBuffs.Select(id => A.状态剩余(A.目标, id))).DefaultIfEmpty(0).Max();
    public static float Cycle => Math.Max(0, Elapsed - JinyuSettings.Current.BurstOffset) % 120;
    public static float UntilBurst => Cycle < 20 ? 0 : 120 - Cycle;
    public static bool Scheduled => Elapsed >= JinyuSettings.Current.BurstOffset && Cycle <= 20;
    // 12 damage events span five ordinary and six accelerated recasts.
    public static float DoubleSpan(float gcd) => gcd * 5 + gcd / 2.5f * 2 * 6 + 0.2f;
    public static float SingleDuration => A.GCD总时长 * (3 + 3.2f);
    public static bool Window => RaidRemaining > 0 || Scheduled && Cycle >= JinyuSettings.Current.MaxBuffWait;
    public static bool CanDouble => A.VPR.灵力 >= 50 && A.玩家有状态(3671)
        && RaidRemaining >= DoubleSpan(A.GCD总时长);
    public static int PredictedOffering
    {
        get
        {
            var seconds = Math.Min(A.技能冷却(34647), UntilBurst == 0 ? 120 : UntilBurst);
            var thirds = (int)(seconds / Math.Max(1, A.GCD总时长 * 3));
            return thirds * 10;
        }
    }
}
