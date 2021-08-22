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
using UnboundLib.Utils;
using System.Collections.ObjectModel;

namespace CompetitiveRounds
{
    internal static class PreGamePickBanHandler
    {
        private static TextMeshProUGUI text;
        internal static GameObject textCanvas = null;
        internal static int currentBans = 0;
        internal static List<string> bannedNames = new List<string>() { };
        internal static List<string> disabledNames = new List<string>() { };

        // UI courtesy of Willis
        private static void CreateText()
        {
            textCanvas = new GameObject("TextCanvas", typeof(Canvas));
            textCanvas.transform.SetParent(Unbound.Instance.canvas.transform);
            GameObject textBackground = new GameObject("TextBackground", typeof(Image));
            textBackground.transform.SetParent(textCanvas.transform);
            textBackground.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            //textBackground.GetComponent<Image>().rectTransform.anchorMin = new Vector2(-2, 0f);
            //textBackground.GetComponent<Image>().rectTransform.anchorMax = new Vector2(3, 0f);
            GameObject textObj = new GameObject("Text", typeof(TextMeshProUGUI));
            textObj.transform.SetParent(textBackground.transform);

            PreGamePickBanHandler.text = textObj.GetComponent<TextMeshProUGUI>();
            PreGamePickBanHandler.text.text = "";
            PreGamePickBanHandler.text.fontSize = 45f;
            textCanvas.transform.position = new Vector2((float)Screen.width / 2f, (float)Screen.height - 50f);
            PreGamePickBanHandler.text.enableWordWrapping = false;
            PreGamePickBanHandler.text.overflowMode = TextOverflowModes.Overflow;
            PreGamePickBanHandler.text.alignment = TextAlignmentOptions.Center;
            textCanvas.SetActive(false);
        }

        internal static IEnumerator PreGameBan(IGameModeHandler gm)
        {
            if (textCanvas == null)
            {
                CreateText();
            }

            if (CompetitiveRounds.PreGameBan == 0)
            {
                yield break;
            }

            // get list of already disabled cards
            disabledNames = new List<string>() { };
            foreach (Card card in CardManager.cards.Values.Where(card => !card.enabled))
            {
                disabledNames.Add(CardManager.cards.First(card_ => card_.Value == card).Key);
            }

            bannedNames = new List<string>() { };

            textCanvas.SetActive(true);

            // let each player ban cards
            foreach (Player player in PlayerManager.instance.players)
            {
                currentBans = 0;

                // set up button actions
                var actions = ToggleCardsMenuHandler.cardObjs.Values.ToArray();
                for (var i = 0; i < actions.Length; i++)
                {
                    var i1 = i;
                    actions[i] = () =>
                    {
                        // ban card via RPC to ensure syncing
                        NetworkingManager.RPC(typeof(PreGamePickBanHandler), nameof(RPCA_BanCard), new object[] { i1 });

                        Unbound.Instance.ExecuteAfterSeconds(0.1f, () =>
                        {
                            if (player.data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                            {
                                ToggleCardsMenuHandler.Close();
                                if (currentBans < CompetitiveRounds.PreGameBan)
                                {
                                    ToggleCardsMenuHandler.Open(true, true, actions, disabledNames.ToArray());
                                }
                            }
                        });

                    };
                }

                // show the menu only to the player currently banning
                if (player.data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    ToggleCardsMenuHandler.Open(true, true, actions, disabledNames.ToArray());
                }
                // show other players some waiting text
                else
                {
                    string[] colors = new string[] { "ORANGE", "BLUE", "RED", "GREEN" };
                    text.text = String.Format("WAITING FOR {0}...", player.playerID < colors.Length ? colors[player.playerID] : "PLAYER");
                }
                // wait for the player to ban all their respective cards
                while (currentBans < CompetitiveRounds.PreGameBan)
                {
                    if (player.data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                    {
                        text.text = "BAN " + (CompetitiveRounds.PreGameBan-currentBans).ToString() + " CARD" + ((CompetitiveRounds.PreGameBan-currentBans > 1) ? "S" : "");
                    }
                    yield return null;
                }
                currentBans = 0;
                yield return new WaitForSecondsRealtime(0.1f);
                ToggleCardsMenuHandler.Close();
            }
            textCanvas.SetActive(false);

            yield return new WaitForSecondsRealtime(0.5f);
            ToggleCardsMenuHandler.Close();

            // show the banned cards
            textCanvas.SetActive(true);
            text.text = "BANNED";

            List<CardInfo> cardsToShow = new List<CardInfo>() { };
            CardInfo[] allCards = ((ObservableCollection<CardInfo>)typeof(CardManager).GetField("activeCards", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).ToList().Concat((List<CardInfo>)typeof(CardManager).GetField("inactiveCards", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).ToArray();

            foreach (string cardName in bannedNames)
            {
                cardsToShow.Add(allCards.Where(card => card.name == cardName).First());
            }

            int teamID = (PlayerManager.instance.players.Where(player => player.data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber).First()).teamID;
            
            foreach (CardInfo cardToShow in cardsToShow)
            {

                ModdingUtils.Utils.CardBarUtils.instance.PlayersCardBar(teamID).OnHover(cardToShow, Vector3.zero);
                ((GameObject)Traverse.Create(ModdingUtils.Utils.CardBarUtils.instance.PlayersCardBar(teamID)).Field("currentCard").GetValue()).gameObject.transform.localScale = Vector3.one * ModdingUtils.Utils.CardBarUtils.cardLocalScaleMult;

                yield return new WaitForSecondsRealtime(1.5f);

                ModdingUtils.Utils.CardBarUtils.instance.PlayersCardBar(teamID).StopHover();

            }

            textCanvas.SetActive(false);

            yield break;
        }
        [UnboundRPC]
        private static void RPCA_BanCard(int idx)
        {
            // increment the number the player has banned
            PreGamePickBanHandler.currentBans++;

            // disable the banned card
            GameObject bannedCard = ToggleCardsMenuHandler.cardObjs.ElementAt(idx).Key;
            //CardManager.cards[bannedCard.name].enabled = false;
            CardManager.DisableCard(CardManager.cards[bannedCard.name].cardInfo, false);


            disabledNames.Add(bannedCard.name);

            // grey out the banned cards' buttons
            bannedNames.Add(CardManager.cards[bannedCard.name].cardInfo.name);
        }
        internal static IEnumerator RestoreCardToggles(IGameModeHandler gm)
        {
            NetworkingManager.RPC(typeof(PreGamePickBanHandler), nameof(RPCA_SyncToggle), new object[] { });
            yield break;
        }
        [UnboundRPC]
        private static void RPCA_SyncToggle()
        {
            foreach (Card card in CardManager.cards.Values)
            {
                if (card.enabled)
                {
                    CardManager.EnableCard(card.cardInfo, true);
                }
                else
                {
                    CardManager.DisableCard(card.cardInfo, true);
                }
            }
        }
    }
}

