using TapCom.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TapCom.Core
{
    /// <summary>
    /// 场景流转（挂在 Core 物体上，CoreBootstrap 装配）：
    /// 进入 = TitleScreen → Loading 切主世界；退出 = 到 TitleScreen 切回开始场景。
    /// 用事件 From 区分「从标题进主世界」与「关内重载/换关」——后者同场景重装，不切场景。
    /// 关卡生命周期归 LevelManager，本类只管场景（职责单一）。
    /// </summary>
    public class SceneFlow : MonoBehaviour
    {
        public const string StartSceneName = "Start";
        public const string GameSceneName = "Game";

        private void OnEnable()
        {
            Services.EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnDisable()
        {
            Services.EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.From == GameState.TitleScreen && e.To == GameState.Loading)
            {
                Load(GameSceneName);
            }
            else if (e.To == GameState.TitleScreen)
            {
                Load(StartSceneName);
            }
        }

        private static void Load(string sceneName)
        {
            var current = SceneManager.GetActiveScene().name;
            if (current == sceneName) return; // 已在目标场景（如编辑器直接 Play 主世界），不重复切
            Debug.Log($"[SceneFlow] {current} -> {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
    }
}
