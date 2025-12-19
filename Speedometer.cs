using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;
using System;
using Pigeon.Movement;

public class Speedometer
{
    private ConfigEntry<bool> enableSpeedometerHUD;
    private TextMeshProUGUI speedText;
    private GameObject speedometerHudContainer;
    private FieldInfo currentMoveSpeedField;
    private FieldInfo vkField;
    private FieldInfo rbField;
    private FieldInfo moveVelocityField;
    private PropertyInfo vkProp;
    private PropertyInfo rbProp;

    private static readonly Color sky = new Color(0.529f, 0.808f, 0.922f);

    private readonly ConfigFile configFile;
    private readonly Harmony harmony;

    public static Speedometer Instance { get; private set; }

    public Speedometer(ConfigFile configFile, Harmony harmony)
    {
        this.configFile = configFile;
        this.harmony = harmony;

        Instance = this;

        enableSpeedometerHUD = configFile.Bind("General", "EnableSpeedometerHUD", true, "Enables the speedometer HUD display.");

        enableSpeedometerHUD.SettingChanged += OnEnableSpeedometerHUDChanged;

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
    }

    public bool IsActive => speedometerHudContainer != null && speedometerHudContainer.activeSelf;
    public Vector2 GetSize => speedometerHudContainer?.GetComponent<RectTransform>().sizeDelta ?? Vector2.zero;

    public void UpdateHudVisibility()
    {
        if (speedometerHudContainer != null)
        {
            speedometerHudContainer.SetActive(enableSpeedometerHUD.Value);
        }
    }

    private void OnEnableSpeedometerHUDChanged(object sender, EventArgs e)
    {
        if (enableSpeedometerHUD.Value == false && speedometerHudContainer != null)
        {
            UnityEngine.Object.Destroy(speedometerHudContainer);
            speedometerHudContainer = null;
            speedText = null;
        }
        UpdateHudVisibility();
    }

    private void CreateSpeedometerHUD()
    {
        if (speedometerHudContainer != null) return;

        if (Player.LocalPlayer == null || Player.LocalPlayer.PlayerLook == null || Player.LocalPlayer.PlayerLook.Reticle == null) return;

        var parent = Player.LocalPlayer.PlayerLook.Reticle;
        speedometerHudContainer = new GameObject("SpeedometerHUD");
        speedometerHudContainer.transform.SetParent(parent, false);

        var containerRect = speedometerHudContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.2f, 0.95f);
        containerRect.anchorMax = new Vector2(0.2f, 0.95f);
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

    public void Update()
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

        if (speedometerHudContainer != null)
        {
            var containerRect = speedometerHudContainer.GetComponent<RectTransform>();
            float yOffset = Carnometer.Instance != null && Carnometer.Instance.IsActive ? -Carnometer.Instance.GetSize.y : 0f;
            containerRect.anchoredPosition = new Vector2(0f, yOffset);
        }

    }

    public void OnDestroy()
    {
        if (speedometerHudContainer != null)
        {
            UnityEngine.Object.Destroy(speedometerHudContainer);
        }
    }
}
