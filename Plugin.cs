using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.expandedhud";
    public const string PluginName = "ExpandedHUD";
    public const string PluginVersion = "1.2.0";

    internal static new ManualLogSource Logger;

    private Harmony harmony;
    private Speedometer speedometer;
    private Carnometer carnometer;
    private GunDisplay gunDisplay;
    private Altimeter altimeter;
    private ConsumableHotkeys consumableHotkeys;


    private void Awake()
    {
        Logger = base.Logger;

        harmony = new Harmony(PluginGUID);

        var configFile = Config;
        var watcher = new FileSystemWatcher(Paths.ConfigPath, "sparroh.expandedhud.cfg");
        watcher.Changed += (s, e) =>
        {
            Logger.LogInfo("Config file changed, reloading");
            configFile.Reload();
        };
        watcher.EnableRaisingEvents = true;

        speedometer = new Speedometer(configFile, harmony);
        carnometer = new Carnometer(configFile, harmony);
        gunDisplay = new GunDisplay(configFile, harmony);
        altimeter = new Altimeter(configFile, harmony);
        consumableHotkeys = new ConsumableHotkeys(configFile, harmony);

        harmony.PatchAll();

        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void Start()
    {
    }

    private void Update()
    {
        gunDisplay.UpdateHudVisibility();
        carnometer.UpdateHudVisibility();
        speedometer.UpdateHudVisibility();
        altimeter.UpdateHudVisibility();
        consumableHotkeys.UpdateHudVisibility();

        gunDisplay.Update();
        carnometer.Update();
        speedometer.Update();
        altimeter.Update();
        consumableHotkeys.Update();

        if (gunDisplay.GetGunStatsHUDSize().y > 0)
        {
        }
    }

    private void OnDestroy()
    {
        gunDisplay.OnDestroy();
        carnometer.OnDestroy();
        speedometer.OnDestroy();
        altimeter.OnDestroy();
        consumableHotkeys.OnDestroy();
        harmony.UnpatchSelf();
    }
}
