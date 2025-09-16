using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ChooseWhereToLand
{
    // 进攻其他派系基地的TransportersArrivalAction
    public class TransportersArrivalAction_CWTLAttackSettlement : TransportersArrivalAction
    {
        private Settlement settlement;


        private static readonly PawnsArrivalModeDef fixedArrivalMode =
            DefDatabase<PawnsArrivalModeDef>.GetNamed("CWTL_ChooseWhereToLand", true);


        public PawnsArrivalModeDef ArrivalMode => fixedArrivalMode;


        public override bool GeneratesMap => true;

        public TransportersArrivalAction_CWTLAttackSettlement() { }

        public TransportersArrivalAction_CWTLAttackSettlement(Settlement settlement)
        {
            this.settlement = settlement;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_References.Look(ref settlement, "settlement");
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {

            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(pods, destinationTile);
            if (!floatMenuAcceptanceReport)
            {
                return floatMenuAcceptanceReport;
            }


            if (settlement != null && settlement.Tile != destinationTile)
            {
                return false;
            }


            return CanAttack(pods, settlement);
        }

        public override bool ShouldUseLongEvent(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {

            return !settlement.HasMap;
        }




        public static FloatMenuAcceptanceReport CanAttack(IEnumerable<IThingHolder> pods, Settlement settlement)
        {

            if (settlement == null || !settlement.Spawned || !settlement.Attackable)
            {
                return false;
            }


            if (!TransportersArrivalActionUtility.AnyNonDownedColonist(pods))
            {
                return false;
            }


            if (settlement.EnterCooldownBlocksEntering())
            {
                return FloatMenuAcceptanceReport.WithFailReasonAndMessage(
                    "EnterCooldownBlocksEntering".Translate(),
                    "MessageEnterCooldownBlocksEntering".Translate(settlement.EnterCooldownTicksLeft().ToStringTicksToPeriod()));
            }

            return true;
        }


        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {

            Thing lookTarget = TransportersArrivalActionUtility.GetLookTarget(transporters);


            bool isFirstTimeGenerate = !settlement.HasMap;


            Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(settlement.Tile, null);


            TaggedString letterLabel = "LetterLabelCaravanEnteredEnemyBase".Translate();
            TaggedString letterText = "LetterTransportPodsLandedInEnemyBase".Translate(settlement.Label).CapitalizeFirst();


            SettlementUtility.AffectRelationsOnAttacked(settlement, ref letterText);


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


            Current.Game.CurrentMap = orGenerateMap;


            CameraJumper.TryHideWorld();
            CameraJumper.TryJump(orGenerateMap.Center, orGenerateMap);


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
