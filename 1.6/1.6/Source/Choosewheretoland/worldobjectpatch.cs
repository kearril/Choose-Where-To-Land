using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
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

            // 再添加自定义的选择落点的菜单选项
            foreach (var option in TransportersArrivalAction_ChooseSpotAndLand.GetFloatMenuOptions(launchAction, pods, __instance))
                yield return option;
        }
    }

    // 对 Site 类中的 GetTransportersFloatMenuOptions 方法进行后置 Patch
    [HarmonyPatch(typeof(Site), nameof(Site.GetTransportersFloatMenuOptions))]
    public static class Patch_Site_GetTransportersFloatMenuOptions
    {
        // Postfix方法，和上面类似，扩展原返回值
        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,
            Site __instance,
            IEnumerable<IThingHolder> pods,
            System.Action<PlanetTile, TransportersArrivalAction> launchAction)
        {
            // 返回原始选项
            foreach (var option in __result)
                yield return option;

            // 添加自定义的选项
            foreach (var option in TransportersArrivalAction_ChooseSpotAndLand.GetFloatMenuOptions(launchAction, pods, __instance))
                yield return option;
        }
    }


    // 静态构造类，在游戏启动时执行
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
