using System.Collections.Generic;
using TapCom.Core;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>
    /// 终点装载器（TASK-016，挂场景持久物体，与 PlayerSpawner 同级，人工配置）：
    /// 订阅 LevelLoadedEvent → 清旧实例（自己清自己，不碰 Binder/CircuitView 的对象）→
    /// Data.GoalPoint ≠ (0,0,0) 时在格心（+(0.5,0.5)）Instantiate 终点预制体；(0,0,0) = 本关无终点，只清不建。
    /// 与 GoalSpot 拆分两文件（用户指令）：Add Component 按类名直查，避免一文件多类查找困扰。
    /// </summary>
    public class GoalSpotLoader : MonoBehaviour
    {
        /// <summary>终点预制体（SpriteRenderer + isTrigger Collider2D + GoalSpot，人工搭建拖入）。</summary>
        [SerializeField] private GameObject _goalPrefab;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private void OnEnable() => Services.EventBus.Subscribe<LevelLoadedEvent>(OnLevelLoaded);

        private void OnDisable() => Services.EventBus.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);

        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            Clear();

            var goal = e.Data.GoalPoint;
            if (goal == Vector3Int.zero) return; // 约定 (0,0,0) = 本关无终点

            if (_goalPrefab == null)
            {
                Debug.LogError("[GoalSpotLoader] 未拖 goal 预制体（SpriteRenderer + isTrigger Collider2D + GoalSpot）", this);
                return;
            }

            var pos = new Vector3(goal.x + 0.5f, goal.y + 0.5f, 0f); // 格心，与 CellActor/PlayerSpawner 定位口径一致
            var go = Instantiate(_goalPrefab, pos, Quaternion.identity, transform);
            go.name = "GoalSpot";
            _spawned.Add(go);
            Debug.Log($"[GoalSpotLoader] goal spawned at {pos} (world '{e.WorldId}')", go);
        }

        /// <summary>清理自己生成的终点实例（帧末 Destroy；同帧内不再收第二个事件，无双清风险）。</summary>
        private void Clear()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Destroy(go);
            }
            _spawned.Clear();
        }
    }
}
