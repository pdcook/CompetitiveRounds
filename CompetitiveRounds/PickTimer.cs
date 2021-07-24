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

namespace CompetitiveRounds
{
    internal static class PickTimer
    {
        private static System.Random rng = new System.Random();
        private static TextMeshProUGUI timer;
        internal static GameObject timerCanvas = null;
        internal static Coroutine timerCR = null;

        internal static IEnumerator StartPickTimer(CardChoice instance)
        {
            if (timerCR != null)
            {
                Unbound.Instance.StopCoroutine(timerCR);
            }
            timerCR = Unbound.Instance.StartCoroutine(PickTimer.Timer(CompetitiveRounds.PickTimer));
            yield return new WaitForSecondsRealtime(CompetitiveRounds.PickTimer);
            instance.Pick(((List<GameObject>)Traverse.Create(instance).Field("spawnedCards").GetValue())[rng.Next(0, ((List<GameObject>)Traverse.Create(instance).Field("spawnedCards").GetValue()).Count)], false);
            Traverse.Create(instance).Field("pickrID").SetValue(-1);
            yield break;
        }
        private static IEnumerator Timer(float timeToWait)
        {
            float start = Time.time;
            if (PickTimer.timer == null)
            {
                PickTimer.CreateText();
            }
            PickTimer.timer.color = Color.white;
            timerCanvas.SetActive(true);

            while (Time.time < start + timeToWait)
            {
                PickTimer.timer.text = UnityEngine.Mathf.CeilToInt(start + timeToWait - Time.time).ToString();
                if (UnityEngine.Mathf.CeilToInt(start + timeToWait - Time.time) <= 5)
                {
                    PickTimer.timer.color = Color.red;
                    PickTimer.timer.text = "<b>" + PickTimer.timer.text + "</b>";
                }
                yield return null;
            }
            timerCanvas.SetActive(false);
            yield break;

        }
        // UI courtesy of Willis
        private static void CreateText()
        {
            timerCanvas = new GameObject("TimerCanvas", typeof(Canvas));
            timerCanvas.transform.SetParent(Unbound.Instance.canvas.transform);
            GameObject timerBackground = new GameObject("TimerBackground", typeof(Image));
            timerBackground.transform.SetParent(timerCanvas.transform);
            timerBackground.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            timerBackground.GetComponent<Image>().rectTransform.anchorMin = new Vector2(-1, 0);
            timerBackground.GetComponent<Image>().rectTransform.anchorMax = new Vector2(2, 1);
            GameObject timerObj = new GameObject("Timer", typeof(TextMeshProUGUI));
            timerObj.transform.SetParent(timerBackground.transform);

            PickTimer.timer = timerObj.GetComponent<TextMeshProUGUI>();
            PickTimer.timer.text = "";
            PickTimer.timer.fontSize = 200f;
            timerCanvas.transform.position = new Vector2((float)Screen.width/2f, 150f);
            PickTimer.timer.enableWordWrapping = false;
            PickTimer.timer.overflowMode = TextOverflowModes.Overflow;
            PickTimer.timer.alignment = TextAlignmentOptions.Center;
            timerCanvas.SetActive(false);
        }
    }
    [Serializable]
    [HarmonyPatch(typeof(CardChoice), "DoPick")]
    class CardChoicePatchDoPick
    {
        internal static Coroutine timer = null;
        private static void Prefix(CardChoice __instance)
        {
            if (timer != null) { __instance.StopCoroutine(timer); }
            if (CompetitiveRounds.PickTimer > 0)
            {
                timer = __instance.StartCoroutine(PickTimer.StartPickTimer(__instance));
            }
        }
    }
    [Serializable]
    [HarmonyPatch(typeof(CardChoice), "RPCA_DoEndPick")]
    class CardChoicePatchRPCA_DoEndPick
    {
        private static void Postfix(CardChoice __instance)
        {
            if (PickTimer.timerCanvas != null && PickTimer.timerCanvas.gameObject.activeInHierarchy)
            {
                PickTimer.timerCanvas.gameObject.SetActive(false);
            }
            if (PickTimer.timerCR != null)
            {
                Unbound.Instance.StopCoroutine(PickTimer.timerCR);
            }
            if (CardChoicePatchDoPick.timer != null)
            {
                __instance.StopCoroutine(CardChoicePatchDoPick.timer);
            }
        }
    }

}

