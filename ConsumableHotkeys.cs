using BepInEx.Configuration;
using HarmonyLib;
using Pigeon.Movement;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ConsumableHotkeys
{
    public static ConsumableHotkeys Instance { get; private set; }

    private const string PersonalAccessTokenName = "Personal Access Token";
    private const string PremiumLootLicenseName = "Premium Loot License";
    private const string BootlegReplicatorName = "Bootleg Replicator";
    private const string ClearanceCertificateName = "Clearance Certificate";
    private const int ClearanceCertificateMaxUses = 5;

    private ConfigEntry<bool> enableHotkeys;
    private ConfigEntry<bool> enableHUD;
    private ConfigEntry<float> consumableHotkeysAnchorX;
    private ConfigEntry<float> consumableHotkeysAnchorY;

    private ConfigEntry<Key> personalAccessTokenHotkey;
    private ConfigEntry<Key> premiumLootLicenseHotkey;
    private ConfigEntry<Key> bootlegReplicatorHotkey;
    private ConfigEntry<Key> clearanceCertificateHotkey;

    private TextMeshProUGUI hudText;
    private GameObject hudContainer;

    private Dictionary<string, ConsumableStatus> consumableStatuses;

    private readonly ConfigFile configFile;
    private readonly Harmony harmony;

    public ConsumableHotkeys(ConfigFile configFile, Harmony harmony)
    {
        this.configFile = configFile;
        this.harmony = harmony;

        Instance = this;

        try
        {
            SetupConfig();
            InitializeStatuses();
            SetupConfigWatcher();
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Failed to initialize ConsumableHotkeys: {ex.Message}");
        }
    }

    private void SetupConfig()
    {
        enableHotkeys = configFile.Bind("Consumables", "EnableHotkeys", true, "Enables hotkey functionality for consumables.");
        enableHUD = configFile.Bind("Consumables", "EnableHUD", true, "Enables the HUD display for consumable statuses.");

        consumableHotkeysAnchorX = configFile.Bind("HUD Positioning", "ConsumableHotkeysAnchorX", 0.35f, "X anchor position for ConsumableHotkeys (0-1).");
        consumableHotkeysAnchorY = configFile.Bind("HUD Positioning", "ConsumableHotkeysAnchorY", 0.9f, "Y anchor position for ConsumableHotkeys (0-1).");
        consumableHotkeysAnchorX.SettingChanged += OnAnchorChanged;
        consumableHotkeysAnchorY.SettingChanged += OnAnchorChanged;

        personalAccessTokenHotkey = configFile.Bind("Consumables", "PersonalAccessToken_Hotkey", Key.Y, "Hotkey for Personal Access Token.");
        premiumLootLicenseHotkey = configFile.Bind("Consumables", "PremiumLootLicense_Hotkey", Key.H, "Hotkey for Premium Loot License.");
        bootlegReplicatorHotkey = configFile.Bind("Consumables", "BootlegReplicator_Hotkey", Key.U, "Hotkey for Bootleg Replicator.");
        clearanceCertificateHotkey = configFile.Bind("Consumables", "ClearanceCertificate_Hotkey", Key.J, "Hotkey for Clearance Certificate.");
    }

    private void InitializeStatuses()
    {
        consumableStatuses = new Dictionary<string, ConsumableStatus>
        {
            { PersonalAccessTokenName, new ConsumableStatus() },
            { PremiumLootLicenseName, new ConsumableStatus() },
            { BootlegReplicatorName, new ConsumableStatus() },
            { ClearanceCertificateName, new ConsumableStatus { MaxUses = ClearanceCertificateMaxUses, UsesRemaining = ClearanceCertificateMaxUses } }
        };
    }

    private void Start()
    {
        if (enableHUD.Value)
        {
            CreateHUD();
        }
        UpdateHudVisibility();
    }

    public void UpdateHudVisibility()
    {
        if (hudContainer != null)
        {
            hudContainer.SetActive(enableHUD.Value);
        }
    }

    private void OnAnchorChanged(object sender, EventArgs e)
    {
        UpdateAnchors();
    }

    private void UpdateAnchors()
    {
        if (hudContainer != null)
        {
            var containerRect = hudContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = containerRect.anchorMax = new Vector2(consumableHotkeysAnchorX.Value, consumableHotkeysAnchorY.Value);
        }
    }

    private void CreateHUD()
    {
        if (hudContainer != null) return;

        if (Player.LocalPlayer == null || Player.LocalPlayer.PlayerLook == null || Player.LocalPlayer.PlayerLook.Reticle == null) return;

        var parent = Player.LocalPlayer.PlayerLook.Reticle;
        hudContainer = new GameObject("TicketStatusHUD");
        hudContainer.transform.SetParent(parent, false);

        var containerRect = hudContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = containerRect.anchorMax = new Vector2(consumableHotkeysAnchorX.Value, consumableHotkeysAnchorY.Value);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(400f, 100f);

        GameObject textGO = new GameObject("StatusText");
        textGO.transform.SetParent(hudContainer.transform, false);
        hudText = textGO.AddComponent<TextMeshProUGUI>();
        hudText.fontSize = 16;
        hudText.color = Color.white;
        hudText.enableWordWrapping = false;
        hudText.alignment = TextAlignmentOptions.Left;
        hudText.verticalAlignment = VerticalAlignmentOptions.Top;

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = Vector2.zero;

        UpdateHudVisibility();
    }

    public void Update()
    {
        try
        {
            if (!enableHUD.Value) return;

            if (hudContainer == null)
            {
                CreateHUD();
                return;
            }

            if (hudText == null || consumableStatuses == null) return;

            UpdateHUDText();
            CheckHotkeys();
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in ConsumableHotkeys.Update(): {ex.Message}");
        }
    }

    private void UpdateHUDText()
    {
        string text = "";
        foreach (var kvp in consumableStatuses)
        {
            string status;
            string color;

            if (kvp.Key.Contains("Clearance Certificate"))
            {
                int remainingUses = PlayerData.Instance.GetFlag("dur_drops");
                status = remainingUses == 0 ? "Inactive" : $"{remainingUses}/{kvp.Value.MaxUses} Uses";
                color = remainingUses > 0 ? "green" : "red";
            }
            else
            {
                status = kvp.Value.IsActive ? "Active" : "Inactive";
                color = kvp.Value.IsActive ? "green" : "red";
            }

            int count = GetCurrentItemCount(kvp.Key);
            text += $"<color={color}>{kvp.Key}: {status} ({count})</color>\n";
        }
        hudText.text = text.Trim();
    }

    private int GetCurrentItemCount(string itemName)
    {
        foreach (var resource in Global.Instance.PlayerResources)
        {
            if (resource.Name == itemName)
            {
                return PlayerData.Instance.GetResource(resource);
            }
        }
        return 0;
    }

    private void CheckHotkeys()
    {
        if (!enableHotkeys.Value) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        CheckHotkey(keyboard, personalAccessTokenHotkey.Value, PersonalAccessTokenName);
        CheckHotkey(keyboard, premiumLootLicenseHotkey.Value, PremiumLootLicenseName);
        CheckHotkey(keyboard, bootlegReplicatorHotkey.Value, BootlegReplicatorName);
        CheckHotkey(keyboard, clearanceCertificateHotkey.Value, ClearanceCertificateName);
    }

    private void CheckHotkey(Keyboard keyboard, Key key, string consumableName)
    {
        if (keyboard[key].wasPressedThisFrame)
        {
            UseConsumable(consumableName);
        }
    }

    private void UseConsumable(string name)
    {
        TryActivateConsumableByName(name);

        UpdateConsumableStatus(name);
    }

    private void TryActivateConsumableByName(string name)
    {

        var storageWindows = UnityEngine.Object.FindObjectsOfType<StorageWindow>();
        foreach (var storageWindow in storageWindows)
        {
            if (TryActivateFromStorageWindow(storageWindow, name))
            {
                return;
            }
        }

        TryDirectActivation(name);
    }

    private bool TryActivateFromStorageWindow(StorageWindow storageWindow, string itemName)
    {
        try
        {
            var slotsField = storageWindow.GetType().GetField("slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (slotsField == null) return false;

            var slots = slotsField.GetValue(storageWindow) as StorageSlot[];
            if (slots == null) return false;

            foreach (var slot in slots)
            {
                var item = slot.GetType().GetField("item", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(slot) as IInventoryItem;
                var playerRes = item as PlayerResource;
                if (item != null && playerRes != null && playerRes.Name == itemName && item.ItemCount > 0)
                {

                    if (item.GetPrimaryBinding(out var binding, out var label))
                    {

                        var primaryActionField = playerRes.GetType().GetField("onPrimaryAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (primaryActionField != null)
                        {
                            var unityEvent = primaryActionField.GetValue(playerRes) as UnityEngine.Events.UnityEvent;
                            if (unityEvent != null)
                            {
                                unityEvent.Invoke();
                                return true;
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            SparrohPlugin.Logger.LogError($"Error activating {itemName} from storage: {e.Message}");
        }

        return false;
    }

    private void TryDirectActivation(string itemName)
    {

        if (IsConsumableActive(itemName))
        {
            return;
        }

        foreach (var resource in Global.Instance.PlayerResources)
        {
            if (resource.Name == itemName)
            {
                if (PlayerData.Instance.GetResource(resource) <= 0)
                {
                    return;
                }


                if (PlayerData.Instance.TryRemoveResource(resource, 1))
                {

                    ActivateConsumableByFlag(itemName);

                    return;
                }
                else
                {
                    return;
                }
            }
        }

    }

    private bool IsConsumableActive(string name)
    {
        if (name.Contains("Personal Access Token"))
        {
            return PlayerData.Instance.GetFlag("pa_token") == 1;
        }
        else if (name.Contains("Bootleg Replicator"))
        {
            return PlayerData.Instance.GetFlag("r_replicator") == 1;
        }
        else if (name.Contains("Premium Loot License"))
        {
            return PlayerData.Instance.GetFlag("equip_loot") == 1;
        }
        else if (name.Contains("Clearance Certificate"))
        {
            return PlayerData.Instance.GetFlag("dur_drops") > 0;
        }

        return false;
    }

    private void ActivateConsumableByFlag(string name)
    {
        if (name.Contains("Personal Access Token"))
        {
            PlayerData.Instance.SetFlag("pa_token", 1);
        }
        else if (name.Contains("Bootleg Replicator"))
        {
            PlayerData.Instance.SetFlag("r_replicator", 1);
        }
        else if (name.Contains("Premium Loot License"))
        {
            PlayerData.Instance.SetFlag("equip_loot", 1);
        }
        else if (name.Contains("Clearance Certificate"))
        {
            PlayerData.Instance.SetFlag("dur_drops", ClearanceCertificateMaxUses);
        }
    }

    private void UpdateConsumableStatus(string name)
    {

        if (!consumableStatuses.TryGetValue(name, out var status))
            return;

        status.IsActive = IsConsumableActive(name);

        if (status.IsActive)
        {
        }
        else
        {
        }
    }

    public void UpdateConsumableStatuses()
    {

        foreach (var kvp in consumableStatuses)
        {
            kvp.Value.IsActive = IsConsumableActive(kvp.Key);
        }

        UpdateHUDText();

    }

    private void SetupConfigWatcher()
    {
        var configFilePath = configFile.ConfigFilePath;
        var configDirectory = Path.GetDirectoryName(configFilePath);

        var watcher = new FileSystemWatcher(configDirectory, Path.GetFileName(configFilePath));
        watcher.Changed += (s, e) =>
        {
            configFile.Reload();
            OnConfigReloaded();
        };
        watcher.EnableRaisingEvents = true;
    }



    private void OnConfigReloaded()
    {
        InitializeStatuses();
        UpdateHudVisibility();
    }

    public void OnDestroy()
    {
        try
        {
            if (hudContainer != null)
            {
                UnityEngine.Object.Destroy(hudContainer);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in ConsumableHotkeys.OnDestroy(): {ex.Message}");
        }
    }

    private class ConsumableStatus
    {
        public bool IsActive { get; set; } = false;
        public int UsesRemaining { get; set; } = -1;
        public int MaxUses { get; set; } = -1;
    }
}

[HarmonyPatch]
public static class StorageSlotPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StorageSlot), "Setup")]
    public static void PostfixSetup(StorageSlot __instance, IInventoryItem item)
    {
        if (item != null)
        {
            string itemName = (item as PlayerResource)?.Name ?? "Unknown Item";
        }
    }
}

[HarmonyPatch]
public static class StorageWindowPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StorageWindow), "Refresh")]
    public static void PostfixRefresh(StorageWindow __instance)
    {
        var slots = __instance.GetType().GetField("slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(__instance) as StorageSlot[];
        if (slots != null)
        {
            foreach (var slot in slots)
            {
                var item = slot.GetType().GetField("item", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(slot) as IInventoryItem;
                if (item != null && item.ItemCount > 0)
                {
                    string itemName = (item as PlayerResource)?.Name ?? "Unknown Item";
                }
            }
        }
    }
}

[HarmonyPatch]
public static class DebugPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerData), "GetFlag", new Type[] { typeof(string) })]
    public static void PostfixGetFlag(PlayerData __instance, string id, ref int __result)
    {
        if (id.ToLower().Contains("token") || id.ToLower().Contains("license") || id.ToLower().Contains("replicator") ||
            id.ToLower().Contains("certificate") || id.ToLower().Contains("clearance") || id.ToLower().Contains("permit") ||
            id.ToLower().Contains("document") || id.ToLower().Contains("upgrade") || id.ToLower().Contains("dur_") ||
            id.ToLower().Contains("pa_") || id.ToLower().Contains("pl_") || id.ToLower().Contains("cc_") ||
            id.ToLower().Contains("loot") || id.ToLower().Contains("premium") ||
            id.Contains("p_l") || id.Contains("c_c") || id.Contains("r_r"))
        {
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerData), "SetFlag", new Type[] { typeof(string), typeof(int) })]
    public static void PostfixSetFlag(PlayerData __instance, string id, int value)
    {
        if (id.ToLower().Contains("token") || id.ToLower().Contains("license") || id.ToLower().Contains("replicator") ||
            id.ToLower().Contains("certificate") || id.ToLower().Contains("pa_") || id.ToLower().Contains("pl_") ||
            id.ToLower().Contains("cc_") || id.ToLower().Contains("loot") || id.ToLower().Contains("premium") ||
            id.Contains("p_l") || id.Contains("c_c") || id.Contains("r_r"))
        {

            if ((id == "pa_token" || id == "r_replicator" || id == "equip_loot" || id == "dur_drops") && value == 0)
            {
                ConsumableHotkeys.Instance?.UpdateConsumableStatuses();
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerData), "TryRemoveResource", new Type[] { typeof(PlayerResource), typeof(int) })]
    public static void PostfixTryRemoveResource(PlayerData __instance, PlayerResource resource, int amount, bool __result)
    {
        if (__result && amount > 0 &&
            (resource.Name.Contains("Personal") || resource.Name.Contains("Premium") ||
             resource.Name.Contains("Bootleg") || resource.Name.Contains("Clearance")))
        {
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerData), "AddResource")]
    public static void PostfixAddResource(PlayerData __instance, PlayerResource resource, int amount)
    {
        if (amount != 0 &&
            (resource.Name.Contains("Personal") || resource.Name.Contains("Premium") ||
             resource.Name.Contains("Bootleg") || resource.Name.Contains("Clearance")))
        {
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MissionManager), "OnUpgradeCollected")]
    public static void PostfixOnUpgradeCollected(UpgradeInstance upgrade)
    {
        if (upgrade.Gear != null)
        {

            int currentDrops = PlayerData.Instance.GetFlag("dur_drops");
            if (currentDrops > 0)
            {
                PlayerData.Instance.SetFlag("dur_drops", currentDrops - 1);
            }
        }
    }
}
