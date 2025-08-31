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
    // 抵达任务点的自定义降落，允许选择落点并生成地图  
    public class TransportersArrivalAction_ChooseSpotAndLand : TransportersArrivalAction
    {

        public RimWorld.Planet.Site site;

        // 固定抵达模式
        private static readonly PawnsArrivalModeDef fixedArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetNamed("CWTL_ChooseWhereToLand", true);


        public PawnsArrivalModeDef ArrivalMode => fixedArrivalMode;


        public override bool GeneratesMap => true;


        public TransportersArrivalAction_ChooseSpotAndLand()
        { }


        public TransportersArrivalAction_ChooseSpotAndLand(RimWorld.Planet.Site site)
        {
            this.site = site;
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref site, "site");
        }


        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {

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

            return CanVisit(pods, site);
        }

        // 判断是否需要长时间事件（如生成地图），Site 尚无地图则返回 true
        public override bool ShouldUseLongEvent(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {
            return !site.HasMap;
        }

        //抵达逻辑
        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {

            Thing lookTarget = TransportersArrivalActionUtility.GetLookTarget(transporters);


            bool isNewMap = !site.HasMap;

            // 获取或生成 Site 对应的地图
            Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile, site.PreferredMapSize, null);

            // 发送消息相关
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

            // 弹出抵达消息
            Messages.Message("MessageShuttleArrived".Translate(), lookTarget, MessageTypeDefOf.TaskCompletion);

            // 切换当前地图
            Current.Game.CurrentMap = orGenerateMap;

            // 隐藏世界地图
            CameraJumper.TryHideWorld();
            // 镜头跳转至地图中心
            CameraJumper.TryJump(orGenerateMap.Center, orGenerateMap);

            // 执行落点逻辑
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
