using Pigeon.Movement;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal class HealthMonoUI : MonoBehaviour
{
  public static TextMeshProUGUI text;
  public float health;
  public float healthPercent;
  public Color healthColor = Color.white;
  public static Vector3 position = new Vector3(21.2162f, -45.0458f, -5.5684f);
  public static Vector3 position2 = new Vector3(187.9581f, -44.3458f, -5.5684f);
  public static Quaternion rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
  public static Vector3 scale = new Vector3(1f, 1f, 1f);
  public bool isDamaged;

  private static FieldInfo playerLookField;

  public void Awake()
  {
    if (playerLookField == null)
    {
      playerLookField = typeof(Player).GetField("playerLook", BindingFlags.NonPublic | BindingFlags.Instance);
    }
    PlayerLook playerLook = (PlayerLook)playerLookField.GetValue(Player.LocalPlayer);
    Transform hudParent = playerLook.DefaultHUDParent;
    HealthMonoUI.text = this.gameObject.AddComponent<TextMeshProUGUI>();
    HealthMonoUI.text.fontSize = 24f;
    ((Graphic) HealthMonoUI.text).color = Color.red;
    HealthMonoUI.text.rectTransform.sizeDelta = new UnityEngine.Vector2(250f, 50f);
    this.gameObject.transform.SetParent(hudParent);
    this.gameObject.transform.rotation = HealthMonoUI.rotation;
    this.gameObject.transform.localPosition = new Vector3(0f, 375f, 0f);
    this.gameObject.transform.localScale = HealthMonoUI.scale;
    HealthMonoUI.text.gameObject.layer = 5;
    HealthMonoUI.text.text = $"100% ({Math.Round((double) this.health, 2):F2})";
  }

  public void Update()
  {
    if (this.isDamaged)
    {
      HealthMonoUI.text.text = $"{(float)(this.healthPercent * 100.0):F2}% ({Math.Round((double) this.health, 2):F2})";
      float weightedHealth = Player.CalculateWeightedHealth(this.healthPercent);
      ((Graphic) HealthMonoUI.text).color = Global.Instance.HealthGradient.Evaluate(weightedHealth);
      this.isDamaged = false;
    }
  }
}
