using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using StreamSurfers.TwitchIntegration;

[assembly: HarmonyDontPatchAll]
[assembly: MelonInfo(typeof(StreamSurfers.Mod), "StreamSurfers", "1.0.2", "ReservedKeyword")]
[assembly: MelonGame("CayPlay", "WaterparkSimulator")]
[assembly: MelonColor(1, 0, 158, 196)]

namespace StreamSurfers
{
  public class Mod : MelonMod
  {
    public static Mod Instance { get; private set; }

    public ModConfig ModConfig { get; private set; }
    public ChatterManager ChatterManager { get; private set; }
    public Dictionary<ulong, string> ChattersInPark { get; private set; }

    private HarmonyLib.Harmony harmony;

    private void ClearChattersInPark()
    {
      LogMsg($"New in-game day! All chatters cleared from in-game park cache!");
      ChattersInPark.Clear();
    }

    private void LogMsg(string msg)
    {
      LoggerInstance.Msg($"[{nameof(Mod)}] {msg}");
    }

    public override void OnInitializeMelon()
    {
      Instance = this;
      ChattersInPark = [];
      ModConfig = new();

      if (!ModConfig.IsEnabled)
      {
        LoggerInstance.Msg("Plugin disabled in configuration file, won't proceed...");
        return;
      }

      // Setup Twitch chatter integration
      ChatterManager = new(this, ModConfig);
      ChatterManager.Connect();

      // Setup Harmony patches
      harmony = new(Constants.ToHarmonyID());
      harmony.PatchAll();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
      if (sceneName == Constants.GAMEPLAY_SCENE_NAME)
      {
        LogMsg("GameplayScene loaded! Starting coroutine, subscribing to OnNewDayStarted event.");
        MelonCoroutines.Start(WaitForGameManagerAndSubscribe());
      }
    }

    private IEnumerator WaitForGameManagerAndSubscribe()
    {
      // Wait for the GameManager instance to be ready
      while (GameManager.rzy == null)
      {
        yield return null;
      }

      GameManager.rzy.OnNewDayStarted.AddListener(ClearChattersInPark);
      LogMsg("Successfully subscribed to OnNewDayStarted event!");
    }
  }
}
