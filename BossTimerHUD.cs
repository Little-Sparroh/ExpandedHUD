using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using System;
using Pigeon.Movement;

public class BossTimerHUD
{
    private ConfigEntry<bool> enableBossTimerHUD;
    private TextMeshProUGUI timerText;
    private GameObject timerHudContainer;
    private readonly ConfigFile configFile;
    private readonly Harmony harmony;

    private bool isTiming = false;
    private float startTime = 0f;
    private float finalTime = 0f;
    private bool hasFinalTime = false;

    private static readonly Color timerColor = new Color(1f, 0.8f, 0f);

    public static BossTimerHUD Instance { get; private set; }

    public BossTimerHUD(ConfigFile configFile, Harmony harmony)
    {
        this.configFile = configFile;
        this.harmony = harmony;

        Instance = this;

        try
        {
            enableBossTimerHUD = configFile.Bind("General", "EnableBossTimerHUD", false, "Enables the Amalgamation boss timer HUD display.");
            enableBossTimerHUD.SettingChanged += OnEnableBossTimerHUDChanged;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Failed to initialize BossTimerHUD: {ex.Message}");
        }
    }

    public bool IsActive => timerHudContainer != null && timerHudContainer.activeSelf;
    public Vector2 GetSize => timerHudContainer?.GetComponent<RectTransform>().sizeDelta ?? Vector2.zero;

    public void UpdateHudVisibility()
    {
        if (timerHudContainer != null)
        {
            timerHudContainer.SetActive(enableBossTimerHUD.Value);
        }
    }

    private void OnEnableBossTimerHUDChanged(object sender, EventArgs e)
    {
        if (enableBossTimerHUD.Value == false && timerHudContainer != null)
        {
            UnityEngine.Object.Destroy(timerHudContainer);
            timerHudContainer = null;
            timerText = null;
        }
        UpdateHudVisibility();
    }

    private void CreateTimerHUD()
    {
        if (timerHudContainer != null) return;

        if (Player.LocalPlayer == null || Player.LocalPlayer.PlayerLook == null || Player.LocalPlayer.PlayerLook.Reticle == null) return;

        var parent = Player.LocalPlayer.PlayerLook.Reticle;
        timerHudContainer = new GameObject("BossTimerHUD");
        timerHudContainer.transform.SetParent(parent, false);

        var containerRect = timerHudContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.05f); // Bottom center
        containerRect.anchorMax = new Vector2(0.5f, 0.05f);
        containerRect.anchoredPosition = new Vector2(0f, 0f);
        containerRect.sizeDelta = new Vector2(350f, 25f);

        GameObject textGO = new GameObject("TimerText");
        textGO.transform.SetParent(timerHudContainer.transform, false);
        timerText = textGO.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 20;
        timerText.color = Color.white;
        timerText.enableWordWrapping = false;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = new Vector2(0f, 0f);

        UpdateHudVisibility();
    }

    public void StartTimer()
    {
        isTiming = true;
        startTime = Time.realtimeSinceStartup;
        finalTime = 0f;
        hasFinalTime = false;
        SparrohPlugin.Logger.LogInfo("Boss timer started");
    }

    public void StopTimer()
    {
        if (isTiming)
        {
            finalTime = Time.realtimeSinceStartup - startTime;
            isTiming = false;
            hasFinalTime = true;
            SparrohPlugin.Logger.LogInfo($"Boss timer stopped: {FormatTime(finalTime)}");
        }
    }

    public void ResetTimer()
    {
        isTiming = false;
        startTime = 0f;
        finalTime = 0f;
        hasFinalTime = false;
        SparrohPlugin.Logger.LogInfo("Boss timer reset");
    }

    public void Update()
    {
        try
        {
            if (timerHudContainer == null)
            {
                CreateTimerHUD();
                return;
            }

            if (timerHudContainer == null || timerText == null)
            {
                return;
            }

            string displayText;
            if (hasFinalTime)
            {
                displayText = $"Amalgamation: <color=#{ColorUtility.ToHtmlStringRGB(timerColor)}>{FormatTime(finalTime)}</color>";
            }
            else if (isTiming)
            {
                float currentTime = Time.realtimeSinceStartup - startTime;
                displayText = $"Amalgamation: <color=#{ColorUtility.ToHtmlStringRGB(timerColor)}>{FormatTime(currentTime)}</color>";
            }
            else
            {
                displayText = "Amalgamation: Waiting...";
            }

            timerText.text = displayText;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in BossTimerHUD.Update(): {ex.Message}");
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = (int)(timeInSeconds / 60f);
        int seconds = (int)(timeInSeconds % 60f);
        int milliseconds = (int)((timeInSeconds % 1f) * 1000f);

        return $"{minutes:D2}:{seconds:D2}.{milliseconds:D3}";
    }

    public void OnDestroy()
    {
        try
        {
            if (timerHudContainer != null)
            {
                UnityEngine.Object.Destroy(timerHudContainer);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in BossTimerHUD.OnDestroy(): {ex.Message}");
        }
    }
}
