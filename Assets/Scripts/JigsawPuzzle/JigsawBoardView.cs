using System;
using UnityEngine;

namespace NanokaGame.Games.Jigsaw
{
    [DisallowMultipleComponent]
    public sealed class JigsawBoardView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sourceRenderer;
        [SerializeField] private Transform _piecesRoot;
        [SerializeField] private JigsawPieceView _piecePrefab;
        [SerializeField] private Vector2 _maximumBoardSize = new Vector2(8.6f, 4.2f);
        [SerializeField] private float _boardPlaneZ;
        [SerializeField] private int _pieceSortingOrder;

        private Sprite[] _pieceSprites;
        private JigsawPieceView[] _pieceViews;
        private JigsawBoardLayout _layout;

        public float BoardPlaneZ
        {
            get { return _boardPlaneZ; }
        }

        public int PieceCount
        {
            get { return _pieceViews == null ? 0 : _pieceViews.Length; }
        }

        public bool HasRequiredReferences
        {
            get
            {
                return _sourceRenderer != null &&
                       _sourceRenderer.sprite != null &&
                       _piecesRoot != null &&
                       _piecePrefab != null;
            }
        }

        public void Configure(
            SpriteRenderer sourceRenderer,
            Transform piecesRoot,
            JigsawPieceView piecePrefab,
            Vector2 maximumBoardSize,
            float boardPlaneZ,
            int pieceSortingOrder)
        {
            if (sourceRenderer == null)
            {
                throw new ArgumentNullException(nameof(sourceRenderer));
            }

            if (piecesRoot == null)
            {
                throw new ArgumentNullException(nameof(piecesRoot));
            }

            if (piecePrefab == null)
            {
                throw new ArgumentNullException(nameof(piecePrefab));
            }

            if (maximumBoardSize.x <= 0f || maximumBoardSize.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumBoardSize));
            }

            _sourceRenderer = sourceRenderer;
            _piecesRoot = piecesRoot;
            _piecePrefab = piecePrefab;
            _maximumBoardSize = maximumBoardSize;
            _boardPlaneZ = boardPlaneZ;
            _pieceSortingOrder = pieceSortingOrder;
        }

        public void InitializePuzzle(
            JigsawPuzzleController controller,
            int columns,
            int rows)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            EnsureRequiredReferences();
            ClearPieces();

            Sprite sourceSprite = _sourceRenderer.sprite;
            Rect sourceSpriteRect = sourceSprite.rect;
            RectInt sourceRect = new RectInt(
                Mathf.RoundToInt(sourceSpriteRect.xMin),
                Mathf.RoundToInt(sourceSpriteRect.yMin),
                Mathf.RoundToInt(sourceSpriteRect.width),
                Mathf.RoundToInt(sourceSpriteRect.height));
            RectInt[] sliceRects = JigsawSpriteSlicer.CalculateSliceRects(sourceRect, columns, rows);
            _pieceSprites = JigsawSpriteSlicer.CreateSprites(sourceSprite, columns, rows);
            _layout = JigsawBoardLayout.Create(
                sourceSprite,
                sliceRects,
                _piecesRoot.position,
                _maximumBoardSize,
                _boardPlaneZ);
            _pieceViews = new JigsawPieceView[_pieceSprites.Length];

            Material sharedMaterial = _sourceRenderer.sharedMaterial;
            int sortingLayerId = _sourceRenderer.sortingLayerID;

            for (int pieceId = 0; pieceId < _pieceViews.Length; pieceId++)
            {
                JigsawPieceView pieceView = Instantiate(_piecePrefab, _piecesRoot);
                pieceView.name = string.Format("JigsawPiece_{0}", pieceId);
                pieceView.Configure(
                    controller,
                    pieceId,
                    _pieceSprites[pieceId],
                    sharedMaterial,
                    sortingLayerId,
                    _pieceSortingOrder,
                    Vector3.one * _layout.PieceWorldScale,
                    _layout.GetSlotWorldPosition(pieceId));
                pieceView.SetCurrentSlotIndex(pieceId);
                _pieceViews[pieceId] = pieceView;
            }

            SetSourceVisible(false);
        }

        public bool IsRegisteredPiece(JigsawPieceView pieceView)
        {
            return pieceView != null &&
                   pieceView.PieceId >= 0 &&
                   _pieceViews != null &&
                   pieceView.PieceId < _pieceViews.Length &&
                   _pieceViews[pieceView.PieceId] == pieceView;
        }

        public JigsawPieceView GetPieceView(int pieceId)
        {
            if (_pieceViews == null || pieceId < 0 || pieceId >= _pieceViews.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(pieceId));
            }

            return _pieceViews[pieceId];
        }

        public Vector3 GetSlotWorldPosition(int slotIndex)
        {
            if (_layout == null)
            {
                throw new InvalidOperationException("The board view is not initialized.");
            }

            return _layout.GetSlotWorldPosition(slotIndex);
        }

        public void SyncAll(JigsawBoardState boardState)
        {
            if (boardState == null)
            {
                throw new ArgumentNullException(nameof(boardState));
            }

            if (_pieceViews == null || _layout == null || boardState.PieceCount != _pieceViews.Length)
            {
                throw new InvalidOperationException("The board view does not match the board state.");
            }

            for (int pieceId = 0; pieceId < _pieceViews.Length; pieceId++)
            {
                JigsawPieceView pieceView = _pieceViews[pieceId];
                int slotIndex = boardState.GetSlotIndexForPiece(pieceId);
                pieceView.SetCurrentSlotIndex(slotIndex);
                pieceView.SetWorldPosition(_layout.GetSlotWorldPosition(slotIndex));
                pieceView.ResetPresentation();
            }
        }

        public void ShowSourcePreview()
        {
            if (_sourceRenderer == null || _sourceRenderer.sprite == null || _piecesRoot == null)
            {
                return;
            }

            Sprite sourceSprite = _sourceRenderer.sprite;
            Rect sourceSpriteRect = sourceSprite.rect;
            RectInt sourceRect = new RectInt(
                0,
                0,
                Mathf.RoundToInt(sourceSpriteRect.width),
                Mathf.RoundToInt(sourceSpriteRect.height));
            Vector2 boardSize = JigsawBoardLayout.CalculateBoardSize(sourceRect, _maximumBoardSize);
            float sourceWorldWidth = sourceSpriteRect.width / sourceSprite.pixelsPerUnit;
            float scale = boardSize.x / sourceWorldWidth;
            Transform sourceTransform = _sourceRenderer.transform;
            sourceTransform.position = new Vector3(_piecesRoot.position.x, _piecesRoot.position.y, _boardPlaneZ);
            sourceTransform.localScale = Vector3.one * scale;
            SetSourceVisible(true);
        }

        public void SetSourceVisible(bool isVisible)
        {
            if (_sourceRenderer != null)
            {
                _sourceRenderer.enabled = isVisible;
            }
        }

        public void ClearPieces()
        {
            if (_pieceViews != null)
            {
                for (int pieceId = 0; pieceId < _pieceViews.Length; pieceId++)
                {
                    JigsawPieceView pieceView = _pieceViews[pieceId];
                    if (pieceView == null)
                    {
                        continue;
                    }

                    pieceView.gameObject.SetActive(false);
                    DestroyUnityObject(pieceView.gameObject);
                }
            }

            if (_pieceSprites != null)
            {
                for (int pieceId = 0; pieceId < _pieceSprites.Length; pieceId++)
                {
                    DestroyUnityObject(_pieceSprites[pieceId]);
                }
            }

            _pieceViews = null;
            _pieceSprites = null;
            _layout = null;
        }

        private void OnDestroy()
        {
            ClearPieces();
        }

        private void OnValidate()
        {
            _maximumBoardSize.x = Mathf.Max(0.01f, _maximumBoardSize.x);
            _maximumBoardSize.y = Mathf.Max(0.01f, _maximumBoardSize.y);
        }

        private void EnsureRequiredReferences()
        {
            if (!HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    $"{nameof(JigsawBoardView)} on '{name}' requires a source SpriteRenderer, pieces root and piece prefab.");
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object unityObject)
        {
            if (unityObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(unityObject);
            }
            else
            {
                DestroyImmediate(unityObject);
            }
        }
    }
}
