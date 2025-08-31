using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ChooseWhereToLand
{
    // 对 Site 类中的 GetShuttleFloatMenuOptions 方法进行后置 Patch
    [HarmonyPatch(typeof(Site), nameof(Site.GetShuttleFloatMenuOptions))]
    public static class Patch_Site_GetShuttleFloatMenuOptions
    {
        // Postfix方法，会在原方法执行后调用，获取原返回值 __result 并进行扩展
        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,   // 原方法返回的选项集合
            Site __instance,                         // 被调用的Site实例
            IEnumerable<IThingHolder> pods,          // 运输物品集合
            System.Action<PlanetTile, TransportersArrivalAction> launchAction) // 发射动作委托
        {
            // 先返回原始结果中的所有选项
            foreach (var option in __result)
                yield return option;

            // 如果自定义落点未启用，直接结束
            if (!ChooseWhereToLand_Mod.settings.useCustomLandingSpot)
                yield break;
            // 再添加自定义的选择落点的菜单选项
            foreach (var option in TransportersArrivalAction_ChooseSpotAndLand.GetFloatMenuOptions(launchAction, pods, __instance))
                yield return option;
        }
    }

    // 对 Site 类中的 GetTransportersFloatMenuOptions 方法进行后置 Patch
    [HarmonyPatch(typeof(Site), nameof(Site.GetTransportersFloatMenuOptions))]
    public static class Patch_Site_GetTransportersFloatMenuOptions
    {

        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,
            Site __instance,
            IEnumerable<IThingHolder> pods,
            System.Action<PlanetTile, TransportersArrivalAction> launchAction)
        {
            // 返回原始选项
            foreach (var option in __result)
                yield return option;
            if (!ChooseWhereToLand_Mod.settings.useCustomLandingSpot)
                yield break;
            // 添加新选项
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
            // 返回原本的所有选项
            foreach (var option in __result)
                yield return option;
            if (!ChooseWhereToLand_Mod.settings.useCustomLandingSpot)
                yield break;
            // 如果满足条件，添加自定义落点攻击选项
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
            // 先返回原有选项
            foreach (var option in __result)
                yield return option;
            if (!ChooseWhereToLand_Mod.settings.useCustomLandingSpot)
                yield break;
            // 获取 Shuttle 第一个 Pod
            IThingHolder thingHolder = pods.FirstOrDefault();
            CompTransporter firstPod = thingHolder as CompTransporter;
            if (firstPod == null || firstPod.Shuttle.shipParent == null)
                yield break;

            // 弹窗提示
            TaggedString message = (__instance.Faction.HostileTo(Faction.OfPlayer)
                ? "ConfirmLandOnHostileFactionBase".Translate(__instance.Faction)
                : "ConfirmLandOnNeutralFactionBase".Translate(__instance.Faction));

            // 添加自定义攻击选项
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
