using TapCom.Core;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>
    /// 挤压失败检测（TASK-016，挂 Player.prefab 根、与 PlayerActor 同级）：
    /// 订阅 CellWorldChangedEvent{Added}（MagicCircuitSimulator 发布——种子落定/运动格回贴均走此事件，本类只订阅不改它），
    /// 检查落定格与玩家 BoxCollider2D 是否重叠 → 重叠即 Publish FailTriggeredEvent{Crush}，只发一次。
    /// 判定实现：格子是数据字典条目、无物理 Collider2D，Physics2D.OverlapBox 检测不到格，
    /// 故用 AABB 数学重叠（格占 [x,x+1)×[y,y+1)，坐标契约同 Ballistics）+ 0.1f 容差滤贴边擦碰。
    /// MVP 简化规则（拍板）：方块落定压进玩家碰撞箱（两轴重叠深度均 > 0.1f）即失败；
    /// 连续推挤过程中的挤压不做（表现归物理，M3 再议）。状态推进归订阅方（A 线面板）。
    /// </summary>
    public class PlayerCrushDetector : MonoBehaviour
    {
        /// <summary>重叠深度容差：两轴重叠量都超过它才算压进（滤掉贴面相切的误报）。</summary>
        private const float OverlapTolerance = 0.1f;

        private BoxCollider2D _body;
        private bool _triggered;

        private void Awake()
        {
            _body = GetComponent<BoxCollider2D>();
            if (_body == null) Debug.LogError("[PlayerCrushDetector] 缺 BoxCollider2D（挂在有玩家碰撞体的物体上）", this);
        }

        private void OnEnable() => Services.EventBus.Subscribe<CellWorldChangedEvent>(OnCellChanged);

        private void OnDisable() => Services.EventBus.Unsubscribe<CellWorldChangedEvent>(OnCellChanged);

        private void OnCellChanged(CellWorldChangedEvent e)
        {
            if (_triggered || e.Type != CellChangeType.Added || _body == null) return;

            // 格 AABB（cell (x,y) 占 [x,x+1)×[y,y+1)）与玩家 collider 包围盒的重叠深度
            var b = _body.bounds;
            var overlapX = Mathf.Min(e.Position.x + 1f, b.max.x) - Mathf.Max((float)e.Position.x, b.min.x);
            var overlapY = Mathf.Min(e.Position.y + 1f, b.max.y) - Mathf.Max((float)e.Position.y, b.min.y);
            if (overlapX <= OverlapTolerance || overlapY <= OverlapTolerance) return;

            _triggered = true; // 玩家每关由 PlayerSpawner 重建，实例重置天然防跨关残留
            Debug.Log($"[PlayerCrushDetector] 挤压：格 ({e.Position.x},{e.Position.y}) 压进玩家碰撞箱（overlap {overlapX:F2}x{overlapY:F2}，TASK-016 MVP 规则）", this);
            Services.EventBus.Publish(new FailTriggeredEvent { Reason = FailReason.Crush });
        }
    }
}
