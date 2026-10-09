using UnityEngine;
using UnityEngine.InputSystem;

namespace TapCom.Gameplay
{
    /// <summary>玩家输入采集：新 Input System（tech-spec §2 playerInput）。
    /// 类名避让 UnityEngine.InputSystem.PlayerInput 组件，取 Reader。
    /// 绑定来自 InputSystem_Actions 的 Player map：Move(WASD) / Jump(Space) / Throw(鼠标左键) / Aim(鼠标位置)。</summary>
    public class PlayerInputReader : MonoBehaviour
    {
        /// <summary>输入动作资产；留空时回退工程级 InputSystem.actions（inference：未实测回退路径）。</summary>
        [SerializeField] private InputActionAsset _actions;

        private InputActionMap _playerMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _throwAction;
        private InputAction _aimAction;

        /// <summary>移动轴（WASD / 方向键）。</summary>
        public Vector2 MoveInput => _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>本帧是否按下跳跃。</summary>
        public bool JumpPressed => _jumpAction != null && _jumpAction.WasPressedThisFrame();

        /// <summary>本帧是否按下投掷。</summary>
        public bool ThrowPressed => _throwAction != null && _throwAction.WasPressedThisFrame();

        /// <summary>瞄准点屏幕坐标（像素）。</summary>
        public Vector2 AimScreenPosition => _aimAction != null ? _aimAction.ReadValue<Vector2>() : Vector2.zero;

        private void Awake()
        {
            if (_actions == null)
                _actions = InputSystem.actions;

            _playerMap = _actions != null ? _actions.FindActionMap("Player") : null;
            if (_playerMap == null)
            {
                Debug.LogError("[PlayerInputReader] 未找到 InputActionAsset 或 Player action map", this);
                return;
            }

            _moveAction = _playerMap.FindAction("Move");
            _jumpAction = _playerMap.FindAction("Jump");
            _throwAction = _playerMap.FindAction("Throw");
            _aimAction = _playerMap.FindAction("Aim");
        }

        private void OnEnable() => _playerMap?.Enable();

        private void OnDisable() => _playerMap?.Disable();
    }
}
