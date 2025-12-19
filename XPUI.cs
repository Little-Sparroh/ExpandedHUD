using HarmonyLib;

[HarmonyPatch]
internal class XPUI
{
  [HarmonyPatch(typeof (PlayerData.GearData), "GetLevelString")]
  [HarmonyPostfix]
  public static void NumericalXP__Postfix2(PlayerData.GearData __instance, ref string __result)
  {
    if (__instance.Level >= __instance.MaxLevel)
      __result = "∞";
    int num = __instance.NextLevelXPCost - __instance.LevelXP;
    __result = $"{TextBlocks.GetNumberString(__instance.Level)} (NEXT LEVEL: {num})";
  }
}
