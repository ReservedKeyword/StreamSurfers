using System.Collections.Generic;
using CayplayAI;
using HarmonyLib;
using StreamSurfers.TwitchIntegration;

namespace StreamSurfers.HarmonyPatches
{
  [HarmonyPatch(typeof(AIBrain))]
  public static class AIBrain_Patch
  {
    private static readonly Mod mod = Mod.Instance;
    private static readonly ChatterManager chatterManager = mod.ChatterManager;

    private static readonly int MAX_CHATTER_FETCH_ATTEMPTS = 5;
    private static readonly Dictionary<ulong, string> chattersInPark = [];

    private static void LogMsg(string msg)
    {
      mod.LoggerInstance.Msg($"[{nameof(AIBrain_Patch)}] {msg}");
    }

    private static void OnEnteringPark(AIBrain ownerBrain)
    {
      AIDataStorage dataStorage = ownerBrain.Data;
      EGameStage gameState = GameManager.rzy.ygl;
      ulong networkObjectId = ownerBrain.NetworkObjectId;
      SimpleInteraction targetInteraction = ownerBrain.yqh;
      EInteractionType targetInteractionType = targetInteraction.yec;

      // Stop if the AI is either (a) a staff member or (b) has the character trait Queso.
      if (dataStorage.yrj || dataStorage.fna(CharacterTraits.Queso))
      {
        return;
      }

      // Stop if the AI is already in the park, the park is not open, or they're
      // not moving toward any specific target.
      if (
        chattersInPark.ContainsKey(networkObjectId)
        || gameState != EGameStage.ParkOpened
        || targetInteraction == null
      )
      {
        return;
      }

      bool isLegitimateEntry = targetInteractionType == EInteractionType.TicketStation;
      bool isSneakingIn =
        dataStorage.fna(CharacterTraits.TicketCheater)
        && targetInteractionType == EInteractionType.ChangingRoom;

      // Stop if we're not heading toward a ticket station, or we're not sneaking in.
      if (!isLegitimateEntry && !isSneakingIn)
      {
        return;
      }

      string chatterName = null;
      int attemptNum = 0;

      // Loop until the maximum reached, attempting to fetch a new, unique chatter name of
      // someone who is not already in the park, or fail if no chatters could be found.
      while (attemptNum < MAX_CHATTER_FETCH_ATTEMPTS)
      {
        string potentialName = chatterManager.GetRandomChatter();

        if (potentialName == null)
        {
          break;
        }

        if (!chattersInPark.ContainsValue(potentialName))
        {
          chatterName = potentialName;
          break;
        }

        attemptNum++;
      }

      // Stop if we weren't able to pick a new, random, unique chatter.
      if (chatterName == null)
      {
        LogMsg($"No chatter found for {ownerBrain.Nameplate.text}, their name remains the same.");
        return;
      }

      if (ownerBrain.Nameplate != null)
      {
        dataStorage.yqy = chatterName;
        ownerBrain.Nameplate.text = chatterName;
        chattersInPark.Add(networkObjectId, chatterName);
        LogMsg($"Adding chatter {chatterName} ({networkObjectId}) to the park!");
      }
    }

    private static void OnLeavingPark(AIBrain ownerBrain)
    {
      ulong networkObjectId = ownerBrain.NetworkObjectId;

      if (chattersInPark.ContainsKey(networkObjectId))
      {
        string ownerName = chattersInPark[networkObjectId];
        chattersInPark.Remove(networkObjectId);
        LogMsg($"Cleaning up {ownerName}, they're leaving the park!");
      }
    }

    [HarmonyPatch(nameof(AIBrain.fha))]
    [HarmonyPrefix]
    public static void OnStateTransition_Prefix(AIBrain __instance, string a)
    {
      string nextStateName = a;

      if (nextStateName == "LeavingPark")
      {
        OnLeavingPark(__instance);
        return;
      }

      AIState currentState = __instance.syf;
      bool isStartingToTravel = currentState.Name == "Idle";
      bool isGoingToAttraction = nextStateName == "GoToTargetAttraction";

      if (isStartingToTravel && isGoingToAttraction)
      {
        OnEnteringPark(__instance);
        return;
      }
    }
  }
}
