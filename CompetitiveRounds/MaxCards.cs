﻿using System;
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
using UnboundLib.Extensions;

namespace CompetitiveRounds
{

    internal class Selectable : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        internal Player player;
        int idx;
        bool hover = false;
        bool down = false;
        Color orig;
        Vector3 origScale;
        void Start()
        {
            orig = ModdingUtils.Utils.CardBarUtils.instance.GetCardSquareColor(this.gameObject.transform.GetChild(0).gameObject);
            origScale = this.gameObject.transform.localScale;
            idx = this.gameObject.transform.GetSiblingIndex();
        }
        void Update()
        {
            idx = this.gameObject.transform.GetSiblingIndex();
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            down = true;
            this.gameObject.transform.localScale = Vector3.one;
            Color.RGBToHSV(ModdingUtils.Utils.CardBarUtils.instance.GetCardSquareColor(this.gameObject.transform.GetChild(0).gameObject), out float h, out float s, out float v);
            Color newColor = Color.HSVToRGB(h, s - 0.1f, v - 0.1f);
            newColor.a = orig.a;
            ModdingUtils.Utils.CardBarUtils.instance.ChangeCardSquareColor(this.gameObject.transform.GetChild(0).gameObject, newColor);
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            if (down)
            {
                down = false;

                this.gameObject.transform.localScale = origScale;
                ModdingUtils.Utils.CardBarUtils.instance.ChangeCardSquareColor(this.gameObject.transform.GetChild(0).gameObject, orig);

                if (hover)
                {
                    if (!PhotonNetwork.OfflineMode)
                    {
                        NetworkingManager.RPC(typeof(Selectable), nameof(RPCA_RemoveCardOnClick), new object[] { player.playerID, idx - 1 });
                    }
                    else
                    {
                        ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer(player, idx-1);
                    }
                }
            }
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            hover = true;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            hover = false;
        }
        void OnDestroy()
        {
            this.gameObject.transform.localScale = origScale;
            ModdingUtils.Utils.CardBarUtils.instance.ChangeCardSquareColor(this.gameObject.transform.GetChild(0).gameObject, orig);
        }
        [UnboundRPC]
        private static void RPCA_RemoveCardOnClick(int playerID, int idx)
        {
            ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer((Player)PlayerManager.instance.InvokeMethod("GetPlayerWithID", playerID), idx);
        }
    }

    static class MaxCardsHandler
    {
        internal static GameObject textCanvas;
        internal static GameObject passCanvas;
        internal static GameObject passButton;
        private static TextMeshProUGUI text;
        internal static bool active = false;
        internal static bool forceRemove = false;
        internal static Dictionary<Player, bool> pass = new Dictionary<Player, bool>() { };
        private static System.Random rng = new System.Random();
        internal static IEnumerator DiscardPhase(IGameModeHandler gm, bool endpick)
        {
            if (CompetitiveRounds.DiscardAfterPick && !endpick)
            {
                yield break;
            }
            if (PlayerManager.instance.GetLastPlayerAlive() == null)
            {
                yield break;
            }
            int winningTeamID = PlayerManager.instance.GetLastPlayerAlive().teamID;

            if (textCanvas == null)
            {
                CreateText();
            }
            if (passCanvas == null)
            {
                CreatePassButton();
            }
            pass = new Dictionary<Player, bool>() { };
            foreach (Player player in PlayerManager.instance.players)
            {
                pass[player] = false;
            }
            yield return new WaitForSecondsRealtime(0.1f);
            if (!endpick)
            {
                foreach (Player player in PlayerManager.instance.players.Where(player => player.teamID != winningTeamID))
                {
                    if (CompetitiveRounds.MaxCards > 0 && ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(player.teamID).Length - 1 >= CompetitiveRounds.MaxCards)
                    {
                        yield return Discard(player, endpick);
                    }
                }
            }
            else
            {
                foreach (Player player in PlayerManager.instance.players)
                {
                    if (CompetitiveRounds.MaxCards > 0 && ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(player.teamID).Length - 1 > CompetitiveRounds.MaxCards)
                    {
                        yield return Discard(player, endpick);
                    }
                }
            }
            yield break;
        }
        private static IEnumerator Discard(Player player, bool endpick)
        {
            active = true;
            forceRemove = false;
            pass = new Dictionary<Player, bool>() { };
            foreach (Player p in PlayerManager.instance.players)
            {
                pass[p] = false;
            }
            int teamID = player.teamID;

            if (PreGamePickBanHandler.skipFirstPickPhase && !endpick)
            {
                yield break;
            }

            if (CompetitiveRounds.MaxCards > 0 && ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(teamID).Length-1 >= ((endpick) ? CompetitiveRounds.MaxCards + 1 : CompetitiveRounds.MaxCards))
            {
                // display text
                textCanvas.SetActive(true);

                // give the player the option to pass if the option is enabled and it is not the end of the pick phase
                if (!endpick && CompetitiveRounds.PassDiscard && player.data.view.IsMine)
                {
                    passCanvas.SetActive(true);
                    passButton.GetOrAddComponent<PassButtonSelectable>().player = player;
                }

                Color orig = Color.clear;
                try
                {
                    orig = ModdingUtils.Utils.CardBarUtils.instance.GetPlayersBarColor(teamID);
                }
                catch
                {
                    yield break;
                }

                ModdingUtils.Utils.CardBarUtils.instance.PlayersCardBar(teamID).gameObject.transform.localPosition += CardBarUtils.localShift;
                // because of the necessary delay when removing cards, this has to be in a nested loop...
                yield return TimerHandler.Start(null);
                while (CompetitiveRounds.MaxCards > 0 && ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(teamID).Length - 1 >= ((endpick) ? CompetitiveRounds.MaxCards + 1 : CompetitiveRounds.MaxCards))
                {
                    ModdingUtils.Utils.CardBarUtils.instance.PlayersCardBar(teamID).gameObject.transform.localScale = Vector3.one * ModdingUtils.Utils.CardBarUtils.barlocalScaleMult;
                    ModdingUtils.Utils.CardBarUtils.instance.ChangePlayersLineColor(teamID, Color.white);
                    Color.RGBToHSV(ModdingUtils.Utils.CardBarUtils.instance.GetPlayersBarColor(teamID), out float h, out float s, out float v);
                    ModdingUtils.Utils.CardBarUtils.instance.ChangePlayersBarColor(teamID, Color.HSVToRGB(h, s + 0.1f, v + 0.1f));
                    while (CompetitiveRounds.MaxCards > 0 && ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(teamID).Length - 1 >= ((endpick) ? CompetitiveRounds.MaxCards + 1 : CompetitiveRounds.MaxCards))
                    {

                        if (player.data.view.IsMine)
                        {
                            text.text = "DISCARD " + (ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(teamID).Length - ((endpick) ? CompetitiveRounds.MaxCards + 1 : CompetitiveRounds.MaxCards)).ToString() + " CARD" + (((ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(teamID).Length - ((endpick) ? CompetitiveRounds.MaxCards + 1 : CompetitiveRounds.MaxCards)) != 1) ? "S" : "");
                            foreach (GameObject cardBarButton in ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(teamID))
                            {
                                Selectable selectable = cardBarButton.GetOrAddComponent<Selectable>();
                                selectable.player = player;
                            }
                        }
                        else
                        {
                            text.text = $"WAITING FOR {UnboundLib.Utils.ExtraPlayerSkins.GetTeamColorName(player.GetAdditionalData().colorID).ToUpper()}";
                        }
                        yield return null;
                    
                        if (forceRemove)
                        {
                            ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer(player, rng.Next(0, player.data.currentCards.Count));
                            yield return new WaitForSecondsRealtime(0.11f);
                        }
                        else if (pass[player] && !endpick)
                        {
                            break;
                        }

                    }
                    if (pass[player] && !forceRemove && !endpick)
                    {
                        break;
                    }
                    yield return new WaitForSecondsRealtime(0.11f);

                }

                yield return new WaitForSecondsRealtime(0.1f);

                if (player.data.view.IsMine)
                {
                    foreach (GameObject cardBarButton in ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(teamID))
                    {
                        if (cardBarButton.GetComponent<Selectable>() != null) { UnityEngine.GameObject.Destroy(cardBarButton.GetComponent<Selectable>()); }
                    }
                }
                try
                {
                    ModdingUtils.Utils.CardBarUtils.instance.PlayersCardBar(teamID).gameObject.transform.localScale = Vector3.one * 1f;
                    ModdingUtils.Utils.CardBarUtils.instance.PlayersCardBar(teamID).gameObject.transform.localPosition -= CardBarUtils.localShift;
                    ModdingUtils.Utils.CardBarUtils.instance.ResetPlayersLineColor(teamID);
                    ModdingUtils.Utils.CardBarUtils.instance.ChangePlayersBarColor(teamID, orig);
                }
                catch
                { }


            }
            textCanvas.SetActive(false);
            passCanvas.SetActive(false);
            active = false;
            yield break;
        }
        private static void CreateText()
        {
            textCanvas = new GameObject("TextCanvas", typeof(Canvas));
            textCanvas.transform.SetParent(Unbound.Instance.canvas.transform);
            GameObject timerBackground = new GameObject("TextBackground", typeof(Image));
            timerBackground.transform.SetParent(textCanvas.transform);
            timerBackground.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            timerBackground.GetComponent<Image>().rectTransform.anchorMin = new Vector2(-2, 0.25f);
            timerBackground.GetComponent<Image>().rectTransform.anchorMax = new Vector2(3, 0.75f);
            GameObject timerObj = new GameObject("Timer", typeof(TextMeshProUGUI));
            timerObj.transform.SetParent(timerBackground.transform);

            text = timerObj.GetComponent<TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 45;
            textCanvas.transform.position = new Vector2((float)Screen.width / 2f, (float)Screen.height - 150f);
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.Center;
            textCanvas.SetActive(false);
        }
        private static void CreatePassButton()
        {
            passCanvas = new GameObject("PassCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            passCanvas.transform.SetParent(Unbound.Instance.canvas.transform);
            passButton = new GameObject("PassBackground", typeof(Image), typeof(PassButtonSelectable));
            passButton.transform.SetParent(passCanvas.transform);
            passButton.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            passButton.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0f, 0f);
            passButton.GetComponent<Image>().rectTransform.anchorMax = new Vector2(1f, 1f);
            GameObject passObj = new GameObject("Pass", typeof(TextMeshProUGUI));
            passObj.transform.SetParent(passButton.transform);

            TextMeshProUGUI passtext = passObj.GetComponent<TextMeshProUGUI>();
            passtext.text = "Pass";
            passtext.fontSize = 45;
            passCanvas.transform.position = new Vector2(5f*(float)Screen.width / 6f, 150f);
            passtext.enableWordWrapping = false;
            passtext.overflowMode = TextOverflowModes.Overflow;
            passtext.alignment = TextAlignmentOptions.Center;
            passCanvas.SetActive(false);
        }

    }
    internal class PassButtonSelectable : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        internal Player player;
        bool hover = false;
        bool down = false;
        Color orig;
        Vector3 origScale;
        void Start()
        {
            orig = this.gameObject.GetComponentInChildren<Image>().color;
            origScale = this.gameObject.transform.localScale;
        }
        void Update()
        {
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            down = true;
            this.gameObject.transform.localScale = origScale * 0.9f;
            Color.RGBToHSV(orig, out float h, out float s, out float v);
            Color newColor = Color.HSVToRGB(h, s - 0.1f, v - 0.1f);
            newColor.a = orig.a;
            this.gameObject.GetComponentInChildren<Image>().color = newColor;
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            if (down)
            {
                down = false;

                this.gameObject.transform.localScale = origScale;
                this.gameObject.GetComponentInChildren<Image>().color = orig;

                if (hover)
                {
                    if (!PhotonNetwork.OfflineMode)
                    {
                        NetworkingManager.RPC(typeof(PassButtonSelectable), nameof(RPCA_PassOnClick), new object[] { player.playerID});
                    }
                    else
                    {
                        PassOnClick(player);
                    }
                }
            }
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            hover = true;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            hover = false;
        }
        void OnDestroy()
        {
            this.gameObject.transform.localScale = origScale;
            this.gameObject.GetComponentInChildren<Image>().color = orig;
        }
        private static void PassOnClick(Player player)
        {
            MaxCardsHandler.pass[player] = true;
        }
        [UnboundRPC]
        private static void RPCA_PassOnClick(int playerID)
        {
            Player player = (Player)PlayerManager.instance.InvokeMethod("GetPlayerWithID",playerID);
            MaxCardsHandler.pass[player] = true;
        }
    }
}

