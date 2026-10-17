using UnityEngine;
using UnityEngine.InputSystem;

namespace TapCom.Presentation
{
    /// <summary>
    /// 图鉴手册（TASK-016 A 线）：Toggle 开/关一张全屏手册图（SetActive 切换），不暂停、不切 GameState。
    /// 挂在 Canvas 等常驻物体上，拖全屏手册面板引用；Inspector 里 Button.onClick → HandbookPanel.Toggle。
    /// 2026-10-17（用户拍板）：Tab 键开/关，Update 轮询 Keyboard.tabKey（同 ResetButton R 键模式），无需改 InputSystem_Actions。
    /// </summary>
    public class HandbookPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel; // 全屏手册图面板（含 Image，人工搭建）

        private void Start()
        {
            if (_panel == null)
            {
                Debug.LogError("[HandbookPanel] 未拖 Panel 引用（Inspector）");
            }
            if (_panel != null) _panel.SetActive(false); // 手册初始收起，场景搭建若忘了默认隐藏，这里兜底
        }

        /// <summary>Tab 键开/关手册（2026-10-17）：新 Input System 直读；wasPressedThisFrame 是属性，勿加括号（CS1955 教训）。</summary>
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        /// <summary>开/关手册图（按钮 onClick 调用；任意状态下可开，不影响状态机）。</summary>
        public void Toggle()
        {
            if (_panel != null) _panel.SetActive(!_panel.activeSelf);
        }
    }
}
