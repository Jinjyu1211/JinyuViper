using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using ECommons.Logging;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;

namespace JinyuViper;

internal static class ViperSettingsUI
{
    private static readonly Vector4 HeaderColor = new(0.16f, 0.47f, 0.82f, 1f);
    private static readonly Vector4 SuccessGreen = new(0.2f, 0.8f, 0.2f, 1f);
    private static readonly Vector4 ErrorRed = new(1f, 0.35f, 0.3f, 1f);
    private static readonly Vector4 WarningYellow = new(1f, 0.8f, 0.2f, 1f);
    private static readonly Vector4 MutedText = new(0.6f, 0.6f, 0.6f, 1f);
    private static readonly Vector4 LinkBlue = new(0.4f, 0.7f, 1f, 1f);

    public static void Draw()
    {
        // 固定字体缩放，防止随窗口/DPI放大
        float origFontScale = ImGui.GetIO().FontGlobalScale;
        ImGui.GetIO().FontGlobalScale = 1.0f;
        try
        {
        if (ImGui.BeginTabBar("VPR_Settings"))
        {
            if (ImGui.BeginTabItem("战斗设置"))
            {
                DrawCombatSettings();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("起手"))
            {
                DrawOpenerSettings();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("快捷键"))
            {
                DrawHotkeySettings();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("调试信息"))
            {
                DrawDebugInfo();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("身位绘制"))
            {
                DrawMDrawSettings();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        ImGui.Spacing();
        ImGui.Separator();
        if (ImGui.Button("保存设置", new Vector2(120, 0))) ViperConfig.Save();
        ImGui.SameLine();
        ImGui.TextDisabled("保存后下次加载时生效");
        }
        finally
        {
        // 恢复原始字体缩放
        ImGui.GetIO().FontGlobalScale = origFontScale;
        }
    }

    // ==================== 战斗设置 ====================

    private static void DrawCombatSettings()
    {
        if (!ImGui.BeginChild("CombatScroll", Vector2.Zero, false, ImGuiWindowFlags.None))
        {
            ImGui.EndChild();
            return;
        }

        SectionHeader("快捷预设");
        for (int i = 0; i < VPRApi.PresetNames.Length; i++)
        {
            if (i > 0) ImGui.SameLine();
            var label = VPRApi.PresetNames[i];
            var col = i switch
            {
                0 => new Vector4(0.2f, 0.6f, 1f, 1f),   // 高难模式 - 蓝
                1 => new Vector4(0.2f, 1f, 0.5f, 1f),   // 日随模式 - 绿
                2 => new Vector4(1f, 0.6f, 0.2f, 1f),   // 倾泻模式 - 橙
                _ => new Vector4(0.8f, 0.8f, 0.8f, 1f), // 全开调试 - 灰
            };
            ImGui.PushStyleColor(ImGuiCol.Button, col);
            if (ImGui.Button(label))
            {
                VPRApi.ApplyPreset(i);
                PluginLog.Information($"[VPR] 已应用预设: {label}");
            }
            ImGui.PopStyleColor();
        }
        Tooltip("一键批量设置多个QT开关，日随/高难模式切换在此处");

        ImGui.Spacing();

        SectionHeader("自动能力技阈值（仅日随模式生效）");
        float 内丹阈值 = ViperConfig.Current.内丹血量阈值;
        ImGui.SetNextItemWidth(80f);
        if (ImGui.InputFloat("##内丹", ref 内丹阈值, 1f, 5f, "%.0f"))
            ViperConfig.Current.内丹血量阈值 = Math.Clamp(内丹阈值, 1f, 100f);
        ImGui.SameLine(); ImGui.Text("内丹触发血量%");
        Tooltip("自身血量低于此值时自动使用内丹（仅日随模式生效）");

        float 浴血阈值 = ViperConfig.Current.浴血血量阈值;
        ImGui.SetNextItemWidth(80f);
        if (ImGui.InputFloat("##浴血", ref 浴血阈值, 1f, 5f, "%.0f"))
            ViperConfig.Current.浴血血量阈值 = Math.Clamp(浴血阈值, 1f, 100f);
        ImGui.SameLine(); ImGui.Text("浴血触发血量%");
        Tooltip("自身血量低于此值时自动使用浴血（仅日随模式生效）");

        ImGui.Spacing();

        SectionHeader("真北设置（仅日随模式生效）");
        int 真北GCD = ViperConfig.Current.真北GCD百分比;
        ImGui.SetNextItemWidth(80f);
        if (ImGui.InputInt("##真北", ref 真北GCD))
            ViperConfig.Current.真北GCD百分比 = Math.Clamp(真北GCD, 0, 100);
        ImGui.SameLine(); ImGui.Text("真北GCD进度%");
        Tooltip("GCD进度超过此百分比时才允许使用真北，避免卡GCD（0=不限制）");

        ImGui.EndChild();
    }

    // ==================== 起手 ====================

    private static void DrawOpenerSettings()
    {
        bool 日随 = JinyuViperRotation.IsDailyMode;
        ImGui.TextColored(日随 ? SuccessGreen : WarningYellow,
            日随 ? ">> 当前: 日随模式（无起手，有什么打什么）" : ">> 当前: 高难模式（含120对齐+起手爆发）");
        ImGui.Spacing();

        var names = JinyuViperRotation.Openers.Keys.ToList();
        var idx = Math.Clamp(JinyuViperRotation.SelectedOpenerIndex, 0, names.Count - 1);
        if (ImGui.Combo("起手方式", ref idx, [.. names]))
        {
            JinyuViperRotation.SelectedOpenerIndex = idx;
            PluginLog.Information($"[VPR] 切换起手为: {names[idx]}");
        }
        Tooltip("标准爆发: 11步完整起手 | 齿剑快速: 12步拿急速 | 空白: 直接主循环");
    }

    // ==================== 快捷键 ====================

    private static void DrawHotkeySettings()
    {
        var visible = ViperConfig.HotkeyVisible;
        ImGui.BeginDisabled();
        ImGui.Checkbox("显示快捷键面板", ref visible);
        ImGui.EndDisabled();
        Tooltip(visible ? "当前Viper职业，面板已自动显示" : "非Viper职业，面板已自动隐藏");

        var cols = ViperConfig.HotkeyColumns;
        ImGui.SetNextItemWidth(80f);
        if (ImGui.InputInt("##Cols", ref cols, 1, 1))
        {
            cols = Math.Max(1, Math.Min(10, cols));
            ViperConfig.SetHotkeyColumns(cols);
            ViperHotkeyManager.Rebuild();
        }
        ImGui.SameLine(); ImGui.Text("每行图标数");
        Tooltip("范围 1~10，调整快捷键面板每行显示的技能数量");
    }

    // ==================== 调试信息（带滚动） ====================

    private static void DrawDebugInfo()
    {
        if (!ImGui.BeginChild("DebugScroll", Vector2.Zero, false, ImGuiWindowFlags.None))
        {
            ImGui.EndChild();
            return;
        }

        try
        {
            // 战斗状态
            SectionHeader("战斗状态");
            DebugLabel("连击剩余", $"{VPRApi.连击剩余ms():F0} ms");
            DebugLabel("飞蛇层数", VPRApi.飞蛇层数().ToString());
            DebugLabel("灵力值", VPRApi.灵力值().ToString());
            DebugLabel("祖灵力档数", VPRApi.祖灵力档数().ToString());
            DebugLabel("蛇剑连阶段", VPRApi.蛇剑连阶段().ToString());
            DebugLabel("续剑阶段", VPRApi.续剑阶段().ToString());
            DebugLabel("等级", VPRApi.等级().ToString());
            DebugLabel("自身血量", $"{VPRApi.自身血量百分比():F1}%");
            DebugLabel("附近敌人(5m)", VPRApi.附近敌人数(5f).ToString());

            ImGui.Spacing();

            // GCD/CD
            SectionHeader("GCD / CD");
            DebugLabel("GCD剩余", $"{VPRApi.GCD剩余ms():F0} ms");
            DebugLabel("GCD总时长", $"{VPRApi.GCD总时长ms():F0} ms");
            DebugLabel("蛇灵气CD", $"{VPRApi.蛇灵气CD毫秒():F0} ms");
            DebugLabel("真北CD", $"{VPRApi.技能CD毫秒(ViperSkill.真北):F0} ms");
            DebugLabel("内丹CD", $"{VPRApi.技能CD毫秒(ViperSkill.内丹):F0} ms");
            DebugLabel("浴血CD", $"{VPRApi.技能CD毫秒(ViperSkill.浴血):F0} ms");
            DebugLabel("强碎灵蛇充能", $"{VPRApi.强碎灵蛇充能():F1} / {VPRApi.技能最大充能(ViperSkill.强碎灵蛇)}");
            DebugLabel("蛇灵气最大", VPRApi.技能最大充能(ViperSkill.蛇灵气).ToString());

            ImGui.Spacing();

            // GCD/装备
            SectionHeader("装备检测");
            float gcdMs = VPRApi.GCD总时长ms();
            float gcdSec = gcdMs / 1000f;
            bool speedOk = VPRApi.技速达标();
            if (speedOk)
                ImGui.TextColored(SuccessGreen, $"  GCD: {gcdSec:F2}s \u2713  (要求\u2264{VPRApi.要求GCD}s)");
            else
                ImGui.TextColored(ErrorRed, $"  GCD: {gcdSec:F2}s \u2717  (要求\u2264{VPRApi.要求GCD}s)");
            DebugLabel("双附体时间", $"{VPRApi.附体时间() * 2f / 1000f:F2}s");
            DebugLabel("蛇剑连时间", $"{(VPRApi.蛇剑复唱时间() + VPRApi.普攻复唱时间() * 2f) / 1000f:F2}s");

            ImGui.Spacing();

            // 目标/身位
            SectionHeader("目标 / 身位");
            DebugLabel("有目标", VPRApi.有目标().ToString());
            DebugLabel("近战范围", VPRApi.目标在近战范围().ToString());
            try { DebugLabel("目标身位", TargetHelper.GetTargetPositional().ToString()); } catch { DebugLabel("目标身位", "未知"); }

            ImGui.Spacing();

            // Buff状态
            SectionHeader("Buff 状态");
            DebugLabel("真北buff", BoolYesNo(VPRApi.有Buff(ViperBuff.真北)) + $" ({VPRApi.Buff剩余ms(ViperBuff.真北):F0}ms)");
            DebugLabel("祖灵预备", BoolYesNo(VPRApi.有Buff(ViperBuff.祖灵预备)) + $" ({VPRApi.Buff剩余ms(ViperBuff.祖灵预备):F0}ms)");
            DebugLabel("祖灵附体", BoolYesNo(VPRApi.有Buff(ViperBuff.祖灵附体)) + $" ({VPRApi.Buff剩余ms(ViperBuff.祖灵附体):F0}ms)");
            DebugLabel("团辅激活", BoolYesNo(VPRApi.团辅激活()));
            DebugLabel("爆发药状态", BoolYesNo(VPRApi.正在喝爆发药()));
            DebugLabel("急速buff", BoolYesNo(VPRApi.有Buff(ViperBuff.急速)) + $" ({VPRApi.Buff剩余ms(ViperBuff.急速):F0}ms)");
            DebugLabel("猛袭buff", BoolYesNo(VPRApi.有Buff(ViperBuff.猛袭)) + $" ({VPRApi.Buff剩余ms(ViperBuff.猛袭):F0}ms)");

            ImGui.Spacing();

            // 下一个技能
            SectionHeader("下一个技能预测");
            try { DebugLabel("Next基础GCD", SkillName(VPRApi.Next基础GCD())); } catch { DebugLabel("Next基础GCD", "-"); }
            try { DebugLabel("Next蛇剑连", SkillName(VPRApi.Next蛇剑连())); } catch { DebugLabel("Next蛇剑连", "-"); }
            try { DebugLabel("Next附体连", SkillName(VPRApi.Next附体连())); } catch { DebugLabel("Next附体连", "-"); }

            ImGui.Spacing();

            // 起手状态机
            SectionHeader("起手状态机");
            DebugLabel("激活", ViperOpenerStateMachine.IsActive.ToString());
            DebugLabel("步骤", $"{ViperOpenerStateMachine.StepIndex}/{ViperOpenerStateMachine.StepCount}");
            DebugLabel("UseOpener", JinyuViperRotation.UseOpener.ToString());
            DebugLabel("日随模式", JinyuViperRotation.IsDailyMode.ToString());

            ImGui.Spacing();

            // QT开关状态（紧凑两列）
            SectionHeader("QT 开关状态");
            ImGui.Columns(2, "QtCols", false);
            foreach (var kvp in JinyuViperRotation.QtList)
            {
                bool val = PromeSettings.Instance.GetQt(kvp.Key);
                ImGui.TextColored(val ? SuccessGreen : MutedText,
                    $"{kvp.Key}: {(val ? "\u25cf" : "\u25cb")}");
                ImGui.NextColumn();
            }
            ImGui.Columns(1);

            ImGui.Spacing();

            // Resolver状态
            SectionHeader("Resolver 决策状态");
            try
            {
                var gcdSolvers = RotationManager.GcdSolverStatus;
                var offGcdSolvers = RotationManager.OffGcdSolverStatus;

                if (gcdSolvers != null && gcdSolvers.Count > 0)
                {
                    ImGui.TextColored(HeaderColor, "GCD:");
                    foreach (var st in gcdSolvers)
                        ImGui.TextColored(st.Success ? SuccessGreen : MutedText,
                            $"  {ShortName(st.Name)}: {(st.Success ? "PASS" : "skip")} {(st.Message.Length > 30 ? st.Message[..30] : st.Message)}");
                }

                if (offGcdSolvers != null && offGcdSolvers.Count > 0)
                {
                    ImGui.TextColored(HeaderColor, "OffGCD:");
                    foreach (var st in offGcdSolvers)
                        ImGui.TextColored(st.Success ? SuccessGreen : MutedText,
                            $"  {ShortName(st.Name)}: {(st.Success ? "PASS" : "skip")} {(st.Message.Length > 30 ? st.Message[..30] : st.Message)}");
                }
            }
            catch (Exception ex)
            {
                ImGui.TextColored(ErrorRed, $"读取异常: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            ImGui.TextColored(ErrorRed, $"调试页面异常: {ex.Message}");
        }

        ImGui.EndChild();
    }

    // ==================== 身位绘制 ====================

    private static void DrawMDrawSettings()
    {
        if (!ImGui.BeginChild("MDrawScroll", Vector2.Zero, false, ImGuiWindowFlags.None))
        {
            ImGui.EndChild();
            return;
        }

        SectionHeader("mDraw 插件状态");
        bool ready = ViperPositionOverlay.IsMDrawReady;

        if (ready)
        {
            ImGui.TextColored(SuccessGreen, "  \u2713 mDraw 已连接，身位绘制正常工作");
        }
        else
        {
            ImGui.TextColored(ErrorRed, "  \u2717 mDraw 未连接，身位绘制不可用");
        }

        ImGui.Spacing();

        SectionHeader("安装步骤");
        ImGui.PushStyleColor(ImGuiCol.Text, LinkBlue);
        ImGui.TextWrapped(
            "1. 下载 mDraw 插件包 (mDraw-PR-xxx.zip)\n" +
            "2. 解压得到 mDraw.dll 文件\n" +
            "3. 将 mDraw.dll 复制到以下路径:\n");
        ImGui.PopStyleColor();

        string path = "%APPDATA%\\XIVLauncherCN\\pluginConfigs\\PromeRotation\\Plugins\\mDraw\\mDraw.dll";
        ImGui.SetNextItemWidth(ImGui.GetWindowContentRegionMax().X - ImGui.GetCursorPosX() - 10f);
        ImGui.InputText("##MdrawPath", ref path, 256, ImGuiInputTextFlags.ReadOnly);
        ImGui.TextColored(MutedText, "   即: ...PromeRotation\\Plugins\\mDraw\\mDraw.dll");

        ImGui.Spacing();
        ImGui.TextWrapped(
            "4. 重载 PromeRotation (/xlplugins)\n" +
            "5. 游戏内输入 /mdraw status 检查\n" +
            "6. 确保 QT「身位指示」已开启");

        ImGui.Spacing();

        if (!ready) ImGui.BeginDisabled();

        SectionHeader("颜色配置");
        ColorEdit("目标圈", ref ViperMDrawColors.TargetRing);
        ColorEdit("正确身位", ref ViperMDrawColors.CorrectPosition);
        ColorEdit("错误身位", ref ViperMDrawColors.WrongPosition);
        ColorEdit("非当前身位", ref ViperMDrawColors.InactivePosition);
        ColorEdit("GCD填充色", ref ViperMDrawColors.Fill);

        ImGui.Spacing();
        SectionHeader("绘制选项");
        ImGui.Checkbox("显示外圈和圆点", ref ViperMDrawColors.ShowPositionGuide);
        ImGui.Checkbox("反向填充", ref ViperMDrawColors.ReverseFill);
        ImGui.TextColored(MutedText, "正确身位 Alpha \u2265 0.65 时启用站错变色和 GCD 填充效果");

        if (!ready) ImGui.EndDisabled();

        ImGui.EndChild();
    }

    // ==================== UI 辅助方法 ====================

    private static void SectionHeader(string title)
    {
        ImGui.Spacing();
        ImGui.TextColored(HeaderColor, title);
        ImGui.Separator();
    }

    private static void DebugLabel(string label, string value)
    {
        float w = ImGui.CalcTextSize(label).X;
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label); ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(80f, 140f - w));
        ImGui.Text(value);
    }

    private static void ColorEdit(string label, ref Vector4 color)
    {
        ImGui.ColorEdit4(label, ref color, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.Float);
    }

    private static void Tooltip(string text)
    {
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(text);
    }

    private static string BoolYesNo(bool b) => b ? "是" : "否";

    private static string ShortName(string name)
    {
        // GCD_ 或 OffGCD_ 前缀去掉
        if (name.StartsWith("GCD")) return name[3..];
        if (name.StartsWith("OffGCD")) return name[5..];
        return name;
    }

    private static string SkillName(uint id)
    {
        return id switch
        {
            34606 => "钢牙", 34607 => "穿裂钢牙", 34608 => "猎牙", 34609 => "滑牙",
            34610 => "锐牙左", 34611 => "锐牙右", 34612 => "背锐牙左", 34613 => "背锐牙右",
            34620 => "强碎灵蛇", 34621 => "猛袭盘蛇", 34622 => "疾速盘蛇",
            34624 => "穿裂灵蛇", 34625 => "崩裂灵蛇",
            34626 => "祖灵降临",
            34633 => "飞蛇之尾",
            _ => id.ToString()
        };
    }
}
