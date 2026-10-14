using System.Collections.Generic;
using TapCom.Core;
using TapCom.Gameplay;
using TapCom.MagicCircuit;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// M1 动态表现（TASK-007 §8 第 9 步）：字典格=CellActor 实例、飞行种子=小图、运动格=格子图、蓄能组=白图高亮。
    /// 离散变化订阅 CellWorldChangedEvent；持续运动每帧拉模拟器快照（不逐步发事件）。
    /// 落定格实例化 CellActor 预制体并放进 CompositeCollider2D 宿主层级（_cellParent 拖 LevelActorBinder 物体），
    /// 与关卡初始格同款贴图与碰撞——投掷落格=正式岩石格（数据层落定即 Locked，铁律 #9）。只读模拟器与事件，绝不回写（铁律 #2/#6）。
    /// </summary>
    public class CircuitView : MonoBehaviour
    {
        [Header("落定格（与 LevelActorBinder 同款预制体）")]
        [SerializeField] private CellActor _cellPrefab;
        [SerializeField] private Transform _cellParent; // 拖 LevelActorBinder 物体：落在其 CompositeCollider2D 下获得碰撞；留空挂自身（无碰撞）

        [Header("飞行/运动表现贴图（不染色，按属性选图）")]
        [SerializeField] private Sprite _blueSprite;
        [SerializeField] private Sprite _redSprite;
        [SerializeField] private Sprite _yellowSprite;

        private readonly Dictionary<Vector3Int, CellActor> _cellViews = new Dictionary<Vector3Int, CellActor>();
        private readonly Dictionary<int, CellActor> _moverActors = new Dictionary<int, CellActor>(); // 运动格本体：同一 CellActor 从静止视图转入运动视图（不换皮）
        private readonly List<GameObject> _seedViews = new List<GameObject>();
        private readonly Dictionary<Vector3Int, GameObject> _chargeViews = new Dictionary<Vector3Int, GameObject>();
        private readonly HashSet<int> _staleIds = new HashSet<int>();
        private MagicCircuitSimulator _simulator;
        private LevelActorBinder _binder;
        private Sprite _square;
        private bool _prefabWarned;
        private bool _spriteWarned;

        private void Start()
        {
            // Texture2D.whiteTexture 是 4×4 白图，pixelsPerUnit=4 ⇒ 1 单位见方
            _square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);

            if (Services.Container.TryGet(out MagicCircuitSimulator simulator)) _simulator = simulator;
            else Debug.LogWarning("[CircuitView] 模拟器未注册（Core 未装配 MagicCircuitSimulator）", this);

            _binder = FindFirstObjectByType<LevelActorBinder>();

            // 初始关卡表现归 LevelActorBinder（LevelLoadedEvent 驱动）；本类只管模拟器动态增删的格子，防双渲染
            Services.EventBus.Subscribe<CellWorldChangedEvent>(OnCellChanged);
            Services.EventBus.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
        }

        private void OnDestroy()
        {
            if (Services.Container == null) return;
            Services.EventBus.Unsubscribe<CellWorldChangedEvent>(OnCellChanged);
            Services.EventBus.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
        }

        /// <summary>关卡重载：动态视图全清（初始表现由 LevelActorBinder 重建）。</summary>
        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            foreach (var view in _cellViews.Values) Destroy(view.gameObject);
            _cellViews.Clear();
            foreach (var view in _moverActors.Values) Destroy(view.gameObject);
            _moverActors.Clear();
            foreach (var view in _chargeViews.Values) Destroy(view);
            _chargeViews.Clear();
        }

        private void OnCellChanged(CellWorldChangedEvent e)
        {
            switch (e.Type)
            {
                case CellChangeType.Added:
                    if (!_cellViews.ContainsKey(e.Position)) CreateCellView(e.Position, e.Attr);
                    break;

                case CellChangeType.Removed:
                    // 动态格（种子落的）：本体在自己字典里
                    if (_cellViews.TryGetValue(e.Position, out var actor))
                    {
                        _cellViews.Remove(e.Position);
                        if (e.MoverId != 0)
                        {
                            actor.transform.SetParent(transform, false);
                            _moverActors[e.MoverId] = actor;
                        }
                        else
                        {
                            Destroy(actor.gameObject);
                        }
                        break;
                    }

                    // 初始格（LevelActorBinder 生成）：取走本体转运动视图——否则贴图滞留原地而数据飞走（双影 bug）
                    if (e.MoverId != 0 && _binder != null && _binder.TryTake(e.Position, out var taken))
                    {
                        taken.transform.SetParent(transform, false);
                        _moverActors[e.MoverId] = taken;
                    }
                    break;

                case CellChangeType.Changed:
                    // 原位属性变化（引力转岩石/静止固定）：重新 Bind 换贴图
                    if (_cellViews.TryGetValue(e.Position, out var changed)) changed.Bind(e.Position, e.Attr);
                    break;
            }
        }

        private void Update()
        {
            if (_simulator == null) return;
            SyncSeeds();
            SyncMovers();
            SyncCharges();
        }

        private void SyncSeeds()
        {
            var seeds = _simulator.FlyingSeeds;
            while (_seedViews.Count < seeds.Count) _seedViews.Add(NewSquare(Vector3.one * 0.375f, _square));
            for (var i = 0; i < _seedViews.Count; i++)
            {
                var active = i < seeds.Count;
                _seedViews[i].SetActive(active);
                if (!active) continue;
                _seedViews[i].transform.position = new Vector3(seeds[i].Position.x, seeds[i].Position.y, 0f);
                _seedViews[i].GetComponent<SpriteRenderer>().sprite = SeedSprite(seeds[i].Type);
            }
        }

        /// <summary>运动格本体驱动：按 MoverId 对账（落定/出界的 Id 自动销毁其本体——落定处 Added 已生成新静止格）。</summary>
        private void SyncMovers()
        {
            var movers = _simulator.MovingCells;

            // 清理已消失 Id（落定/出界）
            _staleIds.Clear();
            foreach (var id in _moverActors.Keys) _staleIds.Add(id);
            foreach (var mover in movers) _staleIds.Remove(mover.Id);
            foreach (var id in _staleIds)
            {
                if (_moverActors.TryGetValue(id, out var stale) && stale != null) Destroy(stale.gameObject);
                _moverActors.Remove(id);
            }

            // 本体跟随：无本体时兜底实例化（理论必有 Removed 先行转交）
            foreach (var mover in movers)
            {
                if (!_moverActors.TryGetValue(mover.Id, out var actor) || actor == null)
                {
                    if (_cellPrefab == null) continue;
                    actor = Instantiate(_cellPrefab, transform);
                    actor.Bind(default, mover.Magic);
                    _moverActors[mover.Id] = actor;
                }
                actor.transform.position = new Vector3(mover.Position.x, mover.Position.y, 0f);
            }
        }

        private void SyncCharges()
        {
            foreach (var view in _chargeViews.Values) view.SetActive(false);
            foreach (var pending in _simulator.PendingCasts)
            foreach (var pos in pending.Cells)
            {
                if (!_chargeViews.TryGetValue(pos, out var view))
                {
                    view = NewSquare(Vector3.one * 0.94f, _square, ChargeSortingOrder);
                    _chargeViews[pos] = view;
                }
                view.SetActive(true);
                view.transform.position = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0f);
            }
        }

        /// <summary>落定格 = CellActor 实例（贴图+碰撞），放进 _cellParent 层级被 CompositeCollider2D 收编。</summary>
        private void CreateCellView(Vector3Int pos, CellMagicAttr attr)
        {
            if (_cellPrefab == null)
            {
                if (!_prefabWarned)
                {
                    Debug.LogWarning("[CircuitView] 未拖 CellActor 预制体（落定格无表现）", this);
                    _prefabWarned = true;
                }
                return;
            }
            var parent = _cellParent != null ? _cellParent : transform;
            var actor = Instantiate(_cellPrefab, parent);
            actor.Bind(pos, attr);
            _cellViews[pos] = actor;
        }

        /// <summary>蓄能白块排序：显式压过方块贴图——双方块 z=0 同 order 时 Unity 排序未定义，
        /// 白块「有时」被盖住 = 看不到预备效果直接反应（2026-10-13 修复）。</summary>
        private const int ChargeSortingOrder = 10;

        private GameObject NewSquare(Vector3 scale, Sprite sprite, int sortingOrder = 0)
        {
            var go = new GameObject("cell-view");
            go.transform.SetParent(transform, false);
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : _square;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        private Sprite SeedSprite(SeedMagicType type) => type switch
        {
            SeedMagicType.Blue => WarnedSprite(_blueSprite, "Blue"),
            SeedMagicType.Red => WarnedSprite(_redSprite, "Red"),
            _ => WarnedSprite(_yellowSprite, "Yellow"),
        };

        /// <summary>贴图槽没拖：一次性告警并回退白图（不中断）。</summary>
        private Sprite WarnedSprite(Sprite sprite, string name)
        {
            if (sprite != null) return sprite;
            if (!_spriteWarned)
            {
                Debug.LogWarning($"[CircuitView] 未拖 {name} 贴图槽（Inspector），回退白图", this);
                _spriteWarned = true;
            }
            return _square;
        }
    }
}
