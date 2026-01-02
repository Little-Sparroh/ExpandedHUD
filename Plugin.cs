using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class SparrohPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.expandedhud";
    public const string PluginName = "ExpandedHUD";
    public const string PluginVersion = "1.5.0";

    internal static new ManualLogSource Logger;

    private Harmony harmony;
    private Speedometer speedometer;
    private Carnometer carnometer;
    private GunDisplay gunDisplay;
    private Altimeter altimeter;
    private ConsumableHotkeys consumableHotkeys;
    private RangeFinder rangeFinder;
    private BossTimer bossTimer;


    private void Awake()
    {
        Logger = base.Logger;

        try
        {
            harmony = new Harmony(PluginGUID);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to create Harmony instance: {ex.Message}");
            return;
        }

        var configFile = Config;
        try
        {
            var watcher = new FileSystemWatcher(Paths.ConfigPath, "sparroh.expandedhud.cfg");
            watcher.Changed += (s, e) =>
            {
                configFile.Reload();
            };
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Failed to set up config watcher: {ex.Message}");
        }

        try
        {
            speedometer = new Speedometer(configFile, harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize Speedometer: {ex.Message}");
        }

        try
        {
            carnometer = new Carnometer(configFile, harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize Carnometer: {ex.Message}");
        }

        try
        {
            gunDisplay = new GunDisplay(configFile, harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize GunDisplay: {ex.Message}");
        }

        try
        {
            altimeter = new Altimeter(configFile, harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize Altimeter: {ex.Message}");
        }

        try
        {
            consumableHotkeys = new ConsumableHotkeys(configFile, harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize ConsumableHotkeys: {ex.Message}");
        }

        try
        {
            rangeFinder = new RangeFinder(configFile, harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize RangeFinder: {ex.Message}");
        }

        try
        {
            bossTimer = new BossTimer(configFile, harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize BossTimer: {ex.Message}");
        }

        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to apply Harmony patches: {ex.Message}");
        }

        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void Start()
    {
    }

    private void Update()
    {
        try
        {
            if (gunDisplay != null) gunDisplay.UpdateHudVisibility();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in GunDisplay.UpdateHudVisibility(): {ex.Message}");
        }

        try
        {
            if (carnometer != null) carnometer.UpdateHudVisibility();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in Carnometer.UpdateHudVisibility(): {ex.Message}");
        }

        try
        {
            if (speedometer != null) speedometer.UpdateHudVisibility();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in Speedometer.UpdateHudVisibility(): {ex.Message}");
        }

        try
        {
            if (altimeter != null) altimeter.UpdateHudVisibility();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in Altimeter.UpdateHudVisibility(): {ex.Message}");
        }

        try
        {
            if (consumableHotkeys != null) consumableHotkeys.UpdateHudVisibility();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in ConsumableHotkeys.UpdateHudVisibility(): {ex.Message}");
        }

        try
        {
            if (rangeFinder != null) rangeFinder.UpdateHudVisibility();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in RangeFinder.UpdateHudVisibility(): {ex.Message}");
        }

        try
        {
            if (bossTimer != null) bossTimer.UpdateHudVisibility();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in BossTimer.UpdateHudVisibility(): {ex.Message}");
        }

        try
        {
            if (gunDisplay != null) gunDisplay.Update();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in GunDisplay.Update(): {ex.Message}");
        }

        try
        {
            if (carnometer != null) carnometer.Update();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in Carnometer.Update(): {ex.Message}");
        }

        try
        {
            if (speedometer != null) speedometer.Update();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in Speedometer.Update(): {ex.Message}");
        }

        try
        {
            if (altimeter != null) altimeter.Update();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in Altimeter.Update(): {ex.Message}");
        }

        try
        {
            if (consumableHotkeys != null) consumableHotkeys.Update();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in ConsumableHotkeys.Update(): {ex.Message}");
        }

        try
        {
            if (rangeFinder != null) rangeFinder.Update();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in RangeFinder.Update(): {ex.Message}");
        }

        try
        {
            if (bossTimer != null) bossTimer.Update();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in BossTimer.Update(): {ex.Message}");
        }

        try
        {
            if (gunDisplay != null && gunDisplay.GetGunStatsHUDSize().y > 0)
            {
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error checking gun stats HUD size: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        try
        {
            if (gunDisplay != null) gunDisplay.OnDestroy();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in GunDisplay.OnDestroy(): {ex.Message}");
        }

        try
        {
            if (carnometer != null) carnometer.OnDestroy();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in Carnometer.OnDestroy(): {ex.Message}");
        }

        try
        {
            if (speedometer != null) speedometer.OnDestroy();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in Speedometer.OnDestroy(): {ex.Message}");
        }

        try
        {
            if (altimeter != null) altimeter.OnDestroy();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in Altimeter.OnDestroy(): {ex.Message}");
        }

        try
        {
            if (consumableHotkeys != null) consumableHotkeys.OnDestroy();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in ConsumableHotkeys.OnDestroy(): {ex.Message}");
        }

        try
        {
            if (rangeFinder != null) rangeFinder.OnDestroy();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in RangeFinder.OnDestroy(): {ex.Message}");
        }

        try
        {
            if (bossTimer != null) bossTimer.OnDestroy();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in BossTimer.OnDestroy(): {ex.Message}");
        }

        try
        {
            if (harmony != null) harmony.UnpatchSelf();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error unpatching Harmony: {ex.Message}");
        }
    }
}
