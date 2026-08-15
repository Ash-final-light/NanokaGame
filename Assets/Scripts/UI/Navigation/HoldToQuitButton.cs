using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
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

        [SerializeField] private Image _progressImage;
        [SerializeField] private GameObject _checkObject;
        [SerializeField] private float _holdDuration = 1.5f;
        [SerializeField] private float _quitDelay = 1f;
        [SerializeField] private bool _quitApplication = true;

        private Coroutine _quitRoutine;
        private float _elapsedHoldTime;
        private bool _isHolding;
        private bool _isCompleted;
        private bool _quitRequested;

        public float HoldProgress
        {
            get { return _progressImage != null ? _progressImage.fillAmount : 0f; }
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

        public void Configure(
            Image progressImage,
            GameObject checkObject,
            float holdDuration,
            float quitDelay,
            bool quitApplication = true)
        {
            if (progressImage == null)
            {
                throw new ArgumentNullException(nameof(progressImage));
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

            if (quitDelay < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(quitDelay),
                    "Quit delay must not be negative.");
            }

            StopQuitRoutine();
            _progressImage = progressImage;
            _checkObject = checkObject;
            _holdDuration = holdDuration;
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

            _elapsedHoldTime = 0f;
            _isHolding = true;
            _checkObject.SetActive(false);
            ApplyProgress(0f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            CancelIncompleteHold();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelIncompleteHold();
        }

        private void Awake()
        {
            ResetInteraction();
        }

        private void Update()
        {
            if (!_isHolding || _isCompleted)
            {
                return;
            }

            _elapsedHoldTime += Time.unscaledDeltaTime;
            ApplyProgress(_elapsedHoldTime / _holdDuration);

            if (_elapsedHoldTime >= _holdDuration)
            {
                CompleteHold();
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
            _quitDelay = Mathf.Max(0f, _quitDelay);
        }

        private void CompleteHold()
        {
            _isHolding = false;
            _isCompleted = true;
            ApplyProgress(1f);
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

        private void CancelIncompleteHold()
        {
            if (!_isHolding || _isCompleted)
            {
                return;
            }

            _isHolding = false;
            _elapsedHoldTime = 0f;
            ApplyProgress(0f);
        }

        private void ResetInteraction()
        {
            _elapsedHoldTime = 0f;
            _isHolding = false;
            _isCompleted = false;
            _quitRequested = false;
            ApplyProgress(0f);

            if (_checkObject != null)
            {
                _checkObject.SetActive(false);
            }
        }

        private void ApplyProgress(float progress)
        {
            if (_progressImage != null)
            {
                _progressImage.fillAmount = Mathf.Clamp01(progress);
            }
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

            if (_progressImage == null)
            {
                Debug.LogError(
                    $"{nameof(HoldToQuitButton)} on '{name}' requires a progress Image.",
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
