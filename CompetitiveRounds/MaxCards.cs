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
                        NetworkingManager.RPC(typeof(Selectable), nameof(RPCA_RemoveCardOnClick), new object[] { player.data.view.ControllerActorNr, idx - 1 });
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
        private static void RPCA_RemoveCardOnClick(int actorID, int idx)
        {
            ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer((Player)typeof(PlayerManager).InvokeMember("GetPlayerWithActorID",
            BindingFlags.Instance | BindingFlags.InvokeMethod |
            BindingFlags.NonPublic, null, PlayerManager.instance, new object[] { actorID }), idx);
        }
    }

    static class MaxCardsHandler
    {
        private static GameObject textCanvas;
        private static GameObject passButton;
        private static TextMeshProUGUI text;
        internal static bool active = false;
        internal static bool forceRemove = false;
        internal static bool pass = false;
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
            if (passButton == null)
            {
                CreatePassButton();
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
            pass = false;
            int teamID = player.teamID;
            if (CompetitiveRounds.MaxCards > 0 && ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(teamID).Length-1 >= ((endpick) ? CompetitiveRounds.MaxCards + 1 : CompetitiveRounds.MaxCards))
            {
                // display text
                textCanvas.SetActive(true);

                if (CompetitiveRounds.PassDiscard && player.data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    passButton.SetActive(true);
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

                        //if (PlayerManager.instance.GetPlayersInTeam(teamID)[0].data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                        if (player.data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                        {
                            text.text = "DISCARD " + (ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(teamID).Length - ((endpick) ? CompetitiveRounds.MaxCards + 1 : CompetitiveRounds.MaxCards)).ToString() + " CARD" + (((ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(teamID).Length - ((endpick) ? CompetitiveRounds.MaxCards + 1 : CompetitiveRounds.MaxCards)) > 1) ? "S" : "");
                            foreach (GameObject cardBarButton in ModdingUtils.Utils.CardBarUtils.instance.GetCardBarSquares(teamID))
                            {
                                Selectable selectable = cardBarButton.GetOrAddComponent<Selectable>();
                                selectable.player = player;
                            }
                        }
                        else
                        {
                            string[] colors = new string[] {"ORANGE", "BLUE", "RED", "GREEN"};
                            text.text = String.Format("WAITING FOR {0}...", player.playerID < colors.Length ? colors[player.playerID] : "PLAYER");
                        }
                        yield return null;
                    
                        if (forceRemove)
                        {
                            ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer(player, rng.Next(0, player.data.currentCards.Count));
                            yield return new WaitForSecondsRealtime(0.11f);
                        }
                        else if (pass)
                        {
                            break;
                        }

                    }
                    if (pass && !forceRemove)
                    {
                        break;
                    }
                    yield return new WaitForSecondsRealtime(0.11f);

                }

                yield return new WaitForSecondsRealtime(0.1f);

                //if (PlayerManager.instance.GetPlayersInTeam(teamID)[0].data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                if (player.data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
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
            passButton.SetActive(false);
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
            passButton = new GameObject("PassCanvas", typeof(Canvas));
            passButton.transform.SetParent(Unbound.Instance.canvas.transform);
            GameObject passBackground = new GameObject("PassBackground", typeof(Image), typeof(PassButtonSelectable), typeof(HoverEvent),typeof(Button));
            passBackground.GetComponent<Button>().onClick.AddListener(() => { UnityEngine.Debug.Log("PRESSED"); MaxCardsHandler.pass = true; });
            passBackground.transform.SetParent(passButton.transform);
            passBackground.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            passBackground.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0f, 0f);
            passBackground.GetComponent<Image>().rectTransform.anchorMax = new Vector2(1f, -0.1f);
            GameObject passObj = new GameObject("Pass", typeof(TextMeshProUGUI));
            passObj.transform.SetParent(passBackground.transform);

            TextMeshProUGUI passtext = passObj.GetComponent<TextMeshProUGUI>();
            passtext.text = "Pass";
            passtext.fontSize = 45;
            passButton.transform.position = new Vector2(5f*(float)Screen.width / 6f, 150f);
            passtext.enableWordWrapping = false;
            passtext.overflowMode = TextOverflowModes.Overflow;
            passtext.alignment = TextAlignmentOptions.Center;
            passButton.SetActive(false);
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
                        NetworkingManager.RPC(typeof(PassButtonSelectable), nameof(RPCA_PassOnClick), new object[] { player.data.view.ControllerActorNr });
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
            MaxCardsHandler.pass = true;
        }
        [UnboundRPC]
        private static void RPCA_PassOnClick(int actorID)
        {
            MaxCardsHandler.pass = true;
        }
    }
}

