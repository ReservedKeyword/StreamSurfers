using System.Collections.Generic;
using CayplayAI;
using HarmonyLib;

namespace StreamSurfers.HarmonyPatches
{
  [HarmonyPatch(typeof(AIBrain))]
  public static class AIBrain_Patch
  {
    private static readonly Mod mod = Mod.Instance;
    private static readonly int MAX_CHATTER_FETCH_ATTEMPTS = 5;
    private static readonly Dictionary<ulong, string> chattersInPark = [];

    private static void LogMsg(string msg)
    {
      mod.LoggerInstance.Msg($"[{nameof(AIBrain_Patch)}] {msg}");
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
        OnEnteringPark(mod, __instance);
        return;
      }
    }

    private static void OnEnteringPark(Mod mod, AIBrain aiBrain)
    {
      AIDataStorage dataStorage = aiBrain.Data;
      EGameStage gameState = GameManager.rzy.ygl;
      ulong networkObjectId = aiBrain.NetworkObjectId;
      SimpleInteraction targetInteraction = aiBrain.yqh;
      EInteractionType targetInteractionType = targetInteraction.yec;

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

      // Return if we're not heading to a ticket station and we're not sneaking in
      if (!isLegitimateEntry && !isSneakingIn)
      {
        return;
      }

      string chatterName = null;
      int attemptNum = 0;

      // Attempt to fetch a unique chatter that's not already in the park, until max
      while (attemptNum < MAX_CHATTER_FETCH_ATTEMPTS)
      {
        string potentialName = mod.ChatterManager.GetRandomChatter();

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

      // Return if we weren't able to pick a chatter from the Set.
      if (chatterName == null)
      {
        LogMsg($"No chatter found for {aiBrain.Nameplate.text}, their name remains the same.");
        return;
      }

      if (aiBrain.Nameplate != null)
      {
        aiBrain.Nameplate.text = chatterName;
      }

      chattersInPark.Add(networkObjectId, chatterName);
      LogMsg($"Adding chatter {chatterName} ({networkObjectId}) to the park!");
    }

    private static void OnLeavingPark(AIBrain aiBrain)
    {
      ulong networkObjectId = aiBrain.NetworkObjectId;

      if (chattersInPark.ContainsKey(networkObjectId))
      {
        string ownerName = chattersInPark[networkObjectId];
        chattersInPark.Remove(networkObjectId);
        LogMsg($"Cleaning up {ownerName}, they're leaving the park!");
      }
    }
  }
}
