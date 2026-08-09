using System;
using UnityEngine;

namespace NanokaGame.Games.Klotski
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(KlotskiBoardView))]
    public sealed class KlotskiGameController : MonoBehaviour
    {
        [SerializeField] private KlotskiBoardView _boardView;
        [SerializeField] private SpriteRenderer _topLeftAnchorRenderer;
        [SerializeField] private float _cellSize = 1.68f;
        [SerializeField] private float _pieceGap;
        [SerializeField] private float _boardPlaneZ;
        [SerializeField] private bool _initializeOnAwake = true;

        private KlotskiBoardModel _model;
        private KlotskiBoardLayout _layout;

        public bool IsInitialized
        {
            get { return _model != null && _layout != null && _boardView != null && _boardView.IsInitialized; }
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

        public void InitializeGame()
        {
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
        }

        public void ResetGame()
        {
            EnsureInitialized();
            _model.Reset();
            _boardView.SyncAllViews();
        }

        public void SyncAllViews()
        {
            EnsureInitialized();
            _boardView.SyncAllViews();
        }

        private void Awake()
        {
            if (_initializeOnAwake)
            {
                InitializeGame();
            }
        }

        private void Reset()
        {
            RefreshReferences();
        }

        private void OnValidate()
        {
            RefreshReferences();
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
    }
}
