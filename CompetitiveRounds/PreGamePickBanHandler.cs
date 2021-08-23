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
        internal static int currentPicks = 0;
        internal static List<string> bannedNames = new List<string>() { };
        internal static List<string> disabledNames = new List<string>() { };
        internal static List<CardInfo> cardsToShow = new List<CardInfo>() { };

        private static bool pregamepickfinished = false;
        private static bool picking = false;
        internal static bool skipFirstPickPhase = false;

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
        internal static IEnumerator SetSkipFirstPickPhase(IGameModeHandler gm)
        {
            PreGamePickBanHandler.skipFirstPickPhase = false;
            yield break;
        }
        internal static IEnumerator PreGamePickReset(IGameModeHandler gm)
        {
            pregamepickfinished = false;
            yield break;
        }
        internal static IEnumerator PreGamePicksFinished(IGameModeHandler gm)
        {
            pregamepickfinished = true;
            yield break;
        }

        internal static IEnumerator PreGamePicksStandard(IGameModeHandler gm)
        {

            if (pregamepickfinished || !CompetitiveRounds.PreGamePickMethod || CompetitiveRounds.PreGamePickStandard <= 1)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(1f);

            for (int _ = 0; _ < CompetitiveRounds.PreGamePickStandard - 1; _++)
            {
                foreach (Player player in PlayerManager.instance.players.ToArray())
                {
                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);
                    CardChoiceVisuals.instance.Show(Enumerable.Range(0, PlayerManager.instance.players.Count).Where(i => PlayerManager.instance.players[i].playerID == player.playerID).First(), true);
                    yield return CardChoice.instance.DoPick(1, player.playerID, PickerType.Player);
                    yield return new WaitForSecondsRealtime(0.1f);
                    yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);
                    yield return new WaitForSecondsRealtime(0.1f);
                }
            }

            CardChoiceVisuals.instance.Hide();

            yield break;
        }
        private static string[] GetCardToggleNamesWithoutRarity(CardInfo.Rarity rarity)
        {
            List<string> cardNames = new List<string>() { };
            foreach (GameObject obj in ToggleCardsMenuHandler.cardObjs.Keys)
            {
                if (CardManager.cards[obj.name].cardInfo.rarity != rarity)
                {
                    cardNames.Add(obj.name);
                }
            }

            return cardNames.ToArray();
        }
        internal static IEnumerator PickFromRarity(Player player, CardInfo.Rarity rarity)
        {
            string[] colors = new string[] { "ORANGE", "BLUE", "RED", "GREEN" };

            currentPicks = 0;

            int cardsToPick = 0;
            string rarityString = "";

            switch (rarity)
            {
                case CardInfo.Rarity.Common:
                    cardsToPick = CompetitiveRounds.PreGamePickCommon;
                    rarityString = "COMMON";
                    break;
                case CardInfo.Rarity.Uncommon:
                    cardsToPick = CompetitiveRounds.PreGamePickUncommon;
                    rarityString = "UNCOMMON";
                    break;
                case CardInfo.Rarity.Rare:
                    cardsToPick = CompetitiveRounds.PreGamePickRare;
                    rarityString = "RARE";
                    break;
            }
            if (cardsToPick == 0)
            {
                yield break;
            }
            // set up button actions
            var actions = ToggleCardsMenuHandler.cardObjs.Values.ToArray();
            for (var i = 0; i < actions.Length; i++)
            {
                var i1 = i;
                actions[i] = () =>
                {
                    if (PhotonNetwork.OfflineMode || player.data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                    {
                        if (!ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, CardManager.cards[ToggleCardsMenuHandler.cardObjs.ElementAt(i1).Key.name].cardInfo))
                        {
                            return;
                        }
                        currentPicks++;
                        if (PhotonNetwork.OfflineMode)
                        {
                            ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, CardManager.cards[ToggleCardsMenuHandler.cardObjs.ElementAt(i1).Key.name].cardInfo);
                        }
                        else
                        {
                            NetworkingManager.RPC(typeof(PreGamePickBanHandler), nameof(RPCA_AddCardToPlayer), new object[] { player.data.view.ControllerActorNr, ToggleCardsMenuHandler.cardObjs.ElementAt(i1).Key.name });
                        }
                        NetworkingManager.RPC(typeof(PreGamePickBanHandler), nameof(RPCA_UpdateCardCount), new object[] { currentPicks });
                    }
                };
            }

            if (textCanvas == null)
            {
                CreateText();
            }
            if (player.data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                ToggleCardsMenuHandler.Open(true, true, actions, disabledNames.ToArray().Concat(GetCardToggleNamesWithoutRarity(rarity)).ToArray());
            }
            textCanvas.SetActive(true);
            while (currentPicks < cardsToPick)
            {
                if (player.data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    text.text = "PICK " + (cardsToPick - currentPicks).ToString() + " " + rarityString + " CARD" + ((cardsToPick - currentPicks != 1) ? "S" : "");
                }
                else
                {
                    text.text = String.Format("WAITING FOR {0}...", player.playerID < colors.Length ? colors[player.playerID] : "PLAYER");
                }
                yield return null;
            }
            textCanvas.SetActive(false);
            ToggleCardsMenuHandler.Close();
            PreGamePickBanHandler.skipFirstPickPhase = true;
            yield break;
        }
        [UnboundRPC]
        private static void RPCA_UpdateCardCount(int cards)
        {
            currentPicks = cards;
        }
        [UnboundRPC]
        private static void RPCA_AddCardToPlayer(int actorID, string cardName)
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                Player player = (Player)typeof(PlayerManager).InvokeMember("GetPlayerWithActorID",
                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                    BindingFlags.NonPublic, null, PlayerManager.instance, new object[] { actorID });
                ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, CardManager.cards[cardName].cardInfo);
            }
        }
        internal static IEnumerator PreGamePicksCommon(IGameModeHandler gm)
        {
            if (pregamepickfinished || CompetitiveRounds.PreGamePickMethod || CompetitiveRounds.PreGamePickCommon < 1)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(1f);


            foreach (Player player in PlayerManager.instance.players.ToArray())
            {

                yield return PickFromRarity(player, CardInfo.Rarity.Common);

                yield return new WaitForSecondsRealtime(0.1f);
            }
            
            yield break;
        }
        internal static IEnumerator PreGamePicksUncommon(IGameModeHandler gm)
        {

            if (pregamepickfinished || CompetitiveRounds.PreGamePickMethod || CompetitiveRounds.PreGamePickUncommon < 1)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(1f);


            foreach (Player player in PlayerManager.instance.players.ToArray())
            {

                yield return PickFromRarity(player, CardInfo.Rarity.Uncommon);

                yield return new WaitForSecondsRealtime(0.1f);
            }
            
            yield break;
        }
        internal static IEnumerator PreGamePicksRare(IGameModeHandler gm)
        {

            if (pregamepickfinished || CompetitiveRounds.PreGamePickMethod || CompetitiveRounds.PreGamePickRare < 1)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(1f);


            foreach (Player player in PlayerManager.instance.players.ToArray())
            {

                yield return PickFromRarity(player, CardInfo.Rarity.Rare);

                yield return new WaitForSecondsRealtime(0.1f);

            }
            
            yield break;
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

            cardsToShow = new List<CardInfo>() { };

            yield return new WaitForSecondsRealtime(0.5f);

            CardInfo[] allCards = ((ObservableCollection<CardInfo>)typeof(CardManager).GetField("activeCards", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).ToList().Concat((List<CardInfo>)typeof(CardManager).GetField("inactiveCards", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).ToArray();

            // get list of already disabled cards
            disabledNames = new List<string>() { };

            // foreach causes crash for some reason...
            for (int i = 0; i < CardManager.cards.Values.ToArray().Length; i++)
            {
                if (!CardManager.cards.Values.ToArray()[i].enabled)
                {
                    disabledNames.Add(CardManager.cards.Keys.ToArray()[i]);
                }
            }

            yield return new WaitForSecondsRealtime(0.5f);

            bannedNames = new List<string>() { };


            textCanvas.SetActive(true);

            // let each player ban cards
            for (int p = 0; p < PlayerManager.instance.players.Count; p++)
            {
                List<string> locallyBanned = new List<string>() { };
                picking = true;
                currentBans = 0;
                // set up button actions
                var actions = ToggleCardsMenuHandler.cardObjs.Values.ToArray();
                for (var i = 0; i < actions.Length; i++)
                {
                    var i1 = i;
                    actions[i] = () =>
                    {
                        currentBans++;
                        GameObject bannedCard = ToggleCardsMenuHandler.cardObjs.ElementAt(i1).Key;
                        locallyBanned.Add(bannedCard.name);

                        ToggleCardsMenuHandler.Close();
                        if (PlayerManager.instance.players[p].data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                        {
                            if (currentBans < CompetitiveRounds.PreGameBan)
                            {
                                ToggleCardsMenuHandler.Open(true, true, actions, disabledNames.ToArray().Concat(locallyBanned.ToArray()).ToArray());
                            }
                            else
                            {
                                NetworkingManager.RPC(typeof(PreGamePickBanHandler), nameof(RPCA_EndBan), new object[] {locallyBanned.ToArray()});
                            }
                        }
                        //});

                    };
                }

                // show the menu only to the player currently banning
                if (PlayerManager.instance.players[p].data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    ToggleCardsMenuHandler.Open(true, true, actions, disabledNames.ToArray().Concat(locallyBanned.ToArray()).ToArray());
                }
                // show other players some waiting text
                else
                {
                    string[] colors = new string[] { "ORANGE", "BLUE", "RED", "GREEN" };
                    text.text = String.Format("WAITING FOR {0}...", PlayerManager.instance.players[p].playerID < colors.Length ? colors[PlayerManager.instance.players[p].playerID] : "PLAYER");
                }
                // wait for the player to ban all their respective cards
                while (picking)
                {
                    if (PlayerManager.instance.players[p].data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                    {
                        text.text = "BAN " + (CompetitiveRounds.PreGameBan - currentBans).ToString() + " CARD" + ((CompetitiveRounds.PreGameBan - currentBans != 1) ? "S" : "");
                    }
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(0.5f);
                ToggleCardsMenuHandler.Close();
            }
            textCanvas.SetActive(false);

            yield return new WaitForSecondsRealtime(0.5f);
            ToggleCardsMenuHandler.Close();

            // show the banned cards
            textCanvas.SetActive(true);
            text.text = "BANNED";

            int teamID = (PlayerManager.instance.players.Where(player => player.data.view.ControllerActorNr == PhotonNetwork.LocalPlayer.ActorNumber).First()).teamID;

            for (int i = 0; i < cardsToShow.Count; i++)
            {

                ModdingUtils.Utils.CardBarUtils.instance.PlayersCardBar(teamID).OnHover(cardsToShow[i], Vector3.zero);
                ((GameObject)Traverse.Create(ModdingUtils.Utils.CardBarUtils.instance.PlayersCardBar(teamID)).Field("currentCard").GetValue()).gameObject.transform.localScale = Vector3.one * ModdingUtils.Utils.CardBarUtils.cardLocalScaleMult;

                yield return new WaitForSecondsRealtime(1.5f);

                ModdingUtils.Utils.CardBarUtils.instance.PlayersCardBar(teamID).StopHover();

            }

            textCanvas.SetActive(false);

            yield return new WaitForSecondsRealtime(0.5f);

            yield break;
        }
        [UnboundRPC]
        private static void RPCA_EndBan(string[] banned)
        {
            for (int i = 0; i < banned.Length; i++)
            {
                disabledNames.Add(banned[i]);
                bannedNames.Add(CardManager.cards[banned[i]].cardInfo.name);
                cardsToShow.Add(CardManager.cards[banned[i]].cardInfo);
                CardManager.DisableCard(CardManager.cards[banned[i]].cardInfo, false);
            }
            picking = false;
        }
        
        internal static IEnumerator RestoreCardToggles(IGameModeHandler gm)
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(PreGamePickBanHandler), nameof(RPCA_SyncToggle), new object[] { });
            }
            yield return new WaitForSecondsRealtime(0.5f);
            yield break;
        }
        [UnboundRPC]
        private static void RPCA_SyncToggle()
        {
            for (int i = 0; i < CardManager.cards.Count; i++)
            {
                if (CardManager.cards.Values.ToArray()[i].enabled)
                {
                    CardManager.EnableCard(CardManager.cards.Values.ToArray()[i].cardInfo, true);
                }
                else
                {
                    CardManager.DisableCard(CardManager.cards.Values.ToArray()[i].cardInfo, true);
                }
            }
        }
    }
}

