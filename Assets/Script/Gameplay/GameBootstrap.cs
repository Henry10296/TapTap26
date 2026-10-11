using TapCom.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TapCom.Gameplay
{
    /// <summary>
    /// 主世界场景引导（挂在 Game.unity，人工搭场景时拖引用）：
    /// 场景加载完注入关卡序列；从标题进入（当前 Loading 态）则装第一关。
    /// 关内重载/换关由触发源直调 LevelManager.Reload/Advance，不经本类。
    /// Esc = 退出关卡回标题（LevelManager.ExitToTitle → SceneFlow 切场景）。
    /// </summary>
    [DefaultExecutionOrder(100)] // 晚于 CoreDriver（BeforeSceneLoad 已建好 Services）
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelData[] _levels; // 关卡序列，按顺序拖：Level_00（新手关）在前

        private void Start()
        {
            var levels = Services.Container.Get<LevelManager>();
            levels.SetSequence(_levels);

            if (Services.Container.Get<GameManager>().Current != GameState.Loading) return;

            // 从标题进入：状态已是 Loading（按钮切换 + SceneFlow 切场景均早于本 Start），直接装当前关
            if (!levels.LoadCurrent())
            {
                Debug.LogError("[GameBootstrap] 关卡序列为空（Inspector 拖 LevelData 数组）");
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Services.Container.Get<LevelManager>().ExitToTitle();
            }
        }
    }
}
