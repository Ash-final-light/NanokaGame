using System;
using System.Collections.Generic;
using UnityEngine;

namespace NanokaGame.Games.Klotski
{
    [DisallowMultipleComponent]
    public sealed class KlotskiBoardView : MonoBehaviour
    {
        [SerializeField] private List<KlotskiPieceView> _pieceViews = new List<KlotskiPieceView>();
        [SerializeField] private string _normalSortingLayerName = "Middle";
        [SerializeField] private int _normalSortingOrder;

        private readonly Dictionary<string, KlotskiPieceView> _viewsById =
            new Dictionary<string, KlotskiPieceView>(StringComparer.Ordinal);

        private KlotskiBoardModel _model;
        private KlotskiBoardLayout _layout;

        public bool IsInitialized
        {
            get { return _model != null && _layout != null; }
        }

        public KlotskiBoardModel Model
        {
            get { return _model; }
        }

        public KlotskiBoardLayout Layout
        {
            get { return _layout; }
        }

        public IReadOnlyList<KlotskiPieceView> PieceViews
        {
            get { return _pieceViews; }
        }

        public void Configure(
            IReadOnlyList<KlotskiPieceView> pieceViews,
            string normalSortingLayerName,
            int normalSortingOrder)
        {
            if (pieceViews == null)
            {
                throw new ArgumentNullException(nameof(pieceViews));
            }

            if (string.IsNullOrWhiteSpace(normalSortingLayerName))
            {
                throw new ArgumentException(
                    "Normal sorting layer name cannot be null, empty, or whitespace.",
                    nameof(normalSortingLayerName));
            }

            _pieceViews.Clear();
            for (int viewIndex = 0; viewIndex < pieceViews.Count; viewIndex++)
            {
                _pieceViews.Add(pieceViews[viewIndex]);
            }

            _normalSortingLayerName = normalSortingLayerName;
            _normalSortingOrder = normalSortingOrder;
        }

        public void Initialize(KlotskiBoardModel model, KlotskiBoardLayout layout)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (model.Columns != layout.Columns || model.Rows != layout.Rows)
            {
                throw new ArgumentException(
                    string.Format(
                        "Model size {0}x{1} does not match layout size {2}x{3}.",
                        model.Columns,
                        model.Rows,
                        layout.Columns,
                        layout.Rows),
                    nameof(layout));
            }

            BuildViewMap(model);
            _model = model;
            _layout = layout;
            SyncAllViews();
        }

        public bool TryGetPieceView(string pieceId, out KlotskiPieceView pieceView)
        {
            if (pieceId == null)
            {
                pieceView = null;
                return false;
            }

            return _viewsById.TryGetValue(pieceId, out pieceView);
        }

        public void SyncAllViews()
        {
            EnsureInitialized();

            foreach (KlotskiPieceState piece in _model.Pieces)
            {
                KlotskiPieceView view = _viewsById[piece.Id];
                ApplyNormalRenderingState(view);
                view.ApplyLayout(piece, _layout);
            }
        }

        public void SyncPiece(string pieceId)
        {
            EnsureInitialized();

            KlotskiPieceState piece;
            if (!_model.TryGetPiece(pieceId, out piece))
            {
                throw new ArgumentException(
                    string.Format("Model piece '{0}' was not found.", pieceId),
                    nameof(pieceId));
            }

            KlotskiPieceView view = _viewsById[pieceId];
            view.ApplyLayout(piece, _layout);
        }

        public void RestoreAllVisualStates()
        {
            EnsureInitialized();

            for (int viewIndex = 0; viewIndex < _pieceViews.Count; viewIndex++)
            {
                _pieceViews[viewIndex].RestoreVisualState();
            }
        }

        private void BuildViewMap(KlotskiBoardModel model)
        {
            if (_pieceViews == null || _pieceViews.Count == 0)
            {
                throw new InvalidOperationException("KlotskiBoardView does not contain any piece views.");
            }

            _viewsById.Clear();

            for (int viewIndex = 0; viewIndex < _pieceViews.Count; viewIndex++)
            {
                KlotskiPieceView view = _pieceViews[viewIndex];
                if (view == null)
                {
                    throw new InvalidOperationException(
                        string.Format("Piece view at index {0} is null.", viewIndex));
                }

                if (!view.IsConfigured)
                {
                    throw new InvalidOperationException(
                        string.Format("Piece view '{0}' is not configured.", view.gameObject.name));
                }

                if (_viewsById.ContainsKey(view.PieceId))
                {
                    throw new InvalidOperationException(
                        string.Format("Piece view ID '{0}' is duplicated.", view.PieceId));
                }

                KlotskiPieceState modelPiece;
                if (!model.TryGetPiece(view.PieceId, out modelPiece))
                {
                    throw new InvalidOperationException(
                        string.Format("Piece view ID '{0}' does not exist in the Model.", view.PieceId));
                }

                _viewsById.Add(view.PieceId, view);
            }

            foreach (KlotskiPieceState piece in model.Pieces)
            {
                if (!_viewsById.ContainsKey(piece.Id))
                {
                    throw new InvalidOperationException(
                        string.Format("Model piece '{0}' does not have a matching View.", piece.Id));
                }
            }
        }

        private void ApplyNormalRenderingState(KlotskiPieceView view)
        {
            SpriteRenderer renderer = view.SpriteRenderer;
            renderer.sortingLayerName = _normalSortingLayerName;
            renderer.sortingOrder = _normalSortingOrder;
            view.CaptureBaseVisualState();
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("KlotskiBoardView has not been initialized.");
            }
        }

        private void Reset()
        {
            RefreshPieceBindings();
        }

        private void OnValidate()
        {
            if (_pieceViews == null)
            {
                _pieceViews = new List<KlotskiPieceView>();
            }

            if (_pieceViews.Count == 0)
            {
                RefreshPieceBindings();
            }

            if (string.IsNullOrWhiteSpace(_normalSortingLayerName))
            {
                _normalSortingLayerName = "Middle";
            }
        }

        private void RefreshPieceBindings()
        {
            KlotskiPieceView[] childViews = GetComponentsInChildren<KlotskiPieceView>(true);
            Array.Sort(
                childViews,
                delegate(KlotskiPieceView left, KlotskiPieceView right)
                {
                    return string.CompareOrdinal(left.gameObject.name, right.gameObject.name);
                });

            _pieceViews.Clear();
            _pieceViews.AddRange(childViews);
        }
    }
}
