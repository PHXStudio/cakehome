
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIGame : UIPage
    {
        [SerializeField] RectTransform safeAreaRectTransform;
        [SerializeField] CurrencyUIPanelSimple coinsPanel;
        [SerializeField] UILevelQuitPopUp quitPopUp;
        [SerializeField] UILevelNumberText levelNumberText;
        [SerializeField] TimerVisualiser gameplayTimer;

        [SerializeField] PUUIController powerUpsUIController;
        public PUUIController PowerUpsUIController => powerUpsUIController;

        [SerializeField] UILevelQuitPopUp exitPopUp;
        [SerializeField] Button exitButton;
        [SerializeField] UIFadeAnimation exitButtonFadeAnimation;

        [SerializeField] GameObject devOverlay;

        [LineSpacer("Tutorial")]
        [SerializeField] GameObject tutorialPanelObject;
        [SerializeField] TextMeshProUGUI tutorialTitleText;
        [SerializeField] TextMeshProUGUI tutorialDescriptionText;
        [SerializeField] Button tutorialSkipButton;

        // 三消爽感：Combo/Fever UI（运行时创建，挂在安全区下）
        private TextMeshProUGUI comboText;

        public TimerVisualiser GameplayTimer => gameplayTimer;

        public override void Init()
        {
            coinsPanel.Init();
            exitButton.onClick.AddListener(ShowExitPopUp);
            exitButtonFadeAnimation.Hide(immediately: true);

            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
            NotchSaveArea.RegisterRectTransform((RectTransform)tutorialPanelObject.transform);

            DevPanelEnabler.RegisterPanel(devOverlay);

            tutorialSkipButton.onClick.AddListener(OnTutorialSkipButtonClicked);
        }

        private void OnEnable()
        {
            exitPopUp.OnConfirmExitEvent += ExitPopUpConfirmExitButton;
            exitPopUp.OnCancelExitEvent += ExitPopCloseButton;

            // 三消爽感：Combo/Fever 事件
            ComboController.OnComboChanged += OnComboChangedHandler;
            ComboController.OnFeverChanged += OnFeverChangedHandler;
        }

        private void OnDisable()
        {
            exitPopUp.OnConfirmExitEvent -= ExitPopUpConfirmExitButton;
            exitPopUp.OnCancelExitEvent -= ExitPopCloseButton;

            ComboController.OnComboChanged -= OnComboChangedHandler;
            ComboController.OnFeverChanged -= OnFeverChangedHandler;
        }

        #region Show/Hide

        protected override void OnShow()
        {
            coinsPanel.Activate();
            exitButtonFadeAnimation.Show();

            if (comboText != null) comboText.gameObject.SetActive(false);

            UILevelNumberText.Show();

            IntToggle timer = LevelController.Level.Timer;
            if (timer.Enabled)
            {
                gameplayTimer.gameObject.SetActive(true);
                gameplayTimer.Show(LevelController.GameplayTimer);
            }
            else
            {
                gameplayTimer.gameObject.SetActive(false);
            }

            NotifyOpened();
        }

        protected override void OnHide()
        {
            coinsPanel.Disable();
            exitButtonFadeAnimation.Hide();

            if (comboText != null) comboText.gameObject.SetActive(false);

            UILevelNumberText.Hide();

            IntToggle timer = LevelController.Level.Timer;
            if (timer.Enabled)
            {
                gameplayTimer.Hide();
            }

            NotifyClosed();
        }

        public void UpdateLevelNumber(int levelNumber)
        {
            levelNumberText.UpdateLevelNumber(levelNumber);
        }
        #endregion

        #region Combo / Fever（三消爽感）

        private void CreateComboUI()
        {
            if (comboText != null) return;

            var go = new GameObject("ComboText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            comboText = go.GetComponent<TextMeshProUGUI>();
            comboText.alignment = TextAlignmentOptions.Center;
            comboText.fontSize = 44;
            comboText.fontStyle = FontStyles.Bold;
            comboText.color = Color.white;
            comboText.outlineWidth = 0.3f;
            comboText.outlineColor = new Color(0f, 0f, 0f, 0.65f);

            // 字体复用金币面板的 TMP 字体
            var sample = coinsPanel != null ? coinsPanel.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (sample != null) comboText.font = sample.font;

            var rt = comboText.rectTransform;
            rt.SetParent(safeAreaRectTransform, false);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -16f);
            rt.sizeDelta = new Vector2(600f, 64f);

            comboText.gameObject.SetActive(false);
        }

        private void OnComboChangedHandler(int combo, float mult)
        {
            CreateComboUI();

            if (combo <= 0)
            {
                if (comboText != null) comboText.gameObject.SetActive(false);
                return;
            }

            comboText.gameObject.SetActive(true);
            comboText.text = ComboController.IsFever
                ? $"FEVER!  {mult:0.#}x"
                : $"COMBO x{combo}   {mult:0.#}x";

            comboText.color = ComboController.IsFever ? new Color(1f, 0.8f, 0.15f) : Color.white;
            comboText.transform.localScale = Vector3.one * 0.55f;
            comboText.transform.DOScale(Vector3.one, 0.25f).SetEasing(Ease.Type.BackOut);

            // 金币面板弹跳（印钞感）
            if (coinsPanel != null)
            {
                var coinRt = coinsPanel.GetComponent<RectTransform>();
                if (coinRt != null) coinRt.DOPushScale(Vector3.one * 1.12f, Vector3.one, 0.2f, 0.08f);
            }
        }

        private void OnFeverChangedHandler(bool isFever)
        {
            CreateComboUI();

            if (!isFever)
            {
                if (comboText != null)
                {
                    comboText.color = Color.white;
                    comboText.text = $"COMBO x{ComboController.Combo}   {ComboController.GetMultiplier():0.#}x";
                }
                return;
            }

            if (comboText != null)
            {
                comboText.gameObject.SetActive(true);
                comboText.color = new Color(1f, 0.8f, 0.15f);
                comboText.text = $"FEVER!  {ComboController.GetMultiplier():0.#}x";
                comboText.transform.DOPushScale(Vector3.one * 1.35f, Vector3.one, 0.3f, 0.08f);
            }
        }

        #endregion

        public void ShowExitPopUp()
        {
            if(!LivesSystem.InfiniteMode)
            {
                exitPopUp.Show();
            }
            else
            {
                ExitPopUpConfirmExitButton();
            }

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }

        public void ExitPopCloseButton()
        {
            exitPopUp.Hide();
        }

        public void ExitPopUpConfirmExitButton()
        {
            LivesSystem.UnlockLife(true);

            UIController.HidePage<UIGame>();

            GameController.ReturnToMenu();

            exitPopUp.Hide();
        }

        #region Tutorial
        public void ActivateTutorial()
        {
            tutorialPanelObject.SetActive(true);

            exitButton.gameObject.SetActive(false);
            levelNumberText.gameObject.SetActive(false);

            powerUpsUIController.HidePanels();
        }

        public void DisableTutorial()
        {
            tutorialPanelObject.SetActive(false);

            exitButton.gameObject.SetActive(true);
            levelNumberText.gameObject.SetActive(true);
        }

        public void SetTutorialText(string title, string description)
        {
            tutorialTitleText.text = title;
            tutorialDescriptionText.text = description;

            tutorialTitleText.transform.localScale = Vector3.one * 0.6f;
            tutorialTitleText.transform.DOScale(1.0f, 0.3f).SetEasing(Ease.Type.BackOut);

            tutorialDescriptionText.transform.localScale = Vector3.one * 0.6f;
            tutorialDescriptionText.transform.DOScale(1.0f, 0.3f).SetEasing(Ease.Type.BackOut);
        }

        private void OnTutorialSkipButtonClicked()
        {
            ITutorial tutorial = TutorialController.GetTutorial(TutorialID.FirstLevel);
            if(tutorial != null)
            {
                FirstLevelTutorial firstLevelTutorial = (FirstLevelTutorial)tutorial;
                firstLevelTutorial.OnSkipButtonClicked();
            }
        }
        #endregion

        #region Development

        public void ReloadDev()
        {
            GameController.ReplayLevel();

            UIController.DisablePage<UIGame>();
            UIController.ShowPage<UIGame>();
        }

        public void HideDev()
        {
            devOverlay.SetActive(false);
        }

        public void OnLevelInputUpdatedDev(string newLevel)
        {
            int level = -1;

            if (int.TryParse(newLevel, out level))
            {
                LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
                levelSave.DisplayLevelIndex = Mathf.Clamp((level - 1), 0, int.MaxValue);
                levelSave.RealLevelIndex = levelSave.DisplayLevelIndex;

                GameController.ReplayLevel();

                UIController.DisablePage<UIGame>();
                UIController.ShowPage<UIGame>();
            }
        }

        public void PrevLevelDev()
        {
            LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
            levelSave.DisplayLevelIndex = Mathf.Clamp(levelSave.DisplayLevelIndex - 1, 0, int.MaxValue);
            levelSave.RealLevelIndex = levelSave.DisplayLevelIndex;

            GameController.ReplayLevel();

            UIController.DisablePage<UIGame>();
            UIController.ShowPage<UIGame>();
        }

        public void NextLevelDev()
        {
            LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
            levelSave.DisplayLevelIndex = levelSave.DisplayLevelIndex + 1;
            levelSave.RealLevelIndex = levelSave.DisplayLevelIndex;

            GameController.ReplayLevel();

            UIController.DisablePage<UIGame>();
            UIController.ShowPage<UIGame>();
        }

        public void CompleteDev()
        {
            StartCoroutine(CompleteCoroutine());
        }

        private IEnumerator CompleteCoroutine()
        {
            while (GameController.IsGameActive)
            {
                PUController.UsePowerUp(PUType.Hint);

                yield return new WaitForSeconds(0.2f);
            }
            
        }

        #endregion
    }
}
