using System;
using NanokaGame.Games.Jigsaw;
using NUnit.Framework;
using UnityEngine;

namespace NanokaGame.Tests.EditMode.Jigsaw
{
    public sealed class JigsawSpriteSlicerTests
    {
        [TestCase(4, 2, 8)]
        [TestCase(6, 3, 18)]
        [TestCase(8, 4, 32)]
        public void CalculateSliceRects_WhenUsingSupportedDifficulty_CreatesExpectedPieceCount(
            int columns,
            int rows,
            int expectedPieceCount)
        {
            RectInt[] sliceRects = JigsawSpriteSlicer.CalculateSliceRects(
                new RectInt(0, 0, 4096, 1816),
                columns,
                rows);

            Assert.That(sliceRects.Length, Is.EqualTo(expectedPieceCount));
        }

        [TestCase(4, 2)]
        [TestCase(6, 3)]
        [TestCase(8, 4)]
        public void CalculateSliceRects_WhenTextureSizeIsNotDivisible_CoversSourceExactly(
            int columns,
            int rows)
        {
            RectInt sourceRect = new RectInt(17, 23, 4096, 1816);

            RectInt[] sliceRects = JigsawSpriteSlicer.CalculateSliceRects(sourceRect, columns, rows);

            int totalArea = 0;
            for (int pieceId = 0; pieceId < sliceRects.Length; pieceId++)
            {
                RectInt sliceRect = sliceRects[pieceId];
                Assert.That(sliceRect.width, Is.GreaterThan(0));
                Assert.That(sliceRect.height, Is.GreaterThan(0));
                Assert.That(sliceRect.xMin, Is.GreaterThanOrEqualTo(sourceRect.xMin));
                Assert.That(sliceRect.xMax, Is.LessThanOrEqualTo(sourceRect.xMax));
                Assert.That(sliceRect.yMin, Is.GreaterThanOrEqualTo(sourceRect.yMin));
                Assert.That(sliceRect.yMax, Is.LessThanOrEqualTo(sourceRect.yMax));
                totalArea += sliceRect.width * sliceRect.height;
            }

            Assert.That(totalArea, Is.EqualTo(sourceRect.width * sourceRect.height));
            AssertRowsAndColumnsAreContiguous(sliceRects, sourceRect, columns, rows);
        }

        [Test]
        public void CalculateSliceRects_WhenBoardRowsStartAtTop_MapsFirstRowToTextureTop()
        {
            RectInt sourceRect = new RectInt(0, 0, 120, 60);

            RectInt[] sliceRects = JigsawSpriteSlicer.CalculateSliceRects(sourceRect, 4, 2);

            Assert.That(sliceRects[0], Is.EqualTo(new RectInt(0, 30, 30, 30)));
            Assert.That(sliceRects[3], Is.EqualTo(new RectInt(90, 30, 30, 30)));
            Assert.That(sliceRects[4], Is.EqualTo(new RectInt(0, 0, 30, 30)));
            Assert.That(sliceRects[7], Is.EqualTo(new RectInt(90, 0, 30, 30)));
        }

        [Test]
        public void CalculateSliceRects_WhenSourceRectHasOffset_PreservesOffset()
        {
            RectInt sourceRect = new RectInt(11, 19, 101, 51);

            RectInt[] sliceRects = JigsawSpriteSlicer.CalculateSliceRects(sourceRect, 3, 2);

            Assert.That(sliceRects[0].xMin, Is.EqualTo(sourceRect.xMin));
            Assert.That(sliceRects[2].xMax, Is.EqualTo(sourceRect.xMax));
            Assert.That(sliceRects[0].yMax, Is.EqualTo(sourceRect.yMax));
            Assert.That(sliceRects[3].yMin, Is.EqualTo(sourceRect.yMin));
        }

        [TestCase(0, 2)]
        [TestCase(-1, 2)]
        [TestCase(4, 0)]
        [TestCase(4, -1)]
        public void CalculateSliceRects_WhenGridDimensionIsNotPositive_ThrowsArgumentOutOfRangeException(
            int columns,
            int rows)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => JigsawSpriteSlicer.CalculateSliceRects(new RectInt(0, 0, 64, 64), columns, rows));
        }

        [TestCase(0, 64)]
        [TestCase(64, 0)]
        [TestCase(-1, 64)]
        [TestCase(64, -1)]
        public void CalculateSliceRects_WhenSourceDimensionIsNotPositive_ThrowsArgumentOutOfRangeException(
            int width,
            int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => JigsawSpriteSlicer.CalculateSliceRects(new RectInt(0, 0, width, height), 4, 2));
        }

        [TestCase(65, 2)]
        [TestCase(4, 65)]
        public void CalculateSliceRects_WhenGridExceedsPixelDimensions_ThrowsArgumentOutOfRangeException(
            int columns,
            int rows)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => JigsawSpriteSlicer.CalculateSliceRects(new RectInt(0, 0, 64, 64), columns, rows));
        }

        [Test]
        public void CreateSprites_WhenTextureIsNotReadable_CreatesSpritesWithoutPixelAccess()
        {
            Texture2D texture = new Texture2D(64, 32, TextureFormat.RGBA32, false);
            Sprite sourceSprite = null;
            Sprite[] pieceSprites = null;

            try
            {
                sourceSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100,
                    0,
                    SpriteMeshType.FullRect,
                    Vector4.zero,
                    false);
                sourceSprite.name = "source";
                texture.Apply(false, true);

                pieceSprites = JigsawSpriteSlicer.CreateSprites(sourceSprite, 4, 2);

                Assert.That(texture.isReadable, Is.False);
                Assert.That(pieceSprites.Length, Is.EqualTo(8));
                Assert.That(pieceSprites[0].rect, Is.EqualTo(new Rect(0, 16, 16, 16)));
                Assert.That(pieceSprites[7].rect, Is.EqualTo(new Rect(48, 0, 16, 16)));
                Assert.That(pieceSprites[0].pixelsPerUnit, Is.EqualTo(100));
            }
            finally
            {
                if (pieceSprites != null)
                {
                    for (int pieceId = 0; pieceId < pieceSprites.Length; pieceId++)
                    {
                        UnityEngine.Object.DestroyImmediate(pieceSprites[pieceId]);
                    }
                }

                UnityEngine.Object.DestroyImmediate(sourceSprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void CreateSprites_WhenSourceSpriteIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => JigsawSpriteSlicer.CreateSprites(null, 4, 2));
        }

        private static void AssertRowsAndColumnsAreContiguous(
            RectInt[] sliceRects,
            RectInt sourceRect,
            int columns,
            int rows)
        {
            for (int boardRow = 0; boardRow < rows; boardRow++)
            {
                int rowStartIndex = boardRow * columns;
                Assert.That(sliceRects[rowStartIndex].xMin, Is.EqualTo(sourceRect.xMin));
                Assert.That(sliceRects[rowStartIndex + columns - 1].xMax, Is.EqualTo(sourceRect.xMax));

                for (int column = 0; column < columns - 1; column++)
                {
                    RectInt leftRect = sliceRects[rowStartIndex + column];
                    RectInt rightRect = sliceRects[rowStartIndex + column + 1];
                    Assert.That(leftRect.xMax, Is.EqualTo(rightRect.xMin));
                    Assert.That(leftRect.yMin, Is.EqualTo(rightRect.yMin));
                    Assert.That(leftRect.yMax, Is.EqualTo(rightRect.yMax));
                }
            }

            Assert.That(sliceRects[0].yMax, Is.EqualTo(sourceRect.yMax));
            Assert.That(sliceRects[(rows - 1) * columns].yMin, Is.EqualTo(sourceRect.yMin));

            for (int boardRow = 0; boardRow < rows - 1; boardRow++)
            {
                for (int column = 0; column < columns; column++)
                {
                    RectInt upperRect = sliceRects[boardRow * columns + column];
                    RectInt lowerRect = sliceRects[(boardRow + 1) * columns + column];
                    Assert.That(lowerRect.yMax, Is.EqualTo(upperRect.yMin));
                    Assert.That(lowerRect.xMin, Is.EqualTo(upperRect.xMin));
                    Assert.That(lowerRect.xMax, Is.EqualTo(upperRect.xMax));
                }
            }
        }
    }
}
