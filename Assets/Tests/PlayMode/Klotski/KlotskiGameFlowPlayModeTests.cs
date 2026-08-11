using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NanokaGame.Games.Klotski;
using NanokaGame.Games.Klotski.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NanokaGame.Tests.PlayMode.Klotski
{
    public sealed class KlotskiGameFlowPlayModeTests
    {
        private const int PointerId = 41;
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
        public IEnumerator InitializeGame_WhenHudIsBound_ShowsZeroProgressAndReadyState()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;

            Assert.That(fixture.Controller.State, Is.EqualTo(KlotskiGameState.Ready));
            Assert.That(fixture.Controller.MoveCount, Is.Zero);
            Assert.That(fixture.Controller.ElapsedTimeSeconds, Is.Zero);
            Assert.That(fixture.StepText.text, Is.EqualTo("0"));
            Assert.That(fixture.TimeText.text, Is.EqualTo("00:00"));
            Assert.That(fixture.CompletionRoot.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ReleaseDrag_WhenMoveIsLegal_IncrementsStepAndStartsTimer()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;

            KlotskiPieceView view = fixture.FindView("nanoka_body_left");
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, PointerId, startPosition);
            KlotskiMoveResult result = fixture.Controller.ReleaseDrag(
                PointerId,
                startPosition + Vector3.down * 0.8f);

            Assert.That(result, Is.EqualTo(KlotskiMoveResult.Success));
            Assert.That(fixture.Controller.MoveCount, Is.EqualTo(1));
            Assert.That(fixture.StepText.text, Is.EqualTo("1"));
            Assert.That(fixture.Controller.State, Is.EqualTo(KlotskiGameState.Ready));

            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(fixture.Controller.ElapsedTimeSeconds, Is.GreaterThan(0d));
        }

        [UnityTest]
        public IEnumerator ReleaseDrag_WhenMoveIsInvalid_DoesNotIncrementStepOrStartTimer()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;

            KlotskiPieceView view = fixture.FindView("nanoka_body_left");
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, PointerId, startPosition);
            KlotskiMoveResult result = fixture.Controller.ReleaseDrag(
                PointerId,
                startPosition + Vector3.down * 0.4f);

            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(result, Is.EqualTo(KlotskiMoveResult.InvalidDistance));
            Assert.That(fixture.Controller.MoveCount, Is.Zero);
            Assert.That(fixture.Controller.ElapsedTimeSeconds, Is.Zero);
            Assert.That(fixture.StepText.text, Is.EqualTo("0"));
        }

        [UnityTest]
        public IEnumerator ResetGame_AfterLegalMove_RestoresBoardProgressAndHud()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;

            KlotskiPieceView view = fixture.FindView("nanoka_body_left");
            Vector3 startPosition = view.transform.position;
            fixture.Controller.TryBeginDrag(view, PointerId, startPosition);
            fixture.Controller.ReleaseDrag(PointerId, startPosition + Vector3.down * 0.8f);
            yield return new WaitForSecondsRealtime(0.03f);

            fixture.Controller.ResetGame();

            KlotskiPieceState piece;
            Assert.That(fixture.Controller.Model.TryGetPiece(view.PieceId, out piece), Is.True);
            Assert.That(piece.Cell, Is.EqualTo(new Vector2Int(1, 1)));
            Assert.That(fixture.Controller.State, Is.EqualTo(KlotskiGameState.Ready));
            Assert.That(fixture.Controller.MoveCount, Is.Zero);
            Assert.That(fixture.Controller.ElapsedTimeSeconds, Is.Zero);
            Assert.That(fixture.StepText.text, Is.EqualTo("0"));
            Assert.That(fixture.TimeText.text, Is.EqualTo("00:00"));
            Assert.That(fixture.CompletionRoot.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ReleaseDrag_WhenTargetReachesExit_LocksInputAndShowsCompletion()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;
            fixture.UseNearVictoryModel();

            KlotskiPieceView targetView = fixture.FindView("nanoka_head");
            Vector3 startPosition = targetView.transform.position;
            fixture.Controller.TryBeginDrag(targetView, PointerId, startPosition);
            KlotskiMoveResult result = fixture.Controller.ReleaseDrag(
                PointerId,
                startPosition + Vector3.down * 0.8f);

            Assert.That(result, Is.EqualTo(KlotskiMoveResult.Success));
            Assert.That(fixture.Controller.Model.IsCompleted, Is.True);
            Assert.That(fixture.Controller.State, Is.EqualTo(KlotskiGameState.Completed));
            Assert.That(fixture.Controller.CanAcceptInput, Is.False);
            Assert.That(fixture.Controller.MoveCount, Is.EqualTo(1));
            Assert.That(fixture.CompletionRoot.activeSelf, Is.True);
            Assert.That(fixture.CompletionStepText.text, Is.EqualTo("步数：1"));
            Assert.That(fixture.CompletionTimeText.text, Does.StartWith("用时："));
            Assert.That(
                fixture.Controller.TryBeginDrag(targetView, PointerId + 1, targetView.transform.position),
                Is.False);
        }

        private FlowFixture CreateFixture()
        {
            GameObject root = new GameObject("Klotski Flow Fixture");
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

            TMP_Text stepText = CreateText(root.transform, "Step Text");
            TMP_Text timeText = CreateText(root.transform, "Time Text");
            TMP_Text completionTimeText = CreateText(root.transform, "Completion Time Text");
            TMP_Text completionStepText = CreateText(root.transform, "Completion Step Text");
            Button exitButton = CreateButton(root.transform, "Exit Button");
            Button restartButton = CreateButton(root.transform, "Restart Button");
            Button completionRestartButton = CreateButton(root.transform, "Completion Restart Button");
            Button completionTitleButton = CreateButton(root.transform, "Completion Title Button");
            GameObject completionRoot = new GameObject("Root_Complete");
            completionRoot.transform.SetParent(root.transform, false);
            completionRoot.SetActive(false);

            KlotskiBoardView boardView = root.AddComponent<KlotskiBoardView>();
            boardView.Configure(views, "Default", 0);
            KlotskiGameController controller = root.AddComponent<KlotskiGameController>();
            controller.Configure(boardView, anchorRenderer, 1.68f, 0f, 0f, false);
            controller.ConfigureAnimationDurations(0f, 0f);
            KlotskiHudView hudView = root.AddComponent<KlotskiHudView>();
            hudView.Configure(
                controller,
                stepText,
                timeText,
                exitButton,
                restartButton,
                completionRoot,
                completionTimeText,
                completionStepText,
                completionRestartButton,
                completionTitleButton);
            controller.ConfigureHud(hudView);
            root.SetActive(true);
            controller.InitializeGame();

            return new FlowFixture(
                controller,
                boardView,
                views,
                stepText,
                timeText,
                completionRoot,
                completionTimeText,
                completionStepText);
        }

        private static TMP_Text CreateText(Transform parent, string name)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            return textObject.AddComponent<TextMeshProUGUI>();
        }

        private static Button CreateButton(Transform parent, string name)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            return buttonObject.AddComponent<Button>();
        }

        private sealed class FlowFixture
        {
            public FlowFixture(
                KlotskiGameController controller,
                KlotskiBoardView boardView,
                List<KlotskiPieceView> views,
                TMP_Text stepText,
                TMP_Text timeText,
                GameObject completionRoot,
                TMP_Text completionTimeText,
                TMP_Text completionStepText)
            {
                Controller = controller;
                BoardView = boardView;
                Views = views;
                StepText = stepText;
                TimeText = timeText;
                CompletionRoot = completionRoot;
                CompletionTimeText = completionTimeText;
                CompletionStepText = completionStepText;
            }

            public KlotskiGameController Controller { get; }

            public KlotskiBoardView BoardView { get; }

            public List<KlotskiPieceView> Views { get; }

            public TMP_Text StepText { get; }

            public TMP_Text TimeText { get; }

            public GameObject CompletionRoot { get; }

            public TMP_Text CompletionTimeText { get; }

            public TMP_Text CompletionStepText { get; }

            public KlotskiPieceView FindView(string pieceId)
            {
                for (int viewIndex = 0; viewIndex < Views.Count; viewIndex++)
                {
                    if (Views[viewIndex].PieceId == pieceId)
                    {
                        return Views[viewIndex];
                    }
                }

                return null;
            }

            public void UseNearVictoryModel()
            {
                KlotskiPieceState[] pieces =
                {
                    new KlotskiPieceState("nanoka_head", KlotskiPieceType.Target, new Vector2Int(1, 1), new Vector2Int(2, 2)),
                    new KlotskiPieceState("nanoka_body", KlotskiPieceType.Horizontal, new Vector2Int(1, 3), new Vector2Int(2, 1)),
                    new KlotskiPieceState("nanoka_left_arm", KlotskiPieceType.Vertical, new Vector2Int(0, 0), new Vector2Int(1, 2)),
                    new KlotskiPieceState("nanoka_right_arm", KlotskiPieceType.Vertical, new Vector2Int(3, 0), new Vector2Int(1, 2)),
                    new KlotskiPieceState("nanoka_left_leg", KlotskiPieceType.Vertical, new Vector2Int(0, 2), new Vector2Int(1, 2)),
                    new KlotskiPieceState("nanoka_right_leg", KlotskiPieceType.Vertical, new Vector2Int(3, 2), new Vector2Int(1, 2)),
                    new KlotskiPieceState("nanoka_body_left", KlotskiPieceType.Soldier, new Vector2Int(0, 4), Vector2Int.one),
                    new KlotskiPieceState("nanoka_body_right", KlotskiPieceType.Soldier, new Vector2Int(1, 4), Vector2Int.one),
                    new KlotskiPieceState("nanoka_skirt_left", KlotskiPieceType.Soldier, new Vector2Int(2, 4), Vector2Int.one),
                    new KlotskiPieceState("nanoka_skirt_right", KlotskiPieceType.Soldier, new Vector2Int(3, 4), Vector2Int.one)
                };
                KlotskiBoardModel model = new KlotskiBoardModel(
                    new KlotskiLevelDefinition(4, 5, new Vector2Int(1, 0), pieces));
                FieldInfo modelField = typeof(KlotskiGameController).GetField(
                    "_model",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(modelField, Is.Not.Null);
                modelField.SetValue(Controller, model);
                BoardView.Initialize(model, Controller.Layout);
                Controller.SyncAllViews();
            }
        }
    }
}
