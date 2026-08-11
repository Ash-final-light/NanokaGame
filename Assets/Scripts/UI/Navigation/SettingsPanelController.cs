using System;
using UnityEngine;
using UnityEngine.UI;

namespace NanokaGame.UI
{
    [DisallowMultipleComponent]
    public sealed class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _panelRoot;

        private bool _isBound;

        public bool IsOpen
        {
            get { return _panelRoot != null && _panelRoot.activeSelf; }
        }

        public void Configure(
            Button openButton,
            Button closeButton,
            GameObject panelRoot)
        {
            if (openButton == null)
            {
                throw new ArgumentNullException(nameof(openButton));
            }

            if (closeButton == null)
            {
                throw new ArgumentNullException(nameof(closeButton));
            }

            if (panelRoot == null)
            {
                throw new ArgumentNullException(nameof(panelRoot));
            }

            Unbind();
            _openButton = openButton;
            _closeButton = closeButton;
            _panelRoot = panelRoot;

            if (isActiveAndEnabled)
            {
                Bind();
            }
        }

        public void Open()
        {
            if (_panelRoot == null)
            {
                Debug.LogError(
                    $"{nameof(SettingsPanelController)} on '{name}' requires a panel root reference.",
                    this);
                return;
            }

            _panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (_panelRoot == null)
            {
                Debug.LogError(
                    $"{nameof(SettingsPanelController)} on '{name}' requires a panel root reference.",
                    this);
                return;
            }

            _panelRoot.SetActive(false);
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
            if (_isBound)
            {
                return;
            }

            if (_openButton == null || _closeButton == null)
            {
                return;
            }

            _openButton.onClick.AddListener(Open);
            _closeButton.onClick.AddListener(Close);
            _isBound = true;
        }

        private void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            if (_openButton != null)
            {
                _openButton.onClick.RemoveListener(Open);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
            }

            _isBound = false;
        }
    }
}
