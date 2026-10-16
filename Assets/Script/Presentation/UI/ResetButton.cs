using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// 重开按钮（TASK-016 A 线）：ResetLevel → LevelManager.Reload()（Playing→Loading 边已有，重载不切场景）。
    /// 挂在重开按钮物体上，Inspector 里 Button.onClick → ResetButton.ResetLevel；显隐归各结算/游戏 UI。
    /// </summary>
    public class ResetButton : MonoBehaviour
    {
        /// <summary>重开当前关（Reload 内部先 TrySetState(Loading)，被拒则不装载）。</summary>
        public void ResetLevel()
        {
            Services.Container.Get<LevelManager>().Reload();
        }
    }
}
