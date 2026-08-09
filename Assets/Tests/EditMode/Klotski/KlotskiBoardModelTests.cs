using System;
using NUnit.Framework;
using NanokaGame.Games.Klotski;
using UnityEngine;

namespace NanokaGame.Tests.EditMode.Klotski
{
    public sealed class KlotskiBoardModelTests
    {
        [Test]
        public void Constructor_WhenUsingDefaultLevel_CreatesExpectedBoard()
        {
            KlotskiBoardModel board = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());

            Assert.That(board.Columns, Is.EqualTo(4));
            Assert.That(board.Rows, Is.EqualTo(5));
            Assert.That(board.Pieces.Count, Is.EqualTo(10));
            Assert.That(board.GetOccupantId(new Vector2Int(1, 0)), Is.Null);
            Assert.That(board.GetOccupantId(new Vector2Int(2, 0)), Is.Null);
            Assert.That(board.GetOccupantId(new Vector2Int(1, 3)), Is.EqualTo("nanoka_head"));
            Assert.That(board.GetOccupantId(new Vector2Int(2, 4)), Is.EqualTo("nanoka_head"));
            Assert.That(board.IsCompleted, Is.False);
        }

        [Test]
        public void CreateDefault_ReturnsConfirmedPieceLayout()
        {
            KlotskiBoardModel board = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());

            Assert.That(board.TargetCell, Is.EqualTo(new Vector2Int(1, 0)));
            AssertPiece(board, "nanoka_head", KlotskiPieceType.Target, new Vector2Int(1, 3), new Vector2Int(2, 2));
            AssertPiece(board, "nanoka_body", KlotskiPieceType.Horizontal, new Vector2Int(1, 2), new Vector2Int(2, 1));
            AssertPiece(board, "nanoka_left_arm", KlotskiPieceType.Vertical, new Vector2Int(0, 3), new Vector2Int(1, 2));
            AssertPiece(board, "nanoka_right_arm", KlotskiPieceType.Vertical, new Vector2Int(3, 3), new Vector2Int(1, 2));
            AssertPiece(board, "nanoka_left_leg", KlotskiPieceType.Vertical, new Vector2Int(0, 1), new Vector2Int(1, 2));
            AssertPiece(board, "nanoka_right_leg", KlotskiPieceType.Vertical, new Vector2Int(3, 1), new Vector2Int(1, 2));
            AssertPiece(board, "nanoka_body_left", KlotskiPieceType.Soldier, new Vector2Int(1, 1), Vector2Int.one);
            AssertPiece(board, "nanoka_body_right", KlotskiPieceType.Soldier, new Vector2Int(2, 1), Vector2Int.one);
            AssertPiece(board, "nanoka_skirt_left", KlotskiPieceType.Soldier, new Vector2Int(0, 0), Vector2Int.one);
            AssertPiece(board, "nanoka_skirt_right", KlotskiPieceType.Soldier, new Vector2Int(3, 0), Vector2Int.one);
        }

        [Test]
        public void Constructor_WhenPieceIdsAreDuplicated_ThrowsArgumentException()
        {
            KlotskiLevelDefinition level = CreateLevel(
                new KlotskiPieceState("target", KlotskiPieceType.Target, new Vector2Int(1, 3), new Vector2Int(2, 2)),
                new KlotskiPieceState("target", KlotskiPieceType.Soldier, Vector2Int.zero, Vector2Int.one));

            Assert.Throws<ArgumentException>(() => new KlotskiBoardModel(level));
        }

        [Test]
        public void Constructor_WhenInitialPiecesOverlap_ThrowsArgumentException()
        {
            KlotskiLevelDefinition level = CreateLevel(
                new KlotskiPieceState("target", KlotskiPieceType.Target, new Vector2Int(1, 3), new Vector2Int(2, 2)),
                new KlotskiPieceState("soldier", KlotskiPieceType.Soldier, new Vector2Int(1, 3), Vector2Int.one));

            Assert.Throws<ArgumentException>(() => new KlotskiBoardModel(level));
        }

        [Test]
        public void Constructor_WhenInitialPieceIsOutsideBoard_ThrowsArgumentException()
        {
            KlotskiLevelDefinition level = CreateLevel(
                new KlotskiPieceState("target", KlotskiPieceType.Target, new Vector2Int(3, 3), new Vector2Int(2, 2)));

            Assert.Throws<ArgumentException>(() => new KlotskiBoardModel(level));
        }

        [Test]
        public void TryMove_WhenDestinationIsFree_MovesPiece()
        {
            KlotskiBoardModel board = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());

            KlotskiMoveResult result = board.TryMove("nanoka_body_left", Vector2Int.down, 1);

            KlotskiPieceState piece;
            Assert.That(result, Is.EqualTo(KlotskiMoveResult.Success));
            Assert.That(board.TryGetPiece("nanoka_body_left", out piece), Is.True);
            Assert.That(piece.Cell, Is.EqualTo(new Vector2Int(1, 0)));
        }

        [Test]
        public void TryMove_WhenDestinationIsOccupied_ReturnsBlockedAndKeepsPosition()
        {
            KlotskiBoardModel board = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());

            KlotskiMoveResult result = board.TryMove("nanoka_head", Vector2Int.down, 1);

            KlotskiPieceState piece;
            Assert.That(result, Is.EqualTo(KlotskiMoveResult.Blocked));
            Assert.That(board.TryGetPiece("nanoka_head", out piece), Is.True);
            Assert.That(piece.Cell, Is.EqualTo(new Vector2Int(1, 3)));
        }

        [Test]
        public void TryMove_WhenDestinationIsOutsideBoard_ReturnsOutOfBounds()
        {
            KlotskiBoardModel board = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());

            KlotskiMoveResult result = board.TryMove("nanoka_skirt_left", Vector2Int.left, 1);

            Assert.That(result, Is.EqualTo(KlotskiMoveResult.OutOfBounds));
        }

        [Test]
        public void TryMove_WhenDirectionIsDiagonal_ReturnsInvalidDirection()
        {
            KlotskiBoardModel board = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());

            KlotskiMoveResult result = board.TryMove("nanoka_body_left", Vector2Int.one, 1);

            Assert.That(result, Is.EqualTo(KlotskiMoveResult.InvalidDirection));
        }

        [Test]
        public void TryMove_WhenDistanceIsZero_ReturnsInvalidDistance()
        {
            KlotskiBoardModel board = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());

            KlotskiMoveResult result = board.TryMove("nanoka_body_left", Vector2Int.down, 0);

            Assert.That(result, Is.EqualTo(KlotskiMoveResult.InvalidDistance));
        }

        [Test]
        public void TryMove_WhenIntermediateCellIsOccupied_ReturnsBlockedEvenIfFinalCellIsFree()
        {
            KlotskiLevelDefinition level = CreateLevel(
                new KlotskiPieceState("target", KlotskiPieceType.Target, new Vector2Int(2, 3), new Vector2Int(2, 2)),
                new KlotskiPieceState("mover", KlotskiPieceType.Soldier, Vector2Int.zero, Vector2Int.one),
                new KlotskiPieceState("blocker", KlotskiPieceType.Soldier, new Vector2Int(0, 1), Vector2Int.one));
            KlotskiBoardModel board = new KlotskiBoardModel(level);

            KlotskiMoveResult result = board.TryMove("mover", Vector2Int.up, 3);

            KlotskiPieceState mover;
            Assert.That(result, Is.EqualTo(KlotskiMoveResult.Blocked));
            Assert.That(board.TryGetPiece("mover", out mover), Is.True);
            Assert.That(mover.Cell, Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void TryMove_WhenMoveSucceeds_ClearsOldOccupancyAndFillsNewOccupancy()
        {
            KlotskiBoardModel board = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());

            board.TryMove("nanoka_body_left", Vector2Int.down, 1);

            Assert.That(board.GetOccupantId(new Vector2Int(1, 1)), Is.Null);
            Assert.That(board.GetOccupantId(new Vector2Int(1, 0)), Is.EqualTo("nanoka_body_left"));
        }

        [Test]
        public void Reset_AfterPieceMoved_RestoresInitialPositionAndOccupancy()
        {
            KlotskiBoardModel board = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());
            board.TryMove("nanoka_body_left", Vector2Int.down, 1);

            board.Reset();

            KlotskiPieceState piece;
            Assert.That(board.TryGetPiece("nanoka_body_left", out piece), Is.True);
            Assert.That(piece.Cell, Is.EqualTo(new Vector2Int(1, 1)));
            Assert.That(board.GetOccupantId(new Vector2Int(1, 0)), Is.Null);
            Assert.That(board.GetOccupantId(new Vector2Int(1, 1)), Is.EqualTo("nanoka_body_left"));
            Assert.That(board.IsCompleted, Is.False);
        }

        [Test]
        public void TryMove_WhenTargetReachesTargetCell_CompletesGame()
        {
            KlotskiLevelDefinition level = new KlotskiLevelDefinition(
                4,
                5,
                new Vector2Int(1, 0),
                new[]
                {
                    new KlotskiPieceState("target", KlotskiPieceType.Target, new Vector2Int(1, 1), new Vector2Int(2, 2))
                });
            KlotskiBoardModel board = new KlotskiBoardModel(level);

            KlotskiMoveResult result = board.TryMove("target", Vector2Int.down, 1);

            Assert.That(result, Is.EqualTo(KlotskiMoveResult.Success));
            Assert.That(board.IsCompleted, Is.True);
            Assert.That(board.GetOccupantId(new Vector2Int(1, 0)), Is.EqualTo("target"));
            Assert.That(board.GetOccupantId(new Vector2Int(2, 0)), Is.EqualTo("target"));
            Assert.That(board.GetOccupantId(new Vector2Int(1, 2)), Is.Null);
            Assert.That(board.GetOccupantId(new Vector2Int(2, 2)), Is.Null);
        }

        [Test]
        public void TryMove_WhenNonTargetReachesTargetCell_DoesNotCompleteGame()
        {
            KlotskiLevelDefinition level = new KlotskiLevelDefinition(
                4,
                5,
                Vector2Int.zero,
                new[]
                {
                    new KlotskiPieceState("target", KlotskiPieceType.Target, new Vector2Int(2, 3), new Vector2Int(2, 2)),
                    new KlotskiPieceState("soldier", KlotskiPieceType.Soldier, new Vector2Int(0, 1), Vector2Int.one)
                });
            KlotskiBoardModel board = new KlotskiBoardModel(level);

            KlotskiMoveResult result = board.TryMove("soldier", Vector2Int.down, 1);

            Assert.That(result, Is.EqualTo(KlotskiMoveResult.Success));
            Assert.That(board.IsCompleted, Is.False);
        }

        [Test]
        public void GetMaxMoveDistance_WhenOneCellIsAvailable_ReturnsOne()
        {
            KlotskiBoardModel board = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());

            int distance = board.GetMaxMoveDistance("nanoka_body_left", Vector2Int.down);

            Assert.That(distance, Is.EqualTo(1));
        }

        private static KlotskiLevelDefinition CreateLevel(params KlotskiPieceState[] pieces)
        {
            return new KlotskiLevelDefinition(4, 5, new Vector2Int(0, 3), pieces);
        }

        private static void AssertPiece(
            KlotskiBoardModel board,
            string pieceId,
            KlotskiPieceType expectedType,
            Vector2Int expectedCell,
            Vector2Int expectedSize)
        {
            KlotskiPieceState piece;
            Assert.That(board.TryGetPiece(pieceId, out piece), Is.True, "Missing piece: " + pieceId);
            Assert.That(piece.Type, Is.EqualTo(expectedType), pieceId + " type");
            Assert.That(piece.Cell, Is.EqualTo(expectedCell), pieceId + " cell");
            Assert.That(piece.InitialCell, Is.EqualTo(expectedCell), pieceId + " initial cell");
            Assert.That(piece.SizeInCells, Is.EqualTo(expectedSize), pieceId + " size");
        }
    }
}
