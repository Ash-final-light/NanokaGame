using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace NanokaGame.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioVolumeSettings : MonoBehaviour
    {
        public const string MusicVolumePreferenceKey = "NanokaGame.Audio.MusicVolume";
        public const string SfxVolumePreferenceKey = "NanokaGame.Audio.SfxVolume";

        private const string MusicVolumeParameter = "MusicVolume";
        private const string SfxVolumeParameter = "SfxVolume";
        private const float DefaultLinearVolume = 1f;
        private const float MinimumLinearVolume = 0.0001f;
        private const float MinimumDecibels = -80f;

        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        private bool _isBound;

        public float MusicVolume
        {
            get { return _musicSlider != null ? _musicSlider.value : DefaultLinearVolume; }
        }

        public float SfxVolume
        {
            get { return _sfxSlider != null ? _sfxSlider.value : DefaultLinearVolume; }
        }

        public void Configure(
            AudioMixer audioMixer,
            Slider musicSlider,
            Slider sfxSlider)
        {
            if (audioMixer == null)
            {
                throw new ArgumentNullException(nameof(audioMixer));
            }

            if (musicSlider == null)
            {
                throw new ArgumentNullException(nameof(musicSlider));
            }

            if (sfxSlider == null)
            {
                throw new ArgumentNullException(nameof(sfxSlider));
            }

            Unbind();
            _audioMixer = audioMixer;
            _musicSlider = musicSlider;
            _sfxSlider = sfxSlider;
            ApplySavedSettings();

            if (isActiveAndEnabled)
            {
                Bind();
            }
        }

        public void ApplySavedSettings()
        {
            if (!HasRequiredReferences())
            {
                return;
            }

            float musicVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(MusicVolumePreferenceKey, DefaultLinearVolume));
            float sfxVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(SfxVolumePreferenceKey, DefaultLinearVolume));

            _musicSlider.SetValueWithoutNotify(musicVolume);
            _sfxSlider.SetValueWithoutNotify(sfxVolume);
            ApplyMixerVolume(MusicVolumeParameter, musicVolume);
            ApplyMixerVolume(SfxVolumeParameter, sfxVolume);
        }

        public static float LinearToDecibels(float linearVolume)
        {
            float clampedVolume = Mathf.Clamp01(linearVolume);
            if (clampedVolume <= MinimumLinearVolume)
            {
                return MinimumDecibels;
            }

            return Mathf.Log10(clampedVolume) * 20f;
        }

        private void Awake()
        {
            ApplySavedSettings();
        }

        private void OnEnable()
        {
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Bind()
        {
            if (_isBound || _musicSlider == null || _sfxSlider == null)
            {
                return;
            }

            _musicSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
            _sfxSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
            _isBound = true;
        }

        private void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            if (_musicSlider != null)
            {
                _musicSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            }

            _isBound = false;
        }

        private void HandleMusicVolumeChanged(float linearVolume)
        {
            ApplyAndSave(MusicVolumeParameter, MusicVolumePreferenceKey, linearVolume);
        }

        private void HandleSfxVolumeChanged(float linearVolume)
        {
            ApplyAndSave(SfxVolumeParameter, SfxVolumePreferenceKey, linearVolume);
        }

        private void ApplyAndSave(
            string parameterName,
            string preferenceKey,
            float linearVolume)
        {
            float clampedVolume = Mathf.Clamp01(linearVolume);
            ApplyMixerVolume(parameterName, clampedVolume);
            PlayerPrefs.SetFloat(preferenceKey, clampedVolume);
            PlayerPrefs.Save();
        }

        private void ApplyMixerVolume(string parameterName, float linearVolume)
        {
            if (!_audioMixer.SetFloat(parameterName, LinearToDecibels(linearVolume)))
            {
                Debug.LogError(
                    $"AudioMixer parameter '{parameterName}' is not exposed or does not exist.",
                    this);
            }
        }

        private bool HasRequiredReferences()
        {
            if (_audioMixer != null && _musicSlider != null && _sfxSlider != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(AudioVolumeSettings)} on '{name}' requires an AudioMixer, Music Slider, and SFX Slider.",
                this);
            return false;
        }
    }
}
