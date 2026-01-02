using HarmonyLib;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[HarmonyPatch]
public static class BossTimerPatches
{
    [HarmonyPatch(typeof(MissionManager), "OnMissionStarted_Client")]
    [HarmonyPostfix]
    private static void OnMissionStarted_Client_Postfix()
    {
        if (BossTimerHUD.Instance != null)
        {
            BossTimerHUD.Instance.ResetTimer();
        }
    }

    [HarmonyPatch(typeof(AmalgamationObjective), "OnAmalgamationSpawned_ClientRpc")]
    [HarmonyPostfix]
    private static void OnAmalgamationSpawned_ClientRpc_Postfix(AmalgamationObjective __instance, NetworkBehaviourReference brainRef)
    {
        if (BossTimerHUD.Instance != null)
        {
            BossTimerHUD.Instance.StartTimer();
        }
    }

    [HarmonyPatch(typeof(AmalgamationObjective), "OnBrainKilled")]
    [HarmonyPostfix]
    private static void OnBrainKilled_Postfix(EnemyBrain brain)
    {
        if (BossTimerHUD.Instance != null)
        {
            BossTimerHUD.Instance.StopTimer();
        }
    }
}
