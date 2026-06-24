using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace JinyuViper;

public class ViperOpenerBlank : IOpener
{
    public string OpenerName => "空白起手";

    public List<PAction> InCombatSequence => [];

    public void InitializeCountdown(CountDownHandler countdownHandler) { }
}
