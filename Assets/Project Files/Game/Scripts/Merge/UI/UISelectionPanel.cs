using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UISelectionPanel : MonoBehaviour
    {
        [SerializeField] GameObject panel;

        [Header("Texts")]
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text singleText;

        [Header("Action")]
        [SerializeField] GameObject actionsContainer;
        [SerializeField] Button actionButton;
        [SerializeField] Image actionIcon;
        [SerializeField] TMP_Text actionTitle;
        [SerializeField] TMP_Text actionExtraText;
        [SerializeField] Button infoButton;

        [Header("Undo")]
        [SerializeField] GameObject undoContainer;
        [SerializeField] Image undoIcon;
        [SerializeField] TMP_Text undoText;
        [SerializeField] Button undoButton;

        private const string DEFAULT_MESSAGE = "Tap an item to learn more.";
        private const string UNDO_MESSAGE = "Undo deleting";
        private const string ITEM_DESCRIPTION = "Merge 2 same items to level up!";
        private const string SPAWNER_DESCRIPTION = "Merge 2 same items to level up! Tap to produce a new one.";

        private MergeFieldObject currentObject;

        private void Awake()
        {
            actionButton.onClick.AddListener(OnActionButtonClicked);
            infoButton.onClick.AddListener(OnInfoButtonClicked);
            undoButton.onClick.AddListener(OnUndoButtonClicked);
            ShowDefaultState();
        }

        private void OnEnable()
        {
            MergeController.OnObjectSelected  += Show;
            MergeController.OnObjectDeselected += Hide;
            MergeController.OnItemDeleted      += ShowUndoState;
        }

        private void OnDisable()
        {
            MergeController.OnObjectSelected  -= Show;
            MergeController.OnObjectDeselected -= Hide;
            MergeController.OnItemDeleted      -= ShowUndoState;
        }

        private void Show(MergeFieldObject obj)
        {
            currentObject = obj;
            undoContainer.SetActive(false);

            if (obj is LockedCell)
            {
                nameText.gameObject.SetActive(false);
                descriptionText.gameObject.SetActive(false);
                singleText.gameObject.SetActive(true);
                singleText.text = "What secrets are hidden inside?";

                actionsContainer.SetActive(false);
                infoButton.gameObject.SetActive(false);
            }
            else
            {
                MergeGradeData gradeData = obj.GradeData;
                bool hasInfo = !string.IsNullOrEmpty(gradeData?.DisplayName);

                nameText.gameObject.SetActive(hasInfo);
                descriptionText.gameObject.SetActive(hasInfo);
                singleText.gameObject.SetActive(!hasInfo);

                if (hasInfo)
                {
                    nameText.text        = $"{obj.GetDisplayName()} <size=70%><alpha=#B4>Lv.({obj.Grade})";

                    if (obj is CurrencyItem || obj is EnergyItem || !string.IsNullOrEmpty(gradeData.Description))
                        descriptionText.text = gradeData.Description; // database value takes priority when set
                    else
                        descriptionText.text = obj is SpawnerObject ? SPAWNER_DESCRIPTION : ITEM_DESCRIPTION;
                }

                // Spawn has no button — re-tapping the selected spawner triggers it (see MergeController.HandleCellClick).
                ActionButtonConfig cfg = obj.GetActionButton();
                bool hasAction = cfg.Type != ActionButtonType.None && cfg.Type != ActionButtonType.Spawn && !obj.IsHalfLocked;
                actionsContainer.SetActive(hasAction);

                bool hasExtra = hasAction && !string.IsNullOrEmpty(cfg.ExtraText);
                actionExtraText.gameObject.SetActive(hasExtra);

                if (hasAction)
                {
                    actionTitle.text = cfg.Title;
                    if (hasExtra) actionExtraText.text = cfg.ExtraText;

                    Sprite icon = GameData.Data.LevelDatabase.GetActionButtonIcon(cfg.Type);
                    if (icon != null) actionIcon.sprite = icon;
                }

                // Info button: any mergeable object, including locked ones (LockedItem stub excluded)
                bool showInfo = !(obj is LockedItem);
                infoButton.gameObject.SetActive(showInfo);
            }

            panel.SetActive(true);
        }

        private void Hide()
        {
            ShowDefaultState();
        }

        private void ShowDefaultState()
        {
            currentObject = null;
            undoContainer.SetActive(false);

            nameText.gameObject.SetActive(false);
            descriptionText.gameObject.SetActive(false);
            singleText.gameObject.SetActive(true);
            singleText.text = DEFAULT_MESSAGE;

            actionsContainer.SetActive(false);
            infoButton.gameObject.SetActive(false);

            panel.SetActive(true);
        }

        private void ShowUndoState(Sprite icon)
        {
            currentObject = null;

            nameText.gameObject.SetActive(false);
            descriptionText.gameObject.SetActive(false);
            singleText.gameObject.SetActive(false);
            actionsContainer.SetActive(false);
            infoButton.gameObject.SetActive(false);

            undoContainer.SetActive(true);
            undoIcon.sprite = icon;
            undoText.text   = UNDO_MESSAGE;

            panel.SetActive(true);
        }

        public void OnActionButtonClicked()
        {
            if (currentObject == null) return;
            MergeController.Instance?.ActivateSelected();
        }

        public void OnInfoButtonClicked()
        {
            if (currentObject != null)
                UIInfoWindow.Show(currentObject);
        }

        public void OnUndoButtonClicked()
        {
            MergeController.Instance?.UndoDelete();
        }
    }
}
