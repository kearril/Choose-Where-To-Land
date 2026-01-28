using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ChooseWhereToLand
{
    public class TransportersArrivalAction_ChooseSpotAndLand : TransportersArrivalAction
    {
        public RimWorld.Planet.Site site;

        private static readonly PawnsArrivalModeDef fixedArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetNamed("CWTL_ChooseWhereToLand", true);

        public PawnsArrivalModeDef ArrivalMode => fixedArrivalMode;
        public override bool GeneratesMap => true;

        public TransportersArrivalAction_ChooseSpotAndLand()
        {
        }

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

            if (site != null && site.Tile != destinationTile)
            {
                return false;
            }

            return CanVisit(pods, site);
        }

        public override bool ShouldUseLongEvent(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {
            return site != null && !site.HasMap;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            Thing lookTarget = TransportersArrivalActionUtility.GetLookTarget(transporters);
            bool isNewMap = !site.HasMap;
            Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile, site.PreferredMapSize, null);

            if (isNewMap)
            {
                Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
                PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(
                    orGenerateMap.mapPawns.AllPawns,
                    "LetterRelatedPawnsInMapWherePlayerLanded".Translate(Faction.OfPlayer.def.pawnsPlural),
                    LetterDefOf.NeutralEvent,
                    informEvenIfSeenBefore: true);
            }

            if (site.Faction != null && site.Faction != Faction.OfPlayer && site.MainSitePartDef.considerEnteringAsAttack)
            {
                Faction.OfPlayer.TryAffectGoodwillWith(
                    site.Faction,
                    Faction.OfPlayer.GoodwillToMakeHostile(site.Faction),
                    canSendMessage: true,
                    canSendHostilityLetter: true,
                    HistoryEventDefOf.AttackedSettlement);
            }

            Messages.Message("MessageShuttleArrived".Translate(), lookTarget, MessageTypeDefOf.TaskCompletion);
            Current.Game.CurrentMap = orGenerateMap;
            CameraJumper.TryHideWorld();
            CameraJumper.TryJump(orGenerateMap.Center, orGenerateMap);
            fixedArrivalMode.Worker.TravellingTransportersArrived(transporters, orGenerateMap);
        }

        public static FloatMenuAcceptanceReport CanVisit(IEnumerable<IThingHolder> pods, RimWorld.Planet.Site site)
        {
            if (site == null || !site.Spawned)
            {
                return false;
            }

            if (!TransportersArrivalActionUtility.AnyNonDownedColonist(pods))
            {
                return false;
            }

            if (site.EnterCooldownBlocksEntering())
            {
                return FloatMenuAcceptanceReport.WithFailMessage(
                    "MessageEnterCooldownBlocksEntering".Translate(site.EnterCooldownTicksLeft().ToStringTicksToPeriod()));
            }
            return true;
        }

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

            void UIConfirmationCallback(Action action)
            {
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
