using MelonLoader;
using StreamSurfers.TwitchIntegration;

[assembly: HarmonyDontPatchAll]
[assembly: MelonInfo(typeof(StreamSurfers.Mod), "StreamSurfers", "1.0.1", "ReservedKeyword")]
[assembly: MelonGame("CayPlay", "WaterparkSimulator")]
[assembly: MelonColor(1, 0, 158, 196)]

namespace StreamSurfers
{
  public class Mod : MelonMod
  {
    public static Mod Instance { get; private set; }

    public ModConfig ModConfig { get; private set; }
    public ChatterManager ChatterManager { get; private set; }

    private HarmonyLib.Harmony harmony;

    public override void OnInitializeMelon()
    {
      Instance = this;
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
  }
}
