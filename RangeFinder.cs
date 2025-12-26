using BepInEx.Configuration;
using HarmonyLib;
using System;

public class RangeFinder
{
    public static ConfigEntry<bool> enableRangeFinder;
    public static ConfigEntry<float> rangeFinderMaxRange;
    private readonly ConfigFile configFile;
    private readonly Harmony harmony;

    public RangeFinder(ConfigFile configFile, Harmony harmony)
    {
        this.configFile = configFile;
        this.harmony = harmony;

        enableRangeFinder = configFile.Bind("General", "EnableRangefinderHUD", true, "Enable the rangefinder display");
        rangeFinderMaxRange = configFile.Bind("General", "RangefinderMaxRange", 500f, "Maximum range for rangefinder (meters)");

        enableRangeFinder.SettingChanged += OnEnableRangeFinderChanged;

        harmony.PatchAll(typeof(RangeFinderPatches));
    }

    public void UpdateHudVisibility()
    {
        RangeFinderSystem.SetEnabled(enableRangeFinder.Value);
    }

    public void Update()
    {
        RangeFinderSystem.UpdateRangeFinder();
    }

    public void OnDestroy()
    {
        RangeFinderSystem.Cleanup();
        harmony.UnpatchSelf();
    }

    private void OnEnableRangeFinderChanged(object sender, EventArgs e)
    {
        UpdateHudVisibility();
    }

    [HarmonyPatch]
    public static class RangeFinderPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MissionHUD), "Start")]
        public static void MissionHUD_Start_Postfix()
        {
            RangeFinderSystem.Initialize();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MissionHUD), "Update")]
        public static void MissionHUD_Update_Postfix()
        {
            RangeFinderSystem.UpdateRangeFinder();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MissionHUD), "OnDestroy")]
        public static void MissionHUD_OnDestroy_Postfix()
        {
            RangeFinderSystem.Cleanup();
        }
    }
}
