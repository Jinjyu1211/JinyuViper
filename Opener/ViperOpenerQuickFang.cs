using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace JinyuViper;

public class ViperOpenerQuickFang : IOpener
{
    public string OpenerName => "齿剑快速起手";

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

        // 第1步：基础GCD开场
        Gcd(list, ViperSkill.钢牙);

        // 第2步：蛇灵气穿插
        if (PromeSettings.Instance.GetQt("蛇灵气") && VPRApi.等级() >= 86)
            OffGcd(list, ViperSkill.蛇灵气, ActionTargetType.Self);

        // 第3步：基础GCD#2（拿急速buff）
        GcdDynamic(list, () => VPRApi.Next基础GCD());

        // 第4步：爆发药（强碎灵蛇后穿插）
        if (PromeSettings.Instance.GetQt("爆发药"))
        {
            uint potionId = VPRApi.获取爆发药ID();
            if (potionId != 0) OffGcd(list, potionId, ActionTargetType.Self);
        }

        // 第5步：强碎灵蛇（有急速buff后开蛇剑连）
        Gcd(list, ViperSkill.强碎灵蛇);

        // 第6-7步：蛇剑连（先侧身后背身，补猛袭buff）
        GcdDynamic(list, () => VPRApi.Next蛇剑连());
        GcdDynamic(list, () => VPRApi.Next蛇剑连());

        // 第8步：祖灵降临
        if (PromeSettings.Instance.GetQt("附体"))
            Gcd(list, ViperSkill.祖灵降临);

        // 第9-12步：附体连×4
        if (PromeSettings.Instance.GetQt("附体"))
        {
            Gcd(list, ViperSkill.祖灵之牙一式);
            Gcd(list, ViperSkill.祖灵之牙二式);
            Gcd(list, ViperSkill.祖灵之牙三式);
            Gcd(list, ViperSkill.祖灵之牙四式);
        }

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
