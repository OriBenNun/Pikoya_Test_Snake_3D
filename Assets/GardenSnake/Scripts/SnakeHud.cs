using GardenSnake.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GardenSnake
{
    public sealed class SnakeHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private GameObject modal;
        [SerializeField] private TMP_Text modalEyebrow;
        [SerializeField] private TMP_Text modalTitle;
        [SerializeField] private TMP_Text modalBody;
        [SerializeField] private TMP_Text primaryLabel;
        [SerializeField] private UnityEngine.UI.Button primaryButton;
        [SerializeField] private UnityEngine.UI.Button pauseButton;
        [SerializeField] private TMP_Text pauseLabel;
        [SerializeField] private UnityEngine.UI.Button muteButton;
        [SerializeField] private TMP_Text muteLabel;
        [SerializeField] private UnityEngine.UI.Button[] directionButtons;
        [SerializeField] private TMP_Text toast;
        [SerializeField] private CanvasGroup modalGroup;
        private SnakeController controller;
        private float toastTime;
        private float modalTime;
        private RunState lastState = (RunState)(-1);

        public void Bind(SnakeController game)
        {
            controller = game;
            primaryButton.onClick.AddListener(controller.PrimaryAction);
            pauseButton.onClick.AddListener(controller.TogglePause);
            muteButton.onClick.AddListener(controller.ToggleMute);
            for (int i = 0; i < directionButtons.Length; i++)
            {
                int direction = i;
                directionButtons[i].onClick.AddListener(() => controller.Turn(direction));
            }
            Refresh();
        }

        public void Refresh()
        {
            var game = controller.Game;
            scoreText.text = game.Score.ToString("00");
            bestText.text = "BEST  " + controller.Best.ToString("00");
            speedText.text = "PACE  " + (.25f / controller.StepSeconds).ToString("0.0") + "x";
            muteLabel.text = controller.Muted ? "SOUND OFF" : "SOUND ON";
            pauseLabel.text = game.State == RunState.Paused ? "RESUME" : "PAUSE";
            pauseButton.interactable = game.State == RunState.Playing || game.State == RunState.Paused;
            if (lastState != game.State) { modalTime = 0; lastState = game.State; }
            modal.SetActive(game.State != RunState.Playing);
            switch (game.State)
            {
                case RunState.Ready:
                    modalEyebrow.text = "A LITTLE GARDEN. A BIG APPETITE.";
                    modalTitle.text = "Room to grow.";
                    modalBody.text = "Eat apples. Grow longer.\nKeep clear of the edges and your tail.";
                    primaryLabel.text = "LET'S GROW   >";
                    break;
                case RunState.Paused:
                    modalEyebrow.text = "TAKE A BREATHER";
                    modalTitle.text = "On a leaf break.";
                    modalBody.text = "Your garden will be right here.\nReady when you are.";
                    primaryLabel.text = "KEEP GROWING   >";
                    break;
                case RunState.Lost:
                case RunState.Won:
                    modalEyebrow.text = game.State == RunState.Won ? "WHAT A HARVEST!" : "ONE MORE LITTLE GO?";
                    modalTitle.text = game.State == RunState.Won ? "Garden complete!" : "A good little run.";
                    modalBody.text = game.EndReason + "\n" + game.Score + " apples picked  /  Best " + controller.Best;
                    primaryLabel.text = "GROW AGAIN   >";
                    break;
            }
        }

        public void ShowToast(string message)
        {
            toast.text = message;
            toastTime = 1.2f;
        }

        private void Update()
        {
            toastTime = Mathf.Max(0, toastTime - Time.unscaledDeltaTime);
            toast.alpha = Mathf.Min(1, toastTime * 3);
            toast.rectTransform.anchoredPosition = new Vector2(0, -137 + (1.2f - toastTime) * 12);
            if (!modal.activeSelf) return;
            modalTime += Time.unscaledDeltaTime;
            bool ended = lastState == RunState.Lost || lastState == RunState.Won;
            float t = Mathf.Clamp01((modalTime - (ended ? .45f : 0)) / .2f);
            modalGroup.alpha = t;
            modalGroup.interactable = t >= 1;
            modalGroup.blocksRaycasts = t > 0;
            modal.transform.localScale = Vector3.one * Mathf.Lerp(.96f, 1, 1 - Mathf.Pow(1 - t, 3));
        }
    }
}
