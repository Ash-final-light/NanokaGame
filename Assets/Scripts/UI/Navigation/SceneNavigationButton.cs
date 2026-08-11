using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NanokaGame.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class SceneNavigationButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private string _sceneName;

        private bool _isBound;

        public string SceneName
        {
            get { return _sceneName; }
        }

        public void Configure(Button button, string sceneName)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("Scene name must not be empty.", nameof(sceneName));
            }

            Unbind();
            _button = button;
            _sceneName = sceneName.Trim();

            if (isActiveAndEnabled)
            {
                Bind();
            }
        }

        public void LoadTargetScene()
        {
            if (string.IsNullOrWhiteSpace(_sceneName))
            {
                Debug.LogError(
                    $"{nameof(SceneNavigationButton)} on '{name}' has no target scene configured.",
                    this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(_sceneName))
            {
                Debug.LogError(
                    $"Cannot load scene '{_sceneName}'. Add it to Build Settings and enable it.",
                    this);
                return;
            }

            SceneManager.LoadScene(_sceneName);
        }

        private void Awake()
        {
            RefreshReferences();
        }

        private void OnEnable()
        {
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Reset()
        {
            RefreshReferences();
        }

        private void OnValidate()
        {
            RefreshReferences();

            if (_sceneName != null)
            {
                _sceneName = _sceneName.Trim();
            }
        }

        private void Bind()
        {
            RefreshReferences();
            if (_button == null)
            {
                Debug.LogError(
                    $"{nameof(SceneNavigationButton)} on '{name}' requires a Button reference.",
                    this);
                return;
            }

            if (_isBound)
            {
                return;
            }

            _button.onClick.AddListener(LoadTargetScene);
            _isBound = true;
        }

        private void Unbind()
        {
            if (!_isBound || _button == null)
            {
                return;
            }

            _button.onClick.RemoveListener(LoadTargetScene);
            _isBound = false;
        }

        private void RefreshReferences()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
        }
    }
}
