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
            }
            Debug.Log($"[LevelActorBinder] bound {_actors.Count} actors for '{e.WorldId}'");
        }

        /// <summary>清理自己名下的表现物（不销毁数据，不碰别人对象）。</summary>
        private void Clear()
        {
            foreach (var actor in _actors)
            {
                if (actor != null) Destroy(actor.gameObject);
            }
            _actors.Clear();
        }
    }
}
