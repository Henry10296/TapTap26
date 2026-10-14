using System.Collections.Generic;
using System.IO;
using TapCom.Gameplay;
using TapCom.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TapCom.EditorTools
{
    /// <summary>
    /// 关卡编辑器（tech-spec §7 Level Editor）：
    /// ①「场景关卡化」给当前场景铺 Grid + Tilemap；②制作者用 Tile Palette 笔刷画 CellTile（笔刷自带格子定义）；
    /// ③「导出 LevelData」把画好的 Tilemap 转成关卡数据资产。
    /// Tilemap 是编辑器工具，运行时只认 LevelData/WorldGrid（铁律 #7）。
    /// </summary>
    public class TilemapLevelWindow : EditorWindow
    {
        private Tilemap _tilemap;
        private string _levelId = "Level_01";
        private int _foreignCount;

        [MenuItem("Tools/魔法回路/关卡编辑器")]
        public static void Open()
        {
            GetWindow<TilemapLevelWindow>("关卡编辑器");
        }

        private void OnGUI()
        {
            _tilemap = (Tilemap)EditorGUILayout.ObjectField("场景 Tilemap", _tilemap, typeof(Tilemap), true);
            _levelId = EditorGUILayout.TextField("关卡 ID", _levelId);

            EditorGUILayout.HelpBox(
                "用法：①「场景关卡化」铺 Tilemap → ② Window > 2D > Tile Palette 建 Palette，" +
                "把 CellTile 资产（Create > 魔法回路 > CellTile）拖进去当笔刷 → ③画关卡 → ④「导出 LevelData」。\n" +
                "笔刷的格子自带 Cell 定义（在 CellTile 资产 Inspector 里配），导出按数据读，不看名字。\n" +
                "出生点：用 SpawnTile 笔刷（Create > 魔法回路 > SpawnTile）画一格，导出记入 SpawnPoint。", MessageType.Info);

            if (GUILayout.Button("① 场景关卡化（Grid + Tilemap）")) SceneSetup();
            if (GUILayout.Button("①b 加背景层（Background_N）")) AddBackgroundLayer();
            if (GUILayout.Button("② 导出 LevelData")) ExportLevelData();
            if (GUILayout.Button("③ 编辑器预览装载")) PreviewLoad();
        }

        /// <summary>把当前场景铺成可画的关卡编辑场景：Grid + Cells(Tilemap+Renderer)。</summary>
        private void SceneSetup()
        {
            if (_tilemap == null)
            {
                var gridGo = GameObject.Find("LevelGrid");
                if (gridGo == null)
                {
                    gridGo = new GameObject("LevelGrid", typeof(Grid));
                    var cellsGo = new GameObject("Cells", typeof(Tilemap), typeof(TilemapRenderer));
                    cellsGo.transform.SetParent(gridGo.transform);
                }
                _tilemap = gridGo.GetComponentInChildren<Tilemap>();
            }
            Selection.activeGameObject = _tilemap.gameObject;
            Debug.Log("[TilemapLevel] 场景已关卡化：用 Palette 笔刷在 Cells 上画 CellTile", _tilemap);
        }

        /// <summary>加一层背景 Tilemap（TASK-015）：Background_N 命名，sortingOrder 递减（越晚加越远）；Palette 拖普通 Tile 当笔刷。</summary>
        private void AddBackgroundLayer()
        {
            var grid = _tilemap != null && _tilemap.transform.parent != null
                ? _tilemap.transform.parent
                : GameObject.Find("LevelGrid")?.transform;
            if (grid == null)
            {
                Debug.LogError("[TilemapLevel] 找不到 LevelGrid（先「场景关卡化」）");
                return;
            }

            var n = 0;
            foreach (Transform child in grid)
                if (child.name.StartsWith("Background_")) n++;

            var go = new GameObject($"Background_{n}", typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = -(n + 1); // Cells 表现为 0，背景依次向远排
            Debug.Log($"[TilemapLevel] 已加 {go.name}（sortingOrder={-(n + 1)}）：Palette 拖普通 Tile 资产当背景笔刷，画完「导出 LevelData」", go);
        }

        /// <summary>导出：读每个笔刷 CellTile 自带的格子定义 → LevelData 资产（Assets/Levels/）。</summary>
        private void ExportLevelData()
        {
            if (_tilemap == null)
            {
                Debug.LogError("[TilemapLevel] 场景里没有 Tilemap（先「场景关卡化」）");
                return;
            }

            var defs = new List<CellDef>();
            _foreignCount = 0;
            Vector3Int? spawnPos = null;
            var spawnCount = 0;
            foreach (var pos in _tilemap.cellBounds.allPositionsWithin)
            {
                var tile = _tilemap.GetTile(pos);
                if (tile == null) continue;
                if (tile is SpawnTile)
                {
                    spawnCount++;
                    spawnPos ??= pos; // 多个时取第一个
                    continue;
                }
                if (tile is CellTile cellTile)
                {
                    defs.Add(new CellDef
                    {
                        Pos = pos,
                        Type = cellTile.cellType,
                        Gravity = cellTile.gravity,
                        Magic = cellTile.magic,
                        Locked = true, // 世界默认冻结（铁律 #9）
                    });
                }
                else
                {
                    _foreignCount++; // 非 CellTile 的笔刷：不认识，跳过并计数
                }
            }

            if (defs.Count == 0)
            {
                Debug.LogWarning("[TilemapLevel] Tilemap 上没有 CellTile（Palette 里拖的应是 Create > 魔法回路 > CellTile 造的资产）");
                return;
            }

            const string dir = "Assets/Levels";
            Directory.CreateDirectory(dir);
            var path = $"{dir}/{_levelId}.asset";
            var data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (data == null)
            {
                data = CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(data, path);
            }
            data.Cells = defs;
            data.SpawnPoint = spawnPos ?? Vector3Int.zero;
            data.BackgroundLayers = ExportBackgroundLayers(_tilemap.transform.parent);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            if (spawnCount == 0)
                Debug.LogWarning("[TilemapLevel] 没画出生点（SpawnTile 笔刷），SpawnPoint 记为 (0,0,0)");
            else if (spawnCount > 1)
                Debug.LogWarning($"[TilemapLevel] 出生点画了 {spawnCount} 个，取第一个 {spawnPos.Value}");

            var warn = _foreignCount > 0 ? $"（跳过非 CellTile {_foreignCount} 个）" : "";
            Debug.Log($"[TilemapLevel] exported: {path}，{defs.Count} defs，spawn={data.SpawnPoint}，背景 {data.BackgroundLayers.Count} 层{warn}", data);
        }

        /// <summary>导出背景层（TASK-015）：读 LevelGrid 下所有 Background_* Tilemap → BackgroundLayerDef（Tile 引用直存，运行时 SetTile 重建）。</summary>
        private static List<BackgroundLayerDef> ExportBackgroundLayers(Transform gridRoot)
        {
            var layers = new List<BackgroundLayerDef>();
            if (gridRoot == null) return layers;

            var maps = new List<Tilemap>();
            foreach (Transform child in gridRoot)
                if (child.name.StartsWith("Background_"))
                {
                    var tm = child.GetComponent<Tilemap>();
                    if (tm != null) maps.Add(tm);
                }
            maps.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            foreach (var tm in maps)
            {
                var renderer = tm.GetComponent<TilemapRenderer>();
                var layer = new BackgroundLayerDef
                {
                    Name = tm.name,
                    SortingOrder = renderer != null ? renderer.sortingOrder : 0,
                    Tiles = new List<BackgroundTileDef>(),
                };
                foreach (var pos in tm.cellBounds.allPositionsWithin)
                {
                    var tile = tm.GetTile(pos);
                    if (tile == null) continue;
                    layer.Tiles.Add(new BackgroundTileDef { Pos = pos, Tile = tile });
                }
                if (layer.Tiles.Count > 0) layers.Add(layer);
            }
            return layers;
        }

        /// <summary>编辑器内预览装载：LevelData → WorldGrid + CellActor，自证整条转换链。</summary>
        private void PreviewLoad()
        {
            var path = $"Assets/Levels/{_levelId}.asset";
            var data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (data == null)
            {
                Debug.LogError($"[TilemapLevel] 找不到 {path}，先「导出 LevelData」");
                return;
            }

            var prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/CellActor.prefab");
            var actor = prefabGo != null ? prefabGo.GetComponent<CellActor>() : null;
            if (actor == null)
            {
                Debug.LogError("[TilemapLevel] 缺 Assets/Prefab/CellActor.prefab（自己做一个，见项目文档）");
                return;
            }

            var old = GameObject.Find("LevelPreview");
            if (old != null) DestroyImmediate(old);
            var parent = new GameObject("LevelPreview");

            // TASK-011 后 LevelLoader 只管数据；编辑器预览自己生成 actor（编辑态没有 ActorBinder 的事件链）
            var mgr = new WorldManager();
            var world = LevelLoader.Load(mgr, _levelId, data);
            foreach (var cell in world.Cells)
            {
                var view = Instantiate(actor, parent.transform);
                view.Bind(cell);
            }
        }
    }
}
