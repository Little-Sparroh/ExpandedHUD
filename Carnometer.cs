using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;
using Pigeon.Movement;

public class Carnometer
{
    private ConfigEntry<bool> enableDamageMeterHUD;
    private ConfigEntry<float> dpsWindowSeconds;
    private ConfigEntry<float> carnometerAnchorX;
    private ConfigEntry<float> carnometerAnchorY;
    private GameObject damageMeterHudContainer;
    private TextMeshProUGUI totalDamageText;
    private TextMeshProUGUI fiveSecDamageText;
    private TextMeshProUGUI totalKillsText;
    private TextMeshProUGUI totalCoresText;
    public Queue<(float time, float damage)> damageQueue = new Queue<(float, float)>();
    public float totalDamage = 0f;
    public int totalKills = 0;
    public int totalCoresKilled = 0;
    public float missionStartTime = 0f;

    private static readonly Color rose = new Color(0.8901960784313725f, 0.1411764705882353f, 0.16862745098039217f);

    private readonly ConfigFile configFile;
    private readonly Harmony harmony;

    public static Carnometer Instance { get; private set; }

    public Carnometer(ConfigFile configFile, Harmony harmony)
    {
        this.configFile = configFile;
        this.harmony = harmony;

        Instance = this;

        enableDamageMeterHUD = configFile.Bind("General", "EnableDamageMeterHUD", true, "Enable or disable the damage meter functionality.");
        enableDamageMeterHUD.SettingChanged += OnEnableDamageMeterHUDChanged;

        dpsWindowSeconds = configFile.Bind("General", "DPSWindowSeconds", 5f, "Time window (in seconds) for calculating DPS. Higher values smooth out spikes.");

        carnometerAnchorX = configFile.Bind("HUD Positioning", "CarnometerAnchorX", 0.15f, "X anchor position for Carnometer (0-1).");
        carnometerAnchorY = configFile.Bind("HUD Positioning", "CarnometerAnchorY", 0.95f, "Y anchor position for Carnometer (0-1).");
        carnometerAnchorX.SettingChanged += OnAnchorChanged;
        carnometerAnchorY.SettingChanged += OnAnchorChanged;

        harmony.PatchAll(typeof(DPSPatches));
        harmony.PatchAll(typeof(MissionPatches));

        missionStartTime = Time.time;
    }

    public bool IsActive => damageMeterHudContainer != null && damageMeterHudContainer.activeSelf;
    public Vector2 GetSize => damageMeterHudContainer?.GetComponent<RectTransform>().sizeDelta ?? Vector2.zero;

    public void UpdateHudVisibility()
    {
        if (damageMeterHudContainer != null)
        {
            damageMeterHudContainer.SetActive(enableDamageMeterHUD.Value);
        }
    }

    private void OnEnableDamageMeterHUDChanged(object sender, EventArgs e)
    {
        UpdateHudVisibility();
    }

    private void OnAnchorChanged(object sender, EventArgs e)
    {
        UpdateAnchors();
    }

    private void UpdateAnchors()
    {
        if (damageMeterHudContainer != null)
        {
            var containerRect = damageMeterHudContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = containerRect.anchorMax = new Vector2(carnometerAnchorX.Value, carnometerAnchorY.Value);
        }
    }

    private void CreateDamageMeterHUD()
    {
        if (damageMeterHudContainer != null) return;

        if (Player.LocalPlayer.PlayerLook == null || Player.LocalPlayer.PlayerLook.Reticle == null) return;

        var parent = Player.LocalPlayer.PlayerLook.Reticle;
        damageMeterHudContainer = new GameObject("DamageMeterHUD");
        damageMeterHudContainer.transform.SetParent(parent, false);

        var containerRect = damageMeterHudContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = containerRect.anchorMax = new Vector2(carnometerAnchorX.Value, carnometerAnchorY.Value);
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

        UpdateHudVisibility();
    }

    public void Update()
    {
        if (!enableDamageMeterHUD.Value) return;

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

    public void OnDestroy()
    {
        if (damageMeterHudContainer != null)
        {
            UnityEngine.Object.Destroy(damageMeterHudContainer);
        }
        harmony.UnpatchSelf();
    }

    internal class DPSPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerData), "OnLocalPlayerDamageTarget")]
        private static bool PrefixDamage(in DamageCallbackData data)
        {
            if (Instance == null) return true;

            if (data.damageData.damage > 0f)
            {
                Instance.totalDamage += data.damageData.damage;

                TargetType type = data.target.Type;
                if ((type & TargetType.Enemy) != 0 || (type & TargetType.Player) != 0)
                {
                    Instance.damageQueue.Enqueue((Time.time, data.damageData.damage));
                }

                if (data.KilledTarget)
                {
                    Instance.totalKills++;
                    if (typeof(EnemyCore).IsAssignableFrom(data.target.GetType()))
                    {
                        Instance.totalCoresKilled++;
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
            if (Instance != null)
            {
                Instance.totalDamage = 0f;
                Instance.totalKills = 0;
                Instance.totalCoresKilled = 0;
                Instance.damageQueue.Clear();
                Instance.missionStartTime = Time.time;
            }
        }
    }
}
