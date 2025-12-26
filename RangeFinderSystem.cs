using System;
using Pigeon.Movement;
using UnityEngine;

internal static class RangeFinderSystem
{
    private static RangeFinderHUD rangeFinderHUD;
    private static bool isInitialized = false;

    private static bool EnableRangeFinder => RangeFinder.enableRangeFinder.Value;
    private static float MaxRange => RangeFinder.rangeFinderMaxRange.Value;
    private static LayerMask raycastLayers;

    static RangeFinderSystem()
    {
        raycastLayers = LayerMask.GetMask("Default", "Terrain", "Environment");
    }

    public static void Initialize()
    {
        if (isInitialized) return;

        try
        {
            var hudPrefab = Resources.Load<GameObject>("Prefabs/HUDs/GenericHUD");
            if (hudPrefab == null)
            {
                var hudObj = new GameObject("RangeFinderHUD");
                rangeFinderHUD = hudObj.AddComponent<RangeFinderHUD>();
                rangeFinderHUD.transform.SetParent(PlayerLook.Instance.DefaultHUDParent, false);
            }
            else
            {
                var hudInstance = UnityEngine.Object.Instantiate(hudPrefab, PlayerLook.Instance.DefaultHUDParent);
                rangeFinderHUD = hudInstance.GetComponent<RangeFinderHUD>();
                if (rangeFinderHUD == null)
                {
                    rangeFinderHUD = hudInstance.AddComponent<RangeFinderHUD>();
                }
            }

            rangeFinderHUD.Setup();
            rangeFinderHUD.SetEnabled(EnableRangeFinder);

            isInitialized = true;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Failed to initialize RangeFinder: {ex.Message}");
        }
    }

    public static void UpdateRangeFinder()
    {
        if (!isInitialized || !EnableRangeFinder || rangeFinderHUD == null) return;

        try
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, MaxRange, raycastLayers))
            {
                float distance = hit.distance;
                rangeFinderHUD.UpdateRange(distance);
            }
            else
            {
                rangeFinderHUD.UpdateRange(-1f);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error updating rangefinder: {ex.Message}");
            rangeFinderHUD.UpdateRange(-1f);
        }
    }

    public static void SetEnabled(bool enabled)
    {
        if (rangeFinderHUD != null)
        {
            rangeFinderHUD.SetEnabled(enabled);
        }
    }

    public static void Cleanup()
    {
        if (rangeFinderHUD != null)
        {
            UnityEngine.Object.Destroy(rangeFinderHUD.gameObject);
            rangeFinderHUD = null;
        }
        isInitialized = false;
    }
}
