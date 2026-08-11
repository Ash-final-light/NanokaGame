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
    public sealed class SceneNavigationButtonPlayModeTests
    {
        private const string MissingSceneName = "MissingNavigationTestScene";
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

        [Test]
        public void Configure_WhenButtonIsNull_ThrowsArgumentNullException()
        {
            GameObject root = new GameObject("Scene Navigation Button");
            _createdObjects.Add(root);
            SceneNavigationButton navigationButton = root.AddComponent<SceneNavigationButton>();

            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    navigationButton.Configure(null, "Klotski");
                });
        }

        [UnityTest]
        public IEnumerator Click_WhenSceneIsNotInBuildSettings_LogsActionableError()
        {
            NavigationFixture fixture = CreateFixture();
            LogAssert.Expect(
                LogType.Error,
                $"Cannot load scene '{MissingSceneName}'. Add it to Build Settings and enable it.");

            fixture.Button.onClick.Invoke();
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnDisable_AfterBinding_RemovesClickListener()
        {
            NavigationFixture fixture = CreateFixture();

            fixture.Root.SetActive(false);
            fixture.Button.onClick.Invoke();
            yield return null;

            LogAssert.NoUnexpectedReceived();
        }

        private NavigationFixture CreateFixture()
        {
            GameObject root = new GameObject("Scene Navigation Button");
            root.SetActive(false);
            _createdObjects.Add(root);
            Button button = root.AddComponent<Button>();
            SceneNavigationButton navigationButton = root.AddComponent<SceneNavigationButton>();
            navigationButton.Configure(button, MissingSceneName);
            root.SetActive(true);
            return new NavigationFixture(root, button);
        }

        private sealed class NavigationFixture
        {
            public NavigationFixture(GameObject root, Button button)
            {
                Root = root;
                Button = button;
            }

            public GameObject Root { get; }

            public Button Button { get; }
        }
    }
}
