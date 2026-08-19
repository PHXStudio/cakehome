using System;
using UnityEngine;
using Watermelon.Map;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Watermelon
{
    public class GameController : MonoBehaviour
    {
        private static GameController gameController;

        [DrawReference]
        [SerializeField] GameData data;

        [LineSpacer]
        [SerializeField] UIController uiController;
        [SerializeField] MapBehavior mapBehavior;
        [SerializeField] MusicSource musicSource;

        private static LevelController levelController;
        private static ParticlesController particlesController;
        private static FloatingTextController floatingTextController;
        private static PUController powerUpController;
        private static TutorialController tutorialController;

        public static GameData Data => gameController.data;

        private static bool isGameActive;
        public static bool IsGameActive => isGameActive;

        private void Awake()
        {
            gameController = this;

            // Cache components
            CacheComponent(out particlesController);
            CacheComponent(out floatingTextController);
            CacheComponent(out levelController);
            CacheComponent(out powerUpController); 
            CacheComponent(out tutorialController);

            musicSource.Init();
            musicSource.Activate();

            uiController.Init();

            particlesController.Init();
            floatingTextController.Init();

            powerUpController.Init();
            levelController.Init();
            tutorialController.Init();

            ShopController.EnsureInitialized();
            HubModuleRouter.EnsureInitialized();

            uiController.InitPages();

            AdsManager.TryToLoadFirstAds();
            CustomAnalytics.Init();
            DailyRewardController.Init();
            DailyRewardController.ResetStreakIfMissed();
            DailyTaskController.Init();
            PushNotificationManager.Init();
        }

        private void Start()
        {
            // Kick the loading screen's fade-out first (it fades over 0.6s). The game UI is
            // revealed a beat later so it never overlaps the loading screen when coming in
            // from the Init scene (LoadingGraphics is DontDestroyOnLoad, so it is still up).
            GameLoading.MarkAsReadyToHide();

            Tween.DelayedCall(0.7f, () =>
            {
                ITutorial tutorial = TutorialController.GetTutorial(TutorialID.FirstLevel);
                if (data.ShowTutorial && !tutorial.IsFinished)
                {
                    // Start first level tutorial
                    CustomAnalytics.TrackTutorialStart();
                    tutorial.StartTutorial();
                }
                else
                {
                    if (PlayerPrefs.GetInt("FirstLaunch", 1) == 1)
                    {
                        PlayerPrefs.SetInt("FirstLaunch", 0);
                        PlayerPrefs.Save();
                        try { CustomAnalytics.TrackInstall(); } catch { }
                    }
                    try { CustomAnalytics.TrackLaunch(); } catch { }

                    mapBehavior.Show();

                    UIBottomNavBar.Show();
                    UIBottomNavBar.SelectTab(MainHubTab.Camper, force: true);

                    AdsManager.EnableBanner();

#if UNITY_EDITOR
                    CheckIfNeedToAutoRunLevel();
#endif
                }
            });
        }

        public static void LoadLevel(int index, SimpleCallback onLevelLoaded = null, bool halfPrice = false)
        {
            CustomAnalytics.TrackLevelStart(index);
            DailyTaskController.AddProgress(DailyTaskType.LevelsPlayed);

            LivesSystem.LockLife(halfPrice);

            AdsManager.ShowInterstitial(null);

            UIBottomNavBar.Hide();
            HubModuleRouter.ExitAll();

            gameController.mapBehavior.Hide();

            AdsManager.EnableBanner();

            levelController.LoadLevel(index, onLevelLoaded);

            UIController.ShowPage<UIGame>();

            isGameActive = true;
        }

        public static void LoadCustomLevel(LevelData levelData, PreloadedLevelData preloadedLevelData, BackgroundData backgroundData, bool animateDock, SimpleCallback onLevelLoaded = null)
        {
            UIBottomNavBar.Hide();
            HubModuleRouter.ExitAll();

            levelController.LoadCustomLevel(levelData, preloadedLevelData, backgroundData, animateDock, onLevelLoaded);

            UIController.ShowPage<UIGame>();

            isGameActive = true;
        }

        public static void OnLevelCompleted()
        {
            if (!isGameActive)
                return;

            // Track level with safe timer fallback
            try
            {
                float playTime = Time.timeSinceLevelLoad > 0 ? Time.timeSinceLevelLoad : 0;
                int tilesLeft = LevelController.LevelRepresentation != null ? LevelController.LevelRepresentation.Tiles.Count : 0;
                CustomAnalytics.TrackLevelComplete(LevelController.DisplayedLevelIndex, playTime, tilesLeft);
                DailyTaskController.AddProgress(DailyTaskType.LevelsWon);
            }
            catch { }

            // First level cleared 鈫?show guest registration
            if (LevelController.DisplayedLevelIndex == 0 && !GuestRegistration.IsRegistered())
            {
                GuestRegistration.RegisterAsGuest();
                GuestRegistration.ClaimRegistrationReward();
            }

            SaveController.Save();

            UIController.HidePage<UIGame>(() =>
            {
                UIController.ShowPage<UIComplete>();
            });

            isGameActive = false;
        }

        public static void OnLevelFailed()
        {
            if (!isGameActive)
                return;

            RaycastController.Disable();

            // Track fail with safe timer fallback
            try
            {
                float playTime = Time.timeSinceLevelLoad > 0 ? Time.timeSinceLevelLoad : 0;
                int tilesLeft = LevelController.LevelRepresentation != null ? LevelController.LevelRepresentation.Tiles.Count : 0;
                CustomAnalytics.TrackLevelFail(LevelController.DisplayedLevelIndex, playTime, tilesLeft);
            }
            catch { }

            SaveController.Save();

            LivesSystem.UnlockLife(true);

            UIController.HidePage<UIGame>(() =>
            {
                UIController.ShowPage<UIGameOver>();
            });

            isGameActive = false;
        }

        public static void LoadNextLevel(SimpleCallback onLevelLoaded = null)
        {
            LoadLevel(LevelController.DisplayedLevelIndex, onLevelLoaded);
        }

        public static void ReplayLevel()
        {
            isGameActive = false;

            SaveController.Save();

            UIController.ShowPage<UIMainMenu>();

            LoadLevel(LevelController.DisplayedLevelIndex);
        }

        /// <summary>D4 半价重试：以半价体力重新进入当前关卡。</summary>
        public static void ReplayLevelHalfPrice()
        {
            isGameActive = false;

            SaveController.Save();

            UIController.ShowPage<UIMainMenu>();

            LoadLevel(LevelController.DisplayedLevelIndex, halfPrice: true);
        }

        /// <summary>D4 积分续局：花费 coins 直接续命，不足则返回 false。</summary>
        public static bool ReviveWithCoins(int coins)
        {
            if (CurrencyController.Get(CurrencyType.Coins) < coins)
                return false;

            CurrencyController.Substract(CurrencyType.Coins, coins);
            CustomAnalytics.TrackCurrencySpend("coins", coins, "revive");

            Revive();

            return true;
        }

        public static void ReturnToMenu()
        {
            isGameActive = false;

            LevelController.UnloadLevel();

            gameController.mapBehavior.Show();

            AdsManager.ShowInterstitial(null);

            UIBottomNavBar.Show();
            UIBottomNavBar.SelectTab(MainHubTab.Camper, force: true);

            AdsManager.EnableBanner();

            SaveController.Save();
        }

        public static void Revive()
        {
            LevelController.Revive();

            Tween.NextFrame(() =>
            {
                isGameActive = true;
            });
        }

        #region Extensions
        public bool CacheComponent<T>(out T component) where T : Component
        {
            Component unboxedComponent = gameObject.GetComponent(typeof(T));

            if (unboxedComponent != null)
            {
                component = (T)unboxedComponent;

                return true;
            }

            Debug.LogError(string.Format("Scripts Holder doesn't have {0} script added to it", typeof(T)));

            component = null;

            return false;
        }
        #endregion

        #region Dev

#if UNITY_EDITOR

        private static readonly string AUTO_RUN_LEVEL_SAVE_NAME = "auto run level editor";

        public static bool AutoRunLevelInEditor
        {
            get { return EditorPrefs.GetBool(AUTO_RUN_LEVEL_SAVE_NAME, false); }
            set { EditorPrefs.SetBool(AUTO_RUN_LEVEL_SAVE_NAME, value); }
        }

        private void CheckIfNeedToAutoRunLevel()
        {
            if (AutoRunLevelInEditor)
                LoadLevel(LevelController.DisplayedLevelIndex);

            AutoRunLevelInEditor = false;
        }
#endif


        #endregion
    }
}




