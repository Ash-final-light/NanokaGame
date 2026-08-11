using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NanokaGame.Games.Klotski.UI
{
    [DisallowMultipleComponent]
    public sealed class KlotskiHudView : MonoBehaviour
    {
        [SerializeField] private KlotskiGameController _controller;
        [SerializeField] private TMP_Text _stepCountText;
        [SerializeField] private TMP_Text _timeCountText;
        [SerializeField] private Button _exitButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private GameObject _completionRoot;
        [SerializeField] private TMP_Text _completionTimeText;
        [SerializeField] private TMP_Text _completionStepText;
        [SerializeField] private Button _completionRestartButton;
        [SerializeField] private Button _completionTitleButton;

        private KlotskiGameController _boundController;

        public GameObject CompletionRoot
        {
            get { return _completionRoot; }
        }

        public void Configure(
            KlotskiGameController controller,
            TMP_Text stepCountText,
            TMP_Text timeCountText,
            Button exitButton,
            Button restartButton,
            GameObject completionRoot,
            TMP_Text completionTimeText,
            TMP_Text completionStepText,
            Button completionRestartButton,
            Button completionTitleButton)
        {
            _controller = controller;
            _stepCountText = stepCountText;
            _timeCountText = timeCountText;
            _exitButton = exitButton;
            _restartButton = restartButton;
            _completionRoot = completionRoot;
            _completionTimeText = completionTimeText;
            _completionStepText = completionStepText;
            _completionRestartButton = completionRestartButton;
            _completionTitleButton = completionTitleButton;
        }

        public void Bind(KlotskiGameController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            Unbind();
            _boundController = controller;
            AddButtonListener(_exitButton, controller.ExitToTitle);
            AddButtonListener(_restartButton, controller.ResetGame);
            AddButtonListener(_completionRestartButton, controller.ResetGame);
            AddButtonListener(_completionTitleButton, controller.ExitToTitle);
        }

        public void Unbind(KlotskiGameController controller)
        {
            if (_boundController == controller)
            {
                Unbind();
            }
        }

        public void ShowReady(int moveCount, double elapsedTimeSeconds)
        {
            SetCounters(moveCount, elapsedTimeSeconds);

            if (_completionRoot != null)
            {
                _completionRoot.SetActive(false);
            }
        }

        public void SetCounters(int moveCount, double elapsedTimeSeconds)
        {
            if (_stepCountText != null)
            {
                _stepCountText.SetText(moveCount.ToString());
            }

            if (_timeCountText != null)
            {
                _timeCountText.SetText(FormatElapsedTime(elapsedTimeSeconds));
            }
        }

        public void ShowCompleted(int moveCount, double elapsedTimeSeconds)
        {
            SetCounters(moveCount, elapsedTimeSeconds);

            if (_completionTimeText != null)
            {
                _completionTimeText.SetText("用时：" + FormatElapsedTime(elapsedTimeSeconds));
            }

            if (_completionStepText != null)
            {
                _completionStepText.SetText("步数：" + moveCount);
            }

            if (_completionRoot != null)
            {
                _completionRoot.SetActive(true);
            }
        }

        public static string FormatElapsedTime(double elapsedTimeSeconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt((float)elapsedTimeSeconds));
            int hours = totalSeconds / 3600;
            int minutes = totalSeconds % 3600 / 60;
            int seconds = totalSeconds % 60;

            return hours > 0
                ? string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds)
                : string.Format("{0:D2}:{1:D2}", minutes, seconds);
        }

        private void OnEnable()
        {
            if (_controller != null)
            {
                Bind(_controller);
            }
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (_boundController == null)
            {
                return;
            }

            RemoveButtonListener(_exitButton, _boundController.ExitToTitle);
            RemoveButtonListener(_restartButton, _boundController.ResetGame);
            RemoveButtonListener(_completionRestartButton, _boundController.ResetGame);
            RemoveButtonListener(_completionTitleButton, _boundController.ExitToTitle);
            _boundController = null;
        }

        private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction listener)
        {
            if (button != null)
            {
                button.onClick.AddListener(listener);
            }
        }

        private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction listener)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(listener);
            }
        }
    }
}
