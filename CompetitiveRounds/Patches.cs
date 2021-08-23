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
        private static bool Prefix(CardChoice __instance)
        {

            // skip pick phase if the player has passed
            if (MaxCardsHandler.pass || PreGamePickBanHandler.skipFirstPickPhase)
            {
                __instance.IsPicking = false;
                return false;
            }
            else
            {
                return true;
            }
        }
    }
    [Serializable]
    [HarmonyPatch(typeof(CardChoiceVisuals), "Show")]
    class CardChoiceVisualsPatchShow
    {
        private static bool Prefix(CardChoiceVisuals __instance)
        {
            // skip pick phase if the player has passed
            if (MaxCardsHandler.pass || PreGamePickBanHandler.skipFirstPickPhase)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}

