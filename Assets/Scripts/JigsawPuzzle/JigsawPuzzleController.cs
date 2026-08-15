using System;
using DG.Tweening;
using NanokaGame.Games.Jigsaw.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace NanokaGame.Games.Jigsaw
{
    [DisallowMultipleComponent]
    public sealed class JigsawPuzzleController : MonoBehaviour
    {
        private const int InvalidPointerId = int.MinValue;
        private const string TitleSceneName = "Title";

        [SerializeField] private JigsawBoardView _boardView;
        [SerializeField] private Camera _inputCamera;
        [SerializeField] private JigsawUiController _uiController;
        [SerializeField] private int _dragSortingOrderOffset = 100;
        [SerializeField] private float _swapDuration = 0.2f;
        [SerializeField] private float _invalidReturnDuration = 0.12f;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioClip _swapClip;
        [SerializeField] private AudioClip _completeClip;
        [SerializeField] private bool _showDifficultySelectionOnStart = true;

        private JigsawBoardState _boardState;
        private JigsawPieceView _activePiece;
        private int _activePointerId = InvalidPointerId;
        private Vector3 _pointerGrabOffset;
        private Tween _activeMoveTween;
        private System.Random _random;
        private JigsawDifficulty _currentDifficulty = JigsawDifficulty.Normal;
        private JigsawGameState _state = JigsawGameState.SelectingDifficulty;
        private int _moveCount;
        private double _elapsedTimeSeconds;
        private bool _timerRunning;
        private int _lastDisplayedSecond = -1;

        public JigsawGameState State
        {
            get { return _state; }
        }

        public JigsawDifficulty CurrentDifficulty
        {
            get { return _currentDifficulty; }
        }

        public JigsawBoardState BoardState
        {
            get { return _boardState; }
        }

        public int MoveCount
        {
            get { return _moveCount; }
        }

        public double ElapsedTimeSeconds
        {
            get { return _elapsedTimeSeconds; }
        }

        public int PieceViewCount
        {
            get { return _boardView == null ? 0 : _boardView.PieceCount; }
        }

        public bool CanAcceptInput
        {
            get { return _state == JigsawGameState.Playing && _boardState != null; }
        }

        public void Configure(
            JigsawBoardView boardView,
            Camera inputCamera,
            JigsawUiController uiController,
            bool showDifficultySelectionOnStart)
        {
            if (boardView == null)
            {
                throw new ArgumentNullException(nameof(boardView));
            }

            if (inputCamera == null)
            {
                throw new ArgumentNullException(nameof(inputCamera));
            }

            _boardView = boardView;
            _inputCamera = inputCamera;
            _uiController = uiController;
            _showDifficultySelectionOnStart = showDifficultySelectionOnStart;
        }

        public void ConfigureAnimation(float swapDuration, float invalidReturnDuration)
        {
            if (swapDuration < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(swapDuration));
            }

            if (invalidReturnDuration < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(invalidReturnDuration));
            }

            _swapDuration = swapDuration;
            _invalidReturnDuration = invalidReturnDuration;
        }

        public void SetRandomSeed(int seed)
        {
            _random = new System.Random(seed);
        }

        public void ShowDifficultySelection()
        {
            CancelInteractionAndSync();
            ClearBoard();
            _timerRunning = false;
            _moveCount = 0;
            _elapsedTimeSeconds = 0d;
            _lastDisplayedSecond = -1;
            _state = JigsawGameState.SelectingDifficulty;

            if (_boardView != null)
            {
                _boardView.ShowSourcePreview();
            }

            if (_uiController != null)
            {
                _uiController.ShowDifficultySelection(_currentDifficulty);
            }
        }

        public bool StartGame(JigsawDifficulty difficulty)
        {
            if (!TryValidateRequiredReferences())
            {
                return false;
            }

            CancelInteractionAndSync();
            ClearBoard();
            _state = JigsawGameState.Preparing;
            _currentDifficulty = difficulty;

            try
            {
                int columns;
                int rows;
                GetGridSize(difficulty, out columns, out rows);

                _boardState = new JigsawBoardState(columns, rows);
                _boardView.InitializePuzzle(this, columns, rows);

                _boardState.Shuffle(GetRandom());
                SyncAllPieceViewsImmediately();

                _moveCount = 0;
                _elapsedTimeSeconds = 0d;
                _lastDisplayedSecond = -1;
                _timerRunning = true;
                _state = JigsawGameState.Playing;

                if (_uiController != null)
                {
                    _uiController.ShowPlaying();
                    _uiController.SetProgress(_moveCount, _elapsedTimeSeconds);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                ClearBoard();
                _timerRunning = false;
                _state = JigsawGameState.SelectingDifficulty;

                if (_boardView != null)
                {
                    _boardView.ShowSourcePreview();
                }

                if (_uiController != null)
                {
                    _uiController.ShowDifficultySelection(_currentDifficulty);
                }

                return false;
            }
        }

        public void RestartGame()
        {
            StartGame(_currentDifficulty);
        }

        public void RequestExitToTitle()
        {
            _timerRunning = false;
            CancelInteractionAndSync();

            if (!Application.CanStreamedLevelBeLoaded(TitleSceneName))
            {
                Debug.LogError(
                    $"Cannot load scene '{TitleSceneName}'. Add it to Build Settings and enable it.",
                    this);
                return;
            }

            SceneManager.LoadScene(TitleSceneName);
        }

        public bool TryBeginDrag(JigsawPieceView pieceView, PointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            if (!CanAcceptInput || pieceView == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return false;
            }

            if (!IsRegisteredPiece(pieceView) || _activePiece != null)
            {
                return false;
            }

            Vector3 pointerWorldPosition;
            if (!TryGetPointerWorldPosition(eventData.position, out pointerWorldPosition))
            {
                return false;
            }

            _activePiece = pieceView;
            _activePointerId = eventData.pointerId;
            _pointerGrabOffset = pointerWorldPosition - pieceView.transform.position;
            pieceView.SetSelected(true, _dragSortingOrderOffset);
            return true;
        }

        public bool UpdateDrag(JigsawPieceView pieceView, PointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            if (_activePiece != pieceView || _activePointerId != eventData.pointerId || _state != JigsawGameState.Playing)
            {
                return false;
            }

            Vector3 pointerWorldPosition;
            if (!TryGetPointerWorldPosition(eventData.position, out pointerWorldPosition))
            {
                return false;
            }

            Vector3 dragPosition = pointerWorldPosition - _pointerGrabOffset;
            dragPosition.z = _boardView.BoardPlaneZ;
            pieceView.SetWorldPosition(dragPosition);
            return true;
        }

        public bool ReleaseDrag(
            JigsawPieceView pieceView,
            JigsawPieceView targetPiece,
            PointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            if (_activePiece != pieceView || _activePointerId != eventData.pointerId)
            {
                return false;
            }

            if (targetPiece == null || targetPiece == pieceView || !IsRegisteredPiece(targetPiece))
            {
                AnimateInvalidReturn(pieceView);
                return false;
            }

            int firstSlotIndex = pieceView.CurrentSlotIndex;
            int secondSlotIndex = targetPiece.CurrentSlotIndex;
            if (!_boardState.SwapSlots(firstSlotIndex, secondSlotIndex))
            {
                AnimateInvalidReturn(pieceView);
                return false;
            }

            pieceView.SetCurrentSlotIndex(secondSlotIndex);
            targetPiece.SetCurrentSlotIndex(firstSlotIndex);
            _moveCount++;
            _state = JigsawGameState.Swapping;
            ClearActiveDrag(false);

            if (_uiController != null)
            {
                _uiController.SetProgress(_moveCount, _elapsedTimeSeconds);
            }

            PlayClip(_swapClip);
            AnimateSwap(pieceView, targetPiece);
            return true;
        }

        public void CancelInteraction(JigsawPieceView pieceView)
        {
            if (pieceView == null)
            {
                return;
            }

            if (_activePiece == pieceView)
            {
                ClearActiveDrag(true);
                SyncAllPieceViewsImmediately();
            }
        }

        private void Awake()
        {
            RefreshReferences();
        }

        private void Start()
        {
            if (_showDifficultySelectionOnStart)
            {
                ShowDifficultySelection();
            }
        }

        private void Update()
        {
            if (!_timerRunning || _state == JigsawGameState.Completed)
            {
                return;
            }

            _elapsedTimeSeconds += Time.unscaledDeltaTime;
            int displayedSecond = Mathf.FloorToInt((float)_elapsedTimeSeconds);
            if (displayedSecond == _lastDisplayedSecond)
            {
                return;
            }

            _lastDisplayedSecond = displayedSecond;
            if (_uiController != null)
            {
                _uiController.SetProgress(_moveCount, _elapsedTimeSeconds);
            }
        }

        private void OnDisable()
        {
            _timerRunning = false;
            CancelInteractionAndSync();
        }

        private void OnDestroy()
        {
            ClearBoard();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CancelInteractionAndSync();
            }
        }

        private void Reset()
        {
            RefreshReferences();
        }

        private void OnValidate()
        {
            _dragSortingOrderOffset = Mathf.Max(0, _dragSortingOrderOffset);
            _swapDuration = Mathf.Max(0f, _swapDuration);
            _invalidReturnDuration = Mathf.Max(0f, _invalidReturnDuration);
            RefreshReferences();
        }

        private void RefreshReferences()
        {
            if (_boardView == null)
            {
                _boardView = GetComponent<JigsawBoardView>();
            }

            if (_inputCamera == null)
            {
                _inputCamera = Camera.main;
            }

            if (_uiController == null)
            {
                _uiController = GetComponent<JigsawUiController>();
            }
        }

        private bool TryValidateRequiredReferences()
        {
            RefreshReferences();

            if (_boardView == null || !_boardView.HasRequiredReferences)
            {
                Debug.LogError(
                    $"{nameof(JigsawPuzzleController)} on '{name}' requires a configured {nameof(JigsawBoardView)}.",
                    this);
                return false;
            }

            if (_inputCamera == null)
            {
                Debug.LogError(
                    $"{nameof(JigsawPuzzleController)} on '{name}' requires an input Camera.",
                    this);
                return false;
            }

            return true;
        }

        private void SyncAllPieceViewsImmediately()
        {
            if (_boardState == null || _boardView == null || _boardView.PieceCount == 0)
            {
                return;
            }

            _boardView.SyncAll(_boardState);
        }

        private void AnimateSwap(JigsawPieceView firstPiece, JigsawPieceView secondPiece)
        {
            KillActiveMoveTween();
            Vector3 firstTarget = _boardView.GetSlotWorldPosition(firstPiece.CurrentSlotIndex);
            Vector3 secondTarget = _boardView.GetSlotWorldPosition(secondPiece.CurrentSlotIndex);

            if (!Application.isPlaying || _swapDuration <= 0f)
            {
                firstPiece.SetWorldPosition(firstTarget);
                secondPiece.SetWorldPosition(secondTarget);
                FinishSwap();
                return;
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(firstPiece.transform.DOMove(firstTarget, _swapDuration).SetEase(Ease.OutCubic));
            sequence.Join(secondPiece.transform.DOMove(secondTarget, _swapDuration).SetEase(Ease.OutCubic));
            _activeMoveTween = sequence.OnComplete(FinishSwap).OnKill(() => _activeMoveTween = null);
        }

        private void AnimateInvalidReturn(JigsawPieceView pieceView)
        {
            _state = JigsawGameState.Swapping;
            int slotIndex = pieceView.CurrentSlotIndex;
            Vector3 targetPosition = _boardView.GetSlotWorldPosition(slotIndex);
            ClearActiveDrag(false);
            KillActiveMoveTween();

            if (!Application.isPlaying || _invalidReturnDuration <= 0f)
            {
                pieceView.SetWorldPosition(targetPosition);
                _state = JigsawGameState.Playing;
                return;
            }

            _activeMoveTween = pieceView.transform
                .DOMove(targetPosition, _invalidReturnDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(() => _state = JigsawGameState.Playing)
                .OnKill(() => _activeMoveTween = null);
        }

        private void FinishSwap()
        {
            _activeMoveTween = null;
            SyncAllPieceViewsImmediately();

            if (_boardState != null && _boardState.IsCompleted)
            {
                CompleteGame();
                return;
            }

            _state = JigsawGameState.Playing;
        }

        private void CompleteGame()
        {
            if (_state == JigsawGameState.Completed)
            {
                return;
            }

            _state = JigsawGameState.Completed;
            _timerRunning = false;
            SyncAllPieceViewsImmediately();
            PlayClip(_completeClip);

            if (_uiController != null)
            {
                _uiController.ShowCompleted(_moveCount, _elapsedTimeSeconds);
            }
        }

        private void CancelInteractionAndSync()
        {
            KillActiveMoveTween();
            ClearActiveDrag(true);
            SyncAllPieceViewsImmediately();

            if (_boardState == null)
            {
                return;
            }

            _state = _boardState.IsCompleted
                ? JigsawGameState.Completed
                : JigsawGameState.Playing;
        }

        private void ClearActiveDrag(bool enableCollider)
        {
            if (_activePiece != null)
            {
                _activePiece.SetSelected(false, _dragSortingOrderOffset);
                if (enableCollider)
                {
                    _activePiece.SetColliderEnabled(true);
                }
            }

            _activePiece = null;
            _activePointerId = InvalidPointerId;
            _pointerGrabOffset = Vector3.zero;
        }

        private void KillActiveMoveTween()
        {
            if (_activeMoveTween == null)
            {
                return;
            }

            _activeMoveTween.Kill(false);
            _activeMoveTween = null;
        }

        private void ClearBoard()
        {
            KillActiveMoveTween();
            ClearActiveDrag(true);

            if (_boardView != null)
            {
                _boardView.ClearPieces();
            }

            _boardState = null;
        }

        private bool IsRegisteredPiece(JigsawPieceView pieceView)
        {
            return _boardView != null && _boardView.IsRegisteredPiece(pieceView);
        }

        private bool TryGetPointerWorldPosition(Vector2 screenPosition, out Vector3 worldPosition)
        {
            if (_inputCamera == null)
            {
                worldPosition = default;
                return false;
            }

            Ray ray = _inputCamera.ScreenPointToRay(screenPosition);
            float boardPlaneZ = _boardView == null ? 0f : _boardView.BoardPlaneZ;
            Plane boardPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, boardPlaneZ));
            float distance;
            if (!boardPlane.Raycast(ray, out distance))
            {
                worldPosition = default;
                return false;
            }

            worldPosition = ray.GetPoint(distance);
            worldPosition.z = boardPlaneZ;
            return true;
        }

        private System.Random GetRandom()
        {
            if (_random == null)
            {
                _random = new System.Random(unchecked(Environment.TickCount + GetInstanceID()));
            }

            return _random;
        }

        private void PlayClip(AudioClip clip)
        {
            if (_sfxSource != null && clip != null)
            {
                _sfxSource.PlayOneShot(clip);
            }
        }

        private static void GetGridSize(JigsawDifficulty difficulty, out int columns, out int rows)
        {
            switch (difficulty)
            {
                case JigsawDifficulty.Easy:
                    columns = 4;
                    rows = 2;
                    break;

                case JigsawDifficulty.Normal:
                    columns = 6;
                    rows = 3;
                    break;

                case JigsawDifficulty.Hard:
                    columns = 8;
                    rows = 4;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unknown difficulty.");
            }
        }

    }
}
