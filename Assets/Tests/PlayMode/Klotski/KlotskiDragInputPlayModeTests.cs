using System.Collections;
using System.Collections.Generic;
using NanokaGame.Games.Klotski;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace NanokaGame.Tests.PlayMode.Klotski
{
    public sealed class KlotskiDragInputPlayModeTests
    {
        private const float Tolerance = 0.0001f;
        private const float AnimationDuration = 0.05f;
        private const int TestPointerId = 17;
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
        public IEnumerator PointerEvents_WithMousePointerId_PreviewWithoutModelChangeThenCommitLegalMove()
        {
            return VerifyPointerDrag(-1);
        }

        [UnityTest]
        public IEnumerator PointerEvents_WithTouchPointerId_PreviewWithoutModelChangeThenCommitLegalMove()
        {
            return VerifyPointerDrag(5);
        }

        private IEnumerator VerifyPointerDrag(int pointerId)
        {
            Camera inputCamera = CreateInputCamera();
            EventSystem eventSystem = CreateEventSystem();
            DragFixture fixture = CreateFixture(inputCamera);
            yield return null;

            KlotskiPieceView view = fixture.BodyLeftView;
            Vector3 startPosition = view.transform.position;
            Vector3 grabPosition = startPosition + new Vector3(0.25f, 0.1f, 0f);
            PointerEventData pointer = new PointerEventData(eventSystem)
            {
                pointerId = pointerId,
                button = PointerEventData.InputButton.Left,
                position = inputCamera.WorldToScreenPoint(grabPosition)
            };

            ExecuteEvents.Execute(view.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            pointer.position = inputCamera.WorldToScreenPoint(grabPosition + Vector3.down * 0.8f);
            ExecuteEvents.Execute(view.gameObject, pointer, ExecuteEvents.dragHandler);

            KlotskiPieceState piece;
            Assert.That(fixture.Controller.Model.TryGetPiece("nanoka_body_left", out piece), Is.True);
            Assert.That(piece.Cell, Is.EqualTo(new Vector2Int(1, 1)), "Drag preview must not modify Model state.");
            Assert.That(view.transform.position.y, Is.LessThan(startPosition.y));
            Assert.That(view.transform.position.x, Is.EqualTo(startPosition.x).Within(Tolerance));

            ExecuteEvents.Execute(view.gameObject, pointer, ExecuteEvents.pointerUpHandler);

            Assert.That(piece.Cell, Is.EqualTo(new Vector2Int(1, 0)));
            Assert.That(fixture.Controller.IsDragging, Is.False);
            Assert.That(fixture.Controller.IsMoving, Is.True);
            Assert.That(fixture.Controller.CanAcceptInput, Is.False);
            Vector3 expectedPosition = fixture.Controller.Layout.GetPieceWorldPosition(
                piece.Cell,
                piece.SizeInCells);
            yield return new WaitForSeconds(AnimationDuration + 0.05f);

            Assert.That(fixture.Controller.IsMoving, Is.False);
            Assert.That(view.transform.position.x, Is.EqualTo(expectedPosition.x).Within(Tolerance));
            Assert.That(view.transform.position.y, Is.EqualTo(expectedPosition.y).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator TryBeginDrag_WhileMoveTweenIsActive_ReturnsFalse()
        {
            DragFixture fixture = CreateFixture(null);
            yield return null;

            StartLegalMove(fixture);

            Assert.That(fixture.Controller.IsMoving, Is.True);
            Assert.That(
                fixture.Controller.TryBeginDrag(
                    fixture.BodyLeftView,
                    TestPointerId + 1,
                    fixture.BodyLeftView.transform.position),
                Is.False);
        }

        [UnityTest]
        public IEnumerator ReleaseDrag_WhenMoveIsInvalid_TweensBackWithoutChangingModel()
        {
            DragFixture fixture = CreateFixture(null);
            yield return null;

            KlotskiPieceView view = fixture.BodyLeftView;
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, TestPointerId, startPosition);

            KlotskiMoveResult result = fixture.Controller.ReleaseDrag(
                TestPointerId,
                startPosition + Vector3.down * 0.4f);

            KlotskiPieceState piece;
            Assert.That(fixture.Controller.Model.TryGetPiece(view.PieceId, out piece), Is.True);
            Assert.That(result, Is.EqualTo(KlotskiMoveResult.InvalidDistance));
            Assert.That(piece.Cell, Is.EqualTo(new Vector2Int(1, 1)));
            Assert.That(fixture.Controller.IsMoving, Is.True);
            Assert.That(view.transform.position.y, Is.LessThan(startPosition.y));

            yield return new WaitForSeconds(AnimationDuration + 0.05f);

            Assert.That(fixture.Controller.IsMoving, Is.False);
            AssertVector3(view.transform.position, startPosition);
        }

        [UnityTest]
        public IEnumerator ResetGame_WhileTweenIsActive_KillsTweenAndRestoresInitialState()
        {
            DragFixture fixture = CreateFixture(null);
            yield return null;

            StartLegalMove(fixture);
            Assert.That(fixture.Controller.IsMoving, Is.True);

            fixture.Controller.ResetGame();

            KlotskiPieceState piece;
            Assert.That(fixture.Controller.Model.TryGetPiece(fixture.BodyLeftView.PieceId, out piece), Is.True);
            Assert.That(fixture.Controller.IsMoving, Is.False);
            Assert.That(piece.Cell, Is.EqualTo(new Vector2Int(1, 1)));
            AssertVector3(
                fixture.BodyLeftView.transform.position,
                fixture.Controller.Layout.GetPieceWorldPosition(piece.Cell, piece.SizeInCells));
        }

        [UnityTest]
        public IEnumerator DisableController_WhileTweenIsActive_KillsTweenAndAlignsViewToModel()
        {
            DragFixture fixture = CreateFixture(null);
            yield return null;

            StartLegalMove(fixture);
            Assert.That(fixture.Controller.IsMoving, Is.True);

            fixture.Controller.enabled = false;

            KlotskiPieceState piece;
            Assert.That(fixture.Controller.Model.TryGetPiece(fixture.BodyLeftView.PieceId, out piece), Is.True);
            Assert.That(fixture.Controller.IsMoving, Is.False);
            Assert.That(piece.Cell, Is.EqualTo(new Vector2Int(1, 0)));
            AssertVector3(
                fixture.BodyLeftView.transform.position,
                fixture.Controller.Layout.GetPieceWorldPosition(piece.Cell, piece.SizeInCells));
        }

        private static void StartLegalMove(DragFixture fixture)
        {
            Vector3 startPosition = fixture.BodyLeftView.transform.position;
            Assert.That(
                fixture.Controller.TryBeginDrag(
                    fixture.BodyLeftView,
                    TestPointerId,
                    startPosition),
                Is.True);

            KlotskiMoveResult result = fixture.Controller.ReleaseDrag(
                TestPointerId,
                startPosition + Vector3.down * 0.8f);

            Assert.That(result, Is.EqualTo(KlotskiMoveResult.Success));
        }

        private static void AssertVector3(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance), "X");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance), "Y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tolerance), "Z");
        }

        private Camera CreateInputCamera()
        {
            GameObject cameraObject = new GameObject("Input Camera");
            _createdObjects.Add(cameraObject);
            Camera inputCamera = cameraObject.AddComponent<Camera>();
            inputCamera.orthographic = true;
            inputCamera.orthographicSize = 6f;
            cameraObject.transform.position = new Vector3(0.06f, 0.12f, -10f);
            cameraObject.AddComponent<Physics2DRaycaster>();
            return inputCamera;
        }

        private EventSystem CreateEventSystem()
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.SetActive(false);
            _createdObjects.Add(eventSystemObject);
            return eventSystemObject.AddComponent<EventSystem>();
        }

        private DragFixture CreateFixture(Camera inputCamera)
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
            KlotskiPieceView bodyLeftView = null;

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
                else if (piece.Id == "nanoka_body_left")
                {
                    bodyLeftView = view;
                }
            }

            KlotskiBoardView boardView = root.AddComponent<KlotskiBoardView>();
            boardView.Configure(views, "Default", 0);
            KlotskiGameController controller = root.AddComponent<KlotskiGameController>();
            controller.Configure(boardView, anchorRenderer, 1.68f, 0f, 0f, true);
            controller.ConfigureAnimationDurations(AnimationDuration, AnimationDuration);
            controller.ConfigureInputCamera(inputCamera);
            root.SetActive(true);

            return new DragFixture(controller, bodyLeftView);
        }

        private sealed class DragFixture
        {
            public DragFixture(KlotskiGameController controller, KlotskiPieceView bodyLeftView)
            {
                Controller = controller;
                BodyLeftView = bodyLeftView;
            }

            public KlotskiGameController Controller { get; }

            public KlotskiPieceView BodyLeftView { get; }
        }
    }
}
