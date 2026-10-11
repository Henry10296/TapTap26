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

        private void OnEnable()
        {
            Services.EventBus.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
        }

        private void OnDisable()
        {
            Services.EventBus.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
        }

        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            if (_cellActorPrefab == null)
            {
                Debug.LogError("[LevelActorBinder] 未拖 CellActor 预制体");
                return;
            }

            Clear();
            foreach (var cell in e.World.Cells)
            {
                var actor = Instantiate(_cellActorPrefab, transform);
                actor.Bind(cell);
                _actors.Add(actor);
                _actorByPos[cell.Position] = actor;
            }
            Debug.Log($"[LevelActorBinder] bound {_actors.Count} actors for '{e.WorldId}'");
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
