using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NanokaGame.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NanokaGame.Tests.PlayMode.Audio
{
    public sealed class AudioVolumeSettingsPlayModeTests
    {
        private const string MixerName = "NanokaGameMixer";
        private const string MusicVolumeParameter = "MusicVolume";
        private const string SfxVolumeParameter = "SfxVolume";

        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        private AudioMixer _audioMixer;
        private bool _hadMusicPreference;
        private bool _hadSfxPreference;
        private float _previousMusicPreference;
        private float _previousSfxPreference;
        private float _previousMusicDecibels;
        private float _previousSfxDecibels;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _audioMixer = Resources.FindObjectsOfTypeAll<AudioMixer>()
                .FirstOrDefault(mixer => mixer.name == MixerName);
            Assert.That(_audioMixer, Is.Not.Null);

            _hadMusicPreference = PlayerPrefs.HasKey(
                AudioVolumeSettings.MusicVolumePreferenceKey);
            _hadSfxPreference = PlayerPrefs.HasKey(
                AudioVolumeSettings.SfxVolumePreferenceKey);
            _previousMusicPreference = PlayerPrefs.GetFloat(
                AudioVolumeSettings.MusicVolumePreferenceKey);
            _previousSfxPreference = PlayerPrefs.GetFloat(
                AudioVolumeSettings.SfxVolumePreferenceKey);
            Assert.That(
                _audioMixer.GetFloat(MusicVolumeParameter, out _previousMusicDecibels),
                Is.True);
            Assert.That(
                _audioMixer.GetFloat(SfxVolumeParameter, out _previousSfxDecibels),
                Is.True);
        }

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
            RestorePreference(
                AudioVolumeSettings.MusicVolumePreferenceKey,
                _hadMusicPreference,
                _previousMusicPreference);
            RestorePreference(
                AudioVolumeSettings.SfxVolumePreferenceKey,
                _hadSfxPreference,
                _previousSfxPreference);
            PlayerPrefs.Save();

            if (_audioMixer != null)
            {
                _audioMixer.SetFloat(MusicVolumeParameter, _previousMusicDecibels);
                _audioMixer.SetFloat(SfxVolumeParameter, _previousSfxDecibels);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_WhenPreferencesExist_RestoresWithoutInvokingSliderEvents()
        {
            PlayerPrefs.SetFloat(AudioVolumeSettings.MusicVolumePreferenceKey, 0.25f);
            PlayerPrefs.SetFloat(AudioVolumeSettings.SfxVolumePreferenceKey, 0.75f);
            Slider musicSlider = CreateSlider("Music Slider");
            Slider sfxSlider = CreateSlider("SFX Slider");
            int musicEventCount = 0;
            int sfxEventCount = 0;
            musicSlider.onValueChanged.AddListener(_ => musicEventCount++);
            sfxSlider.onValueChanged.AddListener(_ => sfxEventCount++);

            AudioVolumeSettings settings = CreateSettings(musicSlider, sfxSlider);
            yield return null;

            Assert.That(settings.MusicVolume, Is.EqualTo(0.25f));
            Assert.That(settings.SfxVolume, Is.EqualTo(0.75f));
            Assert.That(musicEventCount, Is.Zero);
            Assert.That(sfxEventCount, Is.Zero);
            AssertMixerVolume(MusicVolumeParameter, 0.25f);
            AssertMixerVolume(SfxVolumeParameter, 0.75f);
        }

        [UnityTest]
        public IEnumerator SliderChange_WhenControllerIsEnabled_AppliesAndPersistsVolume()
        {
            Slider musicSlider = CreateSlider("Music Slider");
            Slider sfxSlider = CreateSlider("SFX Slider");
            AudioVolumeSettings settings = CreateSettings(musicSlider, sfxSlider);
            settings.gameObject.SetActive(true);

            musicSlider.value = 0.4f;
            sfxSlider.value = 0.2f;
            yield return null;

            Assert.That(
                PlayerPrefs.GetFloat(AudioVolumeSettings.MusicVolumePreferenceKey),
                Is.EqualTo(0.4f));
            Assert.That(
                PlayerPrefs.GetFloat(AudioVolumeSettings.SfxVolumePreferenceKey),
                Is.EqualTo(0.2f));
            AssertMixerVolume(MusicVolumeParameter, 0.4f);
            AssertMixerVolume(SfxVolumeParameter, 0.2f);
        }

        [UnityTest]
        public IEnumerator OnDisable_AfterBinding_IgnoresFurtherSliderChanges()
        {
            Slider musicSlider = CreateSlider("Music Slider");
            Slider sfxSlider = CreateSlider("SFX Slider");
            AudioVolumeSettings settings = CreateSettings(musicSlider, sfxSlider);
            settings.gameObject.SetActive(true);
            musicSlider.value = 0.6f;
            settings.gameObject.SetActive(false);

            musicSlider.value = 0.2f;
            yield return null;

            Assert.That(
                PlayerPrefs.GetFloat(AudioVolumeSettings.MusicVolumePreferenceKey),
                Is.EqualTo(0.6f));
            AssertMixerVolume(MusicVolumeParameter, 0.6f);
        }

        private AudioVolumeSettings CreateSettings(Slider musicSlider, Slider sfxSlider)
        {
            GameObject settingsObject = CreateGameObject("Audio Volume Settings");
            settingsObject.SetActive(false);
            AudioVolumeSettings settings = settingsObject.AddComponent<AudioVolumeSettings>();
            settings.Configure(_audioMixer, musicSlider, sfxSlider);
            return settings;
        }

        private Slider CreateSlider(string name)
        {
            GameObject sliderObject = new GameObject(name, typeof(RectTransform));
            _createdObjects.Add(sliderObject);
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private void AssertMixerVolume(string parameterName, float linearVolume)
        {
            Assert.That(_audioMixer.GetFloat(parameterName, out float actualDecibels), Is.True);
            Assert.That(
                actualDecibels,
                Is.EqualTo(AudioVolumeSettings.LinearToDecibels(linearVolume)).Within(0.0001f));
        }

        private static void RestorePreference(string key, bool hadValue, float previousValue)
        {
            if (hadValue)
            {
                PlayerPrefs.SetFloat(key, previousValue);
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
