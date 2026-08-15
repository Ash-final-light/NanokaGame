using System;
using System.Collections.Generic;
using NanokaGame.Games.Jigsaw;
using NUnit.Framework;

namespace NanokaGame.Tests.EditMode.Jigsaw
{
    public sealed class JigsawBoardStateTests
    {
        [TestCase(4, 2, 8)]
        [TestCase(6, 3, 18)]
        [TestCase(8, 4, 32)]
        public void Constructor_WhenUsingSupportedDifficulty_CreatesExpectedBoard(
            int columns,
            int rows,
            int expectedPieceCount)
        {
            JigsawBoardState board = new JigsawBoardState(columns, rows);

            Assert.That(board.Columns, Is.EqualTo(columns));
            Assert.That(board.Rows, Is.EqualTo(rows));
            Assert.That(board.PieceCount, Is.EqualTo(expectedPieceCount));
            Assert.That(board.IsCompleted, Is.True);

            for (int slotIndex = 0; slotIndex < board.PieceCount; slotIndex++)
            {
                Assert.That(board.GetPieceIdAtSlot(slotIndex), Is.EqualTo(slotIndex));
                Assert.That(board.GetSlotIndexForPiece(slotIndex), Is.EqualTo(slotIndex));
            }
        }

        [TestCase(0, 2)]
        [TestCase(-1, 2)]
        [TestCase(4, 0)]
        [TestCase(4, -1)]
        public void Constructor_WhenDimensionIsNotPositive_ThrowsArgumentOutOfRangeException(
            int columns,
            int rows)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new JigsawBoardState(columns, rows));
        }

        [Test]
        public void SwapSlots_WhenDifferentSlotsAreSelected_UpdatesBothLookupDirections()
        {
            JigsawBoardState board = new JigsawBoardState(4, 2);

            bool didSwap = board.SwapSlots(1, 6);

            Assert.That(didSwap, Is.True);
            Assert.That(board.GetPieceIdAtSlot(1), Is.EqualTo(6));
            Assert.That(board.GetPieceIdAtSlot(6), Is.EqualTo(1));
            Assert.That(board.GetSlotIndexForPiece(1), Is.EqualTo(6));
            Assert.That(board.GetSlotIndexForPiece(6), Is.EqualTo(1));
            Assert.That(board.IsCompleted, Is.False);
        }

        [Test]
        public void SwapSlots_WhenSameSlotIsSelected_KeepsBoardUnchanged()
        {
            JigsawBoardState board = new JigsawBoardState(4, 2);

            bool didSwap = board.SwapSlots(3, 3);

            Assert.That(didSwap, Is.False);
            Assert.That(board.GetPieceIdAtSlot(3), Is.EqualTo(3));
            Assert.That(board.GetSlotIndexForPiece(3), Is.EqualTo(3));
            Assert.That(board.IsCompleted, Is.True);
        }

        [Test]
        public void SwapSlots_WhenSamePairIsSwappedTwice_RestoresSolvedState()
        {
            JigsawBoardState board = new JigsawBoardState(4, 2);

            board.SwapSlots(0, 7);
            board.SwapSlots(0, 7);

            Assert.That(board.IsCompleted, Is.True);
        }

        [Test]
        public void SwapSlots_WhenSlotIsOutsideBoard_ThrowsArgumentOutOfRangeException()
        {
            JigsawBoardState board = new JigsawBoardState(4, 2);

            Assert.Throws<ArgumentOutOfRangeException>(() => board.SwapSlots(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.SwapSlots(0, board.PieceCount));
        }

        [Test]
        public void Shuffle_WhenBoardHasMultiplePieces_ProducesCompleteNonSolvedPermutation()
        {
            JigsawBoardState board = new JigsawBoardState(6, 3);
            HashSet<int> pieceIds = new HashSet<int>();

            board.Shuffle(new Random(20260815));

            Assert.That(board.IsCompleted, Is.False);

            for (int slotIndex = 0; slotIndex < board.PieceCount; slotIndex++)
            {
                int pieceId = board.GetPieceIdAtSlot(slotIndex);
                Assert.That(pieceIds.Add(pieceId), Is.True, "Duplicate piece ID: " + pieceId);
                Assert.That(board.GetSlotIndexForPiece(pieceId), Is.EqualTo(slotIndex));
            }

            Assert.That(pieceIds.Count, Is.EqualTo(board.PieceCount));
        }

        [Test]
        public void Shuffle_WhenUsingSameSeed_ProducesSamePermutation()
        {
            JigsawBoardState firstBoard = new JigsawBoardState(8, 4);
            JigsawBoardState secondBoard = new JigsawBoardState(8, 4);

            firstBoard.Shuffle(new Random(42));
            secondBoard.Shuffle(new Random(42));

            for (int slotIndex = 0; slotIndex < firstBoard.PieceCount; slotIndex++)
            {
                Assert.That(
                    firstBoard.GetPieceIdAtSlot(slotIndex),
                    Is.EqualTo(secondBoard.GetPieceIdAtSlot(slotIndex)));
            }
        }

        [Test]
        public void Shuffle_WhenRandomIsNull_ThrowsArgumentNullException()
        {
            JigsawBoardState board = new JigsawBoardState(4, 2);

            Assert.Throws<ArgumentNullException>(() => board.Shuffle(null));
        }

        [Test]
        public void Shuffle_WhenBoardHasOnePiece_ThrowsInvalidOperationException()
        {
            JigsawBoardState board = new JigsawBoardState(1, 1);

            Assert.Throws<InvalidOperationException>(() => board.Shuffle(new Random(1)));
        }

        [Test]
        public void ResetSolved_AfterBoardChanges_RestoresEveryPieceToTargetSlot()
        {
            JigsawBoardState board = new JigsawBoardState(4, 2);
            board.Shuffle(new Random(13));

            board.ResetSolved();

            Assert.That(board.IsCompleted, Is.True);

            for (int slotIndex = 0; slotIndex < board.PieceCount; slotIndex++)
            {
                Assert.That(board.GetPieceIdAtSlot(slotIndex), Is.EqualTo(slotIndex));
                Assert.That(board.GetSlotIndexForPiece(slotIndex), Is.EqualTo(slotIndex));
            }
        }
    }
}
