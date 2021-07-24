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

namespace CompetitiveRounds
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, "0.0.0.0")]
    [BepInProcess("Rounds.exe")]
    public class CompetitiveRounds : BaseUnityPlugin
    {
        private const string ModId = "pykess.rounds.plugins.competitiverounds";
        private const string ModName = "Competitive Rounds";

        public static ConfigEntry<bool> WinByTwoRoundsConfig;
        public static ConfigEntry<bool> WinByTwoPointsConfig;
        public static ConfigEntry<int> PickTimerConfig;
        public static ConfigEntry<int> MaxCardsConfig;

        public static bool WinByTwoRounds;
        public static bool WinByTwoPoints;
        public static int PickTimer;
        public static int MaxCards;

        private static Toggle WinByTwoPointsCheckbox;
        private static Toggle WinByTwoRoundsCheckbox;

        private void Awake()
        {
            // bind configs with BepInEx
            WinByTwoRoundsConfig = Config.Bind("CompetitiveRounds", "WinByTwoRounds", false, "When enabled, if the game is tied at match point, then players must win by two roudns.");
            WinByTwoPointsConfig = Config.Bind("CompetitiveRounds", "WinByTwoPoints", false, "When enabled, if the game is tied at match point, then players must win by two points.");
            PickTimerConfig = Config.Bind("CompetitiveRounds", "PickTimer", 0, "Time limit in seconds for the pick phase, 0 disables the timer");
            MaxCardsConfig = Config.Bind("CompetitiveRounds", "MaxCards", 0, "Maximum number of cards a player can have, 0 disables the limit");

            // apply patches
            new Harmony(ModId).PatchAll();
        }
        private void Start()
        {
            // add credits
            Unbound.RegisterCredits("Competitive Rounds", new string[] { "Pykess (Code)", "Slicimus (Win by Two code)", "Willis (Pick timer UI)", "ERIC (pick timer idea)" }, "github", "https://github.com/pdcook/CompetitiveRounds");

            // add GUI to modoptions menu
            Unbound.RegisterMenu("Competitive Rounds", ()=> { }, this.NewGUI);

            // add hooks for win by 2
            GameModeManager.AddHook(GameModeHooks.HookGameStart, WinByTwo.ResetPoints);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, WinByTwo.RoundX2);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, WinByTwo.PointX2);

            // handshake to sync settings
            Unbound.RegisterHandshake(CompetitiveRounds.ModId, this.OnHandShakeCompleted);
        }
        private void OnHandShakeCompleted()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC_Others(typeof(CompetitiveRounds), nameof(SyncSettings), new object[] {CompetitiveRounds.WinByTwoRounds, CompetitiveRounds.WinByTwoPoints, CompetitiveRounds.PickTimer, CompetitiveRounds.MaxCards });
            }
        }

        [UnboundRPC]
        private static void SyncSettings(bool win2rounds, bool win2points, int pickTimer, int maxCards)
        {
            CompetitiveRounds.WinByTwoRounds = win2rounds;
            CompetitiveRounds.WinByTwoPoints = win2points;
            CompetitiveRounds.PickTimer = pickTimer;
            CompetitiveRounds.MaxCards = maxCards;
        }
        private void NewGUI(GameObject menu)
        {
            MenuHandler.CreateText("Competitive Rounds Options", menu, out TextMeshProUGUI _);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void TimerChanged(float val)
            {
                CompetitiveRounds.PickTimerConfig.Value = UnityEngine.Mathf.RoundToInt(val);
                CompetitiveRounds.PickTimer = CompetitiveRounds.PickTimerConfig.Value;
            }
            void MaxChanged(float val)
            {
                CompetitiveRounds.MaxCardsConfig.Value = UnityEngine.Mathf.RoundToInt(val);
                CompetitiveRounds.MaxCards = CompetitiveRounds.MaxCardsConfig.Value;
            }
            MenuHandler.CreateSlider("Pick Phase Timer", menu, 60, 0, 100, CompetitiveRounds.PickTimerConfig.Value, TimerChanged, out UnityEngine.UI.Slider timerSlider, true);
            MenuHandler.CreateText("Time limit for each pick phase in seconds - 0 disables timer", menu, out var _, 30);
            MenuHandler.CreateText(" ", menu, out var _, 30);
            MenuHandler.CreateSlider("Maximum Number of Cards", menu, 60, 0, 50, CompetitiveRounds.MaxCardsConfig.Value, MaxChanged, out UnityEngine.UI.Slider maxSlider, true);
            MenuHandler.CreateText("Maximum number of cards each player can have - 0 disables limit", menu, out var _, 30);
            MenuHandler.CreateText(" ", menu, out var _, 30);
            void WinByTwoRoundsCheckboxAction(bool flag)
            {
                CompetitiveRounds.WinByTwoRoundsConfig.Value = flag;
                if (CompetitiveRounds.WinByTwoPointsConfig.Value && CompetitiveRounds.WinByTwoRoundsConfig.Value)
                {
                    CompetitiveRounds.WinByTwoPointsConfig.Value = false;
                    WinByTwoPointsCheckbox.isOn = false;
                }
                CompetitiveRounds.WinByTwoRounds = CompetitiveRounds.WinByTwoRoundsConfig.Value;
            }
            void WinByTwoPointsCheckboxAction(bool flag)
            {
                CompetitiveRounds.WinByTwoPointsConfig.Value = flag;
                if (CompetitiveRounds.WinByTwoPointsConfig.Value && CompetitiveRounds.WinByTwoRoundsConfig.Value)
                {
                    CompetitiveRounds.WinByTwoRoundsConfig.Value = false;
                    WinByTwoRoundsCheckbox.isOn = false;
                }
                CompetitiveRounds.WinByTwoPoints = CompetitiveRounds.WinByTwoPointsConfig.Value;
            }
            WinByTwoPointsCheckbox = MenuHandler.CreateToggle(CompetitiveRounds.WinByTwoPointsConfig.Value, "Win By Two Points", menu, WinByTwoPointsCheckboxAction, 60).GetComponent<Toggle>();
            WinByTwoRoundsCheckbox = MenuHandler.CreateToggle(CompetitiveRounds.WinByTwoRoundsConfig.Value, "Win By Two Rounds", menu, WinByTwoRoundsCheckboxAction, 60).GetComponent<Toggle>();
            MenuHandler.CreateText("when enabled, match point ties must be broken by at least two points or rounds", menu, out var _, 30);
            MenuHandler.CreateText(" ", menu, out var _, 30);

            void ResetButton()
            {
                timerSlider.value = 0f;
                TimerChanged(0f);
                maxSlider.value = 0f;
                MaxChanged(0f);
                WinByTwoPointsCheckbox.isOn = false;
                WinByTwoRoundsCheckbox.isOn = false;
                WinByTwoPointsCheckboxAction(false);
                WinByTwoRoundsCheckboxAction(false);
            }
            MenuHandler.CreateButton("Disable All", menu, ResetButton, 30);
        }
    }
}

