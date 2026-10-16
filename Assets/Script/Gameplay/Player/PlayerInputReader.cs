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
        private InputAction _aimHoldAction;
        private InputAction _selectSeed1Action;
        private InputAction _selectSeed2Action;
        private InputAction _selectSeed3Action;
        private InputAction _seedScrollAction;

        /// <summary>移动轴（WASD / 方向键）。</summary>
        public Vector2 MoveInput => _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>本帧是否按下跳跃。</summary>
        public bool JumpPressed => _jumpAction != null && _jumpAction.WasPressedThisFrame();

        /// <summary>本帧是否按下投掷。</summary>
        public bool ThrowPressed => _throwAction != null && _throwAction.WasPressedThisFrame();

        /// <summary>瞄准点屏幕坐标（像素）。</summary>
        public Vector2 AimScreenPosition => _aimAction != null ? _aimAction.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>按住右键瞄准中（轨迹预览显示开关，TASK-007）。</summary>
        public bool AimHeld => _aimHoldAction != null && _aimHoldAction.IsPressed();

        /// <summary>本帧按下选蓝种子（键盘 1，TASK-016）。</summary>
        public bool SelectSeed1Pressed => _selectSeed1Action != null && _selectSeed1Action.WasPressedThisFrame();

        /// <summary>本帧按下选红种子（键盘 2，TASK-016）。</summary>
        public bool SelectSeed2Pressed => _selectSeed2Action != null && _selectSeed2Action.WasPressedThisFrame();

        /// <summary>本帧按下选黄种子（键盘 3，TASK-016）。</summary>
        public bool SelectSeed3Pressed => _selectSeed3Action != null && _selectSeed3Action.WasPressedThisFrame();

        /// <summary>滚轮滚动增量 y（TASK-016；<Mouse>/scroll 的 Vector2.y，滚动一下非零、松开回 0，按符号判方向）。</summary>
        public float SeedScrollDelta => _seedScrollAction != null ? _seedScrollAction.ReadValue<Vector2>().y : 0f;

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
            _aimHoldAction = _playerMap.FindAction("AimHold");
            _selectSeed1Action = _playerMap.FindAction("SelectSeed1");
            _selectSeed2Action = _playerMap.FindAction("SelectSeed2");
            _selectSeed3Action = _playerMap.FindAction("SelectSeed3");
            _seedScrollAction = _playerMap.FindAction("SeedScroll");
        }

        private void OnEnable() => _playerMap?.Enable();

        private void OnDisable() => _playerMap?.Disable();
    }
}
