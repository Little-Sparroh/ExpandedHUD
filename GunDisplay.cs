using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using Pigeon.Movement;

public class GunDisplay
{
    private ConfigEntry<bool> enableGunStatsHUD;
    private float updateTimer = 0f;
    private const float UpdateInterval = 0.5f;
    private GameObject gunStatsHudContainer;
    private TextMeshProUGUI gunStatsTitleText;
    private TextMeshProUGUI[] gunStatsStatTexts;
    private const int NUM_STAT_LINES = 25;

    public static Gun currentGun;
    public static float currentGunActualDamage = 0f;
    private static FieldInfo playerField;
    private static PropertyInfo activeProp;
    private static MethodInfo modifyBulletDataMethod;
    private static FieldInfo activeUpgradesField;

    private static readonly Color sky = new Color(0.529f, 0.808f, 0.922f);
    private static readonly Color orchid = new Color(0.855f, 0.439f, 0.839f);
    private static readonly Color rose = new Color(0.8901960784313725f, 0.1411764705882353f, 0.16862745098039217f);
    private static readonly Color macaroon = new Color(0.9764705882352941f, 0.8784313725490196f, 0.4627450980392157f);
    private static readonly Color shamrock = new Color(0.011764705882352941f, 0.6745098039215687f, 0.07450980392156863f);

    private readonly ConfigFile configFile;
    private readonly Harmony harmony;

    public GunDisplay(ConfigFile configFile, Harmony harmony)
    {
        this.configFile = configFile;
        this.harmony = harmony;

        try
        {
            enableGunStatsHUD = configFile.Bind("General", "EnableGunStatsHUD", true, "If true, the gun stats HUD will be displayed.");
            enableGunStatsHUD.SettingChanged += OnEnableGunStatsHUDChanged;

            playerField = typeof(Gun).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance);
            activeProp = typeof(IGear).GetProperty("Active");
            modifyBulletDataMethod = typeof(Gun).GetMethod("ModifyBulletData", BindingFlags.NonPublic | BindingFlags.Instance);
            activeUpgradesField = typeof(Player).GetField("ActiveUpgrades", BindingFlags.NonPublic | BindingFlags.Instance);

            harmony.PatchAll(typeof(GunEnablePatch));
            harmony.PatchAll(typeof(GunDisablePatch));
            harmony.PatchAll(typeof(GunFiredPatch));
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Failed to initialize GunDisplay: {ex.Message}");
        }
    }

    public bool IsActive => gunStatsHudContainer != null && gunStatsHudContainer.activeSelf;
    public Vector2 GetSize => gunStatsHudContainer?.GetComponent<RectTransform>().sizeDelta ?? Vector2.zero;

    public void UpdateHudVisibility()
    {
        if (gunStatsHudContainer != null)
        {
            gunStatsHudContainer.SetActive(enableGunStatsHUD.Value);
        }
    }

    private void OnEnableGunStatsHUDChanged(object sender, EventArgs e)
    {
        UpdateHudVisibility();
    }

    private void CreateGunStatsHUD()
    {
        if (gunStatsHudContainer != null) return;

        var parent = Player.LocalPlayer.PlayerLook.Reticle;
        gunStatsHudContainer = new GameObject("GunStatsHUD");
        gunStatsHudContainer.transform.SetParent(parent, false);

        var containerRect = gunStatsHudContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.18f, 1.14f);
        containerRect.anchorMax = new Vector2(0.18f, 1.14f);
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

    public void Update()
    {
        try
        {
            if (!enableGunStatsHUD.Value) return;

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

            AdjustPosition();
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in GunDisplay.Update(): {ex.Message}");
        }
    }

    public Vector2 GetGunStatsHUDSize()
    {
        return gunStatsHudContainer != null ? gunStatsHudContainer.GetComponent<RectTransform>().sizeDelta : Vector2.zero;
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

        if (secondaryStats.TryGetValue("Damage", out StatInfo damageStat) && currentGunActualDamage > 0f)
        {
            damageStat.value = $"{currentGunActualDamage:F1}";
        }

        List<string> lines = new List<string>();
        lines.Add("Current Gun Stats:");

        AddStatFromEnum(ref lines, secondaryStats, "Damage");
        lines.Add($"Bullets per Shot: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{data.bulletsPerShot}</color>");
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
            if (statName == "Damage" && currentGunActualDamage > 0f)
            {
                value = $"{currentGunActualDamage:F1}";
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

    private void AdjustPosition()
    {
        if (gunStatsHudContainer != null)
        {
            var containerRect = gunStatsHudContainer.GetComponent<RectTransform>();
            float yOffset = 0f;
            if (Carnometer.Instance != null && Carnometer.Instance.IsActive)
            {
                yOffset -= Carnometer.Instance.GetSize.y;
            }
            if (Speedometer.Instance != null && Speedometer.Instance.IsActive)
            {
                yOffset -= Speedometer.Instance.GetSize.y;
            }
            if (Altimeter.Instance != null && Altimeter.Instance.IsActive)
            {
                yOffset -= Altimeter.Instance.GetSize.y;
            }
            containerRect.anchoredPosition = new Vector2(0f, yOffset);
        }
    }

    public void OnDestroy()
    {
        try
        {
            if (gunStatsHudContainer != null)
            {
                UnityEngine.Object.Destroy(gunStatsHudContainer);
            }
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in GunDisplay.OnDestroy(): {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(Gun), "Enable")]
    class GunEnablePatch
    {
        [HarmonyPostfix]
        static void Postfix(Gun __instance)
        {
            UpdateCurrentGun();
        }
    }

    [HarmonyPatch(typeof(Gun), "Disable")]
    class GunDisablePatch
    {
        [HarmonyPostfix]
        static void Postfix(Gun __instance)
        {
            UpdateCurrentGun();
        }
    }

    [HarmonyPatch(typeof(Gun), "OnFiredBullet")]
    class GunFiredPatch
    {
        [HarmonyPostfix]
        static void Postfix(Gun __instance, IBullet bullet, BulletFlags flags, int shotIndex, ref BulletData bulletData)
        {
            if (__instance == currentGun)
            {
                currentGunActualDamage = bulletData.damage;
            }
            else
            {
            }
        }
    }
}
