using System;
using System.Collections;
using System.Collections.Generic;
using NanokaGame.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace NanokaGame.Tests.PlayMode.UI
{
    public sealed class SafeAreaOffsetPlayModeTests
    {
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
        public void ApplySafeArea_RightAndBottomInsets_MovesTargetLeftAndUp()
        {
            SafeAreaFixture fixture = CreateFixture(new Vector2(100f, 100f));
            Rect safeArea = new Rect(0f, 60f, 2280f, 1020f);

            fixture.Offset.ApplySafeArea(
                safeArea,
                new Vector2(2400f, 1080f),
                2f);

            Assert.That(
                fixture.Target.anchoredPosition,
                Is.EqualTo(new Vector2(40f, 130f)).Using(Vector2ComparerWithEqualsOperator.Instance));
            Assert.That(
                fixture.Offset.AppliedOffset,
                Is.EqualTo(new Vector2(-60f, 30f)).Using(Vector2ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void ApplySafeArea_WhenCalledRepeatedly_DoesNotAccumulateOffset()
        {
            SafeAreaFixture fixture = CreateFixture(new Vector2(100f, 100f));
            Rect safeArea = new Rect(0f, 60f, 2280f, 1020f);

            fixture.Offset.ApplySafeArea(safeArea, new Vector2(2400f, 1080f), 2f);
            Vector2 firstPosition = fixture.Target.anchoredPosition;
            fixture.Offset.ApplySafeArea(safeArea, new Vector2(2400f, 1080f), 2f);

            Assert.That(
                fixture.Target.anchoredPosition,
                Is.EqualTo(firstPosition).Using(Vector2ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void ApplySafeArea_WhenScreenIsFullySafe_KeepsBasePosition()
        {
            SafeAreaFixture fixture = CreateFixture(new Vector2(100f, 100f));

            fixture.Offset.ApplySafeArea(
                new Rect(0f, 0f, 1920f, 1080f),
                new Vector2(1920f, 1080f),
                2f);

            Assert.That(
                fixture.Target.anchoredPosition,
                Is.EqualTo(new Vector2(100f, 100f))
                    .Using(Vector2ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void Configure_WhenTargetIsMissing_ThrowsArgumentNullException()
        {
            SafeAreaFixture fixture = CreateFixture(Vector2.zero);

            Assert.Throws<ArgumentNullException>(
                () => fixture.Offset.Configure(
                    null,
                    false,
                    true,
                    false,
                    true,
                    Vector2.zero));
        }

        [UnityTest]
        public IEnumerator OnDisable_AfterOffset_RestoresBasePosition()
        {
            SafeAreaFixture fixture = CreateFixture(new Vector2(100f, 100f));
            fixture.Offset.ApplySafeArea(
                new Rect(0f, 60f, 2280f, 1020f),
                new Vector2(2400f, 1080f),
                2f);

            fixture.Offset.enabled = false;
            yield return null;

            Assert.That(
                fixture.Target.anchoredPosition,
                Is.EqualTo(new Vector2(100f, 100f))
                    .Using(Vector2ComparerWithEqualsOperator.Instance));
        }

        private SafeAreaFixture CreateFixture(Vector2 basePosition)
        {
            GameObject root = new GameObject("Safe Area Offset", typeof(RectTransform));
            root.SetActive(false);
            _createdObjects.Add(root);

            RectTransform target = root.GetComponent<RectTransform>();
            target.anchoredPosition = basePosition;
            SafeAreaOffset offset = root.AddComponent<SafeAreaOffset>();
            offset.Configure(
                target,
                false,
                true,
                false,
                true,
                Vector2.zero);
            root.SetActive(true);

            return new SafeAreaFixture(target, offset);
        }

        private sealed class SafeAreaFixture
        {
            public SafeAreaFixture(RectTransform target, SafeAreaOffset offset)
            {
                Target = target;
                Offset = offset;
            }

            public RectTransform Target { get; }

            public SafeAreaOffset Offset { get; }
        }
    }
}
