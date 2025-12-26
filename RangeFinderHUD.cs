using TMPro;
using UnityEngine;

public class RangeFinderHUD : MonoBehaviour
{
    private TextMeshProUGUI rangeText;

    public void Setup()
    {
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }
        rectTransform.sizeDelta = new Vector2(200f, 50f);
        rectTransform.anchoredPosition = new Vector2(0f, -50f);

        var textObj = new GameObject("RangeFinderText");
        textObj.transform.SetParent(this.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(200f, 50f);
        textRect.anchoredPosition = Vector2.zero;

        rangeText = textObj.AddComponent<TextMeshProUGUI>();
        rangeText.alignment = TextAlignmentOptions.Center;
        rangeText.fontSize = 24f;
        rangeText.color = Color.white;
        rangeText.text = "--- m";

        var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1f, -1f);
    }

    public void UpdateRange(float distance)
    {
        if (distance >= 1000f)
        {
            rangeText.text = "∞ m";
        }
        else if (distance < 0f)
        {
            rangeText.text = "--- m";
        }
        else
        {
            rangeText.text = $"{distance:F1} m";
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (rangeText != null)
        {
            rangeText.gameObject.SetActive(enabled);
        }
    }
}
