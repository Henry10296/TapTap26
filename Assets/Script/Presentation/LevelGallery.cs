using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// 关卡陈列场景（TASK-010）：把人工拖入的每一关并排装载出来，供检视。
    /// 关卡 id 编排约定：Level_00 = 新手关，数字递增（tech-spec §5）。
    /// 一切物体人工配置（项目 CLAUDE.md）：Levels 数组与预制体全部手拖，无自动扫描。
    /// 用法：建一个陈列场景 → 空物体挂本组件 → 拖关卡资产数组 + CellActor 预制体 → Play。
    /// TASK-011：归位 Presentation（陈列是表现侧工具）；数据装载走 LevelLoader，actor 自生自管。
    /// </summary>
    public class LevelGallery : MonoBehaviour
    {
        [SerializeField] private LevelData[] _levels;      // 按顺序拖：Level_00（新手关）在前
        [SerializeField] private CellActor _cellActorPrefab;
        [SerializeField] private float _spacing = 32f;     // 相邻关卡的横向间距（单位：格）

        private void Start()
        {
            if (_cellActorPrefab == null)
            {
                Debug.LogError("[LevelGallery] 未拖 CellActor 预制体");
                return;
            }

            var worlds = new WorldManager(); // 陈列自用，不进游戏 Services
            var loaded = 0;
            for (var i = 0; i < _levels.Length; i++)
            {
                var data = _levels[i];
                if (data == null) continue;

                var anchor = new GameObject(data.name);
                anchor.transform.SetParent(transform, false);
                anchor.transform.position = new Vector3(i * _spacing, 0f, 0f);

                var world = LevelLoader.Load(worlds, data.name, data);
                foreach (var cell in world.Cells)
                {
                    var actor = Instantiate(_cellActorPrefab, anchor.transform);
                    actor.Bind(cell);
                }
                loaded++;
            }
            Debug.Log($"[LevelGallery] loaded {loaded}/{_levels.Length} levels");
        }
    }
}
