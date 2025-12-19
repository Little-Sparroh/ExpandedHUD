using System;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Pigeon.Movement;

public class Altimeter
{
    private ConfigEntry<bool> enableAltimeterHUD;
    private TextMeshProUGUI altitudeText;
    private GameObject altitudeHudContainer;
    private const float RaycastMaxDistance = 1000f;
    private static readonly Color altitudeGreen = new Color(0.011764705882352941f, 0.6745098039215687f, 0.07450980392156863f);

    private readonly ConfigFile configFile;
    private readonly Harmony harmony;

    public static Altimeter Instance { get; private set; }

    public Altimeter(ConfigFile configFile, Harmony harmony)
    {
        this.configFile = configFile;
        this.harmony = harmony;

        Instance = this;

        enableAltimeterHUD = configFile.Bind("General", "EnableAltimeterHUD", true, "Enables the Altimeter HUD display showing player altitude above ground.");

        enableAltimeterHUD.SettingChanged += OnEnableAltimeterHUDChanged;
    }

    public bool IsActive => altitudeHudContainer != null && altitudeHudContainer.activeSelf;
    public Vector2 GetSize => altitudeHudContainer?.GetComponent<RectTransform>().sizeDelta ?? Vector2.zero;

    public void UpdateHudVisibility()
    {
        if (altitudeHudContainer != null)
        {
            altitudeHudContainer.SetActive(enableAltimeterHUD.Value);
        }
    }

    private void OnEnableAltimeterHUDChanged(object sender, EventArgs e)
    {
        if (enableAltimeterHUD.Value == false && altitudeHudContainer != null)
        {
            UnityEngine.Object.Destroy(altitudeHudContainer);
            altitudeHudContainer = null;
            altitudeText = null;
        }
        UpdateHudVisibility();
    }

    private void CreateAltimeterHUD()
    {
        if (altitudeHudContainer != null) return;

        if (Player.LocalPlayer == null || Player.LocalPlayer.PlayerLook == null || Player.LocalPlayer.PlayerLook.Reticle == null) return;

        var parent = Player.LocalPlayer.PlayerLook.Reticle;
        altitudeHudContainer = new GameObject("AltimeterHUD");
        altitudeHudContainer.transform.SetParent(parent, false);

        var containerRect = altitudeHudContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.2f, 0.95f);
        containerRect.anchorMax = new Vector2(0.2f, 0.95f);
        containerRect.anchoredPosition = new Vector2(0f, 0f);
        containerRect.sizeDelta = new Vector2(300f, 25f);

        GameObject textGO = new GameObject("AltitudeText");
        textGO.transform.SetParent(altitudeHudContainer.transform, false);
        altitudeText = textGO.AddComponent<TextMeshProUGUI>();
        altitudeText.fontSize = 18;
        altitudeText.color = Color.white;
        altitudeText.enableWordWrapping = false;
        altitudeText.alignment = TextAlignmentOptions.Left;
        altitudeText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = new Vector2(0f, 0f);

        UpdateHudVisibility();
    }

    public void Update()
    {
        if (!enableAltimeterHUD.Value) return;

        if (altitudeHudContainer == null)
        {
            CreateAltimeterHUD();
            return;
        }

        if (altitudeText == null || Player.LocalPlayer == null) return;

        float altitude = CalculateAltitude();

        if (altitude < 1.3f)
        {
            altitudeText.text = "On Ground";
        }
        else if (altitude >= RaycastMaxDistance)
        {
            altitudeText.text = "Too High";
        }
        else
        {
            altitudeText.text = $"Altitude: <color=#{ColorUtility.ToHtmlStringRGB(altitudeGreen)}>{altitude:F1}</color> m";
        }

        if (altitudeHudContainer != null)
        {
            var containerRect = altitudeHudContainer.GetComponent<RectTransform>();
            float yOffset = (Speedometer.Instance != null && Speedometer.Instance.IsActive ? -Speedometer.Instance.GetSize.y : 0f) +
                            (Carnometer.Instance != null && Carnometer.Instance.IsActive ? -Carnometer.Instance.GetSize.y : 0f);
            containerRect.anchoredPosition = new Vector2(0f, yOffset);
        }

    }

    private float CalculateAltitude()
    {
        if (Player.LocalPlayer == null) return 0f;

        Vector3 start = Player.LocalPlayer.transform.position + Vector3.up * 0.1f;
        Vector3 direction = Vector3.down;

        RaycastHit[] hits = Physics.RaycastAll(start, direction, RaycastMaxDistance);

        float minDistance = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject != Player.LocalPlayer.gameObject && !hit.collider.isTrigger)
            {
                minDistance = Mathf.Min(minDistance, hit.distance);
            }
        }

        return minDistance < float.MaxValue ? minDistance : RaycastMaxDistance;
    }

    public void OnDestroy()
    {
        if (altitudeHudContainer != null)
        {
            UnityEngine.Object.Destroy(altitudeHudContainer);
        }
    }
}
