using System.Collections;
using System.Collections.Generic;
using NanokaGame.Games.Klotski;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NanokaGame.Tests.PlayMode.Klotski
{
    public sealed class KlotskiBoardBootstrapPlayModeTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int objectIndex = _createdObjects.Count - 1; objectIndex >= 0; objectIndex--)
            {
                if (_createdObjects[objectIndex] != null)
                {
                    UnityEngine.Object.Destroy(_createdObjects[objectIndex]);
                }
            }

            _createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Awake_WhenSceneBindingsAreValid_InitializesModelLayoutAndViews()
        {
            GameObject root = new GameObject("Pieces");
            root.SetActive(false);
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
                pieceObject.transform.position = new Vector3(50f, 50f, 0f);
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
            controller.Configure(boardView, anchorRenderer, 1.68f, 0f, 0f, true);

            root.SetActive(true);
            yield return null;

            Assert.That(controller.IsInitialized, Is.True);
            Assert.That(boardView.IsInitialized, Is.True);
            Assert.That(controller.Model.Pieces.Count, Is.EqualTo(10));
            Assert.That(controller.Layout.BoardTopLeft.x, Is.EqualTo(-3.30f).Within(0.0001f));
            Assert.That(controller.Layout.BoardTopLeft.y, Is.EqualTo(4.32f).Within(0.0001f));

            KlotskiPieceState target;
            Assert.That(controller.Model.TryGetPiece("nanoka_head", out target), Is.True);
            KlotskiPieceView targetView;
            Assert.That(boardView.TryGetPieceView("nanoka_head", out targetView), Is.True);
            Vector3 expectedPosition = controller.Layout.GetPieceWorldPosition(
                target.Cell,
                target.SizeInCells);
            Assert.That(targetView.transform.position.x, Is.EqualTo(expectedPosition.x).Within(0.0001f));
            Assert.That(targetView.transform.position.y, Is.EqualTo(expectedPosition.y).Within(0.0001f));
        }
    }
}
