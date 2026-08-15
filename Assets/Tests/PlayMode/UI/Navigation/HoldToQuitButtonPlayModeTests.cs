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
        private const float ShortDecayDuration = 0.12f;
        private const float ShortQuitDelay = 0.05f;

        private readonly List<UnityEngine.Object> _createdObjects =
            new List<UnityEngine.Object>();

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
                    ShortDecayDuration,
                    ShortQuitDelay,
                    fixture.AudioSource,
                    fixture.CompletionClip,
                    false));
            Assert.Throws<ArgumentNullException>(
                () => fixture.Controller.Configure(
                    fixture.ProgressImage,
                    null,
                    ShortHoldDuration,
                    ShortDecayDuration,
                    ShortQuitDelay,
                    fixture.AudioSource,
                    fixture.CompletionClip,
                    false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => fixture.Controller.Configure(
                    fixture.ProgressImage,
                    fixture.CheckObject,
                    0f,
                    ShortDecayDuration,
                    ShortQuitDelay,
                    fixture.AudioSource,
                    fixture.CompletionClip,
                    false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => fixture.Controller.Configure(
                    fixture.ProgressImage,
                    fixture.CheckObject,
                    ShortHoldDuration,
                    0f,
                    ShortQuitDelay,
                    fixture.AudioSource,
                    fixture.CompletionClip,
                    false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => fixture.Controller.Configure(
                    fixture.ProgressImage,
                    fixture.CheckObject,
                    ShortHoldDuration,
                    ShortDecayDuration,
                    -0.01f,
                    fixture.AudioSource,
                    fixture.CompletionClip,
                    false));
            Assert.Throws<ArgumentNullException>(
                () => fixture.Controller.Configure(
                    fixture.ProgressImage,
                    fixture.CheckObject,
                    ShortHoldDuration,
                    ShortDecayDuration,
                    ShortQuitDelay,
                    null,
                    fixture.CompletionClip,
                    false));
            Assert.Throws<ArgumentNullException>(
                () => fixture.Controller.Configure(
                    fixture.ProgressImage,
                    fixture.CheckObject,
                    ShortHoldDuration,
                    ShortDecayDuration,
                    ShortQuitDelay,
                    fixture.AudioSource,
                    null,
                    false));
        }

        [UnityTest]
        public IEnumerator PointerDown_WhenHeld_IncreasesProgress()
        {
            HoldFixture fixture = CreateFixture(0.5f, 0.5f, ShortQuitDelay);

            fixture.Controller.OnPointerDown(null);
            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(fixture.Controller.IsHolding, Is.True);
            Assert.That(fixture.Controller.HoldProgress, Is.GreaterThan(0f));
            Assert.That(fixture.Controller.HoldProgress, Is.LessThan(1f));
            Assert.That(fixture.CheckObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator PointerUp_BeforeCompletion_DecaysProgressGradually()
        {
            HoldFixture fixture = CreateFixture(0.5f, 0.5f, ShortQuitDelay);

            fixture.Controller.OnPointerDown(null);
            yield return new WaitForSecondsRealtime(0.1f);
            fixture.Controller.OnPointerUp(null);
            float progressAtRelease = fixture.Controller.HoldProgress;

            Assert.That(fixture.Controller.IsHolding, Is.False);
            Assert.That(fixture.Controller.IsCompleted, Is.False);
            Assert.That(progressAtRelease, Is.GreaterThan(0f));

            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(fixture.Controller.HoldProgress, Is.GreaterThan(0f));
            Assert.That(fixture.Controller.HoldProgress, Is.LessThan(progressAtRelease));
            Assert.That(fixture.CheckObject.activeSelf, Is.False);

            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(fixture.Controller.HoldProgress, Is.Zero.Within(0.001f));
        }

        [UnityTest]
        public IEnumerator PointerExit_BeforeCompletion_DecaysProgressGradually()
        {
            HoldFixture fixture = CreateFixture(0.5f, 0.5f, ShortQuitDelay);

            fixture.Controller.OnPointerDown(null);
            yield return new WaitForSecondsRealtime(0.1f);
            fixture.Controller.OnPointerExit(null);
            float progressAtExit = fixture.Controller.HoldProgress;

            Assert.That(fixture.Controller.IsHolding, Is.False);
            Assert.That(fixture.Controller.IsCompleted, Is.False);
            Assert.That(progressAtExit, Is.GreaterThan(0f));

            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(fixture.Controller.HoldProgress, Is.GreaterThan(0f));
            Assert.That(fixture.Controller.HoldProgress, Is.LessThan(progressAtExit));
        }

        [UnityTest]
        public IEnumerator PointerDown_DuringDecay_ContinuesFromRemainingProgress()
        {
            HoldFixture fixture = CreateFixture(0.5f, 0.5f, ShortQuitDelay);

            fixture.Controller.OnPointerDown(null);
            yield return new WaitForSecondsRealtime(0.1f);
            fixture.Controller.OnPointerUp(null);
            yield return new WaitForSecondsRealtime(0.05f);
            float progressDuringDecay = fixture.Controller.HoldProgress;

            fixture.Controller.OnPointerDown(null);
            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(fixture.Controller.IsHolding, Is.True);
            Assert.That(fixture.Controller.HoldProgress, Is.GreaterThan(progressDuringDecay));
        }

        [UnityTest]
        public IEnumerator HoldUntilComplete_ShowsCheckThenRequestsQuitAfterDelay()
        {
            HoldFixture fixture = CreateFixture(
                ShortHoldDuration,
                ShortDecayDuration,
                ShortQuitDelay);

            fixture.Controller.OnPointerDown(null);
            yield return new WaitForSecondsRealtime(ShortHoldDuration + 0.05f);

            Assert.That(fixture.Controller.IsHolding, Is.False);
            Assert.That(fixture.Controller.IsCompleted, Is.True);
            Assert.That(fixture.Controller.HoldProgress, Is.EqualTo(1f));
            Assert.That(fixture.CheckObject.activeSelf, Is.True);
            Assert.That(fixture.Controller.CompletionSoundPlayCount, Is.EqualTo(1));
            Assert.That(fixture.AudioSource.isPlaying, Is.True);
            Assert.That(fixture.Controller.QuitRequested, Is.False);

            fixture.Controller.OnPointerDown(null);
            yield return null;

            Assert.That(fixture.Controller.CompletionSoundPlayCount, Is.EqualTo(1));

            yield return new WaitForSecondsRealtime(ShortQuitDelay + 0.03f);

            Assert.That(fixture.Controller.QuitRequested, Is.True);
        }

        [UnityTest]
        public IEnumerator OnDisable_DuringHold_StopsAndResetsInteraction()
        {
            HoldFixture fixture = CreateFixture(0.5f, 0.5f, ShortQuitDelay);

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
            float decayDuration = ShortDecayDuration,
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

            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            AudioClip completionClip = AudioClip.Create(
                "Execute Button",
                44100,
                1,
                44100,
                false);
            _createdObjects.Add(completionClip);

            HoldToQuitButton controller = root.AddComponent<HoldToQuitButton>();
            controller.Configure(
                progressImage,
                checkObject,
                holdDuration,
                decayDuration,
                quitDelay,
                audioSource,
                completionClip,
                false);
            root.SetActive(true);

            return new HoldFixture(
                root,
                controller,
                progressImage,
                checkObject,
                audioSource,
                completionClip);
        }

        private sealed class HoldFixture
        {
            public HoldFixture(
                GameObject root,
                HoldToQuitButton controller,
                Image progressImage,
                GameObject checkObject,
                AudioSource audioSource,
                AudioClip completionClip)
            {
                Root = root;
                Controller = controller;
                ProgressImage = progressImage;
                CheckObject = checkObject;
                AudioSource = audioSource;
                CompletionClip = completionClip;
            }

            public GameObject Root { get; }

            public HoldToQuitButton Controller { get; }

            public Image ProgressImage { get; }

            public GameObject CheckObject { get; }

            public AudioSource AudioSource { get; }

            public AudioClip CompletionClip { get; }
        }
    }
}
