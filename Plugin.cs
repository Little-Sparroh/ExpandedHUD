using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Pigeon.Movement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class ExpandedHUDMod : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.expandedhud";
    public const string PluginName = "ExpandedHUD";
    public const string PluginVersion = "1.0.0";

    internal static new ManualLogSource Logger;

    private ConfigEntry<bool> enableGunStatsHUD;
    private ConfigEntry<bool> enableDamageMeterHUD;
    private ConfigEntry<float> dpsWindowSeconds;
    private ConfigEntry<bool> enableSpeedometerHUD;

    private Harmony harmony;

    private static Gun currentGun;
    private static FieldInfo playerField;
    private static PropertyInfo activeProp;
    private float updateTimer = 0f;
    private const float UpdateInterval = 0.5f;
    private static readonly Color sky = new Color(0.529f, 0.808f, 0.922f);
    private static readonly Color orchid = new Color(0.855f, 0.439f, 0.839f);
    private static readonly Color rose = new Color(0.8901960784313725f, 0.1411764705882353f, 0.16862745098039217f);
    private static readonly Color macaroon = new Color(0.9764705882352941f, 0.8784313725490196f, 0.4627450980392157f);
    private static readonly Color shamrock = new Color(0.011764705882352941f, 0.6745098039215687f, 0.07450980392156863f);
    private GameObject gunStatsHudContainer;
    private TextMeshProUGUI gunStatsTitleText;
    private TextMeshProUGUI[] gunStatsStatTexts;
    private const int NUM_STAT_LINES = 25;

    public static Queue<(float time, float damage)> damageQueue = new Queue<(float, float)>();
    private GameObject damageMeterHudContainer;
    private TextMeshProUGUI totalDamageText;
    private TextMeshProUGUI fiveSecDamageText;
    private TextMeshProUGUI totalKillsText;
    private TextMeshProUGUI totalCoresText;
    public static float totalDamage = 0f;
    public static int totalKills = 0;
    public static int totalCoresKilled = 0;
    public static float missionStartTime = 0f;

    private TextMeshProUGUI speedText;
    private GameObject speedometerHudContainer;
    private FieldInfo currentMoveSpeedField;
    private FieldInfo vkField;
    private FieldInfo rbField;
    private FieldInfo moveVelocityField;
    private PropertyInfo vkProp;
    private PropertyInfo rbProp;
    private bool wasLocked = false;

    private void Awake()
    {
        Logger = base.Logger;

        enableGunStatsHUD = Config.Bind("General", "EnableGunStatsHUD", true, "If true, the gun stats HUD will be displayed.");
        enableGunStatsHUD.SettingChanged += OnEnableGunStatsHUDChanged;

        enableDamageMeterHUD = Config.Bind("General", "EnableDamageMeterHUD", true, "Enable or disable the damage meter functionality.");
        enableDamageMeterHUD.SettingChanged += OnEnableDamageMeterHUDChanged;

        dpsWindowSeconds = Config.Bind("General", "DPSWindowSeconds", 5f, "Time window (in seconds) for calculating DPS. Higher values smooth out spikes.");

        enableSpeedometerHUD = Config.Bind("General", "EnableSpeedometerHUD", true, "Enables the speedometer HUD display.");
        enableSpeedometerHUD.SettingChanged += OnEnableSpeedometerHUDChanged;

        var configFile = enableGunStatsHUD.ConfigFile;
        var watcher = new FileSystemWatcher(Paths.ConfigPath, "sparroh.expandedhud.cfg");
        watcher.Changed += (s, e) =>
        {
            Logger.LogInfo("Config file changed, reloading");
            configFile.Reload();
        };
        watcher.EnableRaisingEvents = true;

        harmony = new Harmony(PluginGUID);
        harmony.PatchAll(typeof(GunEnablePatch));
        harmony.PatchAll(typeof(GunDisablePatch));
        harmony.PatchAll(typeof(DPSPatches));
        harmony.PatchAll(typeof(MissionPatches));

        playerField = typeof(Gun).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance);
        activeProp = typeof(IGear).GetProperty("Active");

        currentMoveSpeedField = typeof(Player).GetField("currentMoveSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
        vkField = typeof(Player).GetField("velocity", BindingFlags.NonPublic | BindingFlags.Instance) ??
                  typeof(Player).GetField("velocity", BindingFlags.Public | BindingFlags.Instance);
        if (vkField == null)
        {
            vkProp = typeof(Player).GetProperty("velocity", BindingFlags.Public | BindingFlags.Instance);
        }
        if (vkField == null && vkProp == null)
        {
            rbField = typeof(Player).GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance) ??
                      typeof(Player).GetField("rb", BindingFlags.Public | BindingFlags.Instance);
        }
        if (rbField == null && vkField == null && vkProp == null)
        {
            rbProp = typeof(Player).GetProperty("rb", BindingFlags.Public | BindingFlags.Instance);
        }
        if (rbField == null && rbProp == null && vkField == null && vkProp == null)
        {
            moveVelocityField = typeof(Player).GetField("moveVelocity", BindingFlags.NonPublic | BindingFlags.Instance) ??
                                typeof(Player).GetField("moveVelocity", BindingFlags.Public | BindingFlags.Instance);
        }

        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void Start()
    {
        missionStartTime = Time.time;
    }

    private void UpdateHudVisibility()
    {
        if (gunStatsHudContainer != null)
        {
            gunStatsHudContainer.SetActive(enableGunStatsHUD.Value);
        }
        if (damageMeterHudContainer != null)
        {
            damageMeterHudContainer.SetActive(enableDamageMeterHUD.Value);
        }
        if (speedometerHudContainer != null)
        {
            speedometerHudContainer.SetActive(enableSpeedometerHUD.Value);
        }
    }

    private void OnEnableGunStatsHUDChanged(object sender, EventArgs e)
    {
        UpdateHudVisibility();
    }

    private void OnEnableDamageMeterHUDChanged(object sender, EventArgs e)
    {
        Logger.LogInfo($"DamageMeter config changed to {enableDamageMeterHUD.Value}");
        UpdateHudVisibility();
    }

    private void OnEnableSpeedometerHUDChanged(object sender, EventArgs e)
    {
        Logger.LogInfo($"Speedometer enabled: {enableSpeedometerHUD.Value}");
        if (enableSpeedometerHUD.Value == false && speedometerHudContainer != null)
        {
            UnityEngine.Object.Destroy(speedometerHudContainer);
            speedometerHudContainer = null;
            speedText = null;
        }
        UpdateHudVisibility();
    }

    private void CreateGunStatsHUD()
    {
        if (gunStatsHudContainer != null) return;

        var parent = Player.LocalPlayer.PlayerLook.Reticle;
        gunStatsHudContainer = new GameObject("GunStatsHUD");
        gunStatsHudContainer.transform.SetParent(parent, false);

        var containerRect = gunStatsHudContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.18f, 1.08f);
        containerRect.anchorMax = new Vector2(0.18f, 1.08f);
        containerRect.anchoredPosition = new Vector2(0f, 0f);
        containerRect.sizeDelta = new Vector2(350f, 400f);

        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(gunStatsHudContainer.transform, false);
        gunStatsTitleText = titleGO.AddComponent<TextMeshProUGUI>();
        gunStatsTitleText.fontSize = 18;
        gunStatsTitleText.color = Color.white;
        gunStatsTitleText.enableWordWrapping = false;
        gunStatsTitleText.alignment = TextAlignmentOptions.Left;
        gunStatsTitleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = new Vector2(1f, 0f);
        titleRect.anchoredPosition = new Vector2(0f, 0f);
        titleRect.sizeDelta = new Vector2(0f, 25f);

        gunStatsStatTexts = new TextMeshProUGUI[NUM_STAT_LINES];
        for (int i = 0; i < NUM_STAT_LINES; i++)
        {
            var statGO = new GameObject($"StatText{i}");
            statGO.transform.SetParent(gunStatsHudContainer.transform, false);
            gunStatsStatTexts[i] = statGO.AddComponent<TextMeshProUGUI>();
            gunStatsStatTexts[i].fontSize = 16;
            gunStatsStatTexts[i].color = Color.white;
            gunStatsStatTexts[i].enableWordWrapping = false;
            gunStatsStatTexts[i].alignment = TextAlignmentOptions.Left;
            gunStatsStatTexts[i].verticalAlignment = VerticalAlignmentOptions.Middle;
            var statRect = statGO.GetComponent<RectTransform>();
            statRect.anchorMin = Vector2.zero;
            statRect.anchorMax = new Vector2(1f, 0f);
            statRect.anchoredPosition = new Vector2(0f, -(i * 18f + 25f));
            statRect.sizeDelta = new Vector2(0f, 18f);
        }

        UpdateHudVisibility();
    }

    private void CreateDamageMeterHUD()
    {
        if (damageMeterHudContainer != null) return;

        if (Player.LocalPlayer.PlayerLook == null || Player.LocalPlayer.PlayerLook.Reticle == null) return;

        var parent = Player.LocalPlayer.PlayerLook.Reticle;
        damageMeterHudContainer = new GameObject("DamageMeterHUD");
        damageMeterHudContainer.transform.SetParent(parent, false);

        var containerRect = damageMeterHudContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.2f, 0.9f);
        containerRect.anchorMax = new Vector2(0.2f, 0.9f);
        containerRect.anchoredPosition = new Vector2(0f, 0f);
        containerRect.sizeDelta = new Vector2(300f, 100f);

        var totalDamageGO = new GameObject("TotalDamageText");
        totalDamageGO.transform.SetParent(damageMeterHudContainer.transform, false);
        totalDamageText = totalDamageGO.AddComponent<TextMeshProUGUI>();
        totalDamageText.fontSize = 18;
        totalDamageText.color = Color.white;
        totalDamageText.enableWordWrapping = false;
        totalDamageText.alignment = TextAlignmentOptions.Left;
        totalDamageText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var rect1 = totalDamageGO.GetComponent<RectTransform>();
        rect1.anchorMin = Vector2.zero;
        rect1.anchorMax = Vector2.one;
        rect1.anchoredPosition = new Vector2(0f, 0f);

        var fiveSecGO = new GameObject("FiveSecDamageText");
        fiveSecGO.transform.SetParent(damageMeterHudContainer.transform, false);
        fiveSecDamageText = fiveSecGO.AddComponent<TextMeshProUGUI>();
        fiveSecDamageText.fontSize = 18;
        fiveSecDamageText.color = Color.white;
        fiveSecDamageText.enableWordWrapping = false;
        fiveSecDamageText.alignment = TextAlignmentOptions.Left;
        fiveSecDamageText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var rect2 = fiveSecGO.GetComponent<RectTransform>();
        rect2.anchorMin = Vector2.zero;
        rect2.anchorMax = Vector2.one;
        rect2.anchoredPosition = new Vector2(0f, -25f);

        var killsGO = new GameObject("TotalKillsText");
        killsGO.transform.SetParent(damageMeterHudContainer.transform, false);
        totalKillsText = killsGO.AddComponent<TextMeshProUGUI>();
        totalKillsText.fontSize = 18;
        totalKillsText.color = Color.white;
        totalKillsText.enableWordWrapping = false;
        totalKillsText.alignment = TextAlignmentOptions.Left;
        totalKillsText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var rect3 = killsGO.GetComponent<RectTransform>();
        rect3.anchorMin = Vector2.zero;
        rect3.anchorMax = Vector2.one;
        rect3.anchoredPosition = new Vector2(0f, -50f);

        var coresGO = new GameObject("TotalCoresText");
        coresGO.transform.SetParent(damageMeterHudContainer.transform, false);
        totalCoresText = coresGO.AddComponent<TextMeshProUGUI>();
        totalCoresText.fontSize = 18;
        totalCoresText.color = Color.white;
        totalCoresText.enableWordWrapping = false;
        totalCoresText.alignment = TextAlignmentOptions.Left;
        totalCoresText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var rect4 = coresGO.GetComponent<RectTransform>();
        rect4.anchorMin = Vector2.zero;
        rect4.anchorMax = Vector2.one;
        rect4.anchoredPosition = new Vector2(0f, -75f);
    }

    private void CreateSpeedometerHUD()
    {
        if (speedometerHudContainer != null) return;

        if (Player.LocalPlayer == null || Player.LocalPlayer.PlayerLook == null || Player.LocalPlayer.PlayerLook.Reticle == null) return;

        var parent = Player.LocalPlayer.PlayerLook.Reticle;
        speedometerHudContainer = new GameObject("SpeedometerHUD");
        speedometerHudContainer.transform.SetParent(parent, false);

        var containerRect = speedometerHudContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.2f, 0.9f);
        containerRect.anchorMax = new Vector2(0.2f, 0.9f);
        containerRect.anchoredPosition = new Vector2(0f, 0f);
        containerRect.sizeDelta = new Vector2(300f, 25f);

        GameObject textGO = new GameObject("SpeedText");
        textGO.transform.SetParent(speedometerHudContainer.transform, false);
        speedText = textGO.AddComponent<TextMeshProUGUI>();
        speedText.fontSize = 18;
        speedText.color = Color.white;
        speedText.enableWordWrapping = false;
        speedText.alignment = TextAlignmentOptions.Left;
        speedText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = new Vector2(0f, 0f);

        UpdateHudVisibility();
    }

    private void Update()
    {
        if (!enableGunStatsHUD.Value && !enableDamageMeterHUD.Value && !enableSpeedometerHUD.Value) return;

        if (enableGunStatsHUD.Value)
        {
            if (gunStatsHudContainer == null && Player.LocalPlayer != null && Player.LocalPlayer.PlayerLook != null && Player.LocalPlayer.PlayerLook.Reticle != null)
            {
                CreateGunStatsHUD();
            }
            updateTimer += Time.deltaTime;
            if (updateTimer >= UpdateInterval)
            {
                updateTimer = 0f;
                UpdateCurrentGun();
                UpdateGunStatsHUD();
            }
        }

        if (enableDamageMeterHUD.Value)
        {
            if (Player.LocalPlayer == null) return;
            if (damageMeterHudContainer == null)
            {
                CreateDamageMeterHUD();
                return;
            }

            float now = Time.time;
            float window = dpsWindowSeconds.Value;

            while (damageQueue.Count > 0 && damageQueue.Peek().time < now - window)
            {
                damageQueue.Dequeue();
            }

            float recentDamage = 0f;
            foreach (var entry in damageQueue)
            {
                recentDamage += entry.damage;
            }

            float recentDPS = (damageQueue.Count > 0) ? (recentDamage / window) : 0f;

            float missionTime = Time.time - missionStartTime;
            float totalDPS = (missionTime > 0) ? (totalDamage / missionTime) : 0f;
            float killsPerSec = (missionTime > 0) ? ((float)totalKills / missionTime) : 0f;
            float coresPerSec = (missionTime > 0) ? ((float)totalCoresKilled / missionTime) : 0f;

            totalDamageText.text = $"Total Damage: <color=red>{totalDamage:F0}</color> (<color=red>{totalDPS:F1}</color>/s)";
            fiveSecDamageText.text = $"Last 5sec Damage: <color=red>{recentDamage:F0}</color> (<color=red>{recentDPS:F1}</color>/s)";
            totalKillsText.text = $"Targets Killed: <color=red>{totalKills}</color> (<color=red>{killsPerSec:F1}</color>/s)";
            totalCoresText.text = $"Cores Killed: <color=red>{totalCoresKilled}</color> (<color=red>{coresPerSec:F1}</color>/s)";
        }

        if (enableSpeedometerHUD.Value)
        {
            if (speedometerHudContainer == null)
            {
                CreateSpeedometerHUD();
                return;
            }

            if (speedometerHudContainer == null || speedText == null || Player.LocalPlayer == null)
            {
                if (speedText != null) speedText.text = "No Player";
                return;
            }

            var containerRect = speedometerHudContainer.GetComponent<RectTransform>();
            containerRect.anchoredPosition = new Vector2(0f, (damageMeterHudContainer != null && damageMeterHudContainer.activeSelf) ? -100f : 0f);

            float speed = 0f;

            if (vkField != null || vkProp != null)
            {
                if (vkField != null)
                {
                    object velObj = vkField.GetValue(Player.LocalPlayer);
                    if (velObj is Vector3 vel)
                    {
                        speed = vel.magnitude;
                    }
                }
                else if (vkProp != null)
                {
                    object velObj = vkProp.GetValue(Player.LocalPlayer);
                    if (velObj is Vector3 vel)
                    {
                        speed = vel.magnitude;
                    }
                }
            }
            else if (rbField != null || rbProp != null)
            {
                if (rbField != null)
                {
                    object rbObj = rbField.GetValue(Player.LocalPlayer);
                    if (rbObj is Rigidbody rb)
                    {
                        speed = rb.velocity.magnitude;
                    }
                }
                else if (rbProp != null)
                {
                    object rbObj = rbProp.GetValue(Player.LocalPlayer);
                    if (rbObj is Rigidbody rb)
                    {
                        speed = rb.velocity.magnitude;
                    }
                }
            }

            if (speed == 0f && currentMoveSpeedField != null)
            {
                object cmsObj = currentMoveSpeedField.GetValue(Player.LocalPlayer);
                if (cmsObj is float cms)
                {
                    speed = cms;
                }
            }

            if (speed == 0f && moveVelocityField != null)
            {
                object velObj = moveVelocityField.GetValue(Player.LocalPlayer);
                if (velObj is Vector3 mv)
                {
                    speed = mv.magnitude;
                }
            }

            if (speed > 0f)
            {
                speedText.text = $"Speed: <color=#{ColorUtility.ToHtmlStringRGB(sky)}>{speed:F1}</color> m/s";
            }
            else
            {
                speedText.text = "No Speed Detected";
            }
        }

        if (enableGunStatsHUD.Value && gunStatsHudContainer != null)
        {
            var containerRect = gunStatsHudContainer.GetComponent<RectTransform>();
            float yOffset = 0f;
            if (damageMeterHudContainer != null && damageMeterHudContainer.activeSelf)
            {
                yOffset -= 100f;
            }
            if (speedometerHudContainer != null && speedometerHudContainer.activeSelf)
            {
                yOffset -= 25f;
            }
            containerRect.anchoredPosition = new Vector2(0f, yOffset);
        }
    }

    private void UpdateGunStatsHUD()
    {
        if (gunStatsHudContainer == null || gunStatsTitleText == null || gunStatsStatTexts == null) return;

        if (currentGun == null)
        {
            gunStatsTitleText.text = "No Gun Active";
            for (int i = 0; i < gunStatsStatTexts.Length; i++)
            {
                gunStatsStatTexts[i].text = "";
            }
            return;
        }

        IWeapon weapon = (IWeapon)currentGun;
        UpgradeStatChanges statChanges = new UpgradeStatChanges();
        ref GunData data = ref weapon.GunData;

        Dictionary<string, StatInfo> primaryStats = new Dictionary<string, StatInfo>();
        var primaryEnum = weapon.EnumeratePrimaryStats(statChanges);
        while (primaryEnum.MoveNext())
        {
            primaryStats[primaryEnum.Current.name] = primaryEnum.Current;
        }

        Dictionary<string, StatInfo> secondaryStats = new Dictionary<string, StatInfo>();
        var secondaryEnum = weapon.EnumerateSecondaryStats(statChanges);
        while (secondaryEnum.MoveNext())
        {
            if (secondaryEnum.Current.name != "Aim Zoom")
            {
                secondaryStats[secondaryEnum.Current.name] = secondaryEnum.Current;
            }
        }

        List<string> lines = new List<string>();
        lines.Add("Current Gun Stats:");

        AddStatFromEnum(ref lines, secondaryStats, "Damage");
        AddStatFromEnum(ref lines, primaryStats, "Damage Type");
        AddStatFromEnum(ref lines, secondaryStats, "Fire Rate");
        lines.Add($"Burst Size: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{data.burstSize}</color>");
        lines.Add($"Burst Interval: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{data.burstFireInterval.ToString("F2")}</color>");
        AddStatFromEnumWithCustomLabel(ref lines, secondaryStats, "Ammo Capacity", "Magazine Size");
        lines.Add($"Ammo Capacity: <color=#{ColorUtility.ToHtmlStringRGB(sky)}>{data.ammoCapacity}</color>");
        AddStatFromEnum(ref lines, secondaryStats, "Reload Duration");
        AddStatFromEnum(ref lines, secondaryStats, "Charge Duration");
        lines.Add($"Explosion Size: <color=#{ColorUtility.ToHtmlStringRGB(orchid)}>{Mathf.Round(data.hitForce)}</color>");
        AddStatFromEnum(ref lines, secondaryStats, "Range");
        lines.Add($"Recoil: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>X({Mathf.Round(data.recoilData.recoilX.x)}, {Mathf.Round(data.recoilData.recoilX.y)}) Y({Mathf.Round(data.recoilData.recoilY.x)}, {Mathf.Round(data.recoilData.recoilY.y)})</color>");
        lines.Add($"Spread: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>Size({Mathf.Round(data.spreadData.spreadSize.x)}, {Mathf.Round(data.spreadData.spreadSize.y)})</color>");
        lines.Add($"Fire Mode: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{(data.automatic == 1 ? "Automatic" : "Semi Automatic")}</color>");

        gunStatsTitleText.text = lines[0];
        for (int i = 1; i < lines.Count && i <= gunStatsStatTexts.Length; i++)
        {
            gunStatsStatTexts[i - 1].text = lines[i];
        }
        for (int i = lines.Count - 1; i < gunStatsStatTexts.Length; i++)
        {
            gunStatsStatTexts[i].text = "";
        }
    }

    private void AddStatFromEnumWithCustomLabel(ref List<string> lines, Dictionary<string, StatInfo> stats, string statName, string displayLabel)
    {
        if (stats.TryGetValue(statName, out StatInfo stat))
        {
            string label = displayLabel + ":";
            string value = stat.value;
            Color valueColor = GetStatValueColor(statName);
            lines.Add($"{label} <color=#{ColorUtility.ToHtmlStringRGB(valueColor)}>{value}</color>");
        }
    }

    private void AddStatFromEnum(ref List<string> lines, Dictionary<string, StatInfo> stats, string statName)
    {
        if (stats.TryGetValue(statName, out StatInfo stat))
        {
            string label = stat.name + ":";
            string value = stat.value;
            if (statName == "Fire Rate")
            {
                if (float.TryParse(stat.value, out float rpm))
                {
                    value = (rpm / 60f).ToString("F1") + " /s";
                }
            }
            Color valueColor = (stat.name == "Damage Type") ? stat.color : GetStatValueColor(stat.name);
            lines.Add($"{label} <color=#{ColorUtility.ToHtmlStringRGB(valueColor)}>{value}</color>");
        }
    }

    private Color GetStatValueColor(string statName)
    {
        return statName switch
        {
            "Damage" => rose,
            "Fire Rate" => macaroon,
            "Ammo Capacity" => sky,
            "Reload Duration" => sky,
            "Charge Duration" => sky,
            "Range" => shamrock,
            _ => Color.white
        };
    }

    public static void UpdateCurrentGun()
    {
        if (Player.LocalPlayer == null) return;

        var guns = UnityEngine.Object.FindObjectsOfType<Gun>();
        foreach (var gun in guns)
        {
            var player = (Player)playerField.GetValue(gun);
            if (player == Player.LocalPlayer)
            {
                var gear = (IGear)gun;
                bool isActive = (bool)activeProp.GetValue(gear);
                if (isActive)
                {
                    currentGun = gun;
                    return;
                }
            }
        }
        currentGun = null;
    }

    private void OnDestroy()
    {
        if (gunStatsHudContainer != null)
        {
            UnityEngine.Object.Destroy(gunStatsHudContainer);
        }
        if (damageMeterHudContainer != null)
        {
            UnityEngine.Object.Destroy(damageMeterHudContainer);
        }
        if (speedometerHudContainer != null)
        {
            UnityEngine.Object.Destroy(speedometerHudContainer);
        }
        harmony.UnpatchSelf();
    }

    public void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            wasLocked = (Cursor.lockState == CursorLockMode.Locked);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (wasLocked && Player.LocalPlayer != null && !Keyboard.current.tabKey.isPressed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}

[HarmonyPatch(typeof(Gun), "Enable")]
class GunEnablePatch
{
    [HarmonyPostfix]
    static void Postfix(Gun __instance)
    {
        ExpandedHUDMod.UpdateCurrentGun();
    }
}

[HarmonyPatch(typeof(Gun), "Disable")]
class GunDisablePatch
{
    [HarmonyPostfix]
    static void Postfix(Gun __instance)
    {
        ExpandedHUDMod.UpdateCurrentGun();
    }
}

internal class DPSPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerData), "OnLocalPlayerDamageTarget")]
    private static bool PrefixDamage(in DamageCallbackData data)
    {
        if (data.damageData.damage > 0f)
        {
            ExpandedHUDMod.totalDamage += data.damageData.damage;

            TargetType type = data.target.Type;
            if ((type & TargetType.Enemy) != 0 || (type & TargetType.Player) != 0)
            {
                ExpandedHUDMod.damageQueue.Enqueue((Time.time, data.damageData.damage));
            }

            if (data.KilledTarget)
            {
                ExpandedHUDMod.totalKills++;
                if (typeof(EnemyCore).IsAssignableFrom(data.target.GetType()))
                {
                    ExpandedHUDMod.totalCoresKilled++;
                }
            }
        }

        return true;
    }
}

internal class MissionPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MissionManager), "SpawnHUD")]
    private static void MissionStart()
    {
        ExpandedHUDMod.totalDamage = 0f;
        ExpandedHUDMod.totalKills = 0;
        ExpandedHUDMod.totalCoresKilled = 0;
        ExpandedHUDMod.damageQueue.Clear();
        ExpandedHUDMod.missionStartTime = Time.time;
    }
}
