using System.Collections;
using System.Collections.Generic;
using NanokaGame.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NanokaGame.Tests.PlayMode.Audio
{
    public sealed class MusicPlayerPlayModeTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int objectIndex = _createdObjects.Count - 1; objectIndex >= 0; objectIndex--)
            {
                if (_createdObjects[objectIndex] != null)
                {
                    Object.Destroy(_createdObjects[objectIndex]);
                }
            }

            _createdObjects.Clear();
            yield return null;
            Assert.That(MusicPlayer.Instance, Is.Null);
        }

        [UnityTest]
        public IEnumerator Awake_WhenConfigured_StartsLoopingInitialMusic()
        {
            AudioClip clip = CreateAudioClip("Initial Music");
            MusicFixture fixture = CreateFixture("Music Player", clip);

            yield return null;

            Assert.That(MusicPlayer.Instance, Is.SameAs(fixture.Player));
            Assert.That(fixture.Player.CurrentClip, Is.SameAs(clip));
            Assert.That(fixture.Source.playOnAwake, Is.False);
            Assert.That(fixture.Source.loop, Is.True);
            Assert.That(fixture.Source.spatialBlend, Is.Zero);
            Assert.That(fixture.Source.dopplerLevel, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Play_WhenGivenDifferentClip_ReplacesCurrentMusic()
        {
            AudioClip firstClip = CreateAudioClip("First Music");
            AudioClip secondClip = CreateAudioClip("Second Music");
            MusicFixture fixture = CreateFixture("Music Player", firstClip);
            yield return null;

            fixture.Player.Play(secondClip);

            Assert.That(fixture.Player.CurrentClip, Is.SameAs(secondClip));
        }

        [UnityTest]
        public IEnumerator Awake_WhenAnotherPlayerExists_RequestsItsClipAndRemovesDuplicate()
        {
            AudioClip titleClip = CreateAudioClip("Title Music");
            AudioClip klotskiClip = CreateAudioClip("Klotski Music");
            MusicFixture titleFixture = CreateFixture("Title Music Player", titleClip);
            yield return null;

            MusicFixture klotskiFixture = CreateFixture("Klotski Music Player", klotskiClip);
            yield return null;

            Assert.That(MusicPlayer.Instance, Is.SameAs(titleFixture.Player));
            Assert.That(titleFixture.Player.CurrentClip, Is.SameAs(klotskiClip));
            Assert.That(klotskiFixture.Root == null, Is.True);
        }

        private MusicFixture CreateFixture(string name, AudioClip initialClip)
        {
            GameObject root = new GameObject(name);
            root.SetActive(false);
            _createdObjects.Add(root);
            AudioSource source = root.AddComponent<AudioSource>();
            source.mute = true;
            MusicPlayer player = root.AddComponent<MusicPlayer>();
            player.Configure(source, initialClip, false);
            root.SetActive(true);
            return new MusicFixture(root, source, player);
        }

        private AudioClip CreateAudioClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            _createdObjects.Add(clip);
            return clip;
        }

        private sealed class MusicFixture
        {
            public MusicFixture(GameObject root, AudioSource source, MusicPlayer player)
            {
                Root = root;
                Source = source;
                Player = player;
            }

            public GameObject Root { get; }

            public AudioSource Source { get; }

            public MusicPlayer Player { get; }
        }
    }
}
