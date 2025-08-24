using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;
using static RimWorld.Reward_Pawn;

namespace ChooseWhereToLand
{
    // 自定义运输舱抵达动作类   
    public class TransportersArrivalAction_ChooseSpotAndLand : TransportersArrivalAction
    {
        // 关联的任务地点 Site
        public RimWorld.Planet.Site site;

        // 固定抵达模式
        private static readonly PawnsArrivalModeDef fixedArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetNamed("CWTL_ChooseWhereToLand", true);

        // 只读属性，返回固定的抵达模式
        public PawnsArrivalModeDef ArrivalMode => fixedArrivalMode;

        //会生成地图
        public override bool GeneratesMap => true;

       
        public TransportersArrivalAction_ChooseSpotAndLand()
        { }

        // 带 Site 参数的构造函数，记录关联的 Site
        public TransportersArrivalAction_ChooseSpotAndLand(RimWorld.Planet.Site site)
        {
            this.site = site;
        }

        // 持久化数据，用于保存/加载关联的 site 引用
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref site, "site");
        }

        // 判断运输舱是否仍然有效，主要校验目标 Tile 是否匹配 Site
        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            // 调用基类校验
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(pods, destinationTile);
            if (!floatMenuAcceptanceReport)
            {
                return floatMenuAcceptanceReport;
            }
            // Site 存在但 Tile 不匹配视为无效
            if (site != null && site.Tile != destinationTile)
            {
                return false;
            }
            // 进一步检查是否允许访问
            return CanVisit(pods, site);
        }

        // 判断是否需要长时间事件（如生成地图），Site 尚无地图则返回 true
        public override bool ShouldUseLongEvent(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {
            return !site.HasMap;
        }

        // 当运输器真正抵达时调用，执行地图生成、跳转视角、发送通知等逻辑
        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            // 获取第一个运输器的观察目标（落点）
            Thing lookTarget = TransportersArrivalActionUtility.GetLookTarget(transporters);

            
            bool isNewMap = !site.HasMap;

            // 获取或生成 Site 对应的地图
            Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile, site.PreferredMapSize, null);

            // 如果新生成地图，通知游戏潜在敌对单位并发送相关消息
            if (isNewMap)
            {
                Find.TickManager.Notify_GeneratedPotentiallyHostileMap();

                PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(
                    orGenerateMap.mapPawns.AllPawns,
                    "LetterRelatedPawnsInMapWherePlayerLanded".Translate(Faction.OfPlayer.def.pawnsPlural),
                    LetterDefOf.NeutralEvent,
                    informEvenIfSeenBefore: true);
            }

            // 如果 Site 有敌对派系且视作攻击，则降低玩家好感度
            if (site.Faction != null && site.Faction != Faction.OfPlayer && site.MainSitePartDef.considerEnteringAsAttack)
            {
                Faction.OfPlayer.TryAffectGoodwillWith(
                    site.Faction,
                    Faction.OfPlayer.GoodwillToMakeHostile(site.Faction),
                    canSendMessage: true,
                    canSendHostilityLetter: true,
                    HistoryEventDefOf.AttackedSettlement);
            }

            // 弹出“穿梭机抵达”提示消息
            Messages.Message("MessageShuttleArrived".Translate(), lookTarget, MessageTypeDefOf.TaskCompletion);

            // 切换当前地图
            Current.Game.CurrentMap = orGenerateMap;

            // 隐藏世界地图
            CameraJumper.TryHideWorld();
            // 镜头跳转至地图中心
            CameraJumper.TryJump(orGenerateMap.Center, orGenerateMap);

            // 调用对应抵达模式 Worker，执行落点逻辑
            fixedArrivalMode.Worker.TravellingTransportersArrived(transporters, orGenerateMap);
        }

        // 静态方法：判断是否可以访问指定 Site
        public static FloatMenuAcceptanceReport CanVisit(IEnumerable<IThingHolder> pods, RimWorld.Planet.Site site)
        {
            if (site == null || !site.Spawned)
            {
                return false;
            }
            // 检查是否有非倒地殖民者
            if (!TransportersArrivalActionUtility.AnyNonDownedColonist(pods))
            {
                return false;
            }
            // 检查 Site 是否处于进入冷却状态
            if (site.EnterCooldownBlocksEntering())
            {
                return FloatMenuAcceptanceReport.WithFailMessage(
                    "MessageEnterCooldownBlocksEntering".Translate(site.EnterCooldownTicksLeft().ToStringTicksToPeriod()));
            }
            return true;
        }

        // 获取浮动菜单选项
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Action<PlanetTile, TransportersArrivalAction> launchAction, IEnumerable<IThingHolder> pods, RimWorld.Planet.Site site)
        {
            // 调用工具类获取菜单项
            foreach (FloatMenuOption floatMenuOption in TransportersArrivalActionUtility.GetFloatMenuOptions(
                () => CanVisit(pods, site),
                () => new TransportersArrivalAction_ChooseSpotAndLand(site),
                "CWTL_ChooseSpotAndLand".Translate(),
                launchAction,
                site.Tile,
                UIConfirmationCallback))
            {
                yield return floatMenuOption;
            }

            // UI 确认对话框回调，用于轨道层发射风险提示
            void UIConfirmationCallback(Action action)
            {
                // 如果启用 Odyssey且落点在轨道层，弹出确认对话框
                if (ModsConfig.OdysseyActive && site.Tile.LayerDef == PlanetLayerDefOf.Orbit)
                {
                    TaggedString text = "OrbitalWarning".Translate();
                    text += string.Format("\n\n{0}", "LaunchToConfirmation".Translate());
                    Find.WindowStack.Add(new Dialog_MessageBox(text, null, action, "Cancel".Translate(), delegate
                    {
                    }, null, buttonADestructive: true));
                }
                else
                {
                    action();
                }
            }
        }
    }
}
