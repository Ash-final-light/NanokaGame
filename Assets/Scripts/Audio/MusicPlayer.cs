using System;
using UnityEngine;

namespace NanokaGame.Audio
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicPlayer : MonoBehaviour
    {
        private static MusicPlayer _instance;

        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _initialClip;
        [SerializeField] private bool _persistAcrossScenes = true;

        public static MusicPlayer Instance
        {
            get { return _instance; }
        }

        public AudioClip CurrentClip
        {
            get { return _audioSource != null ? _audioSource.clip : null; }
        }

        public void Configure(
            AudioSource audioSource,
            AudioClip initialClip,
            bool persistAcrossScenes = true)
        {
            if (audioSource == null)
            {
                throw new ArgumentNullException(nameof(audioSource));
            }

            _audioSource = audioSource;
            _initialClip = initialClip;
            _persistAcrossScenes = persistAcrossScenes;
            ApplySourceSettings();
        }

        public void Play(AudioClip clip)
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
            if (_audioSource.clip == clip)
            {
                if (!_audioSource.isPlaying)
                {
                    _audioSource.Play();
                }

                return;
            }

            _audioSource.Stop();
            _audioSource.clip = clip;
            _audioSource.Play();
        }

        public void Stop()
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

            if (_instance != null && _instance != this)
            {
                _instance.Play(_initialClip);
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (_persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (_initialClip == null)
            {
                Debug.LogWarning(
                    $"{nameof(MusicPlayer)} on '{name}' has no initial music clip assigned.",
                    this);
                return;
            }

            Play(_initialClip);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Reset()
        {
            RefreshReferences();
            ApplySourceSettings();
        }

        private void OnValidate()
        {
            RefreshReferences();
            ApplySourceSettings();
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
            _audioSource.loop = true;
            _audioSource.spatialBlend = 0f;
            _audioSource.dopplerLevel = 0f;
        }
    }
}
