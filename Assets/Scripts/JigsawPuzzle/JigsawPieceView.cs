using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NanokaGame.Games.Jigsaw
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class JigsawPieceView : MonoBehaviour,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private BoxCollider2D _boxCollider;
        [SerializeField] private float _selectionScale = 1.04f;
        [SerializeField] private float _selectionDuration = 0.08f;

        private JigsawPuzzleController _controller;
        private Vector3 _baseLocalScale = Vector3.one;
        private Color _baseColor = Color.white;
        private int _baseSortingOrder;
        private int _activePointerId = int.MinValue;
        private Tween _selectionTween;
        private bool _isDragging;

        public int PieceId { get; private set; } = -1;

        public int CurrentSlotIndex { get; private set; } = -1;

        public SpriteRenderer SpriteRenderer
        {
            get { return _spriteRenderer; }
        }

        public void Configure(
            JigsawPuzzleController controller,
            int pieceId,
            Sprite sprite,
            Material sharedMaterial,
            int sortingLayerId,
            int sortingOrder,
            Vector3 localScale,
            Vector3 worldPosition)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (pieceId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pieceId));
            }

            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            RefreshReferences();
            if (_spriteRenderer == null || _boxCollider == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(JigsawPieceView)} on '{name}' requires a SpriteRenderer and BoxCollider2D.");
            }

            _controller = controller;
            PieceId = pieceId;
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.sharedMaterial = sharedMaterial;
            _spriteRenderer.sortingLayerID = sortingLayerId;
            _spriteRenderer.sortingOrder = sortingOrder;
            _baseSortingOrder = sortingOrder;
            _baseColor = _spriteRenderer.color;
            _baseLocalScale = localScale;
            transform.localScale = localScale;
            transform.position = worldPosition;
            _boxCollider.offset = sprite.bounds.center;
            _boxCollider.size = sprite.bounds.size;
            _boxCollider.enabled = true;
            _isDragging = false;
            _activePointerId = int.MinValue;
        }

        public void SetCurrentSlotIndex(int slotIndex)
        {
            if (slotIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            CurrentSlotIndex = slotIndex;
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        public void SetSelected(bool isSelected, int sortingOrderOffset)
        {
            RefreshReferences();
            KillSelectionTween();

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = isSelected
                    ? _baseSortingOrder + Mathf.Max(0, sortingOrderOffset)
                    : _baseSortingOrder;
                _spriteRenderer.color = _baseColor;
            }

            Vector3 targetScale = isSelected
                ? _baseLocalScale * Mathf.Max(1f, _selectionScale)
                : _baseLocalScale;
            float duration = Mathf.Max(0f, _selectionDuration);

            if (!Application.isPlaying || duration <= 0f)
            {
                transform.localScale = targetScale;
                return;
            }

            _selectionTween = transform
                .DOScale(targetScale, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnKill(() => _selectionTween = null);
        }

        public void SetColliderEnabled(bool isEnabled)
        {
            RefreshReferences();
            if (_boxCollider != null)
            {
                _boxCollider.enabled = isEnabled;
            }
        }

        public void ResetPresentation()
        {
            KillSelectionTween();
            transform.localScale = _baseLocalScale;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
                _spriteRenderer.sortingOrder = _baseSortingOrder;
            }

            SetColliderEnabled(true);
            _isDragging = false;
            _activePointerId = int.MinValue;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData != null)
            {
                eventData.useDragThreshold = false;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null || _controller == null)
            {
                return;
            }

            if (_controller.TryBeginDrag(this, eventData))
            {
                _isDragging = true;
                _activePointerId = eventData.pointerId;
                SetColliderEnabled(false);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || eventData == null || eventData.pointerId != _activePointerId)
            {
                return;
            }

            _controller.UpdateDrag(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging || eventData == null || eventData.pointerId != _activePointerId)
            {
                return;
            }

            JigsawPieceView targetPiece = null;
            GameObject targetObject = eventData.pointerCurrentRaycast.gameObject;
            if (targetObject != null)
            {
                targetPiece = targetObject.GetComponentInParent<JigsawPieceView>();
            }

            _controller.ReleaseDrag(this, targetPiece, eventData);
            SetColliderEnabled(true);
            _isDragging = false;
            _activePointerId = int.MinValue;
        }

        private void Awake()
        {
            RefreshReferences();
        }

        private void Reset()
        {
            RefreshReferences();
        }

        private void OnValidate()
        {
            _selectionScale = Mathf.Max(1f, _selectionScale);
            _selectionDuration = Mathf.Max(0f, _selectionDuration);
            RefreshReferences();
        }

        private void OnDisable()
        {
            if (_isDragging && _controller != null)
            {
                _controller.CancelInteraction(this);
            }

            KillSelectionTween();
            _isDragging = false;
            _activePointerId = int.MinValue;

            if (_boxCollider != null)
            {
                _boxCollider.enabled = true;
            }
        }

        private void RefreshReferences()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_boxCollider == null)
            {
                _boxCollider = GetComponent<BoxCollider2D>();
            }
        }

        private void KillSelectionTween()
        {
            if (_selectionTween == null)
            {
                return;
            }

            _selectionTween.Kill(false);
            _selectionTween = null;
        }
    }
}
