using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;

namespace ApiHelper
{
    /// <summary>
    /// 普墨旋转框架的静态API帮助类，为ACR脚本提供简洁的中文调用接口。
    /// 所有属性/方法均使用中文命名，方便理解和编写战斗脚本。
    /// </summary>
    public static class ApiHelper
    {
        #region 玩家相关

        /// <summary>获取当前玩家对象（战斗角色）</summary>
        public static IBattleChara? 玩家 => Core.Me;

        /// <summary>玩家当前血量百分比，范围0-100，死亡时为0</summary>
        public static float 玩家的血量 => 玩家 != null ? PartyHelper.GetHpPercent(玩家) : 0;

        /// <summary>玩家当前蓝量/资源百分比，范围0-100</summary>
        public static float 玩家的蓝量 => 玩家 != null ? PartyHelper.GetMpPercent(玩家) : 0;

        /// <summary>玩家当前所在世界坐标</summary>
        public static Vector3 玩家的位置 => 玩家?.Position ?? Vector3.Zero;

        /// <summary>玩家是否处于战斗状态（进入战斗后为true）</summary>
        public static bool 战斗中 => Svc.Condition[ConditionFlag.InCombat];

        /// <summary>玩家是否存活（未死亡）</summary>
        public static bool 活着 => 玩家 != null && !玩家.IsDead;

        /// <summary>玩家当前等级（1-100）</summary>
        public static uint 玩家的等级 => 玩家?.Level ?? 0;

        /// <summary>玩家当前职业的RowId（用于职业判断）</summary>
        public static uint 玩家的职业 => 玩家?.ClassJob.RowId ?? 0;

        #endregion

        #region 目标相关

        /// <summary>获取当前选中的目标，自动转换为战斗角色类型</summary>
        public static IBattleChara? 目标 => Core.Target as IBattleChara;

        /// <summary>目标的血量百分比，无目标时返回0</summary>
        public static float 目标血量 => 目标 != null ? PartyHelper.GetHpPercent(目标) : 0;

        /// <summary>目标是否存活且未死亡</summary>
        public static bool 目标活着 => 目标 != null && !目标.IsDead;

        /// <summary>目标是否可攻击（战斗NPC且可被选中）</summary>
        public static bool 可攻击
        {
            get
            {
                if (目标 == null || 目标.IsDead) return false;
                return (目标.ObjectKind == ObjectKind.BattleNpc || 目标.ObjectKind == ObjectKind.EventNpc)
                       && 目标.IsTargetable;
            }
        }

        /// <summary>目标到玩家的净距离（米），已扣除双方碰撞箱半径，通常用于近战攻击距离判断</summary>
        public static float 目标距离
        {
            get
            {
                if (目标 == null || 玩家 == null) return 999;
                return Vector3.Distance(玩家的位置, 目标.Position) - (玩家.HitboxRadius + 目标.HitboxRadius);
            }
        }

        /// <summary>获取目标相对于玩家的身位方向（正面/侧面/背面），用于判断是否打中身位</summary>
        public static Positional 目标身位 => TargetHelper.GetTargetPositional();

        /// <summary>目标是否有身位判定要求（如六合星导等技能）</summary>
        public static bool 目标有身位
        {
            get
            {
                if (目标 == null) return false;
                return TargetHelper.HasPositionalRequirement(目标);
            }
        }

        /// <summary>手动选中/切换目标</summary>
        /// <param name="目标">要选中的游戏对象</param>
        public static void 选中(IGameObject? 目标)
        {
            if (目标 != null) Svc.Targets.Target = 目标;
        }

        /// <summary>清除当前选中的目标</summary>
        public static void 取消目标() => Svc.Targets.Target = null;

        #endregion

        #region 技能系统

        /// <summary>技能是否可用（已解锁、冷却结束、有足够资源）</summary>
        public static bool 技能可用(uint 技能ID) => ActionHelper.IsReady(技能ID);

        /// <summary>技能是否已通过学习解锁（不含等级条件是否满足）</summary>
        public static bool 技能已解锁(uint 技能ID) => ActionHelper.IsUnlocked(技能ID);

        /// <summary>技能当前剩余冷却时间（秒），未冷却返回0</summary>
        public static float 技能冷却(uint 技能ID) => ActionHelper.GetActionCooldown(技能ID);

        /// <summary>技能是否正在冷却中</summary>
        public static bool 技能冷却中(uint 技能ID) => 技能冷却(技能ID) > 0;

        /// <summary>技能当前充能层数（如忍者的风魔手里剑）</summary>
        public static float 技能充能(uint 技能ID) => ActionHelper.GetActionCharges(技能ID);

        /// <summary>技能最大充能层数</summary>
        public static int 技能最大充能(uint 技能ID) => ActionHelper.GetMaxCharges(技能ID);

        /// <summary>充能是否已满（可用于判断是否应该消耗充能防止溢出）</summary>
        public static bool 充能已满(uint 技能ID) => 技能充能(技能ID) >= 技能最大充能(技能ID);

        /// <summary>获取调整后的技能ID（用于连击变化，如战技连击会自动变为下一段）</summary>
        public static uint 调整技能ID(uint 技能ID) => ActionHelper.GetAdjustedActionId(技能ID);

        /// <summary>获取上一个连击的技能ID（用于判断连击进度）</summary>
        public static uint 上个连击 => ActionHelper.GetLastComboID();

        /// <summary>技能是否高亮（可发动状态，通常用于UI闪烁提示）</summary>
        public static bool 技能高亮(uint 技能ID) => ActionHelper.IsActionHighlighted(技能ID);

        /// <summary>获取技能类型（GCD/能力技）</summary>
        public static ActionType 技能类型(uint 技能ID) => ActionHelper.ResolveActionType(技能ID);

        /// <summary>创建GCD技能动作（默认对当前目标施放）</summary>
        public static PAction GCD技能(uint 技能ID) => new(技能ID, ActionType.Gcd, ActionTargetType.Target);

        /// <summary>创建指定类型的技能动作（对当前目标）</summary>
        public static PAction 技能(uint 技能ID, ActionType 类型) => new(技能ID, 类型, ActionTargetType.Target);

        /// <summary>创建对指定目标的GCD技能动作，自动识别目标类型（自己/队友/敌人）</summary>
        public static PAction 对目标技能(uint 技能ID, IBattleChara? 目标)
        {
            if (目标 == null) return new PAction(技能ID, ActionType.Gcd, ActionTargetType.Target);
            if (目标 == 玩家) return new PAction(技能ID, ActionType.Gcd, ActionTargetType.Self);
            var party = PartyHelper.GetParty();
            for (int i = 0; i < party.Count; i++)
                if (party[i] == 目标) return new PAction(技能ID, ActionType.Gcd, PartyHelper.GetTargetTypeByIndex(i));
            return new PAction(技能ID, ActionType.Gcd, ActionTargetType.Target);
        }

        /// <summary>创建对指定目标的能力技动作，自动识别目标类型</summary>
        public static PAction 对目标能力(uint 技能ID, IBattleChara? 目标)
        {
            if (目标 == null) return new PAction(技能ID, ActionType.OffGcd, ActionTargetType.Target);
            if (目标 == 玩家) return new PAction(技能ID, ActionType.OffGcd, ActionTargetType.Self);
            var party = PartyHelper.GetParty();
            for (int i = 0; i < party.Count; i++)
                if (party[i] == 目标) return new PAction(技能ID, ActionType.OffGcd, PartyHelper.GetTargetTypeByIndex(i));
            return new PAction(技能ID, ActionType.OffGcd, ActionTargetType.Target);
        }

        /// <summary>创建对自己施放的技能动作（默认为能力技）</summary>
        public static PAction 对自己技能(uint 技能ID, ActionType 类型 = ActionType.OffGcd)
            => new(技能ID, 类型, ActionTargetType.Self);

        /// <summary>在地面指定位置施放地面目标技能（如黑魔纹、神圣领域等）</summary>
        public static void 放地面技能(uint 技能ID, Vector3 位置)
            => ActionHelper.UseActionLocation(技能ID, 0, 位置);

        /// <summary>在指定目标脚下施放地面目标技能</summary>
        public static void 放地面技能(uint 技能ID, IBattleChara 目标)
        {
            if (目标 == null) return;
            ActionHelper.UseActionLocation(技能ID, 目标.GameObjectId, 目标.Position);
        }

        /// <summary>记录技能已使用（用于防重复和冷却追踪）</summary>
        public static void 记录技能(uint 技能ID) => ActionHelper.RecordAction(技能ID);

        /// <summary>判断技能是否在指定毫秒内使用过（防止短时间内重复使用）</summary>
        public static bool 最近用过(uint 技能ID, int 毫秒) => ActionHelper.RecentlyUsed(技能ID, 毫秒);

        #endregion

        #region GCD 系统

        /// <summary>GCD（公共冷却）总时长（秒），受技能速度影响</summary>
        public static float GCD总时长 => ActionHelper.GetGcdTotal();

        /// <summary>GCD从开始到现在的已过时间（秒）</summary>
        public static float GCD已过 => ActionHelper.GetGcdElapsed();

        /// <summary>GCD剩余冷却时间（秒），0表示已经转好</summary>
        public static float GCD剩余 => ActionHelper.GetGcdRemain();

        /// <summary>GCD是否处于激活状态（正在冷却中）</summary>
        public static bool GCD激活 => ActionHelper.GetGcdIsActive();

        /// <summary>是否可以使用GCD技能（剩余冷却小于0.1秒）</summary>
        public static bool 可用GCD => GCD剩余 <= 0.1f;

        /// <summary>是否可以使用能力技（通常GCD剩余小于0.5秒即可插入能力技）</summary>
        public static bool 可用能力技 => GCD剩余 <= 0.5f;

        /// <summary>当前动画锁定时间（秒），某些技能施放后会有动画锁定期间无法使用其他技能</summary>
        public static float 动画锁定 => ActionHelper.GetAnimationLock();

        /// <summary>是否处于动画锁定中</summary>
        public static bool 动画锁定中 => 动画锁定 > 0;

        #endregion

        #region 咏唱系统

        /// <summary>玩家是否正在读条施法</summary>
        public static bool 读条中 => 玩家?.IsCasting ?? false;

        /// <summary>当前读条的总时长（秒）</summary>
        public static float 读条总时长 => ActionHelper.GetCastTimeTotal();

        /// <summary>当前读条已进行的时间（秒）</summary>
        public static float 读条已过 => ActionHelper.GetCastTimeElapsed();

        /// <summary>当前读条剩余时间（秒）</summary>
        public static float 读条剩余 => ActionHelper.GetCastTimeRemain();

        /// <summary>连击判定剩余有效时间（秒），超过此时间连击会中断</summary>
        public static float 连击剩余 => ActionHelper.GetComboLeftTime();

        #endregion

        #region 小队相关

        /// <summary>所有队友列表（包括自己，按小队UI顺序排列）</summary>
        public static List<IBattleChara> 所有队友 => PartyHelper.GetParty();

        /// <summary>所有队友（不包括自己）</summary>
        public static List<IBattleChara> 队友们
        {
            get
            {
                var list = new List<IBattleChara>();
                foreach (var x in 所有队友) if (x != 玩家) list.Add(x);
                return list;
            }
        }

        /// <summary>存活的队友（不包括自己）</summary>
        public static List<IBattleChara> 活队友
        {
            get
            {
                var list = new List<IBattleChara>();
                foreach (var x in 队友们) if (!x.IsDead) list.Add(x);
                return list;
            }
        }

        /// <summary>获取指定范围内（净距离）的存活队友</summary>
        public static List<IBattleChara> 范围内队友(float 范围)
        {
            var result = new List<IBattleChara>();
            if (玩家 == null) return result;
            foreach (var x in 活队友)
            {
                var dist = Vector3.Distance(玩家的位置, x.Position) - (玩家.HitboxRadius + x.HitboxRadius);
                if (dist <= 范围) result.Add(x);
            }
            return result;
        }

        /// <summary>30米内的存活队友（常用治疗/支援技能范围）</summary>
        public static List<IBattleChara> 三十米内 => 范围内队友(30);
        /// <summary>20米内的存活队友</summary>
        public static List<IBattleChara> 二十米内 => 范围内队友(20);
        /// <summary>10米内的存活队友</summary>
        public static List<IBattleChara> 十米内 => 范围内队友(10);

        /// <summary>获取指定队友的血量百分比</summary>
        public static float 队友血量(IBattleChara? 队友) => 队友 != null ? PartyHelper.GetHpPercent(队友) : 0;

        /// <summary>获取指定队友的蓝量百分比</summary>
        public static float 队友蓝量(IBattleChara? 队友) => 队友 != null ? PartyHelper.GetMpPercent(队友) : 0;

        /// <summary>当前血量百分比最低的存活队友（优先治疗目标）</summary>
        public static IBattleChara? 最残血
        {
            get
            {
                var list = 活队友;
                if (list.Count == 0) return null;
                IBattleChara? result = null;
                float lowest = float.MaxValue;
                foreach (var x in list) { var hp = PartyHelper.GetHpPercent(x); if (hp < lowest) { lowest = hp; result = x; } }
                return result;
            }
        }

        /// <summary>按小队索引获取队友（0=自己，1=小队第二位成员）</summary>
        public static IBattleChara? 队友(int 索引) => PartyHelper.GetPartyMember(索引);

        /// <summary>获取指定索引队友的目标类型枚举（用于PAction目标定位）</summary>
        public static ActionTargetType 队友目标类型(int 索引) => PartyHelper.GetTargetTypeByIndex(索引);

        /// <summary>查找带有指定状态（Buff/DEBUFF）的队友，可选择是否包含自己</summary>
        public static IBattleChara? 找带状态的队友(uint 状态ID, bool 包含自己 = true)
            => PartyHelper.FindPartyMemberCharWithStatus(状态ID, 包含自己);

        #endregion

        #region 职业量谱 - 坦克

        /// <summary>暗黑骑士（DRK）职业量谱</summary>
        public static class DRK { public static byte 暗血 => JobGaugeHelper.DRK.Blood; public static bool 暗技 => JobGaugeHelper.DRK.HasDarkArts; public static float 暗黑剩余 => JobGaugeHelper.DRK.DarksideTimeRemaining; public static float 弗雷剩余 => JobGaugeHelper.DRK.ShadowTimeRemaining; }
        /// <summary>战士（WAR）职业量谱</summary>
        public static class WAR { public static byte 兽魂 => JobGaugeHelper.WAR.Beast; }
        /// <summary>骑士（PLD）职业量谱</summary>
        public static class PLD { public static byte 忠义 => JobGaugeHelper.PLD.Oath; }
        /// <summary>绝枪战士（GNB）职业量谱</summary>
        public static class GNB { public static byte 子弹 => JobGaugeHelper.GNB.Ammo; public static byte 连击阶段 => JobGaugeHelper.GNB.AmmoComboStep; }

        #endregion

        #region 职业量谱 - 治疗

        /// <summary>白魔法师（WHM）职业量谱</summary>
        public static class WHM { public static byte 百合 => JobGaugeHelper.WHM.Lily; public static byte 血百合 => JobGaugeHelper.WHM.BloodLily; public static short 百合计时 => JobGaugeHelper.WHM.LilyTimer; }
        /// <summary>学者（SCH）职业量谱</summary>
        public static class SCH { public static short 豆子 => JobGaugeHelper.SCH.AetherflowStack; public static short 炽天剩余 => JobGaugeHelper.SCH.SeraphTimer; public static short 妖精能量 => JobGaugeHelper.SCH.FairyGauge; }
        /// <summary>贤者（SGE）职业量谱</summary>
        public static class SGE { public static byte 蓝豆 => JobGaugeHelper.SGE.Addersgall; public static short 蓝豆计时 => JobGaugeHelper.SGE.AddersgallTimer; public static byte 红豆 => JobGaugeHelper.SGE.Addersting; public static bool 均衡 => JobGaugeHelper.SGE.Eukrasia; }

        #endregion

        #region 职业量谱 - 近战

        /// <summary>武士（SAM）职业量谱</summary>
        public static class SAM { public static byte 剑气 => JobGaugeHelper.SAM.剑气; public static byte 剑压 => JobGaugeHelper.SAM.剑压; public static bool 雪 => JobGaugeHelper.SAM.HasYuki; public static bool 月 => JobGaugeHelper.SAM.HasMoon; public static bool 花 => JobGaugeHelper.SAM.HasHana; public static int 闪数量 => JobGaugeHelper.SAM.GetSenCount(); }
        /// <summary>忍者（NIN）职业量谱</summary>
        public static class NIN { public static byte 风魔 => JobGaugeHelper.NIN.Kazematoi; public static byte 忍气 => JobGaugeHelper.NIN.Ninki; }
        /// <summary>龙骑士（DRG）职业量谱</summary>
        public static class DRG { public static byte 龙眼 => JobGaugeHelper.DRG.FirstmindsFocusCount; public static bool 红龙血 => JobGaugeHelper.DRG.IsLOTDActive; public static float 红龙剩余 => JobGaugeHelper.DRG.LOTDTimer; }
        /// <summary>武僧（MNK）职业量谱</summary>
        public static class MNK { public static byte 脉轮 => JobGaugeHelper.MNK.Chakra; public static float 必杀剩余 => JobGaugeHelper.MNK.BlitzTimeRemaining; }
        /// <summary>钐镰客（RPR）职业量谱</summary>
        public static class RPR { public static byte 灵魂 => JobGaugeHelper.RPR.灵魂值; public static byte 魂衣 => JobGaugeHelper.RPR.魂衣值; public static ushort 夜游剩余 => JobGaugeHelper.RPR.夜游魂衣剩余时间; public static byte 夜游魂 => JobGaugeHelper.RPR.夜游魂; public static byte 虚无魂 => JobGaugeHelper.RPR.虚无魂; }
        /// <summary>蝰蛇剑士（VPR）职业量谱</summary>
        public static class VPR { public static byte 飞蛇 => JobGaugeHelper.VPR.飞蛇之魂层数; public static byte 灵力 => JobGaugeHelper.VPR.灵力值; public static byte 祖灵 => JobGaugeHelper.VPR.祖灵力档数; }

        #endregion

        #region 职业量谱 - 远程

        /// <summary>诗人（BRD）职业量谱</summary>
        public static class BRD { public static Dalamud.Game.ClientState.JobGauge.Enums.Song 当前歌曲 => JobGaugeHelper.BRD.GetCurrentSong; public static ushort 歌曲剩余 => JobGaugeHelper.BRD.GetCurrentSongTimer; public static byte 灵魂之声 => JobGaugeHelper.BRD.GetSoulVoice; public static byte 诗心 => JobGaugeHelper.BRD.GetRepertoire; }
        /// <summary>机工士（MCH）职业量谱</summary>
        public static class MCH { public static byte 热量 => JobGaugeHelper.MCH.Heat; public static byte 电池 => JobGaugeHelper.MCH.Battery; public static bool 机器人激活 => JobGaugeHelper.MCH.IsRobotActive; }
        /// <summary>舞者（DNC）职业量谱</summary>
        public static class DNC { public static bool 跳舞中 => JobGaugeHelper.DNC.IsDancing; public static byte 伶俐 => JobGaugeHelper.DNC.Esprit; public static byte 幻扇 => JobGaugeHelper.DNC.Feathers; public static uint 下一步 => JobGaugeHelper.DNC.NextStep; public static uint 已完成步数 => JobGaugeHelper.DNC.CompletedSteps; }
        /// <summary>召唤师（SMN）职业量谱</summary>
        public static class SMN { public static bool 有宠物 => JobGaugeHelper.SMN.HasPet; public static byte 豆子 => JobGaugeHelper.SMN.AetherflowStacks; public static bool 巴哈姆特 => JobGaugeHelper.SMN.IsBahamutReady; public static bool 凤凰 => JobGaugeHelper.SMN.IsPhoenixReady; }
        /// <summary>赤魔法师（RDM）职业量谱</summary>
        public static class RDM { public static byte 黑魔元 => JobGaugeHelper.RDM.BlackMana; public static byte 白魔元 => JobGaugeHelper.RDM.WhiteMana; public static byte 魔元集 => JobGaugeHelper.RDM.ManaStacks; }
        /// <summary>绘灵法师（PCT）职业量谱</summary>
        public static class PCT { public static byte 颜料 => JobGaugeHelper.PCT.PalleteGauge; public static byte 豆子 => JobGaugeHelper.PCT.Paint; public static bool 生物画 => JobGaugeHelper.PCT.CreatureMotifDrawn; public static bool 武器画 => JobGaugeHelper.PCT.WeaponMotifDrawn; public static bool 风景画 => JobGaugeHelper.PCT.LandscapeMotifDrawn; public static bool 莫古炮 => JobGaugeHelper.PCT.MooglePortraitReady; public static bool 马蒂恩 => JobGaugeHelper.PCT.MadeenPortraitReady; }

        #endregion

        #region 状态/Buff 相关

        /// <summary>检查指定单位是否拥有指定状态（Buff/DEBUFF）</summary>
        public static bool 有状态(IBattleChara? 单位, uint 状态ID) => 单位 != null && StatusHelper.HasStatus(单位, 状态ID);

        /// <summary>获取指定单位上指定状态的层数（如增伤、易伤等）</summary>
        public static int 状态层数(IBattleChara? 单位, uint 状态ID) => 单位 == null ? 0 : StatusHelper.GetStatusStack(单位, 状态ID);

        /// <summary>获取指定单位上指定状态的剩余时间（秒）</summary>
        public static float 状态剩余(IBattleChara? 单位, uint 状态ID) => 单位 == null ? 0 : StatusHelper.GetStatusLeftTime(单位, 状态ID);

        /// <summary>自己是否拥有指定状态</summary>
        public static bool 玩家有状态(uint 状态ID) => 有状态(玩家, 状态ID);

        /// <summary>目标是否拥有指定状态</summary>
        public static bool 目标有状态(uint 状态ID) => 有状态(目标, 状态ID);

        #endregion

        #region 移动相关

        /// <summary>玩家是否正在移动中</summary>
        public static bool 移动中 => MoveManager.IsLocalPlayerMoving;

        /// <summary>获取鼠标指针指向的世界坐标位置（用于地面技能定位）</summary>
        public static Vector3 鼠标位置 => PosHelper.ScreenToWorld();

        /// <summary>传送玩家到指定世界坐标（需认证权限）</summary>
        public static void 传送(Vector3 位置) => HackHelper.TeleportNormal(位置);

        /// <summary>传送玩家到鼠标指向的世界坐标位置（需认证权限）</summary>
        public static void 传送到鼠标() => HackHelper.TeleportToMouse();

        #endregion

        #region 目标查找

        /// <summary>获取玩家周围指定范围内可攻击敌人的数量</summary>
        public static uint 周围敌人(float 范围) => TargetHelper.EnemyInRange(范围);

        /// <summary>玩家周围5米内的敌人数量（常用近战AoE判断）</summary>
        public static uint 五米敌人 => TargetHelper.EnemyIn5m();

        /// <summary>获取指定目标周围指定范围内的敌人数量</summary>
        public static uint 目标周围敌人(IBattleChara? 目标, float 范围) => 目标 == null ? 0 : TargetHelper.EnemyInRangeTarget(目标, 范围);

        /// <summary>获取扇形范围内敌人数量（用于判断扇形AoE技能收益）</summary>
        public static int 扇形敌人(IBattleChara 自身, IBattleChara 目标, float 扇形半径, float 扇形角度)
            => TargetHelper.GetEnemyCountInsideSector(自身, 目标, 扇形半径, 扇形角度);

        /// <summary>寻找最佳的AoE目标（能命中最多敌人的位置）</summary>
        public static IBattleChara? 最佳AoE目标(uint 技能ID, int 最少目标数, float 扇形角度)
            => TargetHelper.GetMostCanTargetObjects(技能ID, 最少目标数, 扇形角度);

        /// <summary>获取攻击距离（含扩程修正，用于判断技能是否可命中）</summary>
        public static float 攻击距离(float 基础距离) => GameData.GetCurrentAttackRange(基础距离);

        /// <summary>切换当前选中的目标</summary>
        public static void 切换目标(IBattleChara? t) => Core.SetTarget(t);

        /// <summary>按BaseId查找一个NPC（常用于特定怪物识别）</summary>
        public static IBattleChara? 找NPC(uint BaseId) => TargetHelper.FindNpcByBaseId(BaseId);

        /// <summary>按BaseId查找所有匹配的NPC（列表）</summary>
        public static List<IBattleChara> 找所有NPC(uint BaseId) => TargetHelper.FindNpcsByBaseId(BaseId);

        #endregion

        #region 物品相关

        /// <summary>获取背包中指定物品的数量，可选择是否计算高品质版本</summary>
        public static uint 物品数量(uint 物品ID, bool 高品质 = false) => ItemHelper.GetItemCountInInventory(物品ID, 高品质);

        /// <summary>判断背包中是否有指定物品</summary>
        public static bool 有物品(uint 物品ID, bool 高品质 = false) => 物品数量(物品ID, 高品质) > 0;

        /// <summary>获取当前等级/职业可用的最佳爆发药ID（自动选择最高品质）</summary>
        public static uint 最佳爆发药 => GameData.GetBestPotionId();

        #endregion

        #region 极限技

        /// <summary>当前极限技（LB）充能段数（0-3段）</summary>
        public static uint LB段数 => LimitBreakHelper.GetLimitBreakCharge();

        /// <summary>当前可用的极限技技能ID</summary>
        public static uint LB技能ID => LimitBreakHelper.GetLimitBreakActionId();

        /// <summary>判断极限技是否可用（充能完成且未满）</summary>
        public static bool LB可用 => LB段数 < 999 && LB段数 >= 0;

        #endregion

        #region UI相关

        /// <summary>让指定技能栏图标产生脉冲闪烁效果（提醒玩家使用）</summary>
        public static void 脉冲技能(uint 技能ID) => UiHelper.PulseActionBar(技能ID);

        /// <summary>在屏幕中央显示提示信息（Toast通知）</summary>
        public static void 屏幕提示(string 文本, float 秒数, HintHelper.HintType 类型 = HintHelper.HintType.Info)
            => HintHelper.ShowToast2(文本, 秒数, 类型);

        #endregion

        #region 日志相关

        /// <summary>输出调试级别日志（仅开发调试时显示）</summary>
        public static void 调试(string 消息) => Svc.Log.Debug(消息);
        /// <summary>输出信息级别日志</summary>
        public static void 信息(string 消息) => Svc.Log.Info(消息);
        /// <summary>输出警告级别日志</summary>
        public static void 警告日志(string 消息) => Svc.Log.Warning(消息);
        /// <summary>输出错误级别日志</summary>
        public static void 错误(string 消息) => Svc.Log.Error(消息);

        #endregion

        #region 数学工具

        /// <summary>计算两个二维向量之间的夹角（弧度）</summary>
        public static double 角度(Vector2 点1, Vector2 点2) => MathHelper.θVector2(点1, 点2);
        /// <summary>将角度标准化到 -180° ~ 180° 范围内</summary>
        public static double 标准化角度(double 角度) => MathHelper.NormalizeAngle(角度);
        /// <summary>对三维向量的各分量进行四舍五入到指定位数</summary>
        public static Vector3 取整(Vector3 向量, int 位数) => MathHelper.Round(向量, 位数);

        #endregion

        #region QT 设置相关

        /// <summary>获取指定的QT开关状态（用户自定义配置开关）</summary>
        public static bool 获取QT(string qtKey) => PromeSettings.Instance.GetQt(qtKey);

        /// <summary>设置指定的QT开关状态</summary>
        public static void 设置QT(string qtKey, bool 值) => PromeSettings.Instance.SetQt(qtKey, 值);

        /// <summary>切换指定的QT开关状态（取反）</summary>
        public static void 切换QT状态(string qtKey)
        {
            var 当前值 = PromeSettings.Instance.GetQt(qtKey);
            PromeSettings.Instance.SetQt(qtKey, !当前值);
        }

        /// <summary>批量设置QT开关状态（使用枚举字典）</summary>
        public static void 批量设置QT状态<T>(Dictionary<T, bool> 配置表) where T : Enum
        {
            foreach (var kvp in 配置表)
                PromeSettings.Instance.SetQt(kvp.Key.ToString(), kvp.Value);
        }

        /// <summary>注册一个新的QT开关（仅在ACR构造函数中调用，用于定义配置项）</summary>
        public static void 添加QT(string qtKey, bool 默认值) => PromeSettings.Instance.AddQt(qtKey, 默认值);

        /// <summary>起手爆发是否已执行（框架状态读写，用于控制爆发循环只执行一次）</summary>
        public static bool 起手已执行
        {
            get => PromeSettings.Instance.OpenerHasBeenExecuted;
            set => PromeSettings.Instance.OpenerHasBeenExecuted = value;
        }

        #endregion

        #region 起手 / Timeline 相关

        /// <summary>获取当前PureTimeline选择的起手名称</summary>
        public static string? 时间轴起手名称 => PromeRotation.PureTimeline.PtlManager.CurrentOpener;

        /// <summary>获取Timeline元数据中定义的起手名称</summary>
        public static string? 元数据起手名称 => PromeRotation.Timeline.TimelineManager.CurrentMeta?.Opener;

        /// <summary>按职业ID获取所有已注册的起手模板字典（名称->类型）</summary>
        public static IReadOnlyDictionary<string, Type>? 获取起手列表(int 职业ID) => RotationManager.GetOpenersByJob(职业ID);

        #endregion

        #region Debug 状态

        /// <summary>清除GCD求解器的调试状态记录</summary>
        public static void 清除GCD调试状态() => RotationManager.GcdSolverStatus.Clear();

        /// <summary>清除oGCD求解器的调试状态记录</summary>
        public static void 清除oGCD调试状态() => RotationManager.OffGcdSolverStatus.Clear();

        /// <summary>向GCD求解器调试状态添加一条记录（用于调试循环决策）</summary>
        public static void 添加GCD调试状态(string name, bool success, string message)
            => RotationManager.GcdSolverStatus.Add(new PromeRotation.Managers.SolverStatus { Name = name, Success = success, Message = message });

        /// <summary>向oGCD求解器调试状态添加一条记录（用于调试循环决策）</summary>
        public static void 添加oGCD调试状态(string name, bool success, string message)
            => RotationManager.OffGcdSolverStatus.Add(new PromeRotation.Managers.SolverStatus { Name = name, Success = success, Message = message });

        #endregion
    }

    /// <summary>
    /// API扩展方法类，为游戏对象（ICharacter/IGameObject）提供便捷的中文方法调用。
    /// </summary>
    public static class ApiExtensions
    {
        /// <summary>获取角色的血量百分比（扩展方法）</summary>
        public static float 血量(this ICharacter? 角色) => 角色 is IBattleChara battle ? PartyHelper.GetHpPercent(battle) : 0;

        /// <summary>获取角色的蓝量百分比（扩展方法）</summary>
        public static float 蓝量(this ICharacter? 角色) => 角色 is IBattleChara battle ? PartyHelper.GetMpPercent(battle) : 0;

        /// <summary>判断角色是否存活（扩展方法）</summary>
        public static bool 活着(this ICharacter? 角色) => 角色 != null && !角色.IsDead;

        /// <summary>判断角色是否死亡（扩展方法）</summary>
        public static bool 死了(this ICharacter? 角色) => 角色 == null || 角色.IsDead;

        /// <summary>计算游戏对象到玩家的净距离（扣除碰撞箱）</summary>
        public static float 距玩家(this IGameObject? 对象)
        {
            if (对象 == null || ApiHelper.玩家 == null) return 999;
            return Vector3.Distance(ApiHelper.玩家的位置, 对象.Position)
                   - (ApiHelper.玩家.HitboxRadius + 对象.HitboxRadius);
        }

        /// <summary>判断游戏对象是否在玩家指定范围内（扩展方法）</summary>
        public static bool 在玩家范围内(this IGameObject? 对象, float 范围) => 对象.距玩家() <= 范围;

        /// <summary>判断游戏对象是否为可攻击的敌人（扩展方法）</summary>
        public static bool 是敌人(this IGameObject? 对象)
            => 对象 != null && (对象.ObjectKind == ObjectKind.BattleNpc || 对象.ObjectKind == ObjectKind.EventNpc);
    }
}