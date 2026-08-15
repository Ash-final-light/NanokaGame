using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NanokaGame.Games.Jigsaw.UI
{
    [DisallowMultipleComponent]
    public sealed class JigsawUiController : MonoBehaviour
    {
        [SerializeField] private JigsawPuzzleController _controller;
        [SerializeField] private GameObject _difficultyRoot;
        [SerializeField] private Button _easyButton;
        [SerializeField] private Button _normalButton;
        [SerializeField] private Button _hardButton;
        [SerializeField] private Button _startButton;
        [SerializeField] private GameObject _gameplayHudRoot;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _moveCountText;
        [SerializeField] private Button _exitButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private GameObject _completionRoot;
        [SerializeField] private TMP_Text _completionTimeText;
        [SerializeField] private TMP_Text _completionMoveCountText;
        [SerializeField] private Button _completionRestartButton;
        [SerializeField] private Button _completionTitleButton;

        private JigsawPuzzleController _boundController;
        private JigsawDifficulty _selectedDifficulty = JigsawDifficulty.Normal;

        public JigsawDifficulty SelectedDifficulty
        {
            get { return _selectedDifficulty; }
        }

        public GameObject DifficultyRoot
        {
            get { return _difficultyRoot; }
        }

        public GameObject CompletionRoot
        {
            get { return _completionRoot; }
        }

        public void Configure(
            JigsawPuzzleController controller,
            GameObject difficultyRoot,
            Button easyButton,
            Button normalButton,
            Button hardButton,
            Button startButton,
            GameObject gameplayHudRoot,
            TMP_Text timeText,
            TMP_Text moveCountText,
            Button exitButton,
            Button restartButton,
            GameObject completionRoot,
            TMP_Text completionTimeText,
            TMP_Text completionMoveCountText,
            Button completionRestartButton,
            Button completionTitleButton)
        {
            Unbind();
            _controller = controller;
            _difficultyRoot = difficultyRoot;
            _easyButton = easyButton;
            _normalButton = normalButton;
            _hardButton = hardButton;
            _startButton = startButton;
            _gameplayHudRoot = gameplayHudRoot;
            _timeText = timeText;
            _moveCountText = moveCountText;
            _exitButton = exitButton;
            _restartButton = restartButton;
            _completionRoot = completionRoot;
            _completionTimeText = completionTimeText;
            _completionMoveCountText = completionMoveCountText;
            _completionRestartButton = completionRestartButton;
            _completionTitleButton = completionTitleButton;

            if (isActiveAndEnabled && _controller != null)
            {
                Bind(_controller);
            }
        }

        public void Bind(JigsawPuzzleController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            Unbind();
            _boundController = controller;
            AddButtonListener(_easyButton, SelectEasy);
            AddButtonListener(_normalButton, SelectNormal);
            AddButtonListener(_hardButton, SelectHard);
            AddButtonListener(_startButton, StartSelectedGame);
            AddButtonListener(_exitButton, controller.RequestExitToTitle);
            AddButtonListener(_restartButton, controller.RestartGame);
            AddButtonListener(_completionRestartButton, controller.RestartGame);
            AddButtonListener(_completionTitleButton, controller.RequestExitToTitle);
            RefreshDifficultyButtons();
        }

        public void Unbind(JigsawPuzzleController controller)
        {
            if (_boundController == controller)
            {
                Unbind();
            }
        }

        public void ShowDifficultySelection(JigsawDifficulty selectedDifficulty)
        {
            _selectedDifficulty = selectedDifficulty;
            SetActive(_difficultyRoot, true);
            SetActive(_gameplayHudRoot, false);
            SetActive(_completionRoot, false);
            RefreshDifficultyButtons();
            SetProgress(0, 0d);
        }

        public void ShowPlaying()
        {
            SetActive(_difficultyRoot, false);
            SetActive(_gameplayHudRoot, true);
            SetActive(_completionRoot, false);
        }

        public void SetProgress(int moveCount, double elapsedTimeSeconds)
        {
            if (_timeText != null)
            {
                _timeText.SetText("时间  " + FormatElapsedTime(elapsedTimeSeconds));
            }

            if (_moveCountText != null)
            {
                _moveCountText.SetText("步数  " + Mathf.Max(0, moveCount));
            }
        }

        public void ShowCompleted(int moveCount, double elapsedTimeSeconds)
        {
            SetProgress(moveCount, elapsedTimeSeconds);

            if (_completionTimeText != null)
            {
                _completionTimeText.SetText("用时  " + FormatElapsedTime(elapsedTimeSeconds));
            }

            if (_completionMoveCountText != null)
            {
                _completionMoveCountText.SetText("步数  " + Mathf.Max(0, moveCount));
            }

            SetActive(_completionRoot, true);
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

        private void SelectEasy()
        {
            SelectDifficulty(JigsawDifficulty.Easy);
        }

        private void SelectNormal()
        {
            SelectDifficulty(JigsawDifficulty.Normal);
        }

        private void SelectHard()
        {
            SelectDifficulty(JigsawDifficulty.Hard);
        }

        private void SelectDifficulty(JigsawDifficulty difficulty)
        {
            _selectedDifficulty = difficulty;
            RefreshDifficultyButtons();
        }

        private void StartSelectedGame()
        {
            if (_boundController != null)
            {
                _boundController.StartGame(_selectedDifficulty);
            }
        }

        private void RefreshDifficultyButtons()
        {
            SetButtonSelected(_easyButton, _selectedDifficulty == JigsawDifficulty.Easy);
            SetButtonSelected(_normalButton, _selectedDifficulty == JigsawDifficulty.Normal);
            SetButtonSelected(_hardButton, _selectedDifficulty == JigsawDifficulty.Hard);
        }

        private void Unbind()
        {
            if (_boundController == null)
            {
                return;
            }

            RemoveButtonListener(_easyButton, SelectEasy);
            RemoveButtonListener(_normalButton, SelectNormal);
            RemoveButtonListener(_hardButton, SelectHard);
            RemoveButtonListener(_startButton, StartSelectedGame);
            RemoveButtonListener(_exitButton, _boundController.RequestExitToTitle);
            RemoveButtonListener(_restartButton, _boundController.RestartGame);
            RemoveButtonListener(_completionRestartButton, _boundController.RestartGame);
            RemoveButtonListener(_completionTitleButton, _boundController.RequestExitToTitle);
            _boundController = null;
        }

        private static void SetButtonSelected(Button button, bool isSelected)
        {
            if (button != null)
            {
                button.interactable = !isSelected;
            }
        }

        private static void SetActive(GameObject gameObject, bool isActive)
        {
            if (gameObject != null)
            {
                gameObject.SetActive(isActive);
            }
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
