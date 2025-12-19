using HarmonyLib;
using Pigeon.Movement;
using UnityEngine;

[HarmonyPatch]
internal class EndScreenUI
{
  public static string playerName;

  [HarmonyPatch(typeof (MissionStat), "Setup")]
  [HarmonyPrefix]
  public static void StatSetup__Prefix(
    MissionStat __instance,
    Player player,
    ref MissionManager.MissionPlayerData data)
  {
    EndScreenUI.playerName = ((Object) player).name;
  }

  [HarmonyPatch(typeof (MissionManager.MissionPlayerData), "GetStatText")]
  [HarmonyPostfix]
  public static void GetEndStats__Postfix(
    MissionManager.MissionPlayerData __instance,
    out string title,
    out string description)
  {
    float damageDealt = __instance.damageDealt;
    int enemiesKilled = __instance.enemiesKilled;
    float appliedStatusEffect = __instance.appliedStatusEffect;
    float damageTaken = __instance.damageTaken;
    float deaths = (float) __instance.deaths;
    float friendlyFireDamageDealt = __instance.friendlyFireDamageDealt;
    float healingDealt = __instance.healingDealt;
    int playersRepaired = __instance.playersRepaired;
    float targetsKilled = (float) __instance.targetsKilled;
    float timeInAir = __instance.timeInAir;
    float timesCorroded = (float) __instance.timesCorroded;
    float timesIgnited = (float) __instance.timesIgnited;
    float timeSliding = __instance.timeSliding;
    title = string.Empty;
    description = $"Damage Dealt: {damageDealt}\n" + $"Enemies Killed: {enemiesKilled}\n" + $"Elemental Stacks: {appliedStatusEffect}";
  }
}
