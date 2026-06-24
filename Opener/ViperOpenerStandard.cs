using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace JinyuViper;

public class ViperOpenerStandard : IOpener
{
    public string OpenerName => "标准起手";

    public List<PAction> InCombatSequence => BuildSequence();

    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        countdownHandler.AddAction(200,
            () => VPRApi.有目标() && !VPRApi.目标在近战范围()
                ? new PAction(ViperSkill.突进, ActionType.OffGcd, ActionTargetType.Target)
                : null);
    }

    private static List<PAction> BuildSequence()
    {
        var list = new List<PAction>();

        Gcd(list, ViperSkill.强碎灵蛇);

        if (PromeSettings.Instance.GetQt("爆发药"))
        {
            uint potionId = VPRApi.获取爆发药ID();
            if (potionId != 0)
                OffGcd(list, potionId, ActionTargetType.Self);
        }

        OffGcd(list, ViperSkill.蛇灵气, ActionTargetType.Self);

        GcdDynamic(list, () => VPRApi.Next蛇剑连());
        OffGcd(list, ViperSkill.双牙连击, ActionTargetType.Target);
        OffGcd(list, ViperSkill.双牙乱击, ActionTargetType.Target);

        GcdDynamic(list, () => VPRApi.Next蛇剑连());
        OffGcd(list, ViperSkill.双牙乱击, ActionTargetType.Target);
        OffGcd(list, ViperSkill.双牙连击, ActionTargetType.Target);

        Gcd(list, ViperSkill.祖灵降临);

        Gcd(list, ViperSkill.祖灵之牙一式);
        OffGcd(list, ViperSkill.祖灵续剑一, ActionTargetType.Target);

        Gcd(list, ViperSkill.祖灵之牙二式);
        OffGcd(list, ViperSkill.祖灵续剑二, ActionTargetType.Target);

        Gcd(list, ViperSkill.祖灵之牙三式);
        OffGcd(list, ViperSkill.祖灵续剑三, ActionTargetType.Target);

        Gcd(list, ViperSkill.祖灵之牙四式);
        OffGcd(list, ViperSkill.祖灵续剑四, ActionTargetType.Target);

        Gcd(list, ViperSkill.祖灵之牙五式);
        OffGcd(list, ViperSkill.蛇灵气, ActionTargetType.Self);

        return list;
    }

    private static void Gcd(List<PAction> list, uint actionId)
    {
        list.Add(new PAction(actionId, ActionType.Gcd, ActionTargetType.Target) { RequiresVerification = true });
    }

    private static void GcdDynamic(List<PAction> list, Func<uint> getId)
    {
        list.Add(new PAction(getId(), ActionType.Gcd, ActionTargetType.Target) { RequiresVerification = true });
    }

    private static void OffGcd(List<PAction> list, uint actionId, ActionTargetType target)
    {
        list.Add(new PAction(actionId, ActionType.OffGcd, target));
    }
}
