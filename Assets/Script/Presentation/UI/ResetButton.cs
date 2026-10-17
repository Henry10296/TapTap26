using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TapCom.Presentation
{
    /// <summary>
    /// 重开按钮（TASK-016 A 线）：ResetLevel → LevelManager.Reload()（Playing→Loading 边已有，重载不切场景）。
    /// 挂在重开按钮物体上，Inspector 里 Button.onClick → ResetButton.ResetLevel；显隐归各结算/游戏 UI。
    /// 2026-10-17（用户拍板）：加 R 键热键，Update 轮询 Keyboard.rKey，与按钮点击同一 ResetLevel 路径。
    /// </summary>
    public class ResetButton : MonoBehaviour
    {
        /// <summary>重开当前关（Reload 内部先 TrySetState(Loading)，被拒则不装载）。</summary>
        public void ResetLevel()
        {
            Services.Container.Get<LevelManager>().Reload();
        }

        /// <summary>R 键热键（2026-10-17）：新 Input System 直读，无需改 InputSystem_Actions；边校验归 Reload 内部。</summary>
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                ResetLevel();
            }
        }
    }
}
