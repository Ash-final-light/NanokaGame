using System;
using UnityEngine;

namespace NanokaGame.UI
{
    [DisallowMultipleComponent]
    public sealed class SafeAreaOffset : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private bool _applyLeftInset;
        [SerializeField] private bool _applyRightInset = true;
        [SerializeField] private bool _applyTopInset;
        [SerializeField] private bool _applyBottomInset = true;
        [SerializeField] private Vector2 _additionalPadding;

        private Vector2 _baseAnchoredPosition;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private float _lastScaleFactor = -1f;
        private bool _hasBasePosition;

        public Vector2 BaseAnchoredPosition
        {
            get { return _baseAnchoredPosition; }
        }

        public Vector2 AppliedOffset
        {
            get
            {
                return _target != null
                    ? _target.anchoredPosition - _baseAnchoredPosition
                    : Vector2.zero;
            }
        }

        public void Configure(
            RectTransform target,
            bool applyLeftInset,
            bool applyRightInset,
            bool applyTopInset,
            bool applyBottomInset,
            Vector2 additionalPadding)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            RestoreBasePosition();
            _target = target;
            _applyLeftInset = applyLeftInset;
            _applyRightInset = applyRightInset;
            _applyTopInset = applyTopInset;
            _applyBottomInset = applyBottomInset;
            _additionalPadding = additionalPadding;
            _hasBasePosition = false;
            CaptureBasePosition();
            InvalidateCachedScreenState();

            if (isActiveAndEnabled)
            {
                ApplyCurrentSafeArea();
            }
        }

        public void ApplySafeArea(
            Rect safeArea,
            Vector2 screenSize,
            float canvasScaleFactor)
        {
            if (_target == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(SafeAreaOffset)} requires a target RectTransform.");
            }

            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(screenSize),
                    "Screen dimensions must be greater than zero.");
            }

            if (canvasScaleFactor <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(canvasScaleFactor),
                    "Canvas scale factor must be greater than zero.");
            }

            CaptureBasePosition();

            float leftInset = Mathf.Max(0f, safeArea.xMin);
            float rightInset = Mathf.Max(0f, screenSize.x - safeArea.xMax);
            float bottomInset = Mathf.Max(0f, safeArea.yMin);
            float topInset = Mathf.Max(0f, screenSize.y - safeArea.yMax);

            Vector2 pixelOffset = new Vector2(
                (_applyLeftInset ? leftInset : 0f) -
                (_applyRightInset ? rightInset : 0f),
                (_applyBottomInset ? bottomInset : 0f) -
                (_applyTopInset ? topInset : 0f));

            _target.anchoredPosition =
                _baseAnchoredPosition +
                pixelOffset / canvasScaleFactor +
                _additionalPadding;
        }

        private void Awake()
        {
            RefreshTarget();
            CaptureBasePosition();
        }

        private void OnEnable()
        {
            RefreshTarget();
            CaptureBasePosition();
            ApplyCurrentSafeArea();
        }

        private void Update()
        {
            ApplyCurrentSafeArea();
        }

        private void OnDisable()
        {
            RestoreBasePosition();
            InvalidateCachedScreenState();
        }

        private void Reset()
        {
            RefreshTarget();
        }

        private void OnValidate()
        {
            RefreshTarget();
            InvalidateCachedScreenState();
        }

        private void ApplyCurrentSafeArea()
        {
            if (_target == null)
            {
                return;
            }

            Canvas canvas = _target.GetComponentInParent<Canvas>();
            float scaleFactor = canvas != null
                ? Mathf.Max(0.0001f, canvas.rootCanvas.scaleFactor)
                : 1f;
            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

            if (_lastSafeArea == safeArea &&
                _lastScreenSize == screenSize &&
                Mathf.Approximately(_lastScaleFactor, scaleFactor))
            {
                return;
            }

            ApplySafeArea(safeArea, screenSize, scaleFactor);
            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;
            _lastScaleFactor = scaleFactor;
        }

        private void RefreshTarget()
        {
            if (_target == null)
            {
                _target = transform as RectTransform;
            }
        }

        private void CaptureBasePosition()
        {
            if (_target == null || _hasBasePosition)
            {
                return;
            }

            _baseAnchoredPosition = _target.anchoredPosition;
            _hasBasePosition = true;
        }

        private void RestoreBasePosition()
        {
            if (_target != null && _hasBasePosition)
            {
                _target.anchoredPosition = _baseAnchoredPosition;
            }
        }

        private void InvalidateCachedScreenState()
        {
            _lastSafeArea = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
            _lastScreenSize = new Vector2Int(-1, -1);
            _lastScaleFactor = -1f;
        }
    }
}
