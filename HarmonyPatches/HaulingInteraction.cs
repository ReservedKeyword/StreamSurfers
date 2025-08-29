using System.Collections.Generic;
using CayplayAI;
using HarmonyLib;
using UnityEngine;

namespace StreamSurfers.HarmonyPatches
{
  [HarmonyPatch(typeof(HaulingInteraction))]
  public static class HaulingInteractionPatches
  {
    private static readonly Mod mod = Mod.Instance;
    private static readonly Dictionary<ulong, string> chattersInPark = mod.ChattersInPark;

    private static void LogMsg(string msg)
    {
      mod.LoggerInstance.Msg($"[{nameof(HaulingInteractionPatches)}] {msg}");
    }

    [HarmonyPatch(nameof(HaulingInteraction.chg))]
    [HarmonyPrefix]
    public static void OnHaulingStarted_Prefix(HaulingInteraction __instance, AIBrain a)
    {
      Transform itemToHaul = Traverse.Create(__instance).Field("ItemToHaul").GetValue<Transform>();

      // Stop if the AI is not currently hauling anything
      if (itemToHaul == null)
      {
        return;
      }

      AIBrain patientBrain = itemToHaul.GetComponentInParent<AIBrain>();

      if (patientBrain != null)
      {
        string chatterName = patientBrain.Nameplate.text;
        LogMsg($"Found patient AIBrain for {chatterName}, removing from park...");

        if (chattersInPark.ContainsValue(chatterName))
        {
          chattersInPark.Remove(patientBrain.NetworkObjectId);
          LogMsg($"Chatter {chatterName} picked up by medic, removed from park.");
        }
      }
    }
  }
}
