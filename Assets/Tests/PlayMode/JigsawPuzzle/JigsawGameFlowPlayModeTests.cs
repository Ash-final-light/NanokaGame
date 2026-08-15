using System.Collections;
using System.Collections.Generic;
using NanokaGame.Games.Jigsaw;
using NanokaGame.Games.Jigsaw.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NanokaGame.Tests.PlayMode.Jigsaw
{
    public sealed class JigsawGameFlowPlayModeTests
    {
        private const int PointerId = 47;
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
        public IEnumerator Start_WhenDifficultySelectionIsEnabled_ShowsPreviewAndSelectionUi()
        {
            FlowFixture fixture = CreateFixture();

            yield return null;

            Assert.That(fixture.Controller.State, Is.EqualTo(JigsawGameState.SelectingDifficulty));
            Assert.That(fixture.Controller.CanAcceptInput, Is.False);
            Assert.That(fixture.Controller.PieceViewCount, Is.Zero);
            Assert.That(fixture.SourceRenderer.enabled, Is.True);
            Assert.That(fixture.DifficultyRoot.activeSelf, Is.True);
            Assert.That(fixture.GameplayHudRoot.activeSelf, Is.False);
            Assert.That(fixture.CompletionRoot.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator StartGame_WhenUsingSupportedDifficulties_CreatesExpectedPieceCounts()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;

            AssertDifficulty(fixture, JigsawDifficulty.Easy, 4, 2, 8);
            AssertDifficulty(fixture, JigsawDifficulty.Normal, 6, 3, 18);
            AssertDifficulty(fixture, JigsawDifficulty.Hard, 8, 4, 32);
        }

        [UnityTest]
        public IEnumerator DifficultyButtons_WhenHardAndStartAreClicked_StartsHardPuzzle()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;

            fixture.HardButton.onClick.Invoke();

            Assert.That(fixture.UiController.SelectedDifficulty, Is.EqualTo(JigsawDifficulty.Hard));
            Assert.That(fixture.HardButton.interactable, Is.False);

            fixture.StartButton.onClick.Invoke();

            Assert.That(fixture.Controller.State, Is.EqualTo(JigsawGameState.Playing));
            Assert.That(fixture.Controller.CurrentDifficulty, Is.EqualTo(JigsawDifficulty.Hard));
            Assert.That(fixture.Controller.PieceViewCount, Is.EqualTo(32));
            Assert.That(fixture.DifficultyRoot.activeSelf, Is.False);
            Assert.That(fixture.GameplayHudRoot.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator StartGame_WhilePlaying_AdvancesElapsedTime()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;

            fixture.Controller.StartGame(JigsawDifficulty.Easy);
            double initialElapsedTime = fixture.Controller.ElapsedTimeSeconds;

            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(fixture.Controller.ElapsedTimeSeconds, Is.GreaterThan(initialElapsedTime));
            Assert.That(fixture.TimeText.text, Does.StartWith("时间  "));
        }

        [UnityTest]
        public IEnumerator ReleaseDrag_WhenTargetIsAnotherPiece_SwapsAndIncrementsMoveCount()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;
            fixture.Controller.StartGame(JigsawDifficulty.Easy);

            JigsawPieceView firstPiece = fixture.BoardView.GetPieceView(0);
            JigsawPieceView secondPiece = fixture.BoardView.GetPieceView(1);
            int firstSlot = firstPiece.CurrentSlotIndex;
            int secondSlot = secondPiece.CurrentSlotIndex;
            PointerEventData eventData = fixture.CreatePointerEvent(firstPiece, PointerId);

            Assert.That(fixture.Controller.TryBeginDrag(firstPiece, eventData), Is.True);
            Assert.That(fixture.Controller.ReleaseDrag(firstPiece, secondPiece, eventData), Is.True);

            Assert.That(fixture.Controller.MoveCount, Is.EqualTo(1));
            Assert.That(firstPiece.CurrentSlotIndex, Is.EqualTo(secondSlot));
            Assert.That(secondPiece.CurrentSlotIndex, Is.EqualTo(firstSlot));
            Assert.That(fixture.Controller.State, Is.EqualTo(JigsawGameState.Playing));
            Assert.That(fixture.MoveCountText.text, Is.EqualTo("步数  1"));
        }

        [UnityTest]
        public IEnumerator ReleaseDrag_WhenThereIsNoTarget_ReturnsPieceWithoutCountingMove()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;
            fixture.Controller.StartGame(JigsawDifficulty.Easy);

            JigsawPieceView piece = fixture.BoardView.GetPieceView(0);
            int originalSlot = piece.CurrentSlotIndex;
            Vector3 originalPosition = fixture.BoardView.GetSlotWorldPosition(originalSlot);
            PointerEventData eventData = fixture.CreatePointerEvent(piece, PointerId);

            Assert.That(fixture.Controller.TryBeginDrag(piece, eventData), Is.True);
            Assert.That(fixture.Controller.ReleaseDrag(piece, null, eventData), Is.False);

            Assert.That(fixture.Controller.MoveCount, Is.Zero);
            Assert.That(piece.CurrentSlotIndex, Is.EqualTo(originalSlot));
            Assert.That(piece.transform.position, Is.EqualTo(originalPosition));
            Assert.That(fixture.Controller.State, Is.EqualTo(JigsawGameState.Playing));
        }

        [UnityTest]
        public IEnumerator ReleaseDrag_DuringSwap_LocksAdditionalInputUntilAnimationCompletes()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;
            fixture.Controller.ConfigureAnimation(0.08f, 0f);
            fixture.Controller.StartGame(JigsawDifficulty.Easy);

            JigsawPieceView firstPiece = fixture.BoardView.GetPieceView(0);
            JigsawPieceView secondPiece = fixture.BoardView.GetPieceView(1);
            JigsawPieceView thirdPiece = fixture.BoardView.GetPieceView(2);
            PointerEventData firstEvent = fixture.CreatePointerEvent(firstPiece, PointerId);

            Assert.That(fixture.Controller.TryBeginDrag(firstPiece, firstEvent), Is.True);
            Assert.That(fixture.Controller.ReleaseDrag(firstPiece, secondPiece, firstEvent), Is.True);
            Assert.That(fixture.Controller.State, Is.EqualTo(JigsawGameState.Swapping));
            Assert.That(fixture.Controller.CanAcceptInput, Is.False);
            Assert.That(
                fixture.Controller.TryBeginDrag(
                    thirdPiece,
                    fixture.CreatePointerEvent(thirdPiece, PointerId + 1)),
                Is.False);

            yield return new WaitForSecondsRealtime(0.12f);

            Assert.That(fixture.Controller.State, Is.EqualTo(JigsawGameState.Playing));
            Assert.That(fixture.Controller.CanAcceptInput, Is.True);
        }

        [UnityTest]
        public IEnumerator RestartGame_AfterHardPuzzle_RebuildsOnlyCurrentDifficultyPieces()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;
            fixture.Controller.StartGame(JigsawDifficulty.Hard);

            fixture.Controller.RestartGame();
            yield return null;

            Assert.That(fixture.Controller.CurrentDifficulty, Is.EqualTo(JigsawDifficulty.Hard));
            Assert.That(fixture.Controller.State, Is.EqualTo(JigsawGameState.Playing));
            Assert.That(fixture.Controller.MoveCount, Is.Zero);
            Assert.That(fixture.Controller.PieceViewCount, Is.EqualTo(32));
            Assert.That(fixture.PiecesRoot.childCount, Is.EqualTo(32));
            Assert.That(
                CountActiveChildren(fixture.PiecesRoot),
                Is.EqualTo(32),
                "Restart should not leave active pieces from the previous board.");
        }

        [UnityTest]
        public IEnumerator ReleaseDrag_WhenFinalPairIsRestored_CompletesAndStopsTimer()
        {
            FlowFixture fixture = CreateFixture();
            yield return null;
            fixture.Controller.StartGame(JigsawDifficulty.Easy);
            yield return new WaitForSecondsRealtime(0.03f);

            JigsawBoardState boardState = fixture.Controller.BoardState;
            boardState.ResetSolved();
            boardState.SwapSlots(0, 1);
            fixture.BoardView.SyncAll(boardState);

            JigsawPieceView firstPiece = fixture.BoardView.GetPieceView(0);
            JigsawPieceView secondPiece = fixture.BoardView.GetPieceView(1);
            PointerEventData eventData = fixture.CreatePointerEvent(firstPiece, PointerId);
            Assert.That(fixture.Controller.TryBeginDrag(firstPiece, eventData), Is.True);

            Assert.That(fixture.Controller.ReleaseDrag(firstPiece, secondPiece, eventData), Is.True);
            double completedTime = fixture.Controller.ElapsedTimeSeconds;

            Assert.That(boardState.IsCompleted, Is.True);
            Assert.That(fixture.Controller.State, Is.EqualTo(JigsawGameState.Completed));
            Assert.That(fixture.Controller.CanAcceptInput, Is.False);
            Assert.That(fixture.Controller.MoveCount, Is.EqualTo(1));
            Assert.That(fixture.CompletionRoot.activeSelf, Is.True);
            Assert.That(fixture.CompletionTimeText.text, Does.StartWith("用时  "));
            Assert.That(fixture.CompletionMoveCountText.text, Is.EqualTo("步数  1"));

            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(fixture.Controller.ElapsedTimeSeconds, Is.EqualTo(completedTime).Within(0.001d));
        }

        private FlowFixture CreateFixture()
        {
            Texture2D texture = new Texture2D(80, 40, TextureFormat.RGBA32, false);
            _createdObjects.Add(texture);
            Sprite sourceSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                10f);
            _createdObjects.Add(sourceSprite);

            GameObject root = new GameObject("Jigsaw Flow Fixture");
            root.SetActive(false);
            _createdObjects.Add(root);

            GameObject cameraObject = new GameObject("Input Camera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera inputCamera = cameraObject.AddComponent<Camera>();
            inputCamera.orthographic = true;
            inputCamera.orthographicSize = 5f;

            GameObject eventSystemObject = new GameObject("Event System");
            eventSystemObject.transform.SetParent(root.transform, false);
            EventSystem eventSystem = eventSystemObject.AddComponent<EventSystem>();

            GameObject sourceObject = new GameObject("Source");
            sourceObject.transform.SetParent(root.transform, false);
            SpriteRenderer sourceRenderer = sourceObject.AddComponent<SpriteRenderer>();
            sourceRenderer.sprite = sourceSprite;

            GameObject piecesRootObject = new GameObject("Pieces Root");
            piecesRootObject.transform.SetParent(root.transform, false);

            GameObject pieceTemplateObject = new GameObject("Piece Template");
            pieceTemplateObject.transform.SetParent(root.transform, false);
            pieceTemplateObject.AddComponent<SpriteRenderer>();
            pieceTemplateObject.AddComponent<BoxCollider2D>();
            JigsawPieceView pieceTemplate = pieceTemplateObject.AddComponent<JigsawPieceView>();

            GameObject difficultyRoot = new GameObject("Difficulty Root");
            difficultyRoot.transform.SetParent(root.transform, false);
            Button easyButton = CreateButton(difficultyRoot.transform, "Easy Button");
            Button normalButton = CreateButton(difficultyRoot.transform, "Normal Button");
            Button hardButton = CreateButton(difficultyRoot.transform, "Hard Button");
            Button startButton = CreateButton(difficultyRoot.transform, "Start Button");

            GameObject gameplayHudRoot = new GameObject("Gameplay HUD");
            gameplayHudRoot.transform.SetParent(root.transform, false);
            gameplayHudRoot.SetActive(false);
            TMP_Text timeText = CreateText(gameplayHudRoot.transform, "Time Text");
            TMP_Text moveCountText = CreateText(gameplayHudRoot.transform, "Move Count Text");
            Button exitButton = CreateButton(gameplayHudRoot.transform, "Exit Button");
            Button restartButton = CreateButton(gameplayHudRoot.transform, "Restart Button");

            GameObject completionRoot = new GameObject("Completion Root");
            completionRoot.transform.SetParent(root.transform, false);
            completionRoot.SetActive(false);
            TMP_Text completionTimeText = CreateText(completionRoot.transform, "Completion Time Text");
            TMP_Text completionMoveCountText = CreateText(completionRoot.transform, "Completion Move Count Text");
            Button completionRestartButton = CreateButton(completionRoot.transform, "Completion Restart Button");
            Button completionTitleButton = CreateButton(completionRoot.transform, "Completion Title Button");

            JigsawBoardView boardView = root.AddComponent<JigsawBoardView>();
            boardView.Configure(
                sourceRenderer,
                piecesRootObject.transform,
                pieceTemplate,
                new Vector2(8f, 4f),
                0f,
                0);

            JigsawPuzzleController controller = root.AddComponent<JigsawPuzzleController>();
            JigsawUiController uiController = root.AddComponent<JigsawUiController>();
            controller.Configure(boardView, inputCamera, uiController, true);
            controller.ConfigureAnimation(0f, 0f);
            controller.SetRandomSeed(20260815);
            uiController.Configure(
                controller,
                difficultyRoot,
                easyButton,
                normalButton,
                hardButton,
                startButton,
                gameplayHudRoot,
                timeText,
                moveCountText,
                exitButton,
                restartButton,
                completionRoot,
                completionTimeText,
                completionMoveCountText,
                completionRestartButton,
                completionTitleButton);

            root.SetActive(true);

            return new FlowFixture(
                controller,
                boardView,
                uiController,
                inputCamera,
                eventSystem,
                sourceRenderer,
                piecesRootObject.transform,
                difficultyRoot,
                gameplayHudRoot,
                completionRoot,
                timeText,
                moveCountText,
                completionTimeText,
                completionMoveCountText,
                hardButton,
                startButton);
        }

        private static void AssertDifficulty(
            FlowFixture fixture,
            JigsawDifficulty difficulty,
            int expectedColumns,
            int expectedRows,
            int expectedPieceCount)
        {
            Assert.That(fixture.Controller.StartGame(difficulty), Is.True);
            Assert.That(fixture.Controller.CurrentDifficulty, Is.EqualTo(difficulty));
            Assert.That(fixture.Controller.BoardState.Columns, Is.EqualTo(expectedColumns));
            Assert.That(fixture.Controller.BoardState.Rows, Is.EqualTo(expectedRows));
            Assert.That(fixture.Controller.BoardState.PieceCount, Is.EqualTo(expectedPieceCount));
            Assert.That(fixture.Controller.PieceViewCount, Is.EqualTo(expectedPieceCount));
            Assert.That(fixture.Controller.BoardState.IsCompleted, Is.False);
        }

        private static TMP_Text CreateText(Transform parent, string name)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            return textObject.AddComponent<TextMeshProUGUI>();
        }

        private static Button CreateButton(Transform parent, string name)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            return buttonObject.GetComponent<Button>();
        }

        private static int CountActiveChildren(Transform parent)
        {
            int activeCount = 0;
            for (int childIndex = 0; childIndex < parent.childCount; childIndex++)
            {
                if (parent.GetChild(childIndex).gameObject.activeSelf)
                {
                    activeCount++;
                }
            }

            return activeCount;
        }

        private sealed class FlowFixture
        {
            public FlowFixture(
                JigsawPuzzleController controller,
                JigsawBoardView boardView,
                JigsawUiController uiController,
                Camera inputCamera,
                EventSystem eventSystem,
                SpriteRenderer sourceRenderer,
                Transform piecesRoot,
                GameObject difficultyRoot,
                GameObject gameplayHudRoot,
                GameObject completionRoot,
                TMP_Text timeText,
                TMP_Text moveCountText,
                TMP_Text completionTimeText,
                TMP_Text completionMoveCountText,
                Button hardButton,
                Button startButton)
            {
                Controller = controller;
                BoardView = boardView;
                UiController = uiController;
                InputCamera = inputCamera;
                EventSystem = eventSystem;
                SourceRenderer = sourceRenderer;
                PiecesRoot = piecesRoot;
                DifficultyRoot = difficultyRoot;
                GameplayHudRoot = gameplayHudRoot;
                CompletionRoot = completionRoot;
                TimeText = timeText;
                MoveCountText = moveCountText;
                CompletionTimeText = completionTimeText;
                CompletionMoveCountText = completionMoveCountText;
                HardButton = hardButton;
                StartButton = startButton;
            }

            public JigsawPuzzleController Controller { get; }

            public JigsawBoardView BoardView { get; }

            public JigsawUiController UiController { get; }

            public Camera InputCamera { get; }

            public EventSystem EventSystem { get; }

            public SpriteRenderer SourceRenderer { get; }

            public Transform PiecesRoot { get; }

            public GameObject DifficultyRoot { get; }

            public GameObject GameplayHudRoot { get; }

            public GameObject CompletionRoot { get; }

            public TMP_Text TimeText { get; }

            public TMP_Text MoveCountText { get; }

            public TMP_Text CompletionTimeText { get; }

            public TMP_Text CompletionMoveCountText { get; }

            public Button HardButton { get; }

            public Button StartButton { get; }

            public PointerEventData CreatePointerEvent(JigsawPieceView pieceView, int pointerId)
            {
                Vector3 screenPosition = InputCamera.WorldToScreenPoint(pieceView.transform.position);
                return new PointerEventData(EventSystem)
                {
                    pointerId = pointerId,
                    button = PointerEventData.InputButton.Left,
                    position = screenPosition
                };
            }
        }
    }
}
