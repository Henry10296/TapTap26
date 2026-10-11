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
        private readonly List<GameObject> _seedViews = new List<GameObject>();
        private readonly List<GameObject> _moverViews = new List<GameObject>();
        private readonly Dictionary<Vector3Int, GameObject> _chargeViews = new Dictionary<Vector3Int, GameObject>();
        private MagicCircuitSimulator _simulator;
        private Sprite _square;
        private bool _prefabWarned;
        private bool _spriteWarned;

        private void Start()
        {
            // Texture2D.whiteTexture 是 4×4 白图，pixelsPerUnit=4 ⇒ 1 单位见方
            _square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);

            if (Services.Container.TryGet(out MagicCircuitSimulator simulator)) _simulator = simulator;
            else Debug.LogWarning("[CircuitView] 模拟器未注册（Core 未装配 MagicCircuitSimulator）", this);

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
            foreach (var view in _chargeViews.Values) Destroy(view);
            _chargeViews.Clear();
        }

        private void OnCellChanged(CellWorldChangedEvent e)
        {
            if (e.Type == CellChangeType.Added)
            {
                if (!_cellViews.ContainsKey(e.Position)) CreateCellView(e.Position, e.Attr);
            }
            else
            {
                if (_cellViews.TryGetValue(e.Position, out var view)) Destroy(view.gameObject);
                _cellViews.Remove(e.Position);
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

        private void SyncMovers()
        {
            var movers = _simulator.MovingCells;
            while (_moverViews.Count < movers.Count) _moverViews.Add(NewSquare(Vector3.one, _square));
            for (var i = 0; i < _moverViews.Count; i++)
            {
                var active = i < movers.Count;
                _moverViews[i].SetActive(active);
                if (!active) continue;
                _moverViews[i].transform.position = new Vector3(movers[i].Position.x, movers[i].Position.y, 0f);
                _moverViews[i].GetComponent<SpriteRenderer>().sprite = AttrSprite(movers[i].Magic);
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
                    view = NewSquare(Vector3.one * 0.94f, _square);
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

        private GameObject NewSquare(Vector3 scale, Sprite sprite)
        {
            var go = new GameObject("cell-view");
            go.transform.SetParent(transform, false);
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : _square;
            return go;
        }

        private Sprite SeedSprite(SeedMagicType type) => type switch
        {
            SeedMagicType.Blue => WarnedSprite(_blueSprite, "Blue"),
            SeedMagicType.Red => WarnedSprite(_redSprite, "Red"),
            _ => WarnedSprite(_yellowSprite, "Yellow"),
        };

        private Sprite AttrSprite(CellMagicAttr attr) => attr switch
        {
            CellMagicAttr.Blue => WarnedSprite(_blueSprite, "Blue"),
            CellMagicAttr.Red => WarnedSprite(_redSprite, "Red"),
            CellMagicAttr.Yellow => WarnedSprite(_yellowSprite, "Yellow"),
            _ => _square,
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
