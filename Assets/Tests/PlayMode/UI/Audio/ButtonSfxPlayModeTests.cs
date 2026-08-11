using System.Collections;
using System.Collections.Generic;
using NanokaGame.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NanokaGame.Tests.PlayMode.UI
{
    public sealed class ButtonSfxPlayModeTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ButtonSfxPlayer[] players = Object.FindObjectsOfType<ButtonSfxPlayer>();
            for (int playerIndex = 0; playerIndex < players.Length; playerIndex++)
            {
                if (players[playerIndex] != null)
                {
                    Object.Destroy(players[playerIndex].gameObject);
                }
            }

            AudioSource[] sources = Object.FindObjectsOfType<AudioSource>();
            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                AudioSource source = sources[sourceIndex];
                if (source != null && source.gameObject.name.StartsWith("ButtonSfx_"))
                {
                    Object.Destroy(source.gameObject);
                }
            }

            for (int objectIndex = _createdObjects.Count - 1; objectIndex >= 0; objectIndex--)
            {
                if (_createdObjects[objectIndex] != null)
                {
                    Object.Destroy(_createdObjects[objectIndex]);
                }
            }

            _createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Click_WhenConfiguredAsChoice_PlaysChoiceClipOnce()
        {
            AudioClip choiceClip = CreateAudioClip("Choice");
            AudioClip cancelClip = CreateAudioClip("Cancel");
            ButtonFixture fixture = CreateFixture(choiceClip, cancelClip, ButtonSfxType.Choice);

            fixture.Button.onClick.Invoke();
            yield return null;

            Assert.That(fixture.Player.PlayCount, Is.EqualTo(1));
            Assert.That(fixture.Player.LastPlayedClip, Is.SameAs(choiceClip));
            Assert.That(fixture.Player.LastCreatedSource, Is.Not.Null);
            Assert.That(fixture.Player.LastCreatedSource.clip, Is.SameAs(choiceClip));
        }

        [UnityTest]
        public IEnumerator Click_WhenConfiguredAsCancel_PlaysCancelClipOnce()
        {
            AudioClip choiceClip = CreateAudioClip("Choice");
            AudioClip cancelClip = CreateAudioClip("Cancel");
            ButtonFixture fixture = CreateFixture(choiceClip, cancelClip, ButtonSfxType.Cancel);

            fixture.Button.onClick.Invoke();
            yield return null;

            Assert.That(fixture.Player.PlayCount, Is.EqualTo(1));
            Assert.That(fixture.Player.LastPlayedClip, Is.SameAs(cancelClip));
        }

        [UnityTest]
        public IEnumerator Click_WhenSourceSceneObjectIsDestroyed_TransientAudioSurvives()
        {
            AudioClip choiceClip = CreateAudioClip("Choice");
            AudioClip cancelClip = CreateAudioClip("Cancel");
            ButtonFixture fixture = CreateFixture(choiceClip, cancelClip, ButtonSfxType.Choice);

            fixture.Button.onClick.Invoke();
            AudioSource transientSource = fixture.Player.LastCreatedSource;
            Object.Destroy(fixture.Player.gameObject);
            yield return null;

            Assert.That(transientSource, Is.Not.Null);
            Assert.That(transientSource.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            Assert.That(transientSource.clip, Is.SameAs(choiceClip));
        }

        [UnityTest]
        public IEnumerator OnDisable_AfterBinding_RemovesClickListener()
        {
            AudioClip choiceClip = CreateAudioClip("Choice");
            AudioClip cancelClip = CreateAudioClip("Cancel");
            ButtonFixture fixture = CreateFixture(choiceClip, cancelClip, ButtonSfxType.Choice);

            fixture.Button.gameObject.SetActive(false);
            fixture.Button.onClick.Invoke();
            yield return null;

            Assert.That(fixture.Player.PlayCount, Is.Zero);
        }

        private ButtonFixture CreateFixture(
            AudioClip choiceClip,
            AudioClip cancelClip,
            ButtonSfxType soundType)
        {
            GameObject playerObject = new GameObject("Button SFX Player");
            _createdObjects.Add(playerObject);
            AudioSource source = playerObject.AddComponent<AudioSource>();
            source.mute = true;
            ButtonSfxPlayer player = playerObject.AddComponent<ButtonSfxPlayer>();
            player.Configure(source, choiceClip, cancelClip);

            GameObject buttonObject = new GameObject("Button");
            buttonObject.SetActive(false);
            _createdObjects.Add(buttonObject);
            Button button = buttonObject.AddComponent<Button>();
            ButtonSfx buttonSfx = buttonObject.AddComponent<ButtonSfx>();
            buttonSfx.Configure(button, player, soundType);
            buttonObject.SetActive(true);

            return new ButtonFixture(button, player);
        }

        private AudioClip CreateAudioClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 44100, 1, 44100, false);
            _createdObjects.Add(clip);
            return clip;
        }

        private sealed class ButtonFixture
        {
            public ButtonFixture(Button button, ButtonSfxPlayer player)
            {
                Button = button;
                Player = player;
            }

            public Button Button { get; }

            public ButtonSfxPlayer Player { get; }
        }
    }
}
