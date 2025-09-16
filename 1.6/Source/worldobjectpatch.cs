using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ChooseWhereToLand
{

    [HarmonyPatch(typeof(Site), nameof(Site.GetShuttleFloatMenuOptions))]
    public static class Patch_Site_GetShuttleFloatMenuOptions
    {

        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,
            Site __instance,
            IEnumerable<IThingHolder> pods,
            System.Action<PlanetTile, TransportersArrivalAction> launchAction)
        {

            foreach (var option in __result)
                yield return option;


            if (!ChooseWhereToLand_Mod.settings.useCustomLandingSpot)
                yield break;
            if (TransportersArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, __instance))
            {
                yield break;
            }

            foreach (var option in TransportersArrivalAction_ChooseSpotAndLand.GetFloatMenuOptions(launchAction, pods, __instance))
                yield return option;
        }
    }


    [HarmonyPatch(typeof(Site), nameof(Site.GetTransportersFloatMenuOptions))]
    public static class Patch_Site_GetTransportersFloatMenuOptions
    {

        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,
            Site __instance,
            IEnumerable<IThingHolder> pods,
            System.Action<PlanetTile, TransportersArrivalAction> launchAction)
        {

            foreach (var option in __result)
                yield return option;
            if (!ChooseWhereToLand_Mod.settings.useCustomLandingSpot)
                yield break;
            if (TransportersArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, __instance))
            {
                yield break;
            }

            foreach (var option in TransportersArrivalAction_ChooseSpotAndLand.GetFloatMenuOptions(launchAction, pods, __instance))
                yield return option;
        }
    }

    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetTransportersFloatMenuOptions))]
    public static class Patch_Settlement_GetTransportersFloatMenuOptions
    {
        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,
            Settlement __instance,
            IEnumerable<IThingHolder> pods,
            Action<PlanetTile, TransportersArrivalAction> launchAction)
        {

            foreach (var option in __result)
                yield return option;
            if (!ChooseWhereToLand_Mod.settings.useCustomLandingSpot)
                yield break;
            if (TransportersArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, __instance))
            {
                yield break;
            }

            foreach (var option in TransportersArrivalAction_CWTLAttackSettlement.GetFloatMenuOptions(
                launchAction, pods, __instance))
            {
                yield return option;
            }
        }
    }

    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetShuttleFloatMenuOptions))]
    public static class Patch_Settlement_GetShuttleFloatMenuOptions
    {
        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,
            Settlement __instance,
            IEnumerable<IThingHolder> pods,
            Action<PlanetTile, TransportersArrivalAction> launchAction)
        {

            foreach (var option in __result)
                yield return option;
            if (!ChooseWhereToLand_Mod.settings.useCustomLandingSpot)
                yield break;
            if (TransportersArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, __instance))
            {
                yield break;
            }

            IThingHolder thingHolder = pods.FirstOrDefault();
            CompTransporter firstPod = thingHolder as CompTransporter;
            if (firstPod == null || firstPod.Shuttle.shipParent == null)
                yield break;


            TaggedString message = (__instance.Faction.HostileTo(Faction.OfPlayer)
                ? "ConfirmLandOnHostileFactionBase".Translate(__instance.Faction)
                : "ConfirmLandOnNeutralFactionBase".Translate(__instance.Faction));


            foreach (var option in TransportersArrivalActionUtility.GetFloatMenuOptions(
                () => TransportersArrivalAction_CWTLAttackSettlement.CanAttack(pods, __instance),
                () => new TransportersArrivalAction_CWTLAttackSettlement(__instance),
                "CWTL_AttackSettlement".Translate(__instance.Label),
                delegate (PlanetTile t, TransportersArrivalAction s)
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(message, delegate
                    {
                        launchAction(t, s);
                    }));
                },
                __instance.Tile))
            {
                yield return option;
            }
        }
    }

    [HarmonyPatch(typeof(SpaceMapParent), nameof(SpaceMapParent.GetTransportersFloatMenuOptions))]
    public static class Patch_SpaceMapParent_GetTransportersFloatMenuOptions
    {
        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,
            SpaceMapParent __instance,
            IEnumerable<IThingHolder> pods,
            Action<PlanetTile, TransportersArrivalAction> launchAction)
        {

            foreach (var option in __result)
                yield return option;
            if (!ChooseWhereToLand_Mod.settings.useCustomLandingSpot)
                yield break;
            if (TransportersArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, __instance))
            {
                yield break;
            }

            foreach (var option in TransportersArrivalAction_CWTLVisitSpace.GetFloatMenuOptions(launchAction, pods, __instance))
                yield return option;
        }
    }

    [HarmonyPatch(typeof(SpaceMapParent), nameof(SpaceMapParent.GetShuttleFloatMenuOptions))]
    public static class Patch_SpaceMapParent_GetShuttleFloatMenuOptions
    {
        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,
            SpaceMapParent __instance,
            IEnumerable<IThingHolder> pods,
            Action<PlanetTile, TransportersArrivalAction> launchAction)
        {

            foreach (var option in __result)
                yield return option;
            if (!ChooseWhereToLand_Mod.settings.useCustomLandingSpot)
                yield break;
            if (TransportersArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, __instance))
            {
                yield break;
            }

            foreach (var option in TransportersArrivalAction_CWTLVisitSpace.GetFloatMenuOptions(launchAction, pods, __instance))
                yield return option;
        }
    }



    [StaticConstructorOnStartup]
    public static class ChooseWhereToLandMod
    {
        static ChooseWhereToLandMod()
        {

            var harmony = new Harmony("CWTL_ChooseWhereToLand");

            harmony.PatchAll();
        }
    }
}
