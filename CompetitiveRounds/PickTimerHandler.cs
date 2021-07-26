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
    internal static class PickTimerHandler
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
            timerCR = Unbound.Instance.StartCoroutine(PickTimerHandler.Timer(CompetitiveRounds.PickTimer));
            yield return new WaitForSecondsRealtime(CompetitiveRounds.PickTimer);
            if (MaxCardsHandler.active)
            {
                MaxCardsHandler.forceRemove = true;
                while (MaxCardsHandler.active)
                {
                    yield return null;
                }
            }
            instance.Pick(((List<GameObject>)Traverse.Create(instance).Field("spawnedCards").GetValue())[rng.Next(0, ((List<GameObject>)Traverse.Create(instance).Field("spawnedCards").GetValue()).Count)], false);
            Traverse.Create(instance).Field("pickrID").SetValue(-1);
            yield break;
        }
        private static IEnumerator Timer(float timeToWait)
        {
            float start = Time.time;
            if (PickTimerHandler.timer == null)
            {
                PickTimerHandler.CreateText();
            }
            PickTimerHandler.timer.color = Color.white;
            timerCanvas.SetActive(true);

            while (Time.time < start + timeToWait)
            {
                PickTimerHandler.timer.text = UnityEngine.Mathf.CeilToInt(start + timeToWait - Time.time).ToString();
                if (UnityEngine.Mathf.CeilToInt(start + timeToWait - Time.time) <= 5)
                {
                    PickTimerHandler.timer.color = Color.red;
                    PickTimerHandler.timer.text = "<b>" + PickTimerHandler.timer.text + "</b>";
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

            PickTimerHandler.timer = timerObj.GetComponent<TextMeshProUGUI>();
            PickTimerHandler.timer.text = "";
            PickTimerHandler.timer.fontSize = 200f;
            timerCanvas.transform.position = new Vector2((float)Screen.width/2f, 150f);
            PickTimerHandler.timer.enableWordWrapping = false;
            PickTimerHandler.timer.overflowMode = TextOverflowModes.Overflow;
            PickTimerHandler.timer.alignment = TextAlignmentOptions.Center;
            timerCanvas.SetActive(false);
        }

        internal static IEnumerator Cleanup(IGameModeHandler gm)
        {
            if (TimerHandler.timer != null) { Unbound.Instance.StopCoroutine(TimerHandler.timer); }
            if (timerCR != null)
            {
                Unbound.Instance.StopCoroutine(timerCR);
            }
            if (timerCanvas != null) { timerCanvas.SetActive(false); }
            yield break;
        }
    }
    internal static class TimerHandler
    {
        internal static Coroutine timer = null;
        internal static IEnumerator Start(IGameModeHandler gm)
        {
            if (timer != null) { Unbound.Instance.StopCoroutine(timer); }
            if (CompetitiveRounds.PickTimer > 0)
            {
                timer = Unbound.Instance.StartCoroutine(PickTimerHandler.StartPickTimer(CardChoice.instance));
            }
            yield break;
        }
    }
    [Serializable]
    [HarmonyPatch(typeof(CardChoice), "RPCA_DoEndPick")]
    class CardChoicePatchRPCA_DoEndPick
    {
        private static void Postfix(CardChoice __instance)
        {
            if (PickTimerHandler.timerCanvas != null && PickTimerHandler.timerCanvas.gameObject.activeInHierarchy)
            {
                PickTimerHandler.timerCanvas.gameObject.SetActive(false);
            }
            if (PickTimerHandler.timerCR != null)
            {
                Unbound.Instance.StopCoroutine(PickTimerHandler.timerCR);
            }
            if (TimerHandler.timer != null)
            {
                __instance.StopCoroutine(TimerHandler.timer);
            }
        }
    }

}

