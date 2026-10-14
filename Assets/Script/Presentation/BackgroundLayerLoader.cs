using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TapCom.Presentation
{
    /// <summary>
    /// 背景层装载（TASK-015）：LevelLoadedEvent 时按事件携带的 LevelData.BackgroundLayers 重建背景 Tilemap（保持 Tile 形态）。
    /// 分类分层 = 数据里的 SortingOrder，随关卡走；Tile/贴图资产人工（项目铁律）。
    /// 数据随加载链到达（LevelLoadedEvent.Data），本组件只需拖 BackgroundRoot 一个引用。
    /// BackgroundRoot 下只放本类重建的背景层（Clear 清空全牌子）。
    /// </summary>
    public class BackgroundLayerLoader : MonoBehaviour
    {
        [SerializeField] private Transform _backgroundRoot; // 背景层父节点（Grid 之下，人工建）

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
            Clear();
            if (e.Data == null)
            {
                Debug.LogError("[BackgroundLayerLoader] 事件未携带 LevelData（背景层不重建）", this);
                return;
            }

            var parent = _backgroundRoot != null ? _backgroundRoot : transform;
            foreach (var layer in e.Data.BackgroundLayers)
            {
                var go = new GameObject(layer.Name, typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(parent, false);
                go.GetComponent<TilemapRenderer>().sortingOrder = layer.SortingOrder;
                var tm = go.GetComponent<Tilemap>();
                foreach (var def in layer.Tiles)
                {
                    tm.SetTile(def.Pos, def.Tile);
                }
            }
            Debug.Log($"[BackgroundLayerLoader] '{e.WorldId}' 重建背景 {e.Data.BackgroundLayers.Count} 层", this);
        }

        /// <summary>清理重建的背景层（关卡重载防叠层；只清挂点下的，不碰别人物体）。</summary>
        private void Clear()
        {
            var parent = _backgroundRoot != null ? _backgroundRoot : transform;
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
