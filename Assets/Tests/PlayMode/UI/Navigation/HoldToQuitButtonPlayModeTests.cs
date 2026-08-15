using System;
using System.Collections;
using System.Collections.Generic;
using NanokaGame.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NanokaGame.Tests.PlayMode.UI
{
    public sealed class HoldToQuitButtonPlayModeTests
    {
        private const float ShortHoldDuration = 0.08f;
        private const float ShortQuitDelay = 0.05f;

        private readonly List<GameObject> _createdObjects = new List<GameObject>();

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

        [Test]
        public void Configure_WhenRequiredValueIsInvalid_ThrowsArgumentException()
        {
            HoldFixture fixture = CreateFixture();

            Assert.Throws<ArgumentNullException>(
                () => fixture.Controller.Configure(
                    null,
                    fixture.CheckObject,
                    ShortHoldDuration,
                    ShortQuitDelay,
                    false));
            Assert.Throws<ArgumentNullException>(
                () => fixture.Controller.Configure(
                    fixture.ProgressImage,
                    null,
                    ShortHoldDuration,
                    ShortQuitDelay,
                    false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => fixture.Controller.Configure(
                    fixture.ProgressImage,
                    fixture.CheckObject,
                    0f,
                    ShortQuitDelay,
                    false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => fixture.Controller.Configure(
                    fixture.ProgressImage,
                    fixture.CheckObject,
                    ShortHoldDuration,
                    -0.01f,
                    false));
        }

        [UnityTest]
        public IEnumerator PointerDown_WhenHeld_IncreasesProgress()
        {
            HoldFixture fixture = CreateFixture(0.5f, ShortQuitDelay);

            fixture.Controller.OnPointerDown(null);
            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(fixture.Controller.IsHolding, Is.True);
            Assert.That(fixture.Controller.HoldProgress, Is.GreaterThan(0f));
            Assert.That(fixture.Controller.HoldProgress, Is.LessThan(1f));
            Assert.That(fixture.CheckObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator PointerUp_BeforeCompletion_ResetsProgress()
        {
            HoldFixture fixture = CreateFixture(0.5f, ShortQuitDelay);

            fixture.Controller.OnPointerDown(null);
            yield return new WaitForSecondsRealtime(0.05f);
            fixture.Controller.OnPointerUp(null);

            Assert.That(fixture.Controller.IsHolding, Is.False);
            Assert.That(fixture.Controller.IsCompleted, Is.False);
            Assert.That(fixture.Controller.HoldProgress, Is.Zero);
            Assert.That(fixture.CheckObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator PointerExit_BeforeCompletion_ResetsProgress()
        {
            HoldFixture fixture = CreateFixture(0.5f, ShortQuitDelay);

            fixture.Controller.OnPointerDown(null);
            yield return new WaitForSecondsRealtime(0.05f);
            fixture.Controller.OnPointerExit(null);

            Assert.That(fixture.Controller.IsHolding, Is.False);
            Assert.That(fixture.Controller.IsCompleted, Is.False);
            Assert.That(fixture.Controller.HoldProgress, Is.Zero);
        }

        [UnityTest]
        public IEnumerator HoldUntilComplete_ShowsCheckThenRequestsQuitAfterDelay()
        {
            HoldFixture fixture = CreateFixture(ShortHoldDuration, ShortQuitDelay);

            fixture.Controller.OnPointerDown(null);
            yield return new WaitForSecondsRealtime(ShortHoldDuration + 0.05f);

            Assert.That(fixture.Controller.IsHolding, Is.False);
            Assert.That(fixture.Controller.IsCompleted, Is.True);
            Assert.That(fixture.Controller.HoldProgress, Is.EqualTo(1f));
            Assert.That(fixture.CheckObject.activeSelf, Is.True);
            Assert.That(fixture.Controller.QuitRequested, Is.False);

            yield return new WaitForSecondsRealtime(ShortQuitDelay + 0.03f);

            Assert.That(fixture.Controller.QuitRequested, Is.True);
        }

        [UnityTest]
        public IEnumerator OnDisable_DuringHold_StopsAndResetsInteraction()
        {
            HoldFixture fixture = CreateFixture(0.5f, ShortQuitDelay);

            fixture.Controller.OnPointerDown(null);
            yield return new WaitForSecondsRealtime(0.05f);
            fixture.Root.SetActive(false);
            yield return new WaitForSecondsRealtime(0.1f);

            Assert.That(fixture.Controller.IsHolding, Is.False);
            Assert.That(fixture.Controller.IsCompleted, Is.False);
            Assert.That(fixture.Controller.HoldProgress, Is.Zero);
            Assert.That(fixture.Controller.QuitRequested, Is.False);
            Assert.That(fixture.CheckObject.activeSelf, Is.False);
        }

        private HoldFixture CreateFixture(
            float holdDuration = ShortHoldDuration,
            float quitDelay = ShortQuitDelay)
        {
            GameObject root = new GameObject("Hold To Quit Button");
            root.SetActive(false);
            _createdObjects.Add(root);

            GameObject progressObject = new GameObject("Progress", typeof(RectTransform), typeof(Image));
            progressObject.transform.SetParent(root.transform, false);
            Image progressImage = progressObject.GetComponent<Image>();

            GameObject checkObject = new GameObject("Check");
            checkObject.transform.SetParent(root.transform, false);

            HoldToQuitButton controller = root.AddComponent<HoldToQuitButton>();
            controller.Configure(
                progressImage,
                checkObject,
                holdDuration,
                quitDelay,
                false);
            root.SetActive(true);

            return new HoldFixture(root, controller, progressImage, checkObject);
        }

        private sealed class HoldFixture
        {
            public HoldFixture(
                GameObject root,
                HoldToQuitButton controller,
                Image progressImage,
                GameObject checkObject)
            {
                Root = root;
                Controller = controller;
                ProgressImage = progressImage;
                CheckObject = checkObject;
            }

            public GameObject Root { get; }

            public HoldToQuitButton Controller { get; }

            public Image ProgressImage { get; }

            public GameObject CheckObject { get; }
        }
    }
}
