using TapCom.Core;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>
    /// 终点本体（TASK-016，挂 goal 预制体根：SpriteRenderer + isTrigger Collider2D + 本组件，人工搭建）：
    /// 玩家碰撞体进入触发区 → Publish VictoryTriggeredEvent{GoalPos=终点世界坐标(格心,z=0)}，只发一次。
    /// 只发事件不切状态：Playing→Victory 推进权归订阅方（A 线 UI 面板）。
    /// 注意订阅与生成不放在本类：LevelLoadedEvent 同步 Publish，预制体实例若也订阅，
    /// 重载瞬间会与装载器对同一事件各自生成一份终点（双生成），故装载职责拆给 GoalSpotLoader。
    /// </summary>
    public class GoalSpot : MonoBehaviour
    {
        private bool _victoryPublished;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_victoryPublished) return;
            if (other.GetComponentInParent<PlayerActor>() == null) return; // 只认玩家（子物体碰撞也归认到 PlayerActor）

            _victoryPublished = true;
            Debug.Log($"[GoalSpot] 玩家抵达终点 {transform.position}（TASK-016）", this);
            Services.EventBus.Publish(new VictoryTriggeredEvent { GoalPos = new Vector2(transform.position.x, transform.position.y) });
        }
    }
}
