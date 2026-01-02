using BepInEx.Configuration;
using HarmonyLib;

public class BossTimer
{
    private BossTimerHUD bossTimerHUD;
    private readonly ConfigFile configFile;
    private readonly Harmony harmony;

    public BossTimer(ConfigFile configFile, Harmony harmony)
    {
        this.configFile = configFile;
        this.harmony = harmony;

        bossTimerHUD = new BossTimerHUD(configFile, harmony);
    }

    public void UpdateHudVisibility()
    {
        bossTimerHUD.UpdateHudVisibility();
    }

    public void Update()
    {
        bossTimerHUD.Update();
    }

    public void OnDestroy()
    {
        bossTimerHUD.OnDestroy();
    }
}
