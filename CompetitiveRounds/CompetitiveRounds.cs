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
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)] // utilities for cards and cardbars
    [BepInPlugin(ModId, ModName, "0.0.0.2")]
    [BepInProcess("Rounds.exe")]
    public class CompetitiveRounds : BaseUnityPlugin
    {
        private const string ModId = "pykess.rounds.plugins.competitiverounds";
        private const string ModName = "Competitive Rounds";

        public static ConfigEntry<bool> WinByTwoRoundsConfig;
        public static ConfigEntry<bool> WinByTwoPointsConfig;
        public static ConfigEntry<int> PickTimerConfig;
        public static ConfigEntry<int> MaxCardsConfig;

        public static ConfigEntry<bool> PassDiscardConfig;
        public static ConfigEntry<bool> DiscardAfterPickConfig;

        public static ConfigEntry<int> PreGamePickCommonConfig;
        public static ConfigEntry<int> PreGamePickUncommonConfig;
        public static ConfigEntry<int> PreGamePickRareConfig;
        public static ConfigEntry<int> PreGamePickStandardConfig;

        public static ConfigEntry<int> PreGameBanConfig;

        public static ConfigEntry<bool> PreGamePickMethodConfig;

        public static bool WinByTwoRounds;
        public static bool WinByTwoPoints;
        public static int PickTimer;
        public static int MaxCards;

        public static bool PassDiscard;
        public static bool DiscardAfterPick;

        public static int PreGamePickCommon;
        public static int PreGamePickUncommon;
        public static int PreGamePickRare;

        public static int PreGamePickStandard;
        public static bool PreGamePickMethod;



        public static int PreGameBan;

        private static Toggle WinByTwoPointsCheckbox;
        private static Toggle WinByTwoRoundsCheckbox;

        private static Toggle PassCheckbox;
        private static Toggle DiscardAfterCheckbox;

        private static Toggle PreGamePickMethodCheckbox;

        private static GameObject StandardSlider;
        private static GameObject CommonSlider;
        private static GameObject UncommonSlider;
        private static GameObject RareSlider;


        private void Awake()
        {
            // bind configs with BepInEx
            WinByTwoRoundsConfig = Config.Bind("CompetitiveRounds", "WinByTwoRounds", false, "When enabled, if the game is tied at match point, then players must win by two roudns.");
            WinByTwoPointsConfig = Config.Bind("CompetitiveRounds", "WinByTwoPoints", false, "When enabled, if the game is tied at match point, then players must win by two points.");
            PickTimerConfig = Config.Bind("CompetitiveRounds", "PickTimer", 0, "Time limit in seconds for the pick phase, 0 disables the timer");
            MaxCardsConfig = Config.Bind("CompetitiveRounds", "MaxCards", 0, "Maximum number of cards a player can have, 0 disables the limit");
            PassDiscardConfig = Config.Bind("CompetitiveRounds", "Pass Discard", false, "Give players to pass during their discard phase");
            DiscardAfterPickConfig = Config.Bind("CompetitiveRounds", "Discard After Pick", false, "Have players discard only after they have exceeded the max number of cards");

            PreGamePickStandardConfig = Config.Bind("CompetitiveRounds", "Pre-game pick cards", 0, "The number of cards each player will pick before the game from the usual 5-card draw");
            
            PreGamePickCommonConfig = Config.Bind("CompetitiveRounds", "Pre-game common pick cards", 0, "The number of common cards each player will pick from the entire deck before the game");
            PreGamePickUncommonConfig = Config.Bind("CompetitiveRounds", "Pre-game uncommon pick cards", 0, "The number of uncommon cards each player will pick from the entire deck before the game");
            PreGamePickRareConfig = Config.Bind("CompetitiveRounds", "Pre-game rare pick cards", 0, "The number of rare cards each player will pick from the entire deck before the game");

            PreGamePickMethodConfig = Config.Bind("CompetitiveRounds", "Pre-game pick method", true, "The method used for pre-game pick. If true, use the standard 5-card draw method, if false use the entire deck.");
            
            PreGameBanConfig = Config.Bind("CompetitiveRounds", "Pre-game baned cards", 0, "The number of cards each player will pick to ban from appearing during the game from the entire deck before the game");

            // apply patches
            new Harmony(ModId).PatchAll();
        }
        private void Start()
        {
            // call settings as to not orphan them

            WinByTwoRounds = WinByTwoRoundsConfig.Value;
            WinByTwoPoints = WinByTwoPointsConfig.Value;
            PickTimer = PickTimerConfig.Value;
            MaxCards = MaxCardsConfig.Value;
            PassDiscard = PassDiscardConfig.Value;
            DiscardAfterPick = DiscardAfterPickConfig.Value;
            PreGamePickStandard = PreGamePickStandardConfig.Value;
            PreGamePickCommon = PreGamePickCommonConfig.Value;
            PreGamePickUncommon = PreGamePickUncommonConfig.Value;
            PreGamePickRare = PreGamePickRareConfig.Value;
            PreGamePickMethod = PreGamePickMethodConfig.Value;
            PreGameBan = PreGameBanConfig.Value;

            // add credits
            Unbound.RegisterCredits("Competitive Rounds", new string[] { "Pykess (Code)", "Slicimus (Win by Two code)", "BossSloth (Card Pick Menus)", "Willis (Pick timer UI)", "ERIC (pick timer idea)", "LilyChickne (discard phase feedback)" }, new string[] { "github", "Buy me a coffee" }, new string[] { "https://github.com/pdcook/CompetitiveRounds", "https://www.buymeacoffee.com/Pykess" });

            // add GUI to modoptions menu
            Unbound.RegisterMenu("Competitive Rounds", ()=> { }, this.NewGUI, null, false);

            // add hooks for pre-game picks and bans
            GameModeManager.AddHook(GameModeHooks.HookGameStart, PreGamePickBanHandler.RestoreCardToggles);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, PreGamePickBanHandler.PreGameBan);
            GameModeManager.AddHook(GameModeHooks.HookGameEnd, PreGamePickBanHandler.RestoreCardToggles);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, PreGamePickBanHandler.PreGamePickReset);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, PreGamePickBanHandler.PreGamePicksStandard);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, PreGamePickBanHandler.PreGamePicksCommon);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, PreGamePickBanHandler.PreGamePicksUncommon);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, PreGamePickBanHandler.PreGamePicksRare);

            // add hooks for pick timer
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickStart, TimerHandler.Start);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, PickTimerHandler.Cleanup);

            // add hooks for win by 2
            GameModeManager.AddHook(GameModeHooks.HookGameStart, WinByTwo.ResetPoints);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, WinByTwo.RoundX2);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, WinByTwo.PointX2);

            // add hooks for max cards
            GameModeManager.AddHook(GameModeHooks.HookPickStart, (gm) => MaxCardsHandler.DiscardPhase(gm, false));
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, (gm) => MaxCardsHandler.DiscardPhase(gm, true));
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, PickTimerHandler.Cleanup);

            // the last pickstart hook should be the pregamepickfinish
            GameModeManager.AddHook(GameModeHooks.HookPickStart, PreGamePickBanHandler.PreGamePicksFinished);

            // handshake to sync settings
            Unbound.RegisterHandshake(CompetitiveRounds.ModId, this.OnHandShakeCompleted);
        }
        private void OnHandShakeCompleted()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC_Others(typeof(CompetitiveRounds), nameof(SyncSettings), new object[] {CompetitiveRounds.WinByTwoRounds, CompetitiveRounds.WinByTwoPoints, CompetitiveRounds.PickTimer, CompetitiveRounds.MaxCards, PassDiscard, DiscardAfterPick, PreGamePickMethod, PreGamePickStandard, PreGamePickCommon, PreGamePickUncommon, PreGamePickRare, PreGameBan});
            }
        }

        [UnboundRPC]
        private static void SyncSettings(bool win2rounds, bool win2points, int pickTimer, int maxCards, bool pass, bool after, bool pickMethod, int pick, int common, int uncommon, int rare, int ban)
        {
            CompetitiveRounds.WinByTwoRounds = win2rounds;
            CompetitiveRounds.WinByTwoPoints = win2points;
            CompetitiveRounds.PickTimer = pickTimer;
            CompetitiveRounds.MaxCards = maxCards;
            CompetitiveRounds.PassDiscard = pass;
            CompetitiveRounds.DiscardAfterPick = after;
            CompetitiveRounds.PreGamePickMethod = pickMethod;
            CompetitiveRounds.PreGamePickStandard = pick;
            CompetitiveRounds.PreGamePickCommon = common;
            CompetitiveRounds.PreGamePickUncommon = uncommon;
            CompetitiveRounds.PreGamePickRare = rare;
            CompetitiveRounds.PreGameBan = ban;
        }
        private void NewGUI(GameObject menu)
        {
            MenuHandler.CreateText("Competitive Rounds Options", menu, out TextMeshProUGUI _, 45);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 15);
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
            MenuHandler.CreateSlider("Pick Phase Timer (seconds)\n0 disables", menu, 30, 0f, 100f, CompetitiveRounds.PickTimerConfig.Value, TimerChanged, out UnityEngine.UI.Slider timerSlider, true);
            MenuHandler.CreateText(" ", menu, out var _, 15);
            MenuHandler.CreateSlider("Maximum Number of Cards\n0 disables", menu, 30, 0f, 50f, CompetitiveRounds.MaxCardsConfig.Value, MaxChanged, out UnityEngine.UI.Slider maxSlider, true);
            void PassCheckboxAction(bool flag)
            {
                CompetitiveRounds.PassDiscardConfig.Value = flag;
                if (CompetitiveRounds.PassDiscardConfig.Value && CompetitiveRounds.DiscardAfterPickConfig.Value)
                {
                    CompetitiveRounds.DiscardAfterPickConfig.Value = false;
                    DiscardAfterCheckbox.isOn = false;
                }
                CompetitiveRounds.PassDiscard = CompetitiveRounds.PassDiscardConfig.Value;
                CompetitiveRounds.DiscardAfterPick = CompetitiveRounds.DiscardAfterPickConfig.Value;
            }
            void DiscardAfterCheckboxAction(bool flag)
            {
                CompetitiveRounds.DiscardAfterPickConfig.Value = flag;
                if (CompetitiveRounds.PassDiscardConfig.Value && CompetitiveRounds.DiscardAfterPickConfig.Value)
                {
                    CompetitiveRounds.PassDiscardConfig.Value = false;
                    PassCheckbox.isOn = false;
                }
                CompetitiveRounds.PassDiscard = CompetitiveRounds.PassDiscardConfig.Value;
                CompetitiveRounds.DiscardAfterPick = CompetitiveRounds.DiscardAfterPickConfig.Value;
            }
            PassCheckbox = MenuHandler.CreateToggle(CompetitiveRounds.PassDiscardConfig.Value, "Allow players to pass during discard phase", menu, PassCheckboxAction, 30).GetComponent<Toggle>();
            DiscardAfterCheckbox = MenuHandler.CreateToggle(CompetitiveRounds.DiscardAfterPickConfig.Value, "Discard phase after pick phase", menu, DiscardAfterCheckboxAction, 30).GetComponent<Toggle>();

            MenuHandler.CreateText(" ", menu, out var _, 15);

            void PickMethodCheckboxAction(bool flag)
            {
                CompetitiveRounds.PreGamePickMethod = flag;
                if (CompetitiveRounds.PreGamePickMethod)
                {
                    StandardSlider.SetActive(true);
                    CommonSlider.SetActive(false);
                    UncommonSlider.SetActive(false);
                    RareSlider.SetActive(false);
                }
                else
                {
                    StandardSlider.SetActive(false);
                    CommonSlider.SetActive(true);
                    UncommonSlider.SetActive(true);
                    RareSlider.SetActive(true);
                }
            }
            PreGamePickMethodCheckbox = MenuHandler.CreateToggle(CompetitiveRounds.PreGamePickMethodConfig.Value, "Use standard 5 card draw for pre-game pick phase", menu, PickMethodCheckboxAction, 30).GetComponent<Toggle>();

            UnityEngine.UI.Slider standard = null;
            UnityEngine.UI.Slider common = null;
            UnityEngine.UI.Slider uncommon = null;
            UnityEngine.UI.Slider rare = null;
            void StandardChanged(float val)
            {
                CompetitiveRounds.PreGamePickStandardConfig.Value = UnityEngine.Mathf.RoundToInt(val);
                CompetitiveRounds.PreGamePickStandard = CompetitiveRounds.PreGamePickStandardConfig.Value;
            }
            void CommonChanged(float val)
            {
                CompetitiveRounds.PreGamePickCommonConfig.Value = UnityEngine.Mathf.RoundToInt(val);
                CompetitiveRounds.PreGamePickCommon = CompetitiveRounds.PreGamePickCommonConfig.Value;
            }
            void UncommonChanged(float val)
            {
                CompetitiveRounds.PreGamePickUncommonConfig.Value = UnityEngine.Mathf.RoundToInt(val);
                CompetitiveRounds.PreGamePickUncommon = CompetitiveRounds.PreGamePickUncommonConfig.Value;
            }
            void RareChanged(float val)
            {
                CompetitiveRounds.PreGamePickRareConfig.Value = UnityEngine.Mathf.RoundToInt(val);
                CompetitiveRounds.PreGamePickRare = CompetitiveRounds.PreGamePickRareConfig.Value;
            }

            StandardSlider = MenuHandler.CreateSlider("Pre-game picks", menu, 30, 0f, 10f, CompetitiveRounds.PreGamePickStandardConfig.Value, StandardChanged, out standard, true);
            StandardSlider.SetActive(false);

            CommonSlider = MenuHandler.CreateSlider("Pre-game common picks", menu, 30, 0f, 10f, CompetitiveRounds.PreGamePickCommonConfig.Value, CommonChanged, out common, true);
            CommonSlider.SetActive(false);

            UncommonSlider = MenuHandler.CreateSlider("Pre-game uncommon picks", menu, 30, 0f, 10f, CompetitiveRounds.PreGamePickUncommonConfig.Value, UncommonChanged, out uncommon, true);
            UncommonSlider.SetActive(false);

            RareSlider = MenuHandler.CreateSlider("Pre-game rare picks", menu, 30, 0f, 10f, CompetitiveRounds.PreGamePickRareConfig.Value, RareChanged, out rare, true);
            RareSlider.SetActive(false);


            if (PreGamePickMethodCheckbox.isOn)
            {
                StandardSlider.SetActive(true);
                CommonSlider.SetActive(false);
                UncommonSlider.SetActive(false);
                RareSlider.SetActive(false);
            }
            else
            {
                StandardSlider.SetActive(false);
                CommonSlider.SetActive(true);
                UncommonSlider.SetActive(true);
                RareSlider.SetActive(true);
            }
            

            MenuHandler.CreateText(" ", menu, out var _, 15);

            void BanChanged(float val)
            {
                CompetitiveRounds.PreGameBanConfig.Value = UnityEngine.Mathf.RoundToInt(val);
                CompetitiveRounds.PreGameBan = CompetitiveRounds.PreGameBanConfig.Value;
            }
            MenuHandler.CreateSlider("Pre-game ban picks", menu, 30, 0f, 10f, CompetitiveRounds.PreGameBanConfig.Value, BanChanged, out UnityEngine.UI.Slider ban, true);

            MenuHandler.CreateText(" ", menu, out var _, 15);

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
            WinByTwoPointsCheckbox = MenuHandler.CreateToggle(CompetitiveRounds.WinByTwoPointsConfig.Value, "Win By Two Points to break ties", menu, WinByTwoPointsCheckboxAction, 30).GetComponent<Toggle>();
            WinByTwoRoundsCheckbox = MenuHandler.CreateToggle(CompetitiveRounds.WinByTwoRoundsConfig.Value, "Win By Two Rounds to break ties", menu, WinByTwoRoundsCheckboxAction, 30).GetComponent<Toggle>();
            MenuHandler.CreateText(" ", menu, out var _, 5);

            void ResetButton()
            {
                timerSlider.value = 0f;
                TimerChanged(0f);
                maxSlider.value = 0f;
                MaxChanged(0f);
                PassCheckbox.isOn = false;
                DiscardAfterCheckbox.isOn = false;
                PassCheckboxAction(false);
                DiscardAfterCheckboxAction(false);
                if (standard != null) { standard.value = 0f; }
                StandardChanged(0f);
                if (common != null) { common.value = 0f; }
                CommonChanged(0f); 
                if (uncommon != null) { uncommon.value = 0f; }
                UncommonChanged(0f); 
                if (rare != null) { rare.value = 0f; }
                RareChanged(0f);
                ban.value = 0f;
                BanChanged(0f);
                WinByTwoPointsCheckbox.isOn = false;
                WinByTwoRoundsCheckbox.isOn = false;
                WinByTwoPointsCheckboxAction(false);
                WinByTwoRoundsCheckboxAction(false);
            }
            void DefaultButton()
            {
                timerSlider.value = 15f;
                TimerChanged(15f);
                maxSlider.value = 0f;
                MaxChanged(0f);
                PassCheckbox.isOn = false;
                DiscardAfterCheckbox.isOn = false;
                PassCheckboxAction(false);
                DiscardAfterCheckboxAction(false);
                PreGamePickMethodCheckbox.isOn = true;
                PickMethodCheckboxAction(true);
                if (standard != null) { standard.value = 2f; }
                StandardChanged(2f);
                if (common != null) { common.value = 0f; }
                CommonChanged(0f);
                if (uncommon != null) { uncommon.value = 0f; }
                UncommonChanged(0f);
                if (rare != null) { rare.value = 0f; }
                RareChanged(0f);
                ban.value = 2f;
                BanChanged(2f);
                WinByTwoPointsCheckbox.isOn = false;
                WinByTwoRoundsCheckbox.isOn = true;
                WinByTwoPointsCheckboxAction(false);
                WinByTwoRoundsCheckboxAction(true);
            }
            MenuHandler.CreateButton("Disable All", menu, ResetButton, 30);
            MenuHandler.CreateButton("Sane Defaults", menu, DefaultButton, 30);
        }
    }
}

