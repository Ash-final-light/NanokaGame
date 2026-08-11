using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NanokaGame.Games.Klotski
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(KlotskiBoardView))]
    public sealed class KlotskiGameController : MonoBehaviour
    {
        private const int InvalidPointerId = int.MinValue;

        [SerializeField] private KlotskiBoardView _boardView;
        [SerializeField] private SpriteRenderer _topLeftAnchorRenderer;
        [SerializeField] private Camera _inputCamera;
        [SerializeField] private float _cellSize = 1.68f;
        [SerializeField] private float _pieceGap;
        [SerializeField] private float _boardPlaneZ;
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private float _dragThresholdRatio = 0.12f;
        [SerializeField] private float _commitThresholdRatio = 0.35f;
        [SerializeField] private Color _selectedColor = Color.white;
        [SerializeField] private int _selectedSortingOrderOffset = 10;
        [SerializeField] private float _legalMoveDuration = 0.15f;
        [SerializeField] private float _invalidReturnDuration = 0.12f;

        private KlotskiBoardModel _model;
        private KlotskiBoardLayout _layout;
        private KlotskiPieceView _activePieceView;
        private string _activePieceId;
        private int _activePointerId = InvalidPointerId;
        private Vector2Int _dragStartCell;
        private Vector2Int _dragPieceSize;
        private Vector3 _dragStartWorldPosition;
        private Vector3 _dragStartPointerWorldPosition;
        private Vector3 _grabOffset;
        private int _maxMoveLeft;
        private int _maxMoveRight;
        private int _maxMoveUp;
        private int _maxMoveDown;
        private KlotskiDragAxis _dragAxis;
        private Tween _activeMoveTween;
        private KlotskiPieceView _movingPieceView;
        private string _movingPieceId;

        public bool IsInitialized
        {
            get { return _model != null && _layout != null && _boardView != null && _boardView.IsInitialized; }
        }

        public bool IsDragging
        {
            get { return _activePieceView != null; }
        }

        public bool IsMoving
        {
            get { return _movingPieceView != null; }
        }

        public bool CanAcceptInput
        {
            get { return IsInitialized && !IsDragging && !IsMoving && !_model.IsCompleted; }
        }

        public int ActivePointerId
        {
            get { return _activePointerId; }
        }

        public string ActivePieceId
        {
            get { return _activePieceId; }
        }

        public KlotskiDragAxis DragAxis
        {
            get { return _dragAxis; }
        }

        public KlotskiBoardModel Model
        {
            get { return _model; }
        }

        public KlotskiBoardLayout Layout
        {
            get { return _layout; }
        }

        public KlotskiBoardView BoardView
        {
            get { return _boardView; }
        }

        public void Configure(
            KlotskiBoardView boardView,
            SpriteRenderer topLeftAnchorRenderer,
            float cellSize,
            float pieceGap,
            float boardPlaneZ,
            bool initializeOnAwake)
        {
            _boardView = boardView;
            _topLeftAnchorRenderer = topLeftAnchorRenderer;
            _cellSize = cellSize;
            _pieceGap = pieceGap;
            _boardPlaneZ = boardPlaneZ;
            _initializeOnAwake = initializeOnAwake;
        }

        public void ConfigureDragPresentation(
            float dragThresholdRatio,
            float commitThresholdRatio,
            Color selectedColor,
            int selectedSortingOrderOffset)
        {
            if (dragThresholdRatio <= 0f || dragThresholdRatio > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dragThresholdRatio),
                    dragThresholdRatio,
                    "Drag threshold ratio must be greater than zero and at most one.");
            }

            if (commitThresholdRatio <= 0f || commitThresholdRatio > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(commitThresholdRatio),
                    commitThresholdRatio,
                    "Commit threshold ratio must be greater than zero and at most one.");
            }

            if (selectedSortingOrderOffset < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(selectedSortingOrderOffset),
                    selectedSortingOrderOffset,
                    "Selected sorting order offset cannot be negative.");
            }

            _dragThresholdRatio = dragThresholdRatio;
            _commitThresholdRatio = commitThresholdRatio;
            _selectedColor = selectedColor;
            _selectedSortingOrderOffset = selectedSortingOrderOffset;
        }

        public void ConfigureInputCamera(Camera inputCamera)
        {
            _inputCamera = inputCamera;
        }

        public void ConfigureAnimationDurations(
            float legalMoveDuration,
            float invalidReturnDuration)
        {
            ValidateAnimationDuration(legalMoveDuration, nameof(legalMoveDuration));
            ValidateAnimationDuration(invalidReturnDuration, nameof(invalidReturnDuration));
            _legalMoveDuration = legalMoveDuration;
            _invalidReturnDuration = invalidReturnDuration;
        }

        public void InitializeGame()
        {
            CancelAllInteractionAndSync();
            EnsureReferences();

            KlotskiLevelDefinition level = KlotskiLevelDefinition.CreateDefault();
            _model = new KlotskiBoardModel(level);
            _layout = KlotskiBoardLayout.CreateFromAnchorBounds(
                _topLeftAnchorRenderer.bounds,
                _model.Columns,
                _model.Rows,
                _cellSize,
                _pieceGap,
                _boardPlaneZ);
            _boardView.Initialize(_model, _layout);
            BindPieceInputs();
        }

        public void ResetGame()
        {
            EnsureInitialized();
            CancelAllInteractionAndSync();
            _model.Reset();
            _boardView.SyncAllViews();
        }

        public void SyncAllViews()
        {
            EnsureInitialized();
            CancelAllInteractionAndSync();
            _boardView.SyncAllViews();
        }

        public bool TryBeginDrag(KlotskiPieceView pieceView, PointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return false;
            }

            Vector3 pointerWorldPosition;
            if (!TryGetPointerWorldPosition(eventData, out pointerWorldPosition))
            {
                return false;
            }

            return TryBeginDrag(pieceView, eventData.pointerId, pointerWorldPosition);
        }

        public bool TryBeginDrag(
            KlotskiPieceView pieceView,
            int pointerId,
            Vector3 pointerWorldPosition)
        {
            if (!CanAcceptInput || pieceView == null)
            {
                return false;
            }

            KlotskiPieceView mappedView;
            if (!_boardView.TryGetPieceView(pieceView.PieceId, out mappedView) || mappedView != pieceView)
            {
                return false;
            }

            KlotskiPieceState piece;
            if (!_model.TryGetPiece(pieceView.PieceId, out piece))
            {
                return false;
            }

            _activePieceView = pieceView;
            _activePieceId = piece.Id;
            _activePointerId = pointerId;
            _dragStartCell = piece.Cell;
            _dragPieceSize = piece.SizeInCells;
            _dragStartWorldPosition = _layout.GetPieceWorldPosition(piece.Cell, piece.SizeInCells);
            _dragStartPointerWorldPosition = MoveToBoardPlane(pointerWorldPosition);
            _grabOffset = _dragStartPointerWorldPosition - _dragStartWorldPosition;
            _maxMoveLeft = _model.GetMaxMoveDistance(piece.Id, Vector2Int.left);
            _maxMoveRight = _model.GetMaxMoveDistance(piece.Id, Vector2Int.right);
            _maxMoveUp = _model.GetMaxMoveDistance(piece.Id, Vector2Int.up);
            _maxMoveDown = _model.GetMaxMoveDistance(piece.Id, Vector2Int.down);
            _dragAxis = KlotskiDragAxis.None;

            _activePieceView.SetWorldPosition(_dragStartWorldPosition);
            _activePieceView.SetSelected(true, _selectedColor, _selectedSortingOrderOffset);
            return true;
        }

        public bool UpdateDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            Vector3 pointerWorldPosition;
            if (!TryGetPointerWorldPosition(eventData, out pointerWorldPosition))
            {
                return false;
            }

            return UpdateDrag(eventData.pointerId, pointerWorldPosition);
        }

        public bool UpdateDrag(int pointerId, Vector3 pointerWorldPosition)
        {
            if (!IsActivePointer(pointerId))
            {
                return false;
            }

            Vector3 boardPointerPosition = MoveToBoardPlane(pointerWorldPosition);
            TryLockDragAxis(boardPointerPosition - _dragStartPointerWorldPosition);

            if (_dragAxis == KlotskiDragAxis.None)
            {
                return true;
            }

            Vector3 desiredPosition = boardPointerPosition - _grabOffset;
            Vector3 previewPosition = _dragStartWorldPosition;

            if (_dragAxis == KlotskiDragAxis.Horizontal)
            {
                float minimumX = GetAllowedWorldPosition(Vector2Int.left, _maxMoveLeft).x;
                float maximumX = GetAllowedWorldPosition(Vector2Int.right, _maxMoveRight).x;
                previewPosition.x = Mathf.Clamp(desiredPosition.x, minimumX, maximumX);
            }
            else
            {
                float minimumY = GetAllowedWorldPosition(Vector2Int.down, _maxMoveDown).y;
                float maximumY = GetAllowedWorldPosition(Vector2Int.up, _maxMoveUp).y;
                previewPosition.y = Mathf.Clamp(desiredPosition.y, minimumY, maximumY);
            }

            _activePieceView.SetWorldPosition(previewPosition);
            return true;
        }

        public KlotskiMoveResult ReleaseDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            Vector3 pointerWorldPosition;
            if (!TryGetPointerWorldPosition(eventData, out pointerWorldPosition))
            {
                if (IsActivePointer(eventData.pointerId))
                {
                    return FinishActiveDrag(KlotskiMoveResult.InvalidDistance);
                }

                return KlotskiMoveResult.InvalidDistance;
            }

            return ReleaseDrag(eventData.pointerId, pointerWorldPosition);
        }

        public KlotskiMoveResult ReleaseDrag(int pointerId, Vector3 pointerWorldPosition)
        {
            if (!IsActivePointer(pointerId))
            {
                return KlotskiMoveResult.InvalidDistance;
            }

            UpdateDrag(pointerId, pointerWorldPosition);

            if (_dragAxis == KlotskiDragAxis.None)
            {
                return FinishActiveDrag(KlotskiMoveResult.InvalidDistance);
            }

            Vector3 previewPosition = _activePieceView.transform.position;
            float axisDisplacement = _dragAxis == KlotskiDragAxis.Horizontal
                ? previewPosition.x - _dragStartWorldPosition.x
                : previewPosition.y - _dragStartWorldPosition.y;

            if (Mathf.Abs(axisDisplacement) < _layout.CellSize * _commitThresholdRatio)
            {
                return FinishActiveDrag(KlotskiMoveResult.InvalidDistance);
            }

            int directionSign = axisDisplacement > 0f ? 1 : -1;
            Vector2Int candidateCell = _layout.GetNearestCell(previewPosition, _dragPieceSize);
            int signedDistance;
            int maximumDistance;
            Vector2Int direction;

            if (_dragAxis == KlotskiDragAxis.Horizontal)
            {
                candidateCell.y = _dragStartCell.y;
                signedDistance = candidateCell.x - _dragStartCell.x;
                maximumDistance = directionSign > 0 ? _maxMoveRight : _maxMoveLeft;
                direction = directionSign > 0 ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                candidateCell.x = _dragStartCell.x;
                signedDistance = candidateCell.y - _dragStartCell.y;
                maximumDistance = directionSign > 0 ? _maxMoveUp : _maxMoveDown;
                direction = directionSign > 0 ? Vector2Int.up : Vector2Int.down;
            }

            if (maximumDistance <= 0)
            {
                return FinishActiveDrag(KlotskiMoveResult.InvalidDistance);
            }

            if (signedDistance == 0 || Math.Sign(signedDistance) != directionSign)
            {
                signedDistance = directionSign;
            }

            int distance = Mathf.Clamp(Mathf.Abs(signedDistance), 1, maximumDistance);
            KlotskiMoveResult moveResult = _model.TryMove(_activePieceId, direction, distance);
            return FinishActiveDrag(moveResult);
        }

        public bool CancelDrag(KlotskiPieceView pieceView, int pointerId)
        {
            if (_activePieceView != pieceView || !IsActivePointer(pointerId))
            {
                return false;
            }

            FinishActiveDrag(KlotskiMoveResult.InvalidDistance);
            return true;
        }

        public bool CancelDrag(KlotskiPieceView pieceView)
        {
            if (_activePieceView != pieceView)
            {
                return false;
            }

            FinishActiveDrag(KlotskiMoveResult.InvalidDistance);
            return true;
        }

        public bool CancelInteraction(KlotskiPieceView pieceView)
        {
            bool cancelled = false;

            if (_activePieceView == pieceView)
            {
                CancelActiveDragImmediately();
                cancelled = true;
            }

            if (_movingPieceView == pieceView)
            {
                CancelActiveAnimationAndSync();
                cancelled = true;
            }

            return cancelled;
        }

        private void Awake()
        {
            if (_initializeOnAwake)
            {
                InitializeGame();
            }
        }

        private void OnDisable()
        {
            CancelAllInteractionAndSync();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CancelAllInteractionAndSync();
            }
        }

        private void Reset()
        {
            RefreshReferences();
        }

        private void OnValidate()
        {
            _dragThresholdRatio = Mathf.Clamp(_dragThresholdRatio, 0.01f, 1f);
            _commitThresholdRatio = Mathf.Clamp(_commitThresholdRatio, 0.01f, 1f);
            _selectedSortingOrderOffset = Mathf.Max(0, _selectedSortingOrderOffset);
            _legalMoveDuration = SanitizeAnimationDuration(_legalMoveDuration, 0.15f);
            _invalidReturnDuration = SanitizeAnimationDuration(_invalidReturnDuration, 0.12f);
            RefreshReferences();
        }

        private void BindPieceInputs()
        {
            IReadOnlyList<KlotskiPieceView> pieceViews = _boardView.PieceViews;
            for (int viewIndex = 0; viewIndex < pieceViews.Count; viewIndex++)
            {
                pieceViews[viewIndex].BindInputController(this);
                pieceViews[viewIndex].SyncInputCollider();
            }
        }

        private void TryLockDragAxis(Vector3 pointerDelta)
        {
            if (_dragAxis != KlotskiDragAxis.None)
            {
                return;
            }

            float absoluteX = Mathf.Abs(pointerDelta.x);
            float absoluteY = Mathf.Abs(pointerDelta.y);
            if (Mathf.Max(absoluteX, absoluteY) < _layout.CellSize * _dragThresholdRatio)
            {
                return;
            }

            bool canMoveHorizontally = _maxMoveLeft > 0 || _maxMoveRight > 0;
            bool canMoveVertically = _maxMoveUp > 0 || _maxMoveDown > 0;

            if (canMoveHorizontally && !canMoveVertically)
            {
                _dragAxis = KlotskiDragAxis.Horizontal;
            }
            else if (canMoveVertically && !canMoveHorizontally)
            {
                _dragAxis = KlotskiDragAxis.Vertical;
            }
            else
            {
                _dragAxis = absoluteX >= absoluteY
                    ? KlotskiDragAxis.Horizontal
                    : KlotskiDragAxis.Vertical;
            }
        }

        private Vector3 GetAllowedWorldPosition(Vector2Int direction, int distance)
        {
            Vector2Int allowedCell = _dragStartCell + direction * distance;
            return _layout.GetPieceWorldPosition(allowedCell, _dragPieceSize);
        }

        private KlotskiMoveResult FinishActiveDrag(KlotskiMoveResult moveResult)
        {
            KlotskiPieceView pieceView = _activePieceView;
            string pieceId = _activePieceId;
            ClearDragState();

            if (pieceView == null || _model == null || _layout == null)
            {
                return moveResult;
            }

            KlotskiPieceState piece;
            if (!_model.TryGetPiece(pieceId, out piece))
            {
                pieceView.RestoreVisualState();
                return moveResult;
            }

            Vector3 targetPosition = _layout.GetPieceWorldPosition(piece.Cell, piece.SizeInCells);
            float duration = moveResult == KlotskiMoveResult.Success
                ? _legalMoveDuration
                : _invalidReturnDuration;

            if (duration <= 0f || !isActiveAndEnabled || !pieceView.gameObject.activeInHierarchy)
            {
                pieceView.SetWorldPosition(targetPosition);
                pieceView.RestoreVisualState();
                return moveResult;
            }

            StartMoveAnimation(pieceView, pieceId, targetPosition, duration, moveResult);
            return moveResult;
        }

        private void StartMoveAnimation(
            KlotskiPieceView pieceView,
            string pieceId,
            Vector3 targetPosition,
            float duration,
            KlotskiMoveResult moveResult)
        {
            CancelActiveAnimationAndSync();
            _movingPieceView = pieceView;
            _movingPieceId = pieceId;
            _activeMoveTween = pieceView.transform
                .DOMove(targetPosition, duration)
                .SetEase(moveResult == KlotskiMoveResult.Success ? Ease.OutCubic : Ease.OutQuad)
                .OnComplete(CompleteActiveAnimation)
                .OnKill(HandleActiveAnimationKilled);
        }

        private void CompleteActiveAnimation()
        {
            KlotskiPieceView pieceView = _movingPieceView;
            string pieceId = _movingPieceId;
            ClearAnimationState();
            SyncPieceToModel(pieceView, pieceId);
        }

        private void HandleActiveAnimationKilled()
        {
            if (!IsMoving)
            {
                return;
            }

            KlotskiPieceView pieceView = _movingPieceView;
            string pieceId = _movingPieceId;
            ClearAnimationState();
            SyncPieceToModel(pieceView, pieceId);
        }

        private void CancelAllInteractionAndSync()
        {
            CancelActiveDragImmediately();
            CancelActiveAnimationAndSync();
        }

        private void CancelActiveDragImmediately()
        {
            if (!IsDragging)
            {
                return;
            }

            KlotskiPieceView pieceView = _activePieceView;
            string pieceId = _activePieceId;
            ClearDragState();
            SyncPieceToModel(pieceView, pieceId);
        }

        private void CancelActiveAnimationAndSync()
        {
            if (!IsMoving)
            {
                return;
            }

            Tween moveTween = _activeMoveTween;
            KlotskiPieceView pieceView = _movingPieceView;
            string pieceId = _movingPieceId;
            ClearAnimationState();

            if (moveTween != null && moveTween.IsActive())
            {
                moveTween.Kill(false);
            }

            SyncPieceToModel(pieceView, pieceId);
        }

        private void SyncPieceToModel(KlotskiPieceView pieceView, string pieceId)
        {
            if (pieceView == null)
            {
                return;
            }

            if (_boardView != null &&
                _boardView.IsInitialized &&
                _model != null &&
                !string.IsNullOrWhiteSpace(pieceId))
            {
                KlotskiPieceState piece;
                if (_model.TryGetPiece(pieceId, out piece))
                {
                    pieceView.SetWorldPosition(
                        _layout.GetPieceWorldPosition(piece.Cell, piece.SizeInCells));
                }
            }

            pieceView.RestoreVisualState();
        }

        private void ClearAnimationState()
        {
            _activeMoveTween = null;
            _movingPieceView = null;
            _movingPieceId = null;
        }

        private void ClearDragState()
        {
            _activePieceView = null;
            _activePieceId = null;
            _activePointerId = InvalidPointerId;
            _dragStartCell = default(Vector2Int);
            _dragPieceSize = default(Vector2Int);
            _dragStartWorldPosition = default(Vector3);
            _dragStartPointerWorldPosition = default(Vector3);
            _grabOffset = default(Vector3);
            _maxMoveLeft = 0;
            _maxMoveRight = 0;
            _maxMoveUp = 0;
            _maxMoveDown = 0;
            _dragAxis = KlotskiDragAxis.None;
        }

        private bool IsActivePointer(int pointerId)
        {
            return IsDragging && _activePointerId == pointerId;
        }

        private bool TryGetPointerWorldPosition(
            PointerEventData eventData,
            out Vector3 pointerWorldPosition)
        {
            if (!IsInitialized)
            {
                pointerWorldPosition = default(Vector3);
                return false;
            }

            Camera eventCamera = eventData.pressEventCamera;
            if (eventCamera == null)
            {
                eventCamera = _inputCamera;
            }

            if (eventCamera == null)
            {
                eventCamera = Camera.main;
            }

            if (eventCamera == null)
            {
                pointerWorldPosition = default(Vector3);
                return false;
            }

            Ray pointerRay = eventCamera.ScreenPointToRay(eventData.position);
            if (Mathf.Abs(pointerRay.direction.z) <= Mathf.Epsilon)
            {
                pointerWorldPosition = default(Vector3);
                return false;
            }

            float rayDistance = (_layout.BoardPlaneZ - pointerRay.origin.z) / pointerRay.direction.z;
            if (rayDistance < 0f)
            {
                pointerWorldPosition = default(Vector3);
                return false;
            }

            pointerWorldPosition = MoveToBoardPlane(pointerRay.GetPoint(rayDistance));
            return true;
        }

        private Vector3 MoveToBoardPlane(Vector3 worldPosition)
        {
            worldPosition.z = _layout == null ? _boardPlaneZ : _layout.BoardPlaneZ;
            return worldPosition;
        }

        private void RefreshReferences()
        {
            if (_boardView == null)
            {
                _boardView = GetComponent<KlotskiBoardView>();
            }

            if (_topLeftAnchorRenderer == null)
            {
                KlotskiPieceView[] views = GetComponentsInChildren<KlotskiPieceView>(true);
                for (int viewIndex = 0; viewIndex < views.Length; viewIndex++)
                {
                    if (string.Equals(
                        views[viewIndex].PieceId,
                        "nanoka_left_arm",
                        StringComparison.Ordinal))
                    {
                        _topLeftAnchorRenderer = views[viewIndex].SpriteRenderer;
                        break;
                    }
                }
            }

            if (_inputCamera == null)
            {
                _inputCamera = Camera.main;
            }
        }

        private void EnsureReferences()
        {
            RefreshReferences();

            if (_boardView == null)
            {
                throw new InvalidOperationException("KlotskiGameController requires a KlotskiBoardView.");
            }

            if (_topLeftAnchorRenderer == null)
            {
                throw new InvalidOperationException(
                    "KlotskiGameController requires the nanoka_left_arm SpriteRenderer as its top-left anchor.");
            }
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("KlotskiGameController has not been initialized.");
            }
        }

        private static void ValidateAnimationDuration(float duration, string parameterName)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    duration,
                    "Animation duration must be finite and cannot be negative.");
            }
        }

        private static float SanitizeAnimationDuration(float duration, float defaultDuration)
        {
            return float.IsNaN(duration) || float.IsInfinity(duration)
                ? defaultDuration
                : Mathf.Max(0f, duration);
        }
    }
}
