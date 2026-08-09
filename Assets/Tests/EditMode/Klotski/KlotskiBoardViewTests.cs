using System;
using System.Collections.Generic;
using NanokaGame.Games.Klotski;
using NUnit.Framework;
using UnityEngine;

namespace NanokaGame.Tests.EditMode.Klotski
{
    public sealed class KlotskiBoardViewTests
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
        public void Initialize_WhenAllViewsAreConfigured_SynchronizesEveryPiece()
        {
            BoardFixture fixture = CreateFixture();
            KlotskiBoardModel model = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());
            KlotskiBoardLayout layout = CreateCurrentLayout();

            fixture.BoardView.Initialize(model, layout);

            Assert.That(fixture.BoardView.IsInitialized, Is.True);
            Assert.That(fixture.BoardView.Model, Is.SameAs(model));
            Assert.That(fixture.BoardView.Layout, Is.SameAs(layout));

            foreach (KlotskiPieceState piece in model.Pieces)
            {
                KlotskiPieceView view;
                Assert.That(fixture.BoardView.TryGetPieceView(piece.Id, out view), Is.True, piece.Id);
                AssertVector3(
                    view.transform.position,
                    layout.GetPieceWorldPosition(piece.Cell, piece.SizeInCells));
                Assert.That(view.SpriteRenderer.sortingLayerName, Is.EqualTo("Default"));
                Assert.That(view.SpriteRenderer.sortingOrder, Is.EqualTo(3));
            }
        }

        [Test]
        public void Initialize_WhenModelPieceViewIsMissing_ThrowsInvalidOperationException()
        {
            BoardFixture fixture = CreateFixture();
            fixture.Views.RemoveAt(fixture.Views.Count - 1);
            fixture.BoardView.Configure(fixture.Views, "Default", 0);
            KlotskiBoardModel model = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());

            Assert.Throws<InvalidOperationException>(
                () => fixture.BoardView.Initialize(model, CreateCurrentLayout()));
        }

        [Test]
        public void Initialize_WhenPieceViewIdIsDuplicated_ThrowsInvalidOperationException()
        {
            BoardFixture fixture = CreateFixture();
            fixture.Views[1].Configure(fixture.Views[0].PieceId, fixture.Views[1].SpriteRenderer);
            KlotskiBoardModel model = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());

            Assert.Throws<InvalidOperationException>(
                () => fixture.BoardView.Initialize(model, CreateCurrentLayout()));
        }

        [Test]
        public void SyncPiece_AfterModelMove_UpdatesOnlyRequestedViewToModelPosition()
        {
            BoardFixture fixture = CreateFixture();
            KlotskiBoardModel model = new KlotskiBoardModel(KlotskiLevelDefinition.CreateDefault());
            KlotskiBoardLayout layout = CreateCurrentLayout();
            fixture.BoardView.Initialize(model, layout);
            KlotskiPieceView movedView = fixture.FindView("nanoka_body_left");
            KlotskiPieceView unchangedView = fixture.FindView("nanoka_body_right");
            Vector3 unchangedPosition = unchangedView.transform.position;

            Assert.That(
                model.TryMove("nanoka_body_left", Vector2Int.down, 1),
                Is.EqualTo(KlotskiMoveResult.Success));
            fixture.BoardView.SyncPiece("nanoka_body_left");

            AssertVector3(
                movedView.transform.position,
                layout.GetPieceWorldPosition(new Vector2Int(1, 0), Vector2Int.one));
            AssertVector3(unchangedView.transform.position, unchangedPosition);
        }

        [Test]
        public void ResetGame_AfterModelMove_RestoresModelAndAllViews()
        {
            BoardFixture fixture = CreateFixture();
            fixture.Controller.Configure(
                fixture.BoardView,
                fixture.AnchorRenderer,
                1.68f,
                0f,
                0f,
                false);
            fixture.Controller.InitializeGame();
            Assert.That(
                fixture.Controller.Model.TryMove("nanoka_body_left", Vector2Int.down, 1),
                Is.EqualTo(KlotskiMoveResult.Success));
            fixture.Controller.BoardView.SyncPiece("nanoka_body_left");

            fixture.Controller.ResetGame();

            KlotskiPieceState piece;
            Assert.That(fixture.Controller.Model.TryGetPiece("nanoka_body_left", out piece), Is.True);
            Assert.That(piece.Cell, Is.EqualTo(new Vector2Int(1, 1)));
            AssertVector3(
                fixture.FindView("nanoka_body_left").transform.position,
                fixture.Controller.Layout.GetPieceWorldPosition(piece.Cell, piece.SizeInCells));
        }

        private BoardFixture CreateFixture()
        {
            GameObject root = CreateGameObject("Pieces");
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
                GameObject pieceObject = CreateGameObject(piece.Id);
                pieceObject.transform.SetParent(root.transform, false);
                pieceObject.transform.position = new Vector3(100f + pieceIndex, 100f, 0f);
                SpriteRenderer renderer = pieceObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                KlotskiPieceView view = pieceObject.AddComponent<KlotskiPieceView>();
                view.Configure(piece.Id, renderer);
                views.Add(view);

                if (piece.Id == "nanoka_left_arm")
                {
                    anchorRenderer = renderer;
                }
            }

            KlotskiBoardView boardView = root.AddComponent<KlotskiBoardView>();
            boardView.Configure(views, "Default", 3);
            KlotskiGameController controller = root.AddComponent<KlotskiGameController>();

            return new BoardFixture(boardView, controller, views, anchorRenderer);
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new GameObject(objectName);
            _createdObjects.Add(gameObject);
            return gameObject;
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

        private static void AssertVector3(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance), "X");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance), "Y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tolerance), "Z");
        }

        private sealed class BoardFixture
        {
            public BoardFixture(
                KlotskiBoardView boardView,
                KlotskiGameController controller,
                List<KlotskiPieceView> views,
                SpriteRenderer anchorRenderer)
            {
                BoardView = boardView;
                Controller = controller;
                Views = views;
                AnchorRenderer = anchorRenderer;
            }

            public KlotskiBoardView BoardView { get; }

            public KlotskiGameController Controller { get; }

            public List<KlotskiPieceView> Views { get; }

            public SpriteRenderer AnchorRenderer { get; }

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
        }
    }
}
