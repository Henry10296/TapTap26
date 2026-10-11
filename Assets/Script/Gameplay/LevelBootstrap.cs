using TapCom.Core;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>
    /// 场景侧关卡引用容器（M1 测试用，M3 归 LevelManager 场景装配）：
    /// 只做两件事——Play 时隐藏编辑用 Tilemap；Loading 进入时调 LevelManager.Load。
    /// 表现物生成归 LevelActorBinder，状态推进归 LevelManager（TASK-011 后本类零编排逻辑）。
    /// </summary>
    [DefaultExecutionOrder(100)] // 晚于 CoreDriver（BeforeSceneLoad 已建好 Services）
    public class LevelBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelData _levelData;
        [SerializeField] private GameObject _editorTilemapRoot; // 编辑用 LevelGrid，运行时隐藏，退出 Play 自动还原
        [SerializeField] private string _worldId = "Level_01";

        private bool _loaded;

        private void Start()
        {
            if (_editorTilemapRoot != null) _editorTilemapRoot.SetActive(false);
            Services.EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnDestroy()
        {
            Services.EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.To != GameState.Loading || _loaded) return;
            _loaded = true;

            if (_levelData == null)
            {
                Debug.LogError("[LevelBootstrap] 未拖 LevelData（先在关卡编辑器里「导出 LevelData」）");
                return;
            }

            Services.Container.Get<LevelManager>().Load(_worldId, _levelData);
        }
    }
}
