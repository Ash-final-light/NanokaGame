using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NanokaGame.Games.Klotski
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class KlotskiPieceView : MonoBehaviour,
        IPointerDownHandler,
        IBeginDragHandler,
        IDragHandler,
        IPointerUpHandler,
        IEndDragHandler
    {
        [SerializeField] private string _pieceId;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private BoxCollider2D _inputCollider;

        private Color _baseColor = Color.white;
        private int _baseSortingOrder;
        private bool _hasBaseVisualState;
        private KlotskiGameController _inputController;

        public string PieceId
        {
            get { return _pieceId; }
        }

        public SpriteRenderer SpriteRenderer
        {
            get { return _spriteRenderer; }
        }

        public BoxCollider2D InputCollider
        {
            get { return _inputCollider; }
        }

        public bool IsConfigured
        {
            get { return !string.IsNullOrWhiteSpace(_pieceId) && _spriteRenderer != null; }
        }

        public void Configure(string pieceId, SpriteRenderer spriteRenderer)
        {
            if (string.IsNullOrWhiteSpace(pieceId))
            {
                throw new ArgumentException("Piece ID cannot be null, empty, or whitespace.", nameof(pieceId));
            }

            if (spriteRenderer == null)
            {
                throw new ArgumentNullException(nameof(spriteRenderer));
            }

            if (spriteRenderer.gameObject != gameObject)
            {
                throw new ArgumentException(
                    "The SpriteRenderer must be attached to the same GameObject as the piece view.",
                    nameof(spriteRenderer));
            }

            _pieceId = pieceId;
            _spriteRenderer = spriteRenderer;
            EnsureInputColliderReference();
            CaptureBaseVisualState();
            SyncInputCollider();
        }

        public void BindInputController(KlotskiGameController inputController)
        {
            _inputController = inputController;
        }

        public void ApplyLayout(KlotskiPieceState piece, KlotskiBoardLayout layout)
        {
            if (piece == null)
            {
                throw new ArgumentNullException(nameof(piece));
            }

            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            EnsureConfigured();

            if (!string.Equals(piece.Id, _pieceId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Piece view '{0}' cannot display model piece '{1}'.",
                        _pieceId,
                        piece.Id));
            }

            transform.position = layout.GetPieceWorldPosition(piece.Cell, piece.SizeInCells);
            ApplyWorldSize(layout.GetPieceWorldSize(piece.SizeInCells));
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            if (!IsFinite(worldPosition.x) ||
                !IsFinite(worldPosition.y) ||
                !IsFinite(worldPosition.z))
            {
                throw new ArgumentException("World position must contain only finite values.", nameof(worldPosition));
            }

            transform.position = worldPosition;
        }

        public void ApplyWorldSize(Vector2 targetWorldSize)
        {
            EnsureConfigured();

            if (!IsFinite(targetWorldSize.x) ||
                !IsFinite(targetWorldSize.y) ||
                targetWorldSize.x <= 0f ||
                targetWorldSize.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetWorldSize),
                    targetWorldSize,
                    "Target world size must be finite and positive on both axes.");
            }

            if (_spriteRenderer.drawMode == SpriteDrawMode.Simple)
            {
                ApplySimpleSpriteWorldSize(targetWorldSize);
                SyncInputCollider();
                return;
            }

            ApplyResizableSpriteWorldSize(targetWorldSize);
            SyncInputCollider();
        }

        public void SyncInputCollider()
        {
            EnsureRendererReference();
            EnsureInputColliderReference();

            if (_inputCollider == null)
            {
                return;
            }

            Bounds localBounds = _spriteRenderer.localBounds;
            if (localBounds.size.x <= Mathf.Epsilon || localBounds.size.y <= Mathf.Epsilon)
            {
                return;
            }

            _inputCollider.isTrigger = true;
            _inputCollider.offset = new Vector2(localBounds.center.x, localBounds.center.y);
            _inputCollider.size = new Vector2(localBounds.size.x, localBounds.size.y);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            KlotskiGameController controller = ResolveInputController();
            if (controller != null)
            {
                controller.TryBeginDrag(this, eventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            KlotskiGameController controller = ResolveInputController();
            if (controller != null)
            {
                controller.UpdateDrag(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            KlotskiGameController controller = ResolveInputController();
            if (controller != null)
            {
                controller.ReleaseDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            KlotskiGameController controller = ResolveInputController();
            if (controller != null)
            {
                controller.CancelDrag(this, eventData.pointerId);
            }
        }

        public void SetSelected(bool isSelected, Color selectedColor, int sortingOrderOffset)
        {
            EnsureConfigured();
            EnsureBaseVisualState();

            if (sortingOrderOffset < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sortingOrderOffset),
                    sortingOrderOffset,
                    "Sorting order offset cannot be negative.");
            }

            _spriteRenderer.color = isSelected ? selectedColor : _baseColor;
            _spriteRenderer.sortingOrder = isSelected
                ? _baseSortingOrder + sortingOrderOffset
                : _baseSortingOrder;
        }

        public void RestoreVisualState()
        {
            EnsureConfigured();
            EnsureBaseVisualState();
            _spriteRenderer.color = _baseColor;
            _spriteRenderer.sortingOrder = _baseSortingOrder;
        }

        public void CaptureBaseVisualState()
        {
            EnsureRendererReference();
            _baseColor = _spriteRenderer.color;
            _baseSortingOrder = _spriteRenderer.sortingOrder;
            _hasBaseVisualState = true;
        }

        private void Awake()
        {
            EnsureRendererReference();
            EnsureInputColliderReference();
            CaptureBaseVisualState();
            SyncInputCollider();
        }

        private void Reset()
        {
            _pieceId = gameObject.name;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _inputCollider = GetComponent<BoxCollider2D>();
            CaptureBaseVisualState();
            SyncInputCollider();
        }

        private void OnValidate()
        {
            EnsureRendererReference();
            EnsureInputColliderReference();

            if (string.IsNullOrWhiteSpace(_pieceId))
            {
                _pieceId = gameObject.name;
            }

            SyncInputCollider();
        }

        private void OnDisable()
        {
            if (_inputController != null)
            {
                _inputController.CancelInteraction(this);
            }
        }

        private void ApplySimpleSpriteWorldSize(Vector2 targetWorldSize)
        {
            Sprite sprite = _spriteRenderer.sprite;
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    string.Format("Simple SpriteRenderer for piece '{0}' does not have a Sprite.", _pieceId));
            }

            Vector2 spriteSize = sprite.bounds.size;
            if (spriteSize.x <= Mathf.Epsilon || spriteSize.y <= Mathf.Epsilon)
            {
                throw new InvalidOperationException(
                    string.Format("Sprite for piece '{0}' has an invalid bounds size {1}.", _pieceId, spriteSize));
            }

            Vector3 parentLossyScale = transform.parent == null
                ? Vector3.one
                : transform.parent.lossyScale;
            float parentScaleX = Mathf.Abs(parentLossyScale.x);
            float parentScaleY = Mathf.Abs(parentLossyScale.y);
            EnsureScaleIsUsable(parentScaleX, parentScaleY);

            Vector3 localScale = transform.localScale;
            localScale.x = GetScaleSign(localScale.x) * targetWorldSize.x / (spriteSize.x * parentScaleX);
            localScale.y = GetScaleSign(localScale.y) * targetWorldSize.y / (spriteSize.y * parentScaleY);
            transform.localScale = localScale;
        }

        private void ApplyResizableSpriteWorldSize(Vector2 targetWorldSize)
        {
            Vector3 lossyScale = transform.lossyScale;
            float worldScaleX = Mathf.Abs(lossyScale.x);
            float worldScaleY = Mathf.Abs(lossyScale.y);
            EnsureScaleIsUsable(worldScaleX, worldScaleY);

            _spriteRenderer.size = new Vector2(
                targetWorldSize.x / worldScaleX,
                targetWorldSize.y / worldScaleY);
        }

        private void EnsureConfigured()
        {
            EnsureRendererReference();

            if (string.IsNullOrWhiteSpace(_pieceId))
            {
                throw new InvalidOperationException(
                    string.Format("KlotskiPieceView on '{0}' does not have a Piece ID.", gameObject.name));
            }
        }

        private void EnsureRendererReference()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_spriteRenderer == null)
            {
                throw new InvalidOperationException(
                    string.Format("KlotskiPieceView on '{0}' requires a SpriteRenderer.", gameObject.name));
            }
        }

        private void EnsureInputColliderReference()
        {
            if (_inputCollider == null)
            {
                _inputCollider = GetComponent<BoxCollider2D>();
            }
        }

        private KlotskiGameController ResolveInputController()
        {
            if (_inputController == null)
            {
                _inputController = GetComponentInParent<KlotskiGameController>();
            }

            return _inputController;
        }

        private void EnsureBaseVisualState()
        {
            if (!_hasBaseVisualState)
            {
                CaptureBaseVisualState();
            }
        }

        private static void EnsureScaleIsUsable(float scaleX, float scaleY)
        {
            if (scaleX <= Mathf.Epsilon || scaleY <= Mathf.Epsilon)
            {
                throw new InvalidOperationException("A piece or its parent has a zero world scale.");
            }
        }

        private static float GetScaleSign(float scale)
        {
            return scale < 0f ? -1f : 1f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
