using System;
using BepInEx;
using BepInEx.Configuration;
using UnboundLib;
using HarmonyLib;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnboundLib.Utils.UI;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnboundLib.GameModes;
using System.Linq;
using Photon.Pun;
using UnboundLib.Networking;
using System.Collections.Generic;
using ModdingUtils.Utils;
using UnityEngine.EventSystems;

namespace CompetitiveRounds
{

    [Serializable]
    [HarmonyPatch(typeof(CardChoice), "DoPick")]
    class CardChoicePatchDoPick
    {
        internal static Dictionary<Player, bool> playerHasPicked = new Dictionary<Player, bool>() { };
        private static bool Prefix(CardChoice __instance, int picketIDToSet)
        {
            Player player = (Player)typeof(PlayerManager).InvokeMember("GetPlayerWithID",
                BindingFlags.Instance | BindingFlags.InvokeMethod |
                BindingFlags.NonPublic, null, PlayerManager.instance, new object[] { picketIDToSet });
            // skip pick phase if the player has passed
            if ((CompetitiveRounds.MaxCards > 0 && ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(player.teamID).Length - 1 >= CompetitiveRounds.MaxCards && !CompetitiveRounds.DiscardAfterPick && CompetitiveRounds.PassDiscard && !playerHasPicked[player]) || PreGamePickBanHandler.skipFirstPickPhase)
            {
                playerHasPicked[player] = false;
                __instance.IsPicking = false;
                return false;
            }
            else
            {
                playerHasPicked[player] = true;
                return true;
            }
        }
    }
    [Serializable]
    [HarmonyPatch(typeof(CardChoiceVisuals), "Show")]
    class CardChoiceVisualsPatchShow
    {
        internal static Dictionary<Player, bool> playerHasPicked = new Dictionary<Player, bool>() { };
        private static bool Prefix(CardChoiceVisuals __instance, int pickerID)
        {
            Player player = (Player)typeof(PlayerManager).InvokeMember("GetPlayerWithID",
                BindingFlags.Instance | BindingFlags.InvokeMethod |
                BindingFlags.NonPublic, null, PlayerManager.instance, new object[] { pickerID });
            // skip pick phase if the player has passed
            if ((CompetitiveRounds.MaxCards > 0 && ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(player.teamID).Length - 1 >= CompetitiveRounds.MaxCards && !CompetitiveRounds.DiscardAfterPick && CompetitiveRounds.PassDiscard && !playerHasPicked[player]) || PreGamePickBanHandler.skipFirstPickPhase)
            {
                playerHasPicked[player] = false;
                return false;
            }
            else
            {
                playerHasPicked[player] = true;
                return true;
            }
        }
    }
}

