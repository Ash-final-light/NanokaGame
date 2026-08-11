using System;
using UnityEngine;

namespace NanokaGame.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class ButtonSfxPlayer : MonoBehaviour
    {
        private const string ChoiceSourceName = "ButtonSfx_Choice";
        private const string CancelSourceName = "ButtonSfx_Cancel";

        [SerializeField] private AudioSource _templateSource;
        [SerializeField] private AudioClip _choiceClip;
        [SerializeField] private AudioClip _cancelClip;
        [SerializeField, Range(0f, 1f)] private float _volumeScale = 1f;

        private AudioClip _lastPlayedClip;
        private AudioSource _lastCreatedSource;
        private int _playCount;

        public AudioClip LastPlayedClip
        {
            get { return _lastPlayedClip; }
        }

        public AudioSource LastCreatedSource
        {
            get { return _lastCreatedSource; }
        }

        public int PlayCount
        {
            get { return _playCount; }
        }

        public void Configure(
            AudioSource templateSource,
            AudioClip choiceClip,
            AudioClip cancelClip,
            float volumeScale = 1f)
        {
            if (templateSource == null)
            {
                throw new ArgumentNullException(nameof(templateSource));
            }

            if (volumeScale < 0f || volumeScale > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(volumeScale),
                    volumeScale,
                    "Volume scale must be between zero and one.");
            }

            _templateSource = templateSource;
            _choiceClip = choiceClip;
            _cancelClip = cancelClip;
            _volumeScale = volumeScale;
            ApplyTemplateSettings();
        }

        public void Play(ButtonSfxType soundType)
        {
            AudioClip clip = soundType == ButtonSfxType.Cancel
                ? _cancelClip
                : _choiceClip;
            if (clip == null)
            {
                return;
            }

            RefreshReferences();
            if (_templateSource == null)
            {
                Debug.LogError(
                    $"{nameof(ButtonSfxPlayer)} on '{name}' requires an AudioSource reference.",
                    this);
                return;
            }

            ApplyTemplateSettings();
            GameObject transientObject = new GameObject(
                soundType == ButtonSfxType.Cancel ? CancelSourceName : ChoiceSourceName);
            DontDestroyOnLoad(transientObject);
            AudioSource transientSource = transientObject.AddComponent<AudioSource>();
            CopyTemplateSettings(transientSource);
            transientSource.clip = clip;
            transientSource.Play();

            _lastPlayedClip = clip;
            _lastCreatedSource = transientSource;
            _playCount++;

            float pitch = Mathf.Max(0.01f, Mathf.Abs(transientSource.pitch));
            float lifetime = Mathf.Max(0.1f, clip.length / pitch + 0.1f);
            Destroy(transientObject, lifetime);
        }

        private void Awake()
        {
            RefreshReferences();
            ApplyTemplateSettings();
        }

        private void Reset()
        {
            RefreshReferences();
            ApplyTemplateSettings();
        }

        private void OnValidate()
        {
            _volumeScale = Mathf.Clamp01(_volumeScale);
            RefreshReferences();
            ApplyTemplateSettings();
        }

        private void RefreshReferences()
        {
            if (_templateSource == null)
            {
                _templateSource = GetComponent<AudioSource>();
            }
        }

        private void ApplyTemplateSettings()
        {
            if (_templateSource == null)
            {
                return;
            }

            _templateSource.playOnAwake = false;
            _templateSource.loop = false;
            _templateSource.spatialBlend = 0f;
            _templateSource.dopplerLevel = 0f;
        }

        private void CopyTemplateSettings(AudioSource targetSource)
        {
            targetSource.playOnAwake = false;
            targetSource.loop = false;
            targetSource.spatialBlend = 0f;
            targetSource.dopplerLevel = 0f;
            targetSource.outputAudioMixerGroup = _templateSource.outputAudioMixerGroup;
            targetSource.volume = _templateSource.volume * _volumeScale;
            targetSource.pitch = _templateSource.pitch;
            targetSource.priority = _templateSource.priority;
            targetSource.mute = _templateSource.mute;
            targetSource.ignoreListenerPause = _templateSource.ignoreListenerPause;
        }
    }
}
