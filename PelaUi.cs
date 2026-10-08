using System.Numerics;
using Dalamud.Interface.Textures;
using Lumina.Excel.Sheets;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using PromeRotation.Data;
using PromeRotation.Managers;
using A = ApiHelper.ApiHelper;

namespace Jinyu;

internal static class PelaUi
{
    private static bool attached, visible = true, expanded, qtVisible = true, hotkeyVisible = true;
    private static int page;
    private static double transitionStart;
    private static bool nativeSettingsVisible, nativeQtVisible;
    private static readonly string PortraitPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncherCN", "pluginConfigs", "PromeRotation", "ACR", "Jinyu", "assets", "pela-reference.png");
    private static readonly HashSet<int> pressed = new();
    private static readonly Dictionary<uint,float> cooldownSpans=new();
    private static readonly (string Name, uint Id, ActionType Type, ActionTargetType Target)[] buttons =
    {
        ("真北",7546,ActionType.OffGcd,ActionTargetType.Self), ("牵制",7549,ActionType.OffGcd,ActionTargetType.Target),
        ("内丹",7541,ActionType.OffGcd,ActionTargetType.Self), ("浴血",7542,ActionType.OffGcd,ActionTargetType.Self),
        ("亲疏自行",7548,ActionType.OffGcd,ActionTargetType.Self), ("蛇行",34646,ActionType.OffGcd,ActionTargetType.Target),
        ("飞蛇之尾",34633,ActionType.Gcd,ActionTargetType.Target), ("祖灵降临",34626,ActionType.Gcd,ActionTargetType.Self)
    };
    public static void Attach()
    {
        if (attached) return;
        var plugin=PromeRotation.Plugin.Instance;
        nativeSettingsVisible=plugin.SettingsWindow.IsOpen;
        nativeQtVisible=plugin.QTWindow.IsOpen;
        Svc.PluginInterface.UiBuilder.Draw += Draw;
        attached = true;
    }
    public static void Detach()
    {
        if (!attached) return;
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
        attached = false;
        PromeRotation.Plugin.Instance.SettingsWindow.IsOpen=nativeSettingsVisible;
        PromeRotation.Plugin.Instance.QTWindow.IsOpen=nativeQtVisible;
        pressed.Clear();
    }
    public static void Open() { Attach(); visible = true; }
    private static void Queue(int index) { var b=buttons[index]; HotkeyQueueManager.TryEnqueue(new(b.Id,b.Type,b.Target){RequiresVerification=true}); }
    private static void Draw()
    {
        // These are the combat overlays. MainWindow is the plugin itself and must remain accessible.
        PromeRotation.Plugin.Instance.SettingsWindow.IsOpen=false;
        PromeRotation.Plugin.Instance.QTWindow.IsOpen=false;
        var alpha=Math.Clamp(JinyuSettings.Current.BackgroundOpacity,0,1);
        ImGui.PushStyleColor(ImGuiCol.Text,new Vector4(.96f,.98f,1,1));
        ImGui.PushStyleColor(ImGuiCol.TextDisabled,new Vector4(.72f,.8f,.87f,1));
        ImGui.PushStyleColor(ImGuiCol.WindowBg,new Vector4(.09f,.13f,.19f,alpha));
        ImGui.PushStyleColor(ImGuiCol.ChildBg,Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.Button,new Vector4(.65f,.74f,.82f,alpha*.32f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered,new Vector4(.4f,.75f,.95f,.55f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,new Vector4(.25f,.65f,.8f,.75f));
        ImGui.PushStyleColor(ImGuiCol.FrameBg,new Vector4(.55f,.68f,.78f,alpha*.35f));
        ImGui.PushStyleColor(ImGuiCol.Header,new Vector4(.25f,.65f,.8f,.5f));
        ImGui.PushStyleColor(ImGuiCol.Border,new Vector4(.8f,.88f,.95f,.3f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding,2f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize,0f);
        try
        {
            DrawPanel(false); DrawPanel(true);
            if(!visible)return;
            ImGui.PushStyleColor(ImGuiCol.WindowBg,new Vector4(.055f,.09f,.13f,.96f));
            ImGui.SetNextWindowSizeConstraints(new Vector2(expanded?895:430,50),new Vector2(1200,1000));
            if(ImGui.Begin("佩拉 · 系统菜单###JinyuPela",ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.NoCollapse))
            {
                var running=PromeSettings.Instance.EnableAcr==AcrState.On;
                if(ImGui.Button(running?"运行中 · 点击暂停":"已暂停 · 点击运行",new Vector2(180,34)))
                    PromeSettings.Instance.EnableAcr=running?AcrState.Off:AcrState.On;
                ImGui.SameLine();
                if(ImGui.Button(expanded?"收起菜单":"系统菜单",new Vector2(110,34))) {expanded=!expanded;transitionStart=ImGui.GetTime();}
                ImGui.SameLine();if(ImGui.Button("QT"))qtVisible=!qtVisible;
                ImGui.SameLine();if(ImGui.Button("Hotkey"))hotkeyVisible=!hotkeyVisible;
                if(expanded)SystemMenu();
            }
            ImGui.End();
            ImGui.PopStyleColor();
        }
        finally {ImGui.PopStyleVar(2);ImGui.PopStyleColor(10);}
    }
    private static void SystemMenu()
    {
        Header();
        var origin=ImGui.GetCursorScreenPos();
        var labels=new[]{"战斗设置","爆发药品","界面设置"};
        if(ImGui.BeginChild("Navigation",new Vector2(64,360)))
        {
            for(var i=0;i<labels.Length;i++)
            {
                var p=ImGui.GetCursorScreenPos();
                if(ImGui.InvisibleButton("##nav"+i,new Vector2(54,54))) {page=i;transitionStart=ImGui.GetTime();}
                var d=ImGui.GetWindowDrawList();var center=p+new Vector2(27,27);
                d.AddRectFilled(p,p+new Vector2(54,54),page==i?C(.18f,.47f,.61f,.55f):C(.1f,.2f,.29f,.4f),5);
                d.AddRect(p,p+new Vector2(54,54),page==i?C(.45f,.85f,1,.9f):C(.4f,.65f,.76f,.45f),5,ImDrawFlags.None,1.2f);
                if(i==0)
                {
                    d.AddLine(center+new Vector2(-12,12),center+new Vector2(12,-12),C(.55f,.9f,1),3);
                    d.AddLine(center+new Vector2(-10,2),center+new Vector2(-2,10),C(.55f,.9f,1),2);
                }
                if(i==1)
                {
                    d.AddRect(center+new Vector2(-9,-5),center+new Vector2(9,13),C(.55f,.9f,1),2,ImDrawFlags.None,2);
                    d.AddLine(center+new Vector2(-5,-5),center+new Vector2(-5,-11),C(.55f,.9f,1),2);
                    d.AddLine(center+new Vector2(-5,-11),center+new Vector2(5,-11),C(.55f,.9f,1),2);
                    d.AddLine(center+new Vector2(5,-11),center+new Vector2(5,-5),C(.55f,.9f,1),2);
                }
                if(i==2)Crystal(d,center,13,0,C(.55f,.9f,1));
                if(ImGui.IsItemHovered())ImGui.SetTooltip(labels[i]);
                ImGui.Dummy(new Vector2(1,10));
            }
        }
        ImGui.EndChild();ImGui.SameLine();
        Portrait();ImGui.SameLine();
        var slide=JinyuSettings.Current.Animate?Math.Clamp((float)(ImGui.GetTime()-transitionStart)*5,0,1):1;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX()+(1-slide)*12);
        var panel=ImGui.GetCursorScreenPos();
        var d2=ImGui.GetWindowDrawList();
        d2.AddRectFilled(panel,panel+new Vector2(550,360),C(.025f,.075f,.12f,.95f),8);
        d2.AddRect(panel,panel+new Vector2(550,360),C(.4f,.8f,.95f,.65f),8,ImDrawFlags.None,1.2f);
        // Silver-blue frame with small gold corners and an ice crest, like the reference.
        d2.AddLine(panel+new Vector2(1,20),panel+new Vector2(1,1),C(.86f,.76f,.43f),2);
        d2.AddLine(panel+new Vector2(1,1),panel+new Vector2(25,1),C(.86f,.76f,.43f),2);
        d2.AddLine(panel+new Vector2(525,359),panel+new Vector2(549,359),C(.86f,.76f,.43f),2);
        if(ImGui.BeginChild("Content",new Vector2(550,360)))
        {
            ImGui.SetCursorPos(new Vector2(16,14));
            ImGui.TextColored(new Vector4(.55f,.88f,1,1),labels[page]);
            ImGui.Separator();
            ImGui.SetCursorPosX(16);
            ImGui.PushItemWidth(280);
            if(page==0)JinyuRotation.DrawConfiguration();
            else if(page==1)Potions();
            else
            {
                var s=JinyuSettings.Current;
                var transparency=1-Math.Clamp(s.BackgroundOpacity,0,1);
                if(ImGui.SliderFloat("QT / Hotkey 背景透明度",ref transparency,0,1,"%.2f")) {s.BackgroundOpacity=1-transparency;s.Save();}
                ImGui.TextWrapped("仅影响QT与Hotkey；主设置面板使用固定背景。");
                var animate=s.Animate;
                if(ImGui.Checkbox("面板动画 / 冰晶 / 扫描效果",ref animate)){s.Animate=animate;s.Save();}
                if(JinyuSettings.SaveError!=null)ImGui.TextWrapped("保存失败："+JinyuSettings.SaveError);
            }
            ImGui.PopItemWidth();
        }
        ImGui.EndChild();
        ImGui.Spacing();
        ImGui.TextDisabled(page==0?"起手与战斗阈值 · 修改自动保存":page==1?"药品选择与使用条件 · 修改自动保存":"窗口透明度与动态效果 · 修改自动保存");
    }
    private static void Portrait()
    {
        var pos=ImGui.GetCursorScreenPos();
        var size=new Vector2(245,360);
        var d=ImGui.GetWindowDrawList();
        if(File.Exists(PortraitPath))
        {
            var image=Svc.Texture.GetFromFile(PortraitPath).GetWrapOrEmpty();
            if(image.Width>0&&image.Height>0)
                d.AddImage(image.Handle,pos+new Vector2(4,4),pos+size-new Vector2(4,4));
        }
        d.AddRect(pos,pos+size,C(.4f,.8f,.95f,.8f),7,ImDrawFlags.None,1.4f);
        d.AddLine(pos+new Vector2(0,22),pos,C(.88f,.78f,.46f),2);
        d.AddLine(pos,pos+new Vector2(24,0),C(.88f,.78f,.46f),2);
        d.AddLine(pos+size-new Vector2(24,0),pos+size,C(.88f,.78f,.46f),2);
        if(JinyuSettings.Current.Animate)
        {
            var t=(float)ImGui.GetTime();
            for(var i=0;i<6;i++)
            {
                var particle=pos+new Vector2(10+i*39,12+(t*14+i*51)%330);
                d.AddCircleFilled(particle,1.4f,C(.6f,.9f,1,.35f+.2f*MathF.Sin(t+i)));
            }
            Crystal(d,pos+new Vector2(size.X/2,12),8,t*.15f,C(.65f,.9f,1,.8f));
        }
        ImGui.Dummy(size);
    }
    private static void Potions()
    {
        var s=JinyuSettings.Current;
        var enabled=s.Potion;if(ImGui.Checkbox("自动使用爆发药",ref enabled)){s.Potion=enabled;s.Save();}
        var hq=s.PreferHq;if(ImGui.Checkbox("优先 HQ",ref hq)){s.PreferHq=hq;s.Save();}
        var preview=s.PotionId==0?"自动最优（背包中的最高品级巧力药）":PotionCatalog.SelectedName;
        if(ImGui.BeginCombo("药品",preview))
        {
            if(ImGui.Selectable("自动最优",s.PotionId==0)){s.PotionId=0;s.Save();}
            foreach(var p in PotionCatalog.Items)
                if(ImGui.Selectable($"{p.Name} · NQ {A.物品数量(p.Id)} / HQ {A.物品数量(p.Id,true)}##{p.Id}",s.PotionId==p.Id)){s.PotionId=p.Id;s.Save();}
            ImGui.EndCombo();
        }
        ImGui.TextWrapped($"当前药品：{PotionCatalog.SelectedName}。自动使用时等待爆发窗口与双附体资源。指定药品不足时不自动替换。");
        if(ImGui.Button("手动排入爆发药"))
        {
            var id=PotionCatalog.SelectedId;
            if(id!=0 && (A.有物品(id)||A.有物品(id,true)))
                HotkeyQueueManager.TryEnqueue(new(s.PreferHq&&A.有物品(id,true)?id+1000000:A.有物品(id)?id:id+1000000,ActionType.Item,ActionTargetType.Self));
        }
    }
    private static void CooldownOverlay(uint actionId, Vector2 pos)
    {
        var cd=A.技能冷却(actionId);
        var charges=A.技能充能(actionId);
        if(cd<=0) {cooldownSpans.Remove(actionId);return;}
        var d=ImGui.GetWindowDrawList();
        var text=cd>0?cd.ToString("0.0"):"0";
        d.AddRectFilled(pos,pos+new Vector2(46,46),C(.02f,.04f,.08f,.58f),3);
        d.AddText(pos+new Vector2(4,15),C(1,1,1,.95f),text);
        var max=Math.Max(cd,cooldownSpans.GetValueOrDefault(actionId,cd));
        cooldownSpans[actionId]=max;
        var progress=Math.Clamp(cd/max,0,1);
        d.AddRect(pos+new Vector2(2,42),pos+new Vector2(44,44),C(.25f,.75f,.95f,.8f),1);
        d.AddRectFilled(pos+new Vector2(2,42),pos+new Vector2(2+42*(1-progress),44),C(.8f,.9f,1,.9f),1);
        if(charges>=1)
            d.AddText(pos+new Vector2(32,1),C(.65f,1,.7f),((int)charges).ToString());
    }
    private static void DrawPanel(bool hotkey)
    {
        ref var show=ref (hotkey?ref hotkeyVisible:ref qtVisible);
        if(!show)return;
        ImGui.SetNextWindowSize(new Vector2(490,145),ImGuiCond.FirstUseEver);
        if(ImGui.Begin(hotkey?"佩拉 · 战术指令###JinyuHotkey":"佩拉 · 战术开关###JinyuQT",ref show,ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
        {
            var columns=Math.Max(1,(int)(ImGui.GetContentRegionAvail().X/(hotkey?54:108)));
            var count=hotkey?buttons.Length+2:JinyuRotation.QtList.Count;
            for(var i=0;i<count;i++)
            {
                if(i%columns!=0)ImGui.SameLine();
                if(hotkey&&i==buttons.Length+1)
                {
                    var pos=ImGui.GetCursorScreenPos();
                    if(ImGui.InvisibleButton("##clearQueue",new Vector2(46,46)))ActionQueueManager.ClearAllQueues();
                    var d=ImGui.GetWindowDrawList();
                    d.AddLine(pos+new Vector2(31,8),pos+new Vector2(19,26),C(.8f,.9f,1),3);
                    d.AddQuadFilled(pos+new Vector2(16,22),pos+new Vector2(24,28),pos+new Vector2(21,38),pos+new Vector2(7,29),C(.87f,.75f,.43f));
                    for(var k=0;k<4;k++)d.AddLine(pos+new Vector2(16+k*2,26+k),pos+new Vector2(9+k*3,31+k),C(.3f,.4f,.5f),1);
                    if(ImGui.IsItemHovered())ImGui.SetTooltip("清空技能队列");
                    continue;
                }
                var lb=hotkey&&i==buttons.Length;
                var name=hotkey?(lb?"极限技":buttons[i].Name):JinyuRotation.QtList.ElementAt(i).Key;
                var active=!hotkey&&A.获取QT(name);
                ImGui.PushStyleColor(ImGuiCol.Button,active?new Vector4(.28f,.40f,.67f,1):new Vector4(.08f,.12f,.22f,1));
                if(hotkey)
                {
                    var actionId=lb?A.LB技能ID:buttons[i].Id;
                    var iconId=Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRow(actionId==0?200u:actionId).Icon;
                    var icon=Svc.Texture.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrEmpty();
                    var pos=ImGui.GetCursorScreenPos();
                    if(ImGui.InvisibleButton("##skill"+i,new Vector2(46,46)))
                    {
                        if(!lb)Queue(i);
                        else if(actionId!=0)HotkeyQueueManager.TryEnqueue(new(actionId,ActionType.Gcd,ActionTargetType.Target){RequiresVerification=true});
                    }
                    if(icon.Width>0)ImGui.GetWindowDrawList().AddImage(icon.Handle,pos,pos+new Vector2(46,46));
                    else ImGui.GetWindowDrawList().AddRect(pos,pos+new Vector2(46,46),C(.5f,.8f,1));
                    if(!lb&&A.玩家!=null)CooldownOverlay(actionId,pos);
                    if(ImGui.IsItemHovered())ImGui.SetTooltip(name+(lb&&actionId==0?" · 当前未充能":" · 点击加入技能队列"));
                }
                else if(ImGui.Button(name,new Vector2(100,38)))A.切换QT状态(name);
                if(ImGui.IsItemHovered()&&JinyuSettings.Current.Animate)
                {
                    var d=ImGui.GetWindowDrawList();var min=ImGui.GetItemRectMin();var max=ImGui.GetItemRectMax();
                    d.AddRect(min,max,C(.55f,.85f,1,.7f),5,ImDrawFlags.None,1.5f);
                    var center=(min+max)/2;var radius=10+3*MathF.Sin((float)ImGui.GetTime()*3);
                    d.AddCircle(center,radius,C(.65f,.9f,1,.3f),24,1);
                }
                ImGui.PopStyleColor();
            }
        }
        ImGui.End();
    }    private static uint C(float r,float g,float b,float a=1)=>ImGui.ColorConvertFloat4ToU32(new(r,g,b,a));
    private static void Glasses(ImDrawListPtr d,Vector2 p,uint color)
    {
        d.AddCircle(p+new Vector2(-10,0),9,color,24,1.7f);
        d.AddCircle(p+new Vector2(10,0),9,color,24,1.7f);
        d.AddLine(p+new Vector2(-1,-2),p+new Vector2(1,-2),color,2);
        d.AddLine(p+new Vector2(-19,-2),p+new Vector2(-24,-6),color,1.7f);
        d.AddLine(p+new Vector2(19,-2),p+new Vector2(24,-6),color,1.7f);
    }
    private static void Crystal(ImDrawListPtr d,Vector2 p,float radius,float rotation,uint color)
    {
        for(var j=0;j<6;j++)
        {
            var a=j*MathF.Tau/6+rotation;var v=new Vector2(MathF.Cos(a),MathF.Sin(a));var n=new Vector2(-v.Y,v.X);
            d.AddLine(p,p+v*radius,color,1.2f);
            d.AddLine(p+v*radius*.6f,p+v*radius*.35f+n*radius*.22f,color,1);
            d.AddLine(p+v*radius*.6f,p+v*radius*.35f-n*radius*.22f,color,1);
        }
    }
    private static void Header()
    {
        var d=ImGui.GetWindowDrawList();var p=ImGui.GetCursorScreenPos();var width=ImGui.GetContentRegionAvail().X;
        var t=JinyuSettings.Current.Animate?(float)ImGui.GetTime():0;
        var valid=A.玩家!=null&&A.玩家的职业==41;
        var card=p+new Vector2(6,8);
        d.AddRectFilled(card,card+new Vector2(width-12,106),C(.025f,.075f,.12f,.95f),7);
        d.AddRect(card,card+new Vector2(width-12,106),C(.35f,.75f,.9f,.6f),7,ImDrawFlags.None,1.2f);
        var hp=A.玩家的血量/100f;
        d.AddText(card+new Vector2(16,12),C(.9f,.96f,1),$"HP {A.玩家的血量:F0}%");
        d.AddRectFilled(card+new Vector2(16,34),card+new Vector2(220,40),C(.5f,.65f,.75f,.2f),3);
        d.AddRectFilled(card+new Vector2(16,34),card+new Vector2(16+204*Math.Clamp(hp,0,1),40),C(.35f,.8f,1),3);
        d.AddText(card+new Vector2(16,52),C(.55f,.86f,1),valid?$"灵力 {A.VPR.灵力}  /  飞蛇 {A.VPR.飞蛇}  /  祖灵 {A.VPR.祖灵}":"等待蝰蛇角色");
        var g=valid?ViperLogic.Gcd():0;var o=valid?ViperLogic.Ogcd():0;
        string Name(uint id)=>id==0?"—":Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRow(id).Name.ToString();
        d.AddText(card+new Vector2(260,12),C(.9f,.96f,1),valid?$"GCD {A.GCD剩余:F2}s  /  团辅 {BurstAlignment.RaidRemaining:F1}s":"GCD —  /  团辅 —");
        d.AddText(card+new Vector2(260,40),C(.68f,.86f,.95f),"下一 GCD · "+Name(g));
        d.AddText(card+new Vector2(260,66),C(.68f,.86f,.95f),"下一能力 · "+Name(o));
        d.AddText(card+new Vector2(16,78),C(.84f,.74f,.46f),"战斗时长 "+BurstAlignment.Elapsed.ToString("F1")+"s");
        var y=p.Y+128;
        d.AddLine(new Vector2(p.X,y),new Vector2(p.X+width,y),C(.72f,.66f,.41f,.6f),1);
        var emblem=new Vector2(p.X+width/2,y);
        Glasses(d,emblem,C(.6f,.87f,1));
        for(var i=0;i<2;i++)Crystal(d,new Vector2(p.X+width-25-i*30,p.Y+114),7,t*.15f,C(.55f,.85f,1,.6f));
        ImGui.Dummy(new Vector2(width,150));
    }
}
