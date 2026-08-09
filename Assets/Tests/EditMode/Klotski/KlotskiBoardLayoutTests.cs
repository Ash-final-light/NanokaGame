using System;
using NanokaGame.Games.Klotski;
using NUnit.Framework;
using UnityEngine;

namespace NanokaGame.Tests.EditMode.Klotski
{
    public sealed class KlotskiBoardLayoutTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void CreateFromAnchorBounds_WhenUsingLeftArmBounds_DerivesConfirmedBoardGeometry()
        {
            Bounds leftArmBounds = new Bounds(
                new Vector3(-2.46f, 2.64f, 0f),
                new Vector3(1.68f, 3.36f, 0f));

            KlotskiBoardLayout layout = KlotskiBoardLayout.CreateFromAnchorBounds(
                leftArmBounds,
                4,
                5,
                1.68f,
                0f,
                0f);

            AssertVector2(layout.BoardTopLeft, new Vector2(-3.30f, 4.32f));
            AssertVector2(layout.BoardBottomLeft, new Vector2(-3.30f, -4.08f));
            AssertVector2(layout.BoardCenter, new Vector2(0.06f, 0.12f));
            AssertVector2(layout.BoardWorldSize, new Vector2(6.72f, 8.40f));
        }

        [Test]
        public void GetPieceWorldPosition_WhenPieceIsOneByOneAtOrigin_ReturnsCellCenter()
        {
            KlotskiBoardLayout layout = CreateCurrentLayout();

            Vector3 position = layout.GetPieceWorldPosition(Vector2Int.zero, Vector2Int.one);

            AssertVector3(position, new Vector3(-2.46f, -3.24f, 0f));
        }

        [TestCase(2, 1, -1.62f, -3.24f)]
        [TestCase(1, 2, -2.46f, -2.40f)]
        [TestCase(2, 2, -1.62f, -2.40f)]
        public void GetPieceWorldPosition_WhenPieceHasMultipleCells_UsesCellAreaCenter(
            int width,
            int height,
            float expectedX,
            float expectedY)
        {
            KlotskiBoardLayout layout = CreateCurrentLayout();

            Vector3 position = layout.GetPieceWorldPosition(
                Vector2Int.zero,
                new Vector2Int(width, height));

            AssertVector3(position, new Vector3(expectedX, expectedY, 0f));
        }

        [Test]
        public void GetPieceWorldPosition_WhenTargetIsAtGoal_ReturnsConfirmedTargetWorldPosition()
        {
            KlotskiBoardLayout layout = CreateCurrentLayout();

            Vector3 position = layout.GetPieceWorldPosition(
                new Vector2Int(1, 0),
                new Vector2Int(2, 2));

            AssertVector3(position, new Vector3(0.06f, -2.40f, 0f));
        }

        [Test]
        public void GetPieceWorldPosition_WhenPieceOccupiesTopRightCell_RemainsInsideBoardBounds()
        {
            KlotskiBoardLayout layout = CreateCurrentLayout();
            Vector3 position = layout.GetPieceWorldPosition(new Vector2Int(3, 4), Vector2Int.one);
            Vector2 pieceSize = layout.GetPieceWorldSize(Vector2Int.one);
            Bounds pieceBounds = new Bounds(position, new Vector3(pieceSize.x, pieceSize.y, 0f));
            Bounds boardBounds = layout.GetBoardWorldBounds();

            Assert.That(pieceBounds.max.x, Is.LessThanOrEqualTo(boardBounds.max.x + Tolerance));
            Assert.That(pieceBounds.max.y, Is.LessThanOrEqualTo(boardBounds.max.y + Tolerance));
            Assert.That(pieceBounds.min.x, Is.GreaterThanOrEqualTo(boardBounds.min.x - Tolerance));
            Assert.That(pieceBounds.min.y, Is.GreaterThanOrEqualTo(boardBounds.min.y - Tolerance));
        }

        [Test]
        public void CreateFromCenter_WhenBoardCenterMoves_PreservesRelativePieceLayout()
        {
            KlotskiBoardLayout original = KlotskiBoardLayout.CreateFromCenter(
                new Vector2(0.06f, 0.12f),
                4,
                5,
                1.68f,
                0f,
                0f);
            Vector2 centerOffset = new Vector2(7.25f, -3.5f);
            KlotskiBoardLayout moved = KlotskiBoardLayout.CreateFromCenter(
                original.BoardCenter + centerOffset,
                4,
                5,
                1.68f,
                0f,
                0f);

            Vector3 originalPosition = original.GetPieceWorldPosition(
                new Vector2Int(1, 3),
                new Vector2Int(2, 2));
            Vector3 movedPosition = moved.GetPieceWorldPosition(
                new Vector2Int(1, 3),
                new Vector2Int(2, 2));

            AssertVector3(movedPosition - originalPosition, new Vector3(centerOffset.x, centerOffset.y, 0f));
        }

        [Test]
        public void CreateFromCenter_WhenCellSizeDoubles_PositionsAndSizesScaleFromBoardCenter()
        {
            KlotskiBoardLayout original = KlotskiBoardLayout.CreateFromCenter(
                Vector2.zero,
                4,
                5,
                1f,
                0f,
                0f);
            KlotskiBoardLayout doubled = KlotskiBoardLayout.CreateFromCenter(
                Vector2.zero,
                4,
                5,
                2f,
                0f,
                0f);

            Vector3 originalPosition = original.GetPieceWorldPosition(Vector2Int.zero, Vector2Int.one);
            Vector3 doubledPosition = doubled.GetPieceWorldPosition(Vector2Int.zero, Vector2Int.one);
            Vector2 originalSize = original.GetPieceWorldSize(new Vector2Int(2, 1));
            Vector2 doubledSize = doubled.GetPieceWorldSize(new Vector2Int(2, 1));

            AssertVector3(doubledPosition, originalPosition * 2f);
            AssertVector2(doubledSize, originalSize * 2f);
        }

        [Test]
        public void GetPieceWorldSize_WhenPieceGapChanges_ChangesSizeWithoutChangingPosition()
        {
            KlotskiBoardLayout withoutGap = KlotskiBoardLayout.CreateFromTopLeft(
                new Vector2(-3.30f, 4.32f),
                4,
                5,
                1.68f,
                0f,
                0f);
            KlotskiBoardLayout withGap = KlotskiBoardLayout.CreateFromTopLeft(
                new Vector2(-3.30f, 4.32f),
                4,
                5,
                1.68f,
                0.12f,
                0f);
            Vector2Int cell = new Vector2Int(1, 2);
            Vector2Int size = new Vector2Int(2, 1);

            Vector3 positionWithoutGap = withoutGap.GetPieceWorldPosition(cell, size);
            Vector3 positionWithGap = withGap.GetPieceWorldPosition(cell, size);
            Vector2 sizeWithoutGap = withoutGap.GetPieceWorldSize(size);
            Vector2 sizeWithGap = withGap.GetPieceWorldSize(size);

            AssertVector3(positionWithGap, positionWithoutGap);
            AssertVector2(sizeWithoutGap, new Vector2(3.36f, 1.68f));
            AssertVector2(sizeWithGap, new Vector2(3.24f, 1.56f));
        }

        [TestCase(0, 0, 1, 1)]
        [TestCase(1, 2, 2, 1)]
        [TestCase(0, 3, 1, 2)]
        [TestCase(1, 3, 2, 2)]
        public void GetNearestCell_WhenGivenExactPieceCenter_RoundTripsToLogicalCell(
            int column,
            int row,
            int width,
            int height)
        {
            KlotskiBoardLayout layout = CreateCurrentLayout();
            Vector2Int expectedCell = new Vector2Int(column, row);
            Vector2Int size = new Vector2Int(width, height);
            Vector3 worldPosition = layout.GetPieceWorldPosition(expectedCell, size);

            Vector2Int actualCell = layout.GetNearestCell(worldPosition, size);

            Assert.That(actualCell, Is.EqualTo(expectedCell));
        }

        [Test]
        public void GetPieceWorldPosition_WhenPieceDoesNotFit_ThrowsArgumentOutOfRangeException()
        {
            KlotskiBoardLayout layout = CreateCurrentLayout();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => layout.GetPieceWorldPosition(new Vector2Int(3, 4), new Vector2Int(2, 2)));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void CreateFromTopLeft_WhenCellSizeIsNotPositive_ThrowsArgumentOutOfRangeException(float cellSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => KlotskiBoardLayout.CreateFromTopLeft(Vector2.zero, 4, 5, cellSize, 0f, 0f));
        }

        [TestCase(-0.01f)]
        [TestCase(1.68f)]
        [TestCase(2f)]
        public void CreateFromTopLeft_WhenPieceGapIsOutsideRange_ThrowsArgumentOutOfRangeException(float pieceGap)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => KlotskiBoardLayout.CreateFromTopLeft(Vector2.zero, 4, 5, 1.68f, pieceGap, 0f));
        }

        private static KlotskiBoardLayout CreateCurrentLayout()
        {
            return KlotskiBoardLayout.CreateFromTopLeft(
                new Vector2(-3.30f, 4.32f),
                4,
                5,
                1.68f,
                0f,
                0f);
        }

        private static void AssertVector2(Vector2 actual, Vector2 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance), "X");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance), "Y");
        }

        private static void AssertVector3(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance), "X");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance), "Y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tolerance), "Z");
        }
    }
}
