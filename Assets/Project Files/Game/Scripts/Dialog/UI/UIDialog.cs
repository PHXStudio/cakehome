using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIDialog : UIPage
    {
        [SerializeField] TMP_Text  titleText;
        [SerializeField] Transform contentTransform;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] Button    tapToContinueArea;
        [SerializeField] Button    skipButton;
        [SerializeField] bool      instantTyping;

        [Header("Row prefabs")]
        [SerializeField] UIMessageStepView  userMessagePrefab;
        [SerializeField] UIMessageStepView  characterMessagePrefab;
        [SerializeField] UIThoughtStepView  userThoughtPrefab;
        [SerializeField] UIImageStepView    imagePrefab;

        private DialogData currentData;
        private Action     onComplete;
        private int        stepIndex;
        private UIDialogStepView currentView;

        public override void Init()
        {
            DialogController.RegisterPanel(this);
        }

        protected override void OnShow()
        {
            NotifyOpened();
        }

        protected override void OnHide()
        {
            ClearContent();
            NotifyClosed();
        }

        public void Play(DialogData data, Action onComplete)
        {
            currentData     = data;
            this.onComplete = onComplete;
            stepIndex       = -1;

            titleText.text = data.ChapterTitle;
            ClearContent();

            UIController.ShowPage<UIDialog>();

            Advance();
        }

        private void Advance()
        {
            stepIndex++;
            if (stepIndex >= currentData.Steps.Length) { Close(); return; }

            AppendStep(currentData.Steps[stepIndex], instantTyping);
        }

        private void AppendStep(DialogStep step, bool instant, bool playSound = true)
        {
            currentView = step switch
            {
                MessageStep s when s.character != null && s.character.IsMainCharacter
                                => Instantiate(userMessagePrefab, contentTransform).Setup(s, instant),
                MessageStep s   => Instantiate(characterMessagePrefab, contentTransform).Setup(s, instant),
                ThoughtStep s   => Instantiate(userThoughtPrefab, contentTransform).Setup(s, instant),
                ImageStep   s   => Instantiate(imagePrefab, contentTransform).Setup(s, instant),
                _               => null
            };

            if (playSound)
                AudioController.PlaySound(AudioController.GetClip("message_appear"));

            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        // Called by tap area button
        public void OnTapToContinue()
        {
            if (currentView != null && currentView.IsAnimating)
            {
                currentView.CompleteInstant();
                return;
            }
            Advance();
        }

        // Called by skip button
        public void OnSkipClicked()
        {
            // Fast-forward through all skippable steps until next non-skippable. Appending each
            // step still plays its sound individually — silence the ones skipped in this loop so
            // only the final, actually-visible step plays "message_appear".
            while (stepIndex + 1 < currentData.Steps.Length)
            {
                stepIndex++;
                DialogStep next = currentData.Steps[stepIndex];

                if (!next.IsSkippable)
                {
                    AppendStep(next, instantTyping);
                    return;
                }

                bool isLastStep = stepIndex + 1 >= currentData.Steps.Length;
                AppendStep(next, instant: true, playSound: isLastStep);
            }

            Close();
        }

        private void ClearContent()
        {
            for (int i = contentTransform.childCount - 1; i >= 0; i--)
                Destroy(contentTransform.GetChild(i).gameObject);

            currentView = null;
        }

        private void Close()
        {
            UIController.HidePage<UIDialog>(() => onComplete?.Invoke());
        }
    }
}
