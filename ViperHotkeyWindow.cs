using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using PromeRotation.Helpers;
using PromeRotation.Managers;

namespace JinyuViper;

public class ViperHotkeyWindow : Window
{
    public record HotkeyItem(string Name, uint ActionId, Action OnClick);

    private readonly List<HotkeyItem> _items = new();
    public int Columns { get; }
    public float ButtonSize { get; }
    public float Spacing { get; }
    public float CdFontScale { get; set; } = 2.0f;
    public int ItemCount => _items.Count;

    public ViperHotkeyWindow(int columns, float buttonSize, float spacing, string title)
        : base(title, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse, false)
    {
        Columns = Math.Max(1, columns);
        ButtonSize = buttonSize;
        Spacing = spacing;
        RespectCloseHotkey = false;
        IsOpen = false;  // 默认隐藏，由HotkeyManager根据职业控制显示
    }

    public void AddItem(string name, uint actionId, Action onClick)
    {
        _items.Add(new HotkeyItem(name, actionId, onClick));
    }

    public void ClearItems() => _items.Clear();

    public override void Draw()
    {
        // 每帧检测当前ACR职业，非Viper自动隐藏
        try
        {
            var reg = RotationManager.GetCurrentRegistration();
            if (reg?.JobId != 41) { IsOpen = false; return; }
        }
        catch { IsOpen = false; return; }

        if (_items.Count == 0) return;

        // 窗口拖拽
        if (ImGui.IsWindowHovered() && !ImGui.IsAnyItemHovered() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            var io = ImGui.GetIO();
            ImGui.SetWindowPos(ImGui.GetWindowPos() + io.MouseDelta, ImGuiCond.Always);
        }

        int cols = Math.Min(_items.Count, Columns);
        int rows = (int)Math.Ceiling((double)_items.Count / Columns);
        float w = ButtonSize * cols + Spacing * (cols - 1);
        float h = ButtonSize * rows + Spacing * (rows - 1);
        ImGui.SetWindowSize(new Vector2(w + 20f, h + 20f), ImGuiCond.Always);

        var dl = ImGui.GetWindowDrawList();
        var wp = ImGui.GetWindowPos();
        var ws = ImGui.GetWindowSize();
        dl.AddRectFilled(wp, wp + ws, ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 0.45f)), 8f);

        var size = new Vector2(ButtonSize, ButtonSize);

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            int col = i % cols;
            int row = i / cols;
            float x = wp.X + 10f + col * (ButtonSize + Spacing);
            float y = wp.Y + 10f + row * (ButtonSize + Spacing);
            var pos = new Vector2(x, y);

            // 1. 绘制图标（独立try）
            bool iconOk = false;
            if (item.ActionId != 0)
            {
                try { var icon = item.ActionId.GetActionIcon(); if (icon != null) { dl.AddImage(icon.Handle, pos, pos + size); iconOk = true; } } catch { }
            }

            // 2. 图标失败时绘制文字兜底
            if (!iconOk)
            {
                try
                {
                    dl.AddRectFilled(pos, pos + size, ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.5f, 0.8f)), 4f);
                    var ts = ImGui.CalcTextSize(item.Name);
                    if (ts.X > ButtonSize - 4f) ts *= (ButtonSize - 4f) / ts.X;
                    var tp = pos + (size - ts) * 0.5f;
                    dl.AddText(tp + new Vector2(1, 1), 4278190080, item.Name);
                    dl.AddText(tp, uint.MaxValue, item.Name);
                }
                catch { }
            }

            // 3. CD遮罩（独立try）
            if (item.ActionId != 0)
            {
                try
                {
                    float cd = ActionHelper.GetActionCooldown(item.ActionId);
                    float recast = ActionHelper.GetActionRecastTime(item.ActionId);
                    if (cd > 0f && recast > 0.001f)
                    {
                        int maxCharges = ActionHelper.GetMaxCharges(item.ActionId);
                        float cdNorm = cd, recastNorm = recast;
                        if (maxCharges > 1) { cdNorm /= maxCharges; recastNorm /= maxCharges; }
                        float progress = Math.Clamp(cdNorm / recastNorm, 0f, 1f);
                        DrawCooldown(dl, pos, size, progress, cd);
                    }
                    // 充能数显示
                    float charges = ActionHelper.GetActionCharges(item.ActionId);
                    int maxC = ActionHelper.GetMaxCharges(item.ActionId);
                    if (maxC > 1 && charges > 0f && charges < (float)maxC)
                    {
                        string txt = ((int)charges).ToString();
                        var ts = ImGui.CalcTextSize(txt);
                        var tp = pos + new Vector2(size.X - ts.X - 2f, size.Y - ts.Y - 1f);
                        dl.AddText(tp + new Vector2(1, 1), 4278190080, txt);
                        dl.AddText(tp, uint.MaxValue, txt);
                    }
                }
                catch { }
            }

            // 4. 点击区域（独立try）
            try
            {
                ImGui.SetCursorScreenPos(pos);
                if (ImGui.InvisibleButton($"##vpr_hk_{i}_{item.Name}", size))
                {
                    try { item.OnClick?.Invoke(); } catch { }
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(item.Name);
            }
            catch { }
        }
    }

    private static void DrawCooldown(ImDrawListPtr dl, Vector2 pos, Vector2 size, float progress, float cd)
    {
        try
        {
            dl.AddRectFilled(pos, pos + size, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.55f)), 4f);
            if (cd > 0.05f)
            {
                string text = ((int)Math.Ceiling(cd)).ToString();
                var font = ImGui.GetFont();
                float fontSize = ImGui.GetFontSize() * 2.0f;
                var ts = ImGui.CalcTextSize(text) * 2.0f;
                var center = (pos + pos + size) * 0.5f;
                var tp = center - ts * 0.5f;
                dl.AddText(font, fontSize, tp + new Vector2(1, 1), 4278190080, text);
                dl.AddText(font, fontSize, tp, uint.MaxValue, text);
            }
        }
        catch { }
    }
}
