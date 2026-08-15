using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace NanokaGame.UI
{
    [DisallowMultipleComponent]
    public sealed class HoldToQuitButton : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        private const float MinimumDuration = 0.01f;

        [FormerlySerializedAs("_progressImage")]
        [SerializeField] private Image _progressFillImage;
        [SerializeField] private GameObject _checkObject;
        [SerializeField] private AudioSource _completionAudioSource;
        [SerializeField] private AudioClip _completionClip;
        [SerializeField] private float _holdDuration = 1.5f;
        [SerializeField] private float _decayDuration = 1.5f;
        [SerializeField] private float _quitDelay = 1f;
        [SerializeField] private bool _quitApplication = true;

        private Coroutine _quitRoutine;
        private float _holdProgress;
        private bool _isHolding;
        private bool _isCompleted;
        private bool _quitRequested;
        private int _completionSoundPlayCount;

        public float HoldProgress
        {
            get { return _holdProgress; }
        }

        public bool IsHolding
        {
            get { return _isHolding; }
        }

        public bool IsCompleted
        {
            get { return _isCompleted; }
        }

        public bool QuitRequested
        {
            get { return _quitRequested; }
        }

        public int CompletionSoundPlayCount
        {
            get { return _completionSoundPlayCount; }
        }

        public void Configure(
            Image progressFillImage,
            GameObject checkObject,
            float holdDuration,
            float decayDuration,
            float quitDelay,
            AudioSource completionAudioSource,
            AudioClip completionClip,
            bool quitApplication = true)
        {
            if (progressFillImage == null)
            {
                throw new ArgumentNullException(nameof(progressFillImage));
            }

            if (checkObject == null)
            {
                throw new ArgumentNullException(nameof(checkObject));
            }

            if (holdDuration < MinimumDuration)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(holdDuration),
                    $"Hold duration must be at least {MinimumDuration} seconds.");
            }

            if (decayDuration < MinimumDuration)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(decayDuration),
                    $"Decay duration must be at least {MinimumDuration} seconds.");
            }

            if (quitDelay < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(quitDelay),
                    "Quit delay must not be negative.");
            }

            if (completionAudioSource == null)
            {
                throw new ArgumentNullException(nameof(completionAudioSource));
            }

            if (completionClip == null)
            {
                throw new ArgumentNullException(nameof(completionClip));
            }

            StopQuitRoutine();
            _progressFillImage = progressFillImage;
            _checkObject = checkObject;
            _completionAudioSource = completionAudioSource;
            _completionClip = completionClip;
            _holdDuration = holdDuration;
            _decayDuration = decayDuration;
            _quitDelay = quitDelay;
            _quitApplication = quitApplication;
            ResetInteraction();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isCompleted || _isHolding)
            {
                return;
            }

            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (!ValidateReferences())
            {
                return;
            }

            _isHolding = true;
            _checkObject.SetActive(false);
            ApplyProgress(_holdProgress);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StopHolding();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopHolding();
        }

        private void Awake()
        {
            ResetInteraction();
        }

        private void Update()
        {
            if (_isCompleted)
            {
                return;
            }

            if (_isHolding)
            {
                _holdProgress += Time.unscaledDeltaTime / _holdDuration;
                ApplyProgress(_holdProgress);

                if (_holdProgress >= 1f)
                {
                    CompleteHold();
                }

                return;
            }

            if (_holdProgress > 0f)
            {
                _holdProgress = Mathf.MoveTowards(
                    _holdProgress,
                    0f,
                    Time.unscaledDeltaTime / _decayDuration);
                ApplyProgress(_holdProgress);
            }
        }

        private void OnDisable()
        {
            StopQuitRoutine();
            ResetInteraction();
        }

        private void OnValidate()
        {
            _holdDuration = Mathf.Max(MinimumDuration, _holdDuration);
            _decayDuration = Mathf.Max(MinimumDuration, _decayDuration);
            _quitDelay = Mathf.Max(0f, _quitDelay);
        }

        private void CompleteHold()
        {
            _isHolding = false;
            _isCompleted = true;
            _holdProgress = 1f;
            ApplyProgress(1f);
            PlayCompletionSound();
            _checkObject.SetActive(true);
            _quitRoutine = StartCoroutine(QuitAfterDelay());
        }

        private IEnumerator QuitAfterDelay()
        {
            if (_quitDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(_quitDelay);
            }

            _quitRoutine = null;
            _quitRequested = true;

            if (_quitApplication)
            {
                QuitGame();
            }
        }

        private void StopHolding()
        {
            if (!_isHolding || _isCompleted)
            {
                return;
            }

            _isHolding = false;
        }

        private void ResetInteraction()
        {
            _holdProgress = 0f;
            _isHolding = false;
            _isCompleted = false;
            _quitRequested = false;
            _completionSoundPlayCount = 0;
            ApplyProgress(0f);

            if (_checkObject != null)
            {
                _checkObject.SetActive(false);
            }
        }

        private void ApplyProgress(float progress)
        {
            _holdProgress = Mathf.Clamp01(progress);

            if (_progressFillImage != null)
            {
                _progressFillImage.fillAmount = _holdProgress;
            }
        }

        private void PlayCompletionSound()
        {
            if (_completionAudioSource == null || _completionClip == null)
            {
                return;
            }

            _completionAudioSource.PlayOneShot(_completionClip);
            _completionSoundPlayCount++;
        }

        private void StopQuitRoutine()
        {
            if (_quitRoutine == null)
            {
                return;
            }

            StopCoroutine(_quitRoutine);
            _quitRoutine = null;
        }

        private bool ValidateReferences()
        {
            bool isValid = true;

            if (_progressFillImage == null)
            {
                Debug.LogError(
                    $"{nameof(HoldToQuitButton)} on '{name}' requires a progress fill Image.",
                    this);
                isValid = false;
            }

            if (_checkObject == null)
            {
                Debug.LogError(
                    $"{nameof(HoldToQuitButton)} on '{name}' requires a check object.",
                    this);
                isValid = false;
            }

            if (_completionAudioSource == null)
            {
                Debug.LogError(
                    $"{nameof(HoldToQuitButton)} on '{name}' requires a completion AudioSource.",
                    this);
                isValid = false;
            }

            if (_completionClip == null)
            {
                Debug.LogError(
                    $"{nameof(HoldToQuitButton)} on '{name}' requires a completion AudioClip.",
                    this);
                isValid = false;
            }

            return isValid;
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
