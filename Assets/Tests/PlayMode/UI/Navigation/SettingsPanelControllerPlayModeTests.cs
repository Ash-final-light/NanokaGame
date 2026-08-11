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
    public sealed class SettingsPanelControllerPlayModeTests
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

        [UnityTest]
        public IEnumerator OpenAndClose_WhenButtonsAreClicked_UpdatesPanelVisibility()
        {
            SettingsFixture fixture = CreateFixture();
            fixture.PanelRoot.SetActive(false);

            fixture.OpenButton.onClick.Invoke();
            yield return null;

            Assert.That(fixture.Controller.IsOpen, Is.True);
            Assert.That(fixture.PanelRoot.activeSelf, Is.True);

            fixture.CloseButton.onClick.Invoke();
            yield return null;

            Assert.That(fixture.Controller.IsOpen, Is.False);
            Assert.That(fixture.PanelRoot.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator OnDisable_AfterBinding_RemovesButtonListeners()
        {
            SettingsFixture fixture = CreateFixture();
            fixture.PanelRoot.SetActive(false);
            fixture.Controller.gameObject.SetActive(false);

            fixture.OpenButton.onClick.Invoke();
            yield return null;

            Assert.That(fixture.PanelRoot.activeSelf, Is.False);
        }

        [Test]
        public void Configure_WhenRequiredReferenceIsMissing_ThrowsArgumentNullException()
        {
            SettingsFixture fixture = CreateFixture();

            Assert.Throws<ArgumentNullException>(
                () => fixture.Controller.Configure(null, fixture.CloseButton, fixture.PanelRoot));
            Assert.Throws<ArgumentNullException>(
                () => fixture.Controller.Configure(fixture.OpenButton, null, fixture.PanelRoot));
            Assert.Throws<ArgumentNullException>(
                () => fixture.Controller.Configure(fixture.OpenButton, fixture.CloseButton, null));
        }

        private SettingsFixture CreateFixture()
        {
            GameObject controllerObject = CreateGameObject("Settings Panel Controller");
            SettingsPanelController controller =
                controllerObject.AddComponent<SettingsPanelController>();
            Button openButton = CreateButton("Open Button");
            Button closeButton = CreateButton("Close Button");
            GameObject panelRoot = CreateGameObject("Settings Panel");

            controller.Configure(openButton, closeButton, panelRoot);
            return new SettingsFixture(controller, openButton, closeButton, panelRoot);
        }

        private Button CreateButton(string name)
        {
            return CreateGameObject(name).AddComponent<Button>();
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class SettingsFixture
        {
            public SettingsFixture(
                SettingsPanelController controller,
                Button openButton,
                Button closeButton,
                GameObject panelRoot)
            {
                Controller = controller;
                OpenButton = openButton;
                CloseButton = closeButton;
                PanelRoot = panelRoot;
            }

            public SettingsPanelController Controller { get; }

            public Button OpenButton { get; }

            public Button CloseButton { get; }

            public GameObject PanelRoot { get; }
        }
    }
}
