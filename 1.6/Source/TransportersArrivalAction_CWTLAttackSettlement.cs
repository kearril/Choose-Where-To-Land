using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static RimWorld.Reward_Pawn;

namespace ChooseWhereToLand
{
    public class TransportersArrivalAction_CWTLAttackSettlement : TransportersArrivalAction
    {
        private Settlement settlement;


        private static readonly PawnsArrivalModeDef fixedArrivalMode =
            DefDatabase<PawnsArrivalModeDef>.GetNamed("CWTL_ChooseWhereToLand", true);


        public PawnsArrivalModeDef ArrivalMode => fixedArrivalMode;

        // 攻击定居点时会生成地图，所以这里返回 true
        public override bool GeneratesMap => true;

        public TransportersArrivalAction_CWTLAttackSettlement() { }

        public TransportersArrivalAction_CWTLAttackSettlement(Settlement settlement)
        {
            this.settlement = settlement;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // 保存/加载目标定居点
            Scribe_References.Look(ref settlement, "settlement");
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            // 基础合法性检测（例如运输舱是否还存在）
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(pods, destinationTile);
            if (!floatMenuAcceptanceReport)
            {
                return floatMenuAcceptanceReport;
            }

            // 检查目标是否发生了变化
            if (settlement != null && settlement.Tile != destinationTile)
            {
                return false;
            }

            // 执行自定义的“能否攻击”逻辑
            return CanAttack(pods, settlement);
        }

        public override bool ShouldUseLongEvent(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {
            // 如果定居点没有地图，则需要生成地图 属于长事件
            return !settlement.HasMap;
        }


        // 判定是否能攻击该定居点

        public static FloatMenuAcceptanceReport CanAttack(IEnumerable<IThingHolder> pods, Settlement settlement)
        {
            // 无效或不可攻击的目标
            if (settlement == null || !settlement.Spawned || !settlement.Attackable)
            {
                return false;
            }

            // 必须有未倒地的殖民者
            if (!TransportersArrivalActionUtility.AnyNonDownedColonist(pods))
            {
                return false;
            }

            // 检查是否处于冷却中
            if (settlement.EnterCooldownBlocksEntering())
            {
                return FloatMenuAcceptanceReport.WithFailReasonAndMessage(
                    "EnterCooldownBlocksEntering".Translate(),
                    "MessageEnterCooldownBlocksEntering".Translate(settlement.EnterCooldownTicksLeft().ToStringTicksToPeriod()));
            }

            return true;
        }

        /// <summary>
        /// 执行抵达逻辑
        /// </summary>
        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {

            Thing lookTarget = TransportersArrivalActionUtility.GetLookTarget(transporters);

            // 是否首次生成地图
            bool isFirstTimeGenerate = !settlement.HasMap;

            // 获取或生成地图
            Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(settlement.Tile, null);


            TaggedString letterLabel = "LetterLabelCaravanEnteredEnemyBase".Translate();
            TaggedString letterText = "LetterTransportPodsLandedInEnemyBase".Translate(settlement.Label).CapitalizeFirst();

            // 调整派系关系（被攻击时会恶化）
            SettlementUtility.AffectRelationsOnAttacked(settlement, ref letterText);

            // 如果是首次生成地图，触发相关逻辑（敌对地图、亲属发现提示等）
            if (isFirstTimeGenerate)
            {
                Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
                PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(
                    orGenerateMap.mapPawns.AllPawns,
                    ref letterLabel,
                    ref letterText,
                    "LetterRelatedPawnsInMapWherePlayerLanded".Translate(Faction.OfPlayer.def.pawnsPlural),
                    informEvenIfSeenBefore: true);
            }


            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NeutralEvent, lookTarget, settlement.Faction);

            // 设置当前地图为目标地图
            Current.Game.CurrentMap = orGenerateMap;

            // 隐藏世界地图 → 镜头切换到战场
            CameraJumper.TryHideWorld();
            CameraJumper.TryJump(orGenerateMap.Center, orGenerateMap);

            // 使用自定义抵达模式的 Worker 处理运输舱的落点逻辑
            fixedArrivalMode.Worker.TravellingTransportersArrived(transporters, orGenerateMap);
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Action<PlanetTile, TransportersArrivalAction> launchAction, IEnumerable<IThingHolder> pods, Settlement settlement)
        {
            foreach (FloatMenuOption floatMenuOption in TransportersArrivalActionUtility.GetFloatMenuOptions(() => CanAttack(pods, settlement), () => new TransportersArrivalAction_CWTLAttackSettlement(settlement), "CWTL_AttackSettlement".Translate(settlement.Label), launchAction, settlement.Tile))
            {
                yield return floatMenuOption;
            }

        }
    }

}
