using NanokaGame.Games.Jigsaw;
using NUnit.Framework;
using UnityEngine;

namespace NanokaGame.Tests.EditMode.Jigsaw
{
    public sealed class JigsawBoardLayoutTests
    {
        [Test]
        public void CalculateBoardSize_WhenSourceIsWiderThanArea_FitsWidthAndPreservesAspect()
        {
            Vector2 boardSize = JigsawBoardLayout.CalculateBoardSize(
                new RectInt(0, 0, 400, 100),
                new Vector2(8f, 4f));

            Assert.That(boardSize, Is.EqualTo(new Vector2(8f, 2f)));
        }

        [Test]
        public void CalculateBoardSize_WhenSourceIsTallerThanArea_FitsHeightAndPreservesAspect()
        {
            Vector2 boardSize = JigsawBoardLayout.CalculateBoardSize(
                new RectInt(0, 0, 100, 200),
                new Vector2(8f, 4f));

            Assert.That(boardSize, Is.EqualTo(new Vector2(2f, 4f)));
        }

        [Test]
        public void Create_WhenUsingFourByTwoSlices_CalculatesScaleAndSlotCenters()
        {
            Texture2D texture = new Texture2D(400, 200, TextureFormat.RGBA32, false);
            Sprite sourceSprite = null;

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
                RectInt[] sliceRects = JigsawSpriteSlicer.CalculateSliceRects(
                    new RectInt(0, 0, texture.width, texture.height),
                    4,
                    2);

                JigsawBoardLayout layout = JigsawBoardLayout.Create(
                    sourceSprite,
                    sliceRects,
                    new Vector3(1f, 2f, 0f),
                    new Vector2(8f, 4f),
                    3f);

                Assert.That(layout.BoardSize, Is.EqualTo(new Vector2(8f, 4f)));
                Assert.That(layout.PieceWorldScale, Is.EqualTo(2f));
                Assert.That(layout.SlotCount, Is.EqualTo(8));
                Assert.That(layout.GetSlotWorldPosition(0), Is.EqualTo(new Vector3(-2f, 3f, 3f)));
                Assert.That(layout.GetSlotWorldPosition(3), Is.EqualTo(new Vector3(4f, 3f, 3f)));
                Assert.That(layout.GetSlotWorldPosition(4), Is.EqualTo(new Vector3(-2f, 1f, 3f)));
                Assert.That(layout.GetSlotWorldPosition(7), Is.EqualTo(new Vector3(4f, 1f, 3f)));
            }
            finally
            {
                Object.DestroyImmediate(sourceSprite);
                Object.DestroyImmediate(texture);
            }
        }
    }
}
