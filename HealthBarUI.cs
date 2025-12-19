using HarmonyLib;
using Pigeon.Movement;
using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

[HarmonyPatch]
internal class HealthBarUI
{
  public static HealthMonoUI health_MB;
  public static TextMeshProUGUI healthText;
  public static GameObject healthTextGO;
  public static Player player;

  [HarmonyPatch(typeof (Player), "Start")]
  [HarmonyPrefix]
  public static void GetPlayer__Prefix2(Player __instance)
  {
    if (!((NetworkBehaviour) __instance).IsLocalPlayer)
      return;
    HealthBarUI.player = __instance;
  }

  [HarmonyPatch(typeof (CharacterSelectWindow), "SelectCharacter", typeof(Character))]
  [HarmonyPostfix]
  public static void CreateObject__Postfix2(CharacterSelectWindow __instance, Character character)
  {
    if (!HealthBarUI.player.IsLocalPlayer || HealthBarUI.healthTextGO)
      return;
    HealthBarUI.healthTextGO = new GameObject("HealthText");
    HealthBarUI.health_MB = HealthBarUI.healthTextGO.AddComponent<HealthMonoUI>();
    HealthBarUI.healthTextGO.TryGetComponent<TextMeshProUGUI>(out HealthBarUI.healthText);
    HealthBarUI.healthTextGO.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
    HealthBarUI.healthTextGO.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
    HealthBarUI.healthTextGO.layer = 5;
    HealthBarUI.health_MB.health = HealthBarUI.player.Health;
    HealthBarUI.healthTextGO.SetActive(true);
  }

  [HarmonyPatch(typeof (Player), "OnSyncedHealthChanged")]
  [HarmonyPostfix]
  public static void OwnerHealthChange__Postfix(Player __instance)
  {
    if (!__instance.IsLocalPlayer || !(HealthBarUI.healthText != null))
      return;
    HealthBarUI.health_MB.health = __instance.Health;
    HealthBarUI.health_MB.healthPercent = HealthBarUI.health_MB.health / __instance.MaxHealth;
    HealthBarUI.health_MB.isDamaged = true;
  }
}
