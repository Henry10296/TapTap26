using System.Collections.Generic;
using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// 玩家生成器（挂在 Game.unity，人工拖 Player.prefab 与相机引用）：
    /// 订阅 LevelLoadedEvent → 清旧玩家 → 在关卡出生点实例化新玩家 → 喂给相机跟随。
    /// 位置 = 出生点格心（+0.5,+0.5），与 CellActor 定位口径一致。
    /// 重载/换关时事件重到，先清后生，玩家状态天然干净。
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private PlayerCameraRig _cameraRig;

        private readonly List<GameObject> _spawned = new List<GameObject>();

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
            if (_playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] 未拖 Player.prefab");
                return;
            }

            Clear();

            var pos = new Vector3(e.SpawnPoint.x + 0.5f, e.SpawnPoint.y + 0.5f, 0f);
            var player = Instantiate(_playerPrefab, pos, Quaternion.identity, transform);
            player.name = "Player";
            _spawned.Add(player);

            if (_cameraRig != null) _cameraRig.SetTarget(player.transform);
            Debug.Log($"[PlayerSpawner] player spawned at {pos} (world '{e.WorldId}')");
        }

        /// <summary>清理自己生成的玩家（不碰数据层，不碰别人对象）。</summary>
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
