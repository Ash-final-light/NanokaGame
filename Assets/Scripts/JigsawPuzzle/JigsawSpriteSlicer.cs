using System;
using UnityEngine;

namespace NanokaGame.Games.Jigsaw
{
    public static class JigsawSpriteSlicer
    {
        public static RectInt[] CalculateSliceRects(RectInt sourceRect, int columns, int rows)
        {
            ValidateSourceRect(sourceRect);
            ValidateGrid(sourceRect, columns, rows);

            int pieceCount = checked(columns * rows);
            RectInt[] sliceRects = new RectInt[pieceCount];

            for (int boardRow = 0; boardRow < rows; boardRow++)
            {
                int textureRow = rows - 1 - boardRow;
                int bottom = sourceRect.yMin + CalculateBoundary(textureRow, sourceRect.height, rows);
                int top = sourceRect.yMin + CalculateBoundary(textureRow + 1, sourceRect.height, rows);

                for (int column = 0; column < columns; column++)
                {
                    int left = sourceRect.xMin + CalculateBoundary(column, sourceRect.width, columns);
                    int right = sourceRect.xMin + CalculateBoundary(column + 1, sourceRect.width, columns);
                    int pieceId = boardRow * columns + column;

                    sliceRects[pieceId] = new RectInt(left, bottom, right - left, top - bottom);
                }
            }

            return sliceRects;
        }

        public static Sprite[] CreateSprites(Sprite sourceSprite, int columns, int rows)
        {
            if (sourceSprite == null)
            {
                throw new ArgumentNullException(nameof(sourceSprite));
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
            RectInt[] sliceRects = CalculateSliceRects(sourceRect, columns, rows);
            Sprite[] sprites = new Sprite[sliceRects.Length];

            for (int pieceId = 0; pieceId < sliceRects.Length; pieceId++)
            {
                RectInt sliceRect = sliceRects[pieceId];
                Sprite sprite = Sprite.Create(
                    sourceSprite.texture,
                    new Rect(sliceRect.x, sliceRect.y, sliceRect.width, sliceRect.height),
                    new Vector2(0.5f, 0.5f),
                    sourceSprite.pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect,
                    Vector4.zero,
                    false);

                sprite.name = string.Format("{0}_piece_{1}", sourceSprite.name, pieceId);
                sprites[pieceId] = sprite;
            }

            return sprites;
        }

        private static int CalculateBoundary(int index, int length, int segmentCount)
        {
            return Mathf.RoundToInt(index * (float)length / segmentCount);
        }

        private static void ValidateSourceRect(RectInt sourceRect)
        {
            if (sourceRect.width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceRect),
                    sourceRect,
                    "Source width must be greater than zero.");
            }

            if (sourceRect.height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceRect),
                    sourceRect,
                    "Source height must be greater than zero.");
            }
        }

        private static void ValidateGrid(RectInt sourceRect, int columns, int rows)
        {
            if (columns <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(columns),
                    columns,
                    "Column count must be greater than zero.");
            }

            if (rows <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rows),
                    rows,
                    "Row count must be greater than zero.");
            }

            if (columns > sourceRect.width)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(columns),
                    columns,
                    "Column count cannot exceed the source pixel width.");
            }

            if (rows > sourceRect.height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rows),
                    rows,
                    "Row count cannot exceed the source pixel height.");
            }
        }
    }
}
