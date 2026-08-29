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

        private static MergeController mergeController;
        private static SpawnerController spawnerController;
        private static TaskController taskController;
        private static ClientOrderHighlightController clientOrderHighlightController;

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

            CacheComponent(out mergeController);
            CacheComponent(out spawnerController);
            CacheComponent(out taskController);
            CacheComponent(out clientOrderHighlightController);

            musicSource.Init();
            musicSource.Activate();

            uiController.Init();

            particlesController.Init();
            floatingTextController.Init();

            powerUpController.Init();
            levelController.Init();
            tutorialController.Init();

            HubModuleRouter.EnsureInitialized();

            uiController.InitPages();

            // Merge side (mirrors mergedev boot order):
            // MergeDatabase must exist before TaskController fires OnTasksChanged for the
            // first task, or its icon lookup resolves to null.
            mergeController.Init();

            // Master tutorial switch: when GameData.showTutorial is off, onboarding counts as
            // completed (progression/order chain unlocks immediately, no tutorial UI).
            MergeViewController.TutorialsEnabled = data.ShowTutorial;

            // Zone progression tasks are held while onboarding runs — must be set before
            // taskController.Init() evaluates the queue; FirstStartTutorial unlocks on finish.
            TaskController.SetProgressionLocked(!MergeViewController.IsOnboardingCompleted());

            // Interstitials must stay silent for the whole onboarding tutorial.
            MergeViewController.AttachInterstitialGuard();

            taskController.Init();
            clientOrderHighlightController.Init();
            spawnerController.Init();

            FragmentController.Init();
            MergeStatsController.Init();

            // Game.Scripts → HotUpdate bridge: template pages open the IAP store through us
            CakeUIBridge.OpenStore = () => UIController.ShowPage<Watermelon.IAPStore.UIStore>();
            CakeUIBridge.OrderCompleted = count => DailyTaskController.AddProgress(DailyTaskType.MergeOrders, count);

            AdsManager.TryToLoadFirstAds();
            CustomAnalytics.Init();
            DailyRewardController.Init();
            DailyRewardController.ResetStreakIfMissed();
            DailyTaskController.Init();
            AvatarController.Init();
            MatchBonusController.Init();
            PushNotificationManager.Init();
        }

        private void Start()
        {
            // Init the merge board grid (board page is hidden until the shop tab opens;
            // mirrors mergedev boot — must run in Start so the canvas has been laid out).
            mergeController.InitGrid();

            // Board builds while its page is still active (GridLayoutGroup needs an active
            // canvas to measure cells) — hide it immediately afterwards: the merge pages have
            // nested canvases (Items / Flying Objects) that ignore the page-level canvas toggle,
            // and the default tab is Camper, not the merge board.
            MergeViewController.ExitHub();

            // Zone-start (atUpgrade: 0) progression orders/rewards/dialog.
            // Held back during onboarding like the task queue; FirstStartTutorial
            // triggers it on finish instead (mirrors mergedev).
            MergeViewController.TriggerInitialProgressionIfOnboarded();

            // Activate merge-side tutorials (scene objects under Scripts Holder, auto-discovered).
            // Routed through Game.Scripts — HotUpdate has its own TutorialController type.
            MergeViewController.ActivateTutorials();

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
            // 门票预扣失败（能量不足）直接中止，不进关——避免"没扣成却锁了票"的胜利白返漏洞
            if (!LivesSystem.LockLife(halfPrice))
            {
                Debug.LogWarning($"[Game] LoadLevel({index}) 中止：能量不足以支付门票");
                return;
            }

            CustomAnalytics.TrackLevelStart(index);
            DailyTaskController.AddProgress(DailyTaskType.LevelsPlayed);

            // 积分棋子：进入关卡启用
            MatchBonusController.OnLevelStarted();

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

            // First level cleared → show guest registration
            if (LevelController.DisplayedLevelIndex == 0 && !GuestRegistration.IsRegistered())
            {
                GuestRegistration.RegisterAsGuest();
                GuestRegistration.ClaimRegistrationReward();
            }

            // 胜利当场返还门票并解锁——此前返还只在 UIComplete 点 Home 时发生，
            // 结算页杀进程会被吞。Next Level 会重新 LockLife 预扣下一张票，经济净额不变。
            LivesSystem.UnlockLife(false);

            // 轻引导（C.6 观察 1）：首胜后让门店 Tab 红点出现
            UIBottomNavBar.NotifyLevelCompleted();

            SaveController.Save();

            UIController.HidePage<UIGame>(() =>
            {
                UIController.ShowPage<UIComplete>();
            });

            MatchBonusController.OnLevelEnded();
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

            MatchBonusController.OnLevelEnded();
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

            // 直接重进关卡。不 ShowPage<UIMainMenu>：失败重试时 activeModule 已为 null，
            // Camper.Exit() 不会触发来隐藏主菜单，会导致主菜单与关卡 UI 重叠。
            LoadLevel(LevelController.DisplayedLevelIndex);
        }

        /// <summary>D4 半价重试：以半价体力重新进入当前关卡。</summary>
        public static void ReplayLevelHalfPrice()
        {
            isGameActive = false;

            SaveController.Save();

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

            // 兜底同步隐藏全部游戏页面，防异步隐藏被页面切换打断后残留遮挡主菜单
            UIController.DisablePage<UIGame>();
            UIController.DisablePage<UIGameOver>();
            UIController.DisablePage<UIComplete>();

            // 兜底解锁门票锁存（失败/中途退出不返还；通关路径 UIComplete.HomeButton 已返还，此处幂等）
            LivesSystem.UnlockLife(true);

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

        private void OnDestroy()
        {
            MergeViewController.DetachInterstitialGuard();
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




