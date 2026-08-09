using System;
using System.Collections.Generic;
using NanokaGame.Games.Klotski;
using NUnit.Framework;
using UnityEngine;

namespace NanokaGame.Tests.EditMode.Klotski
{
    public sealed class KlotskiPieceViewTests
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
        public void ApplyLayout_WhenRendererUsesSimpleMode_SetsPositionAndScaleFromLayout()
        {
            KlotskiPieceView view = CreateView("piece", 100, 50, 10f, Vector4.zero);
            view.transform.localScale = new Vector3(1f, 1f, 3f);
            view.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
            KlotskiPieceState piece = new KlotskiPieceState(
                "piece",
                KlotskiPieceType.Horizontal,
                new Vector2Int(1, 2),
                new Vector2Int(2, 1));
            KlotskiBoardLayout layout = KlotskiBoardLayout.CreateFromCenter(
                Vector2.zero,
                4,
                5,
                2f,
                0.2f,
                0.5f);

            view.ApplyLayout(piece, layout);

            AssertVector3(view.transform.position, new Vector3(0f, 0f, 0.5f));
            Assert.That(view.transform.localScale.x, Is.EqualTo(0.38f).Within(Tolerance));
            Assert.That(view.transform.localScale.y, Is.EqualTo(0.36f).Within(Tolerance));
            Assert.That(view.transform.localScale.z, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(view.transform.localEulerAngles.z, Is.EqualTo(15f).Within(Tolerance));
        }

        [Test]
        public void ApplyWorldSize_WhenParentIsScaled_CompensatesSimpleSpriteLocalScale()
        {
            GameObject parent = CreateGameObject("parent");
            parent.transform.localScale = new Vector3(2f, 4f, 1f);
            KlotskiPieceView view = CreateView("piece", 100, 100, 10f, Vector4.zero);
            view.transform.SetParent(parent.transform, false);

            view.ApplyWorldSize(new Vector2(2f, 4f));

            Assert.That(view.transform.localScale.x, Is.EqualTo(0.1f).Within(Tolerance));
            Assert.That(view.transform.localScale.y, Is.EqualTo(0.1f).Within(Tolerance));
            Assert.That(view.SpriteRenderer.bounds.size.x, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(view.SpriteRenderer.bounds.size.y, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void ApplyWorldSize_WhenRendererUsesSlicedMode_SetsRendererSize()
        {
            KlotskiPieceView view = CreateView(
                "piece",
                100,
                100,
                10f,
                new Vector4(5f, 5f, 5f, 5f));
            view.SpriteRenderer.drawMode = SpriteDrawMode.Sliced;
            view.transform.localScale = new Vector3(0.5f, 0.25f, 1f);

            view.ApplyWorldSize(new Vector2(4f, 2f));

            Assert.That(view.SpriteRenderer.size.x, Is.EqualTo(8f).Within(Tolerance));
            Assert.That(view.SpriteRenderer.size.y, Is.EqualTo(8f).Within(Tolerance));
            Assert.That(view.SpriteRenderer.bounds.size.x, Is.EqualTo(4f).Within(Tolerance));
            Assert.That(view.SpriteRenderer.bounds.size.y, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void ApplyLayout_WhenPieceIdDoesNotMatch_ThrowsInvalidOperationException()
        {
            KlotskiPieceView view = CreateView("view_piece", 100, 100, 10f, Vector4.zero);
            KlotskiPieceState piece = new KlotskiPieceState(
                "model_piece",
                KlotskiPieceType.Soldier,
                Vector2Int.zero,
                Vector2Int.one);
            KlotskiBoardLayout layout = KlotskiBoardLayout.CreateFromCenter(
                Vector2.zero,
                4,
                5,
                1f,
                0f,
                0f);

            Assert.Throws<InvalidOperationException>(() => view.ApplyLayout(piece, layout));
        }

        [Test]
        public void SetSelected_WhenSelectionEnds_RestoresBaseColorAndSortingOrder()
        {
            GameObject gameObject = CreateGameObject("piece");
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.color = new Color(0.7f, 0.8f, 0.9f, 1f);
            renderer.sortingOrder = 4;
            KlotskiPieceView view = gameObject.AddComponent<KlotskiPieceView>();
            view.Configure("piece", renderer);
            Color selectedColor = new Color(1f, 0.9f, 0.6f, 1f);

            view.SetSelected(true, selectedColor, 20);

            Assert.That(renderer.color, Is.EqualTo(selectedColor));
            Assert.That(renderer.sortingOrder, Is.EqualTo(24));

            view.SetSelected(false, selectedColor, 20);

            Assert.That(renderer.color, Is.EqualTo(new Color(0.7f, 0.8f, 0.9f, 1f)));
            Assert.That(renderer.sortingOrder, Is.EqualTo(4));
        }

        [Test]
        public void Configure_WhenRendererBelongsToAnotherObject_ThrowsArgumentException()
        {
            GameObject viewObject = CreateGameObject("view");
            viewObject.AddComponent<SpriteRenderer>();
            KlotskiPieceView view = viewObject.AddComponent<KlotskiPieceView>();
            GameObject otherObject = CreateGameObject("other");
            SpriteRenderer otherRenderer = otherObject.AddComponent<SpriteRenderer>();

            Assert.Throws<ArgumentException>(() => view.Configure("piece", otherRenderer));
        }

        private KlotskiPieceView CreateView(
            string pieceId,
            int textureWidth,
            int textureHeight,
            float pixelsPerUnit,
            Vector4 border)
        {
            GameObject gameObject = CreateGameObject(pieceId);
            Texture2D texture = new Texture2D(textureWidth, textureHeight);
            _createdObjects.Add(texture);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                border);
            _createdObjects.Add(sprite);
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            KlotskiPieceView view = gameObject.AddComponent<KlotskiPieceView>();
            view.Configure(pieceId, renderer);
            return view;
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new GameObject(objectName);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void AssertVector3(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance), "X");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance), "Y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tolerance), "Z");
        }
    }
}
