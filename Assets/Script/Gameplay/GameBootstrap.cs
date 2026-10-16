using TapCom.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TapCom.Gameplay
{
    /// <summary>
    /// 主世界场景引导（挂在 Game.unity，人工搭场景时拖引用）：
    /// 场景加载完注入关卡序列（LevelSequence 资产，TASK-016）；从标题进入（当前 Loading 态）则装第一关。
    /// 关内重载/换关由触发源直调 LevelManager.Reload/Advance，不经本类。
    /// Esc = 暂停切换（TASK-016：Playing→Paused / Paused→Playing；面板显隐归 PausePanel）。
    /// </summary>
    [DefaultExecutionOrder(100)] // 晚于 CoreDriver（BeforeSceneLoad 已建好 Services）
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelSequence _sequence; // 关卡序列资产（TASK-016），按序配置 LevelId + LevelData

        private void Start()
        {
            var levels = Services.Container.Get<LevelManager>();
            if (_sequence == null)
            {
                Debug.LogError("[GameBootstrap] 未拖 LevelSequence 资产（Inspector）");
                levels.SetSequence(new LevelData[0]);
                return;
            }
            levels.SetSequence(_sequence.ToArray());

            if (Services.Container.Get<GameManager>().Current != GameState.Loading) return;

            // 从标题进入：状态已是 Loading（按钮切换 + SceneFlow 切场景均早于本 Start），直接装当前关
            if (!levels.LoadCurrent())
            {
                Debug.LogError("[GameBootstrap] LevelSequence 未配任何关卡（Entries 为空）");
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

            // 暂停切换（TASK-016）：仅在 Playing/Paused 之间摆动，其余状态（Cg/Victory/Defeat 等）Esc 无效
            var game = Services.Container.Get<GameManager>();
            if (game.Current == GameState.Playing) game.TrySetState(GameState.Paused);
            else if (game.Current == GameState.Paused) game.TrySetState(GameState.Playing);
        }
    }
}
