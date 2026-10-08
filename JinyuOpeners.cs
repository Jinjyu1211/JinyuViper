using A = ApiHelper.ApiHelper;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace Jinyu;

public abstract class JinyuOpener : IOpener
{
    public abstract string OpenerName { get; }
    protected abstract bool Fast { get; }
    public void InitializeCountdown(CountDownHandler countdownHandler) { }
    public List<PAction> InCombatSequence
    {
        get
        {
            var actions = new List<PAction>();
            void Add(uint id, ActionType type = ActionType.Gcd)
            {
                if (!A.技能已解锁(id)) return;
                var target = id is 34647 or 34626 ? ActionTargetType.Self : ActionTargetType.Target;
                actions.Add(new(id, type, target) { RequiresVerification = true });
            }
            if (Fast) { Add(34607); Add(34609); }
            Add(34620); Add(34622);
            Add(34637, ActionType.OffGcd); Add(34636, ActionType.OffGcd);
            Add(34621);
            Add(34636, ActionType.OffGcd); Add(34637, ActionType.OffGcd);
            if (A.获取QT("爆发")) Add(34647, ActionType.OffGcd);
            return actions;
        }
    }
}
public sealed class SerpentOpener : JinyuOpener
{
    public override string OpenerName => "Jinyu_通用蛇剑起手";
    protected override bool Fast => false;
}
public sealed class FastFangOpener : JinyuOpener
{
    public override string OpenerName => "Jinyu_齿剑快速起手";
    protected override bool Fast => true;
}
// Registered aliases use the saved choice unless explicit timeline control is enabled.
public sealed class SelectedSerpentOpener : JinyuOpener
{
    protected override bool Fast => !JinyuSettings.Current.UseTimelineOpener && JinyuSettings.Current.Opener == 1;
    public override string OpenerName => Fast ? "Jinyu_齿剑快速起手" : "Jinyu_通用蛇剑起手";
}
public sealed class SelectedFangOpener : JinyuOpener
{
    protected override bool Fast => JinyuSettings.Current.UseTimelineOpener || JinyuSettings.Current.Opener == 1;
    public override string OpenerName => Fast ? "Jinyu_齿剑快速起手" : "Jinyu_通用蛇剑起手";
}
