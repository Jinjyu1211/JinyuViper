using System;
using System.Numerics;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;
using ECommons.Logging;

namespace JinyuViper;

internal sealed class ViperMDrawClient
{
    private const string Prefix = "mDraw.V1";
    private const string Ns = "JinyuViper";

    private const int RefreshIntervalMs = 33;
    private const int RingTtlMs = 1500;
    private const int GuideTtlMs = 180;

    private ICallGateSubscriber<bool>? isReady;
    private ICallGateSubscriber<string, Vector4, Vector4, object?>? configure;
    private ICallGateSubscriber<string, bool, bool, object?>? configureOptions;
    private ICallGateSubscriber<string, string, Vector3, float, Vector4, int, object?>? targetRing;
    private ICallGateSubscriber<string, string, Vector3, float, float, Vector4, int, object?>? rear;
    private ICallGateSubscriber<string, string, Vector3, float, float, Vector4, int, object?>? flank;
    private ICallGateSubscriber<string, object?>? clean;

    private bool hasShapes;
    private long nextConnectAt;
    private long nextRefreshAt;
    private long suppressUntil;
    private bool loggedConnected;

    public bool IsReady
    {
        get
        {
            try
            {
                isReady ??= Svc.PluginInterface.GetIpcSubscriber<bool>($"{Prefix}.IsReady");
                return isReady.InvokeFunc();
            }
            catch { return false; }
        }
    }

    public void DrawTargetRing(Vector3 position, float radius, Vector4 color)
    {
        long now = Environment.TickCount64;
        if (now < suppressUntil || now < nextRefreshAt) return;
        if (!EnsureConnected(now)) return;

        try
        {
            configure?.InvokeAction(Ns, ViperMDrawColors.WrongPosition, ViperMDrawColors.Fill);
            configureOptions?.InvokeAction(Ns, ViperMDrawColors.ShowPositionGuide, ViperMDrawColors.ReverseFill);
            targetRing?.InvokeAction(Ns, "TargetRing", position, radius, color, RingTtlMs);
            hasShapes = true;
            nextRefreshAt = now + RefreshIntervalMs;
        }
        catch (Exception ex) { Suppress(ex); }
    }

    public void DrawRear(Vector3 position, float radius, float targetRotation, Vector4 activeColor, Vector4 inactiveColor)
    {
        long now = Environment.TickCount64;
        if (now < suppressUntil || now < nextRefreshAt) return;
        if (!EnsureConnected(now)) return;

        try
        {
            configure?.InvokeAction(Ns, ViperMDrawColors.WrongPosition, ViperMDrawColors.Fill);
            configureOptions?.InvokeAction(Ns, ViperMDrawColors.ShowPositionGuide, ViperMDrawColors.ReverseFill);
            targetRing?.InvokeAction(Ns, "TargetRing", position, radius, ViperMDrawColors.TargetRing, RingTtlMs);
            rear?.InvokeAction(Ns, "Rear", position, radius, targetRotation, activeColor, GuideTtlMs);
            flank?.InvokeAction(Ns, "Flank", position, radius, targetRotation, inactiveColor, GuideTtlMs);
            hasShapes = true;
            nextRefreshAt = now + RefreshIntervalMs;
        }
        catch (Exception ex) { Suppress(ex); }
    }

    public void DrawFlank(Vector3 position, float radius, float targetRotation, Vector4 activeColor, Vector4 inactiveColor)
    {
        long now = Environment.TickCount64;
        if (now < suppressUntil || now < nextRefreshAt) return;
        if (!EnsureConnected(now)) return;

        try
        {
            configure?.InvokeAction(Ns, ViperMDrawColors.WrongPosition, ViperMDrawColors.Fill);
            configureOptions?.InvokeAction(Ns, ViperMDrawColors.ShowPositionGuide, ViperMDrawColors.ReverseFill);
            targetRing?.InvokeAction(Ns, "TargetRing", position, radius, ViperMDrawColors.TargetRing, RingTtlMs);
            rear?.InvokeAction(Ns, "Rear", position, radius, targetRotation, inactiveColor, GuideTtlMs);
            flank?.InvokeAction(Ns, "Flank", position, radius, targetRotation, activeColor, GuideTtlMs);
            hasShapes = true;
            nextRefreshAt = now + RefreshIntervalMs;
        }
        catch (Exception ex) { Suppress(ex); }
    }

    public void Clean()
    {
        if (!hasShapes) return;
        try
        {
            if (EnsureConnected(Environment.TickCount64))
                clean?.InvokeAction(Ns);
        }
        catch { }
        hasShapes = false;
        nextRefreshAt = 0;
    }

    private bool EnsureConnected(long now)
    {
        try
        {
            if (isReady != null && isReady.InvokeFunc())
            {
                if (!loggedConnected)
                {
                    loggedConnected = true;
                    PluginLog.Information("[VPR-mDraw] IPC连接成功");
                }
                return true;
            }
        }
        catch { }

        if (now < nextConnectAt) return false;
        nextConnectAt = now + 2000;

        try
        {
            isReady ??= Svc.PluginInterface.GetIpcSubscriber<bool>($"{Prefix}.IsReady");
            configure ??= Svc.PluginInterface.GetIpcSubscriber<string, Vector4, Vector4, object?>($"{Prefix}.Configure");
            configureOptions ??= Svc.PluginInterface.GetIpcSubscriber<string, bool, bool, object?>($"{Prefix}.ConfigureOptions");
            targetRing ??= Svc.PluginInterface.GetIpcSubscriber<string, string, Vector3, float, Vector4, int, object?>($"{Prefix}.TargetRing");
            rear ??= Svc.PluginInterface.GetIpcSubscriber<string, string, Vector3, float, float, Vector4, int, object?>($"{Prefix}.Rear");
            flank ??= Svc.PluginInterface.GetIpcSubscriber<string, string, Vector3, float, float, Vector4, int, object?>($"{Prefix}.Flank");
            clean ??= Svc.PluginInterface.GetIpcSubscriber<string, object?>($"{Prefix}.Clean");

            if (isReady.InvokeFunc())
            {
                if (!loggedConnected)
                {
                    loggedConnected = true;
                    PluginLog.Information("[VPR-mDraw] IPC连接成功(首次)");
                }
                return true;
            }
            PluginLog.Debug("[VPR-mDraw] IsReady=false, mDraw未就绪");
            return false;
        }
        catch (Exception ex)
        {
            PluginLog.Debug($"[VPR-mDraw] 连接失败: {ex.Message}");
            return false;
        }
    }

    private void Suppress(Exception ex)
    {
        suppressUntil = Environment.TickCount64 + 3000;
        PluginLog.Warning($"[VPR-mDraw] 绘制异常, 暂停3s: {ex.Message}");
    }
}

internal static class ViperMDrawColors
{
    public static Vector4 TargetRing = new(0.15f, 0.80f, 1.00f, 0.95f);
    public static Vector4 CorrectPosition = new(0.15f, 1.00f, 0.35f, 0.95f);
    public static Vector4 WrongPosition = new(1.00f, 0.15f, 0.08f, 0.95f);
    public static Vector4 InactivePosition = new(0.75f, 0.90f, 1.00f, 0.42f);
    public static Vector4 Fill = new(0.15f, 1.00f, 0.35f, 0.95f);
    public static bool ShowPositionGuide = true;
    public static bool ReverseFill;
}
