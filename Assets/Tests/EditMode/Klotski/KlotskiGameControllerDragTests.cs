using System;
using System.Collections.Generic;
using NanokaGame.Games.Klotski;
using NUnit.Framework;
using UnityEngine;

namespace NanokaGame.Tests.EditMode.Klotski
{
    public sealed class KlotskiGameControllerDragTests
    {
        private const float Tolerance = 0.0001f;
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int objectIndex = _createdObjects.Count - 1; objectIndex >= 0; objectIndex--)
            {
                if (_createdObjects[objectIndex] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[objectIndex]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void TryBeginDrag_WhenPointerIsOffCenter_DoesNotMovePieceToPointerCenter()
        {
            DragFixture fixture = CreateFixture();
            KlotskiPieceView view = fixture.FindView("nanoka_body_left");
            Vector3 startPosition = view.transform.position;
            Vector3 pointerPosition = startPosition + new Vector3(0.35f, 0.25f, 0f);

            bool beganDrag = fixture.Controller.TryBeginDrag(view, 11, pointerPosition);

            Assert.That(beganDrag, Is.True);
            Assert.That(fixture.Controller.IsDragging, Is.True);
            Assert.That(fixture.Controller.ActivePointerId, Is.EqualTo(11));
            Assert.That(fixture.Controller.ActivePieceId, Is.EqualTo("nanoka_body_left"));
            AssertVector3(view.transform.position, startPosition);
        }

        [Test]
        public void UpdateDrag_WhenMovementIsBelowThreshold_KeepsAxisUnlockedAndPieceAtStart()
        {
            DragFixture fixture = CreateFixture();
            KlotskiPieceView view = fixture.FindView("nanoka_body_left");
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, 12, startPosition);

            fixture.Controller.UpdateDrag(12, startPosition + Vector3.down * 0.1f);

            Assert.That(fixture.Controller.DragAxis, Is.EqualTo(KlotskiDragAxis.None));
            AssertVector3(view.transform.position, startPosition);
        }

        [Test]
        public void UpdateDrag_WhenOnlyVerticalMovementIsAvailable_PrefersVerticalAxisAndDoesNotSwitch()
        {
            DragFixture fixture = CreateFixture();
            KlotskiPieceView view = fixture.FindView("nanoka_body_left");
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, 13, startPosition);

            fixture.Controller.UpdateDrag(13, startPosition + new Vector3(0.8f, -0.3f, 0f));
            Assert.That(fixture.Controller.DragAxis, Is.EqualTo(KlotskiDragAxis.Vertical));

            fixture.Controller.UpdateDrag(13, startPosition + new Vector3(1.5f, -0.8f, 0f));

            Assert.That(fixture.Controller.DragAxis, Is.EqualTo(KlotskiDragAxis.Vertical));
            Assert.That(view.transform.position.x, Is.EqualTo(startPosition.x).Within(Tolerance));
        }

        [Test]
        public void UpdateDrag_WhenPointerMovesPastReachableSpace_ClampsPreviewToLastLegalCell()
        {
            DragFixture fixture = CreateFixture();
            KlotskiPieceView view = fixture.FindView("nanoka_body_left");
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, 14, startPosition);

            fixture.Controller.UpdateDrag(14, startPosition + Vector3.down * 10f);

            KlotskiPieceState piece = fixture.GetPiece("nanoka_body_left");
            Vector3 expectedPosition = fixture.Controller.Layout.GetPieceWorldPosition(
                piece.Cell + Vector2Int.down,
                piece.SizeInCells);
            AssertVector3(view.transform.position, expectedPosition);
            Assert.That(piece.Cell, Is.EqualTo(new Vector2Int(1, 1)), "Preview must not modify the Model.");
        }

        [Test]
        public void ReleaseDrag_WhenMoveIsLegal_CommitsModelAndSnapsViewToTargetCell()
        {
            DragFixture fixture = CreateFixture();
            KlotskiPieceView view = fixture.FindView("nanoka_body_left");
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, 15, startPosition);

            KlotskiMoveResult result = fixture.Controller.ReleaseDrag(
                15,
                startPosition + Vector3.down * 0.7f);

            KlotskiPieceState piece = fixture.GetPiece("nanoka_body_left");
            Assert.That(result, Is.EqualTo(KlotskiMoveResult.Success));
            Assert.That(piece.Cell, Is.EqualTo(new Vector2Int(1, 0)));
            Assert.That(fixture.Controller.IsDragging, Is.False);
            AssertVector3(
                view.transform.position,
                fixture.Controller.Layout.GetPieceWorldPosition(piece.Cell, piece.SizeInCells));
        }

        [Test]
        public void ReleaseDrag_WhenMovementIsBelowCommitThreshold_DoesNotModifyModelAndSnapsBack()
        {
            DragFixture fixture = CreateFixture();
            KlotskiPieceView view = fixture.FindView("nanoka_body_left");
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, 16, startPosition);

            KlotskiMoveResult result = fixture.Controller.ReleaseDrag(
                16,
                startPosition + Vector3.down * 0.4f);

            Assert.That(result, Is.EqualTo(KlotskiMoveResult.InvalidDistance));
            Assert.That(fixture.GetPiece("nanoka_body_left").Cell, Is.EqualTo(new Vector2Int(1, 1)));
            AssertVector3(view.transform.position, startPosition);
        }

        [Test]
        public void UpdateAndReleaseDrag_WhenPointerIdDoesNotMatch_IgnoresSecondPointer()
        {
            DragFixture fixture = CreateFixture();
            KlotskiPieceView view = fixture.FindView("nanoka_body_left");
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, 17, startPosition);

            bool updated = fixture.Controller.UpdateDrag(18, startPosition + Vector3.down);
            KlotskiMoveResult releaseResult = fixture.Controller.ReleaseDrag(
                18,
                startPosition + Vector3.down);

            Assert.That(updated, Is.False);
            Assert.That(releaseResult, Is.EqualTo(KlotskiMoveResult.InvalidDistance));
            Assert.That(fixture.Controller.IsDragging, Is.True);
            Assert.That(fixture.Controller.ActivePointerId, Is.EqualTo(17));
            Assert.That(fixture.GetPiece("nanoka_body_left").Cell, Is.EqualTo(new Vector2Int(1, 1)));
        }

        [Test]
        public void CancelDrag_AfterPreview_RestoresViewWithoutChangingModel()
        {
            DragFixture fixture = CreateFixture();
            KlotskiPieceView view = fixture.FindView("nanoka_body_left");
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, 19, startPosition);
            fixture.Controller.UpdateDrag(19, startPosition + Vector3.down);

            bool cancelled = fixture.Controller.CancelDrag(view, 19);

            Assert.That(cancelled, Is.True);
            Assert.That(fixture.Controller.IsDragging, Is.False);
            Assert.That(fixture.GetPiece("nanoka_body_left").Cell, Is.EqualTo(new Vector2Int(1, 1)));
            AssertVector3(view.transform.position, startPosition);
        }

        [Test]
        public void UpdateDrag_WhenPieceIsFullyBlocked_DoesNotPreviewThroughOtherPieces()
        {
            DragFixture fixture = CreateFixture();
            KlotskiPieceView view = fixture.FindView("nanoka_head");
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, 20, startPosition);

            fixture.Controller.UpdateDrag(20, startPosition + Vector3.down * 10f);

            AssertVector3(view.transform.position, startPosition);
            Assert.That(fixture.GetPiece("nanoka_head").Cell, Is.EqualTo(new Vector2Int(1, 3)));
        }

        [Test]
        public void ReleaseDrag_WhenPieceIsFullyBlocked_DoesNotModifyModelAndSnapsBack()
        {
            DragFixture fixture = CreateFixture();
            KlotskiPieceView view = fixture.FindView("nanoka_head");
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, 21, startPosition);

            KlotskiMoveResult result = fixture.Controller.ReleaseDrag(
                21,
                startPosition + Vector3.down * 10f);

            Assert.That(result, Is.EqualTo(KlotskiMoveResult.InvalidDistance));
            Assert.That(fixture.GetPiece("nanoka_head").Cell, Is.EqualTo(new Vector2Int(1, 3)));
            Assert.That(fixture.Controller.IsDragging, Is.False);
            AssertVector3(view.transform.position, startPosition);
        }

        [Test]
        public void InitializeGame_WhenPieceCollidersExist_MatchesColliderAndRendererWorldBounds()
        {
            DragFixture fixture = CreateFixture();
            Physics2D.SyncTransforms();

            for (int viewIndex = 0; viewIndex < fixture.Views.Count; viewIndex++)
            {
                KlotskiPieceView view = fixture.Views[viewIndex];
                Assert.That(view.InputCollider, Is.Not.Null, view.PieceId);
                Assert.That(view.InputCollider.isTrigger, Is.True, view.PieceId);
                Assert.That(
                    view.InputCollider.bounds.size.x,
                    Is.EqualTo(view.SpriteRenderer.bounds.size.x).Within(Tolerance),
                    view.PieceId + " width");
                Assert.That(
                    view.InputCollider.bounds.size.y,
                    Is.EqualTo(view.SpriteRenderer.bounds.size.y).Within(Tolerance),
                    view.PieceId + " height");
            }
        }

        private DragFixture CreateFixture()
        {
            GameObject root = new GameObject("Pieces");
            _createdObjects.Add(root);
            Texture2D texture = new Texture2D(10, 10);
            _createdObjects.Add(texture);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 10f, 10f),
                new Vector2(0.5f, 0.5f),
                10f);
            _createdObjects.Add(sprite);
            KlotskiLevelDefinition level = KlotskiLevelDefinition.CreateDefault();
            List<KlotskiPieceView> views = new List<KlotskiPieceView>();
            SpriteRenderer anchorRenderer = null;

            for (int pieceIndex = 0; pieceIndex < level.InitialPieces.Count; pieceIndex++)
            {
                KlotskiPieceState piece = level.InitialPieces[pieceIndex];
                GameObject pieceObject = new GameObject(piece.Id);
                pieceObject.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = pieceObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                KlotskiPieceView view = pieceObject.AddComponent<KlotskiPieceView>();
                view.Configure(piece.Id, renderer);
                views.Add(view);

                if (piece.Id == "nanoka_left_arm")
                {
                    anchorRenderer = renderer;
                    pieceObject.transform.position = new Vector3(-2.46f, 2.64f, 0f);
                    pieceObject.transform.localScale = new Vector3(1.68f, 3.36f, 1f);
                }
            }

            KlotskiBoardView boardView = root.AddComponent<KlotskiBoardView>();
            boardView.Configure(views, "Default", 0);
            KlotskiGameController controller = root.AddComponent<KlotskiGameController>();
            controller.Configure(boardView, anchorRenderer, 1.68f, 0f, 0f, false);
            controller.InitializeGame();

            return new DragFixture(controller, views);
        }

        private static void AssertVector3(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance), "X");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance), "Y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tolerance), "Z");
        }

        private sealed class DragFixture
        {
            public DragFixture(KlotskiGameController controller, List<KlotskiPieceView> views)
            {
                Controller = controller;
                Views = views;
            }

            public KlotskiGameController Controller { get; }

            public List<KlotskiPieceView> Views { get; }

            public KlotskiPieceView FindView(string pieceId)
            {
                for (int viewIndex = 0; viewIndex < Views.Count; viewIndex++)
                {
                    if (Views[viewIndex].PieceId == pieceId)
                    {
                        return Views[viewIndex];
                    }
                }

                throw new InvalidOperationException("Piece view was not found: " + pieceId);
            }

            public KlotskiPieceState GetPiece(string pieceId)
            {
                KlotskiPieceState piece;
                if (Controller.Model.TryGetPiece(pieceId, out piece))
                {
                    return piece;
                }

                throw new InvalidOperationException("Model piece was not found: " + pieceId);
            }
        }
    }
}
