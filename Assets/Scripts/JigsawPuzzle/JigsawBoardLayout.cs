using System;
using UnityEngine;

namespace NanokaGame.Games.Jigsaw
{
    public sealed class JigsawBoardLayout
    {
        private readonly Vector3[] _slotWorldPositions;

        private JigsawBoardLayout(
            RectInt sourceRect,
            Vector2 boardSize,
            float pieceWorldScale,
            Vector3[] slotWorldPositions)
        {
            SourceRect = sourceRect;
            BoardSize = boardSize;
            PieceWorldScale = pieceWorldScale;
            _slotWorldPositions = slotWorldPositions;
        }

        public RectInt SourceRect { get; }

        public Vector2 BoardSize { get; }

        public float PieceWorldScale { get; }

        public int SlotCount
        {
            get { return _slotWorldPositions.Length; }
        }

        public Vector3 GetSlotWorldPosition(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotWorldPositions.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            return _slotWorldPositions[slotIndex];
        }

        public static JigsawBoardLayout Create(
            Sprite sourceSprite,
            RectInt[] sliceRects,
            Vector3 boardCenter,
            Vector2 maximumBoardSize,
            float boardPlaneZ)
        {
            if (sourceSprite == null)
            {
                throw new ArgumentNullException(nameof(sourceSprite));
            }

            if (sliceRects == null)
            {
                throw new ArgumentNullException(nameof(sliceRects));
            }

            if (sliceRects.Length == 0)
            {
                throw new ArgumentException("At least one slice is required.", nameof(sliceRects));
            }

            if (maximumBoardSize.x <= 0f || maximumBoardSize.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumBoardSize),
                    maximumBoardSize,
                    "Maximum board dimensions must be greater than zero.");
            }

            Rect sourceSpriteRect = sourceSprite.rect;
            int sourceXMin = Mathf.RoundToInt(sourceSpriteRect.xMin);
            int sourceYMin = Mathf.RoundToInt(sourceSpriteRect.yMin);
            int sourceXMax = Mathf.RoundToInt(sourceSpriteRect.xMax);
            int sourceYMax = Mathf.RoundToInt(sourceSpriteRect.yMax);
            RectInt sourceRect = new RectInt(
                sourceXMin,
                sourceYMin,
                sourceXMax - sourceXMin,
                sourceYMax - sourceYMin);
            Vector2 boardSize = CalculateBoardSize(sourceRect, maximumBoardSize);
            float pieceWorldScale = boardSize.x / (sourceRect.width / sourceSprite.pixelsPerUnit);
            Vector3[] slotWorldPositions = new Vector3[sliceRects.Length];
            float boardLeft = boardCenter.x - boardSize.x * 0.5f;
            float boardBottom = boardCenter.y - boardSize.y * 0.5f;

            for (int pieceId = 0; pieceId < sliceRects.Length; pieceId++)
            {
                RectInt sliceRect = sliceRects[pieceId];
                float normalizedCenterX = (sliceRect.center.x - sourceRect.xMin) / sourceRect.width;
                float normalizedCenterY = (sliceRect.center.y - sourceRect.yMin) / sourceRect.height;
                slotWorldPositions[pieceId] = new Vector3(
                    boardLeft + normalizedCenterX * boardSize.x,
                    boardBottom + normalizedCenterY * boardSize.y,
                    boardPlaneZ);
            }

            return new JigsawBoardLayout(sourceRect, boardSize, pieceWorldScale, slotWorldPositions);
        }

        public static Vector2 CalculateBoardSize(RectInt sourceRect, Vector2 maximumBoardSize)
        {
            if (sourceRect.width <= 0 || sourceRect.height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceRect));
            }

            if (maximumBoardSize.x <= 0f || maximumBoardSize.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumBoardSize));
            }

            float sourceAspect = sourceRect.width / (float)sourceRect.height;
            float maximumAspect = maximumBoardSize.x / maximumBoardSize.y;

            return sourceAspect >= maximumAspect
                ? new Vector2(maximumBoardSize.x, maximumBoardSize.x / sourceAspect)
                : new Vector2(maximumBoardSize.y * sourceAspect, maximumBoardSize.y);
        }
    }
}
