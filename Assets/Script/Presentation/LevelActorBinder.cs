using System.Collections.Generic;
using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// 关卡表现绑定器（TASK-011）：订阅 LevelLoadedEvent，生成/清理 CellActor。
    /// 表现归 Presentation（tech-spec §2/铁律 #2）；场景里挂一个，拖 CellActor 预制体即可。
    /// </summary>
    public class LevelActorBinder : MonoBehaviour
    {
        [SerializeField] private CellActor _cellActorPrefab;

        private readonly List<CellActor> _actors = new List<CellActor>();
        private readonly Dictionary<Vector3Int, CellActor> _actorByPos = new Dictionary<Vector3Int, CellActor>();
        private readonly Dictionary<Vector3Int, Sprite> _spriteByPos = new Dictionary<Vector3Int, Sprite>(); // 本关数据贴图（2026-10-17：一个 prefab + 每 tile 数据）

        private void OnEnable()
        {
            Services.EventBus.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
            Services.EventBus.Subscribe<CellWorldChangedEvent>(OnCellWorldChanged);
        }

        private void OnDisable()
        {
            Services.EventBus.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
            Services.EventBus.Unsubscribe<CellWorldChangedEvent>(OnCellWorldChanged);
        }

        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            if (_cellActorPrefab == null)
            {
                Debug.LogError("[LevelActorBinder] 未拖 CellActor 预制体");
                return;
            }

            Clear();
            _spriteByPos.Clear();
            if (e.Data != null)
                foreach (var def in e.Data.Cells)
                    if (def.Sprite != null) _spriteByPos[def.Pos] = def.Sprite;
            foreach (var cell in e.World.Cells)
            {
                var actor = Instantiate(_cellActorPrefab, transform);
                actor.Bind(cell, _spriteByPos.TryGetValue(cell.Position, out var dataSprite) ? dataSprite : null);
                _actors.Add(actor);
                _actorByPos[cell.Position] = actor;
            }
            Debug.Log($"[LevelActorBinder] bound {_actors.Count} actors for '{e.WorldId}'");
        }

        /// <summary>
        /// 初始格表现响应模拟器增删（修「幽灵方块」：消失配方/爆炸三块销毁发 Removed(MoverId=0)，
        /// 数据已出字典而本类 actor 滞留 → 表现看得见、种子穿过、玩家仍被碰撞）。
        /// MoverId≠0 的 Removed = 解锁转运动视图，本体由 CircuitView.TryTake 接管，本类不动作。
        /// </summary>
        private void OnCellWorldChanged(CellWorldChangedEvent e)
        {
            switch (e.Type)
            {
                case CellChangeType.Removed:
                    if (e.MoverId != 0) return;
                    if (_actorByPos.TryGetValue(e.Position, out var actor) && actor != null)
                    {
                        _actors.Remove(actor);
                        _actorByPos.Remove(e.Position);
                        Destroy(actor.gameObject);
                    }
                    break;

                case CellChangeType.Changed:
                    // 原位属性变化（引力三块转岩石/静止固定）：重绑换贴图
                    if (_actorByPos.TryGetValue(e.Position, out var changed) && changed != null)
                        changed.Bind(e.Position, e.Attr);
                    break;
            }
        }

        /// <summary>
        /// 取走指定格子的表现本体（解锁转运动视图时由 CircuitView 调用）：
        /// 本体跟随数据飞行，不销毁重建（GDD「数据与图片一起更新」）。取走后不再归本类清理。
        /// </summary>
        public bool TryTake(Vector3Int pos, out CellActor actor)
        {
            if (_actorByPos.TryGetValue(pos, out actor) && actor != null)
            {
                _actorByPos.Remove(pos);
                _actors.Remove(actor);
                return true;
            }
            actor = null;
            return false;
        }

        /// <summary>按位置查询初始格本体（只读，不取走；蓄能闪烁等表现查询用，2026-10-17）。</summary>
        public bool TryGet(Vector3Int pos, out CellActor actor) => _actorByPos.TryGetValue(pos, out actor);

        /// <summary>清理自己名下的表现物（不销毁数据，不碰别人对象）。</summary>
        private void Clear()
        {
            foreach (var actor in _actors)
            {
                if (actor != null) Destroy(actor.gameObject);
            }
            _actors.Clear();
            _actorByPos.Clear();
        }
    }
}
