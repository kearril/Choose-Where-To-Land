using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ChooseWhereToLand
{
    // 访问太空时的TransportersArrivalAction
    public class TransportersArrivalAction_CWTLVisitSpace : TransportersArrivalAction
    {
        private MapParent parent;


        private static readonly PawnsArrivalModeDef fixedArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetNamed("CWTL_ChooseWhereToLand", true);

        public PawnsArrivalModeDef ArrivalMode => fixedArrivalMode;

        public override bool GeneratesMap => true;

        public TransportersArrivalAction_CWTLVisitSpace()
        {
        }

        public TransportersArrivalAction_CWTLVisitSpace(MapParent parent)
        {
            this.parent = parent;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref parent, "parent");
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(pods, destinationTile);
            if (!floatMenuAcceptanceReport)
            {
                return floatMenuAcceptanceReport;
            }
            if (parent != null && parent.Tile != destinationTile)
            {
                return false;
            }
            return CanVisit(pods, parent);
        }

        public override bool ShouldUseLongEvent(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {
            return !parent.HasMap;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            Thing lookTarget = TransportersArrivalActionUtility.GetLookTarget(transporters);
            IntVec3 size = Find.World.info.initialMapSize;
            if (parent.def.overrideMapSize.HasValue)
            {
                size = parent.def.overrideMapSize.Value;
            }
            Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(parent.Tile, size, parent.def);


            Current.Game.CurrentMap = orGenerateMap;


            CameraJumper.TryHideWorld();
            CameraJumper.TryJump(orGenerateMap.Center, orGenerateMap);
            if (transporters.IsShuttle())
            {
                Messages.Message("MessageShuttleArrived".Translate(), lookTarget, MessageTypeDefOf.TaskCompletion);
            }
            else
            {
                Messages.Message("MessageTransportPodsArrived".Translate(), lookTarget, MessageTypeDefOf.TaskCompletion);
            }

            fixedArrivalMode.Worker.TravellingTransportersArrived(transporters, orGenerateMap);
        }

        private static FloatMenuAcceptanceReport CanVisit(IEnumerable<IThingHolder> pods, MapParent parent)
        {
            if (parent == null || !parent.Spawned)
            {
                return false;
            }
            if (!TransportersArrivalActionUtility.AnyNonDownedColonist(pods))
            {
                return false;
            }
            return true;
        }

        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Action<PlanetTile, TransportersArrivalAction> launchAction, IEnumerable<IThingHolder> pods, MapParent parent)
        {
            foreach (FloatMenuOption floatMenuOption in TransportersArrivalActionUtility.GetFloatMenuOptions(() => CanVisit(pods, parent), () => new TransportersArrivalAction_CWTLVisitSpace(parent), "CWTL_LaunchTo".Translate(parent.Named("LOCATION")), launchAction, parent.Tile, UIConfirmationCallback))
            {
                yield return floatMenuOption;
            }
            void UIConfirmationCallback(Action action)
            {
                if (ModsConfig.OdysseyActive && parent.Tile.LayerDef == PlanetLayerDefOf.Orbit)
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
