using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// cellActor 占位表现（tech-spec §3/§6）：单预制体复用，按 Cell 数据定位 + 贴图。
    /// 只读 Cell、绝不回写（铁律 #2 表现不拥有状态）；贴图槽人工拖（项目铁律：资产人工配置）。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CellActor : MonoBehaviour
    {
        [SerializeField] private Sprite _rockSprite;
        [SerializeField] private Sprite _blueSprite;
        [SerializeField] private Sprite _redSprite;
        [SerializeField] private Sprite _yellowSprite;

        private SpriteRenderer _renderer;
        private bool _missingWarned;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>绑定 Cell 数据并刷新占位外观。</summary>
        public void Bind(Cell cell) => Bind(cell.Position, cell.MagicAttr);

        /// <summary>绑定位置与魔法属性（动态落定格无 Cell 实例时用，如 CircuitView 生成投掷落格）。</summary>
        public void Bind(Vector3Int position, CellMagicAttr magicAttr)
        {
            // 格子 (x,y) 的视觉中心在 (x+0.5, y+0.5)——与 Tilemap 渲染对齐（TASK-007 Design 发现的半格偏差，本轮修正）
            transform.position = (Vector3)position + new Vector3(0.5f, 0.5f, 0f); // z=0，深度只属于景观（铁律 #11）

            var sprite = magicAttr switch
            {
                CellMagicAttr.Blue => _blueSprite,
                CellMagicAttr.Red => _redSprite,
                CellMagicAttr.Yellow => _yellowSprite,
                _ => _rockSprite,
            };
            if (sprite == null)
            {
                if (!_missingWarned)
                {
                    Debug.LogWarning($"[CellActor] 未拖 {magicAttr} 对应贴图槽（prefab Inspector）", this);
                    _missingWarned = true;
                }
                return;
            }
            _renderer.sprite = sprite;
        }
    }
}
