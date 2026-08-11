using System;
using UnityEngine;
using UnityEngine.UI;

namespace NanokaGame.UI
{
    [DefaultExecutionOrder(-2000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonSfx : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private ButtonSfxPlayer _player;
        [SerializeField] private ButtonSfxType _soundType = ButtonSfxType.Choice;

        private bool _isBound;

        public ButtonSfxType SoundType
        {
            get { return _soundType; }
        }

        public void Configure(
            Button button,
            ButtonSfxPlayer player,
            ButtonSfxType soundType)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            Unbind();
            _button = button;
            _player = player;
            _soundType = soundType;

            if (isActiveAndEnabled)
            {
                Bind();
            }
        }

        public void Play()
        {
            if (_player == null)
            {
                Debug.LogError(
                    $"{nameof(ButtonSfx)} on '{name}' requires a ButtonSfxPlayer reference.",
                    this);
                return;
            }

            _player.Play(_soundType);
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
        }

        private void Bind()
        {
            RefreshReferences();
            if (_button == null || _isBound)
            {
                return;
            }

            _button.onClick.AddListener(Play);
            _isBound = true;
        }

        private void Unbind()
        {
            if (!_isBound || _button == null)
            {
                return;
            }

            _button.onClick.RemoveListener(Play);
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
