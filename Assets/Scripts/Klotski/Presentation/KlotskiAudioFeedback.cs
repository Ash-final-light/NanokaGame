using System;
using UnityEngine;

namespace NanokaGame.Games.Klotski
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class KlotskiAudioFeedback : MonoBehaviour
    {
        private const string TransientCancelSourceName = "Klotski_CancelSfx";

        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _choiceClip;
        [SerializeField] private AudioClip _cancelClip;
        [SerializeField] private AudioClip _moveClip;
        [SerializeField] private AudioClip _alertClip;
        [SerializeField] private AudioClip _victoryClip;
        [SerializeField, Range(0f, 1f)] private float _volumeScale = 1f;

        private AudioClip _lastPlayedClip;
        private int _playCount;

        public AudioClip LastPlayedClip
        {
            get { return _lastPlayedClip; }
        }

        public int PlayCount
        {
            get { return _playCount; }
        }

        public void Configure(
            AudioSource audioSource,
            AudioClip choiceClip,
            AudioClip cancelClip,
            AudioClip moveClip,
            AudioClip alertClip,
            AudioClip victoryClip,
            float volumeScale = 1f)
        {
            if (volumeScale < 0f || volumeScale > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(volumeScale),
                    volumeScale,
                    "Volume scale must be between zero and one.");
            }

            _audioSource = audioSource;
            _choiceClip = choiceClip;
            _cancelClip = cancelClip;
            _moveClip = moveClip;
            _alertClip = alertClip;
            _victoryClip = victoryClip;
            _volumeScale = volumeScale;
            ApplySourceSettings();
        }

        public void PlayChoice()
        {
            PlayOneShot(_choiceClip);
        }

        public void PlayCancel()
        {
            PlayOneShot(_cancelClip);
        }

        public void PlayMove()
        {
            PlayOneShot(_moveClip);
        }

        public void PlayAlert()
        {
            PlayOneShot(_alertClip);
        }

        public void PlayVictory()
        {
            PlayOneShot(_victoryClip);
        }

        public void PlayCancelAcrossSceneLoad()
        {
            if (_cancelClip == null)
            {
                return;
            }

            RefreshReferences();
            GameObject transientSourceObject = new GameObject(TransientCancelSourceName);
            DontDestroyOnLoad(transientSourceObject);
            AudioSource transientSource = transientSourceObject.AddComponent<AudioSource>();
            CopySourceSettings(transientSource);
            transientSource.PlayOneShot(_cancelClip, _volumeScale);
            Destroy(transientSourceObject, Mathf.Max(0.1f, _cancelClip.length + 0.1f));
            RecordPlayback(_cancelClip);
        }

        public void StopAll()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
        }

        private void Awake()
        {
            RefreshReferences();
            ApplySourceSettings();
        }

        private void OnDisable()
        {
            StopAll();
        }

        private void Reset()
        {
            RefreshReferences();
            ApplySourceSettings();
        }

        private void OnValidate()
        {
            _volumeScale = Mathf.Clamp01(_volumeScale);
            RefreshReferences();
            ApplySourceSettings();
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            RefreshReferences();
            if (_audioSource == null)
            {
                return;
            }

            ApplySourceSettings();
            _audioSource.PlayOneShot(clip, _volumeScale);
            RecordPlayback(clip);
        }

        private void RefreshReferences()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        private void ApplySourceSettings()
        {
            if (_audioSource == null)
            {
                return;
            }

            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;
        }

        private void CopySourceSettings(AudioSource targetSource)
        {
            targetSource.playOnAwake = false;
            targetSource.loop = false;
            targetSource.spatialBlend = 0f;

            if (_audioSource == null)
            {
                return;
            }

            targetSource.outputAudioMixerGroup = _audioSource.outputAudioMixerGroup;
            targetSource.volume = _audioSource.volume;
            targetSource.pitch = _audioSource.pitch;
            targetSource.priority = _audioSource.priority;
            targetSource.mute = _audioSource.mute;
            targetSource.ignoreListenerPause = _audioSource.ignoreListenerPause;
        }

        private void RecordPlayback(AudioClip clip)
        {
            _lastPlayedClip = clip;
            _playCount++;
        }
    }
}
