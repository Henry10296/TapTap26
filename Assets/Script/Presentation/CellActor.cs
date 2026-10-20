using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// cellActor 占位表现（tech-spec §3/§6）：单预制体复用，按 Cell 数据定位 + 贴图。
    /// 只读 Cell、绝不回写（铁律 #2 表现不拥有状态）；贴图槽人工拖（项目铁律：资产人工配置）。
    /// TASK-016：新增基岩贴图槽——按 Cell.Type 先行分流（基岩无色，与 MagicAttr 无关）；
    /// 两参 Bind 保持缺省 Rock，动态落格（种子/运动格，恒 Rock）调用点零改动。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CellActor : MonoBehaviour
    {
        [SerializeField] private Sprite _rockSprite;
        [SerializeField] private Sprite _rock02Sprite;
        [SerializeField] private Sprite _blueSprite;
        [SerializeField] private Sprite _redSprite;
        [SerializeField] private Sprite _yellowSprite;

        /// <summary>基岩地形格贴图（TASK-016）：按 Type 选图；缺槽一次性告警与各色槽同风格。</summary>
        [SerializeField] private Sprite _bedrockSprite;
        [SerializeField] private Sprite _bedrock02Sprite;

        private SpriteRenderer _renderer;
        private bool _missingWarned;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>绑定 Cell 数据并刷新占位外观。</summary>
        public void Bind(Cell cell, Sprite sprite = null) => Bind(cell.Position, cell.MagicAttr, cell.Type, sprite);

        /// <summary>绑定位置与魔法属性（动态落定格无 Cell 实例时用，如 CircuitView 生成投掷落格）。
        /// type 缺省 Rock：动态落格恒为岩石（AddCell 只造 Rock），既有两参调用点零改动。</summary>
        public void Bind(Vector3Int position, CellMagicAttr magicAttr, CellType type = CellType.Rock, Sprite sprite = null)
        {
            // 格子 (x,y) 的视觉中心在 (x+0.5, y+0.5)——与 Tilemap 渲染对齐（TASK-007 Design 发现的半格偏差，本轮修正）
            transform.position = (Vector3)position + new Vector3(0.5f, 0.5f, 0f); // z=0，深度只属于景观（铁律 #11）

            // 数据贴图优先（2026-10-17 拍板：一个 prefab + 每 tile 数据驱动贴图）；null = 按属性槽位回退
            if (sprite != null)
            {
                _renderer.sprite = sprite;
                return;
            }

            var slot = type == CellType.Bedrock
                ? _bedrockSprite // 基岩按类型选图（TASK-016），不落颜色分支
                : magicAttr switch
                {
                    CellMagicAttr.Blue => _blueSprite,
                    CellMagicAttr.Red => _redSprite,
                    CellMagicAttr.Yellow => _yellowSprite,
                    _ => _rockSprite,
                };
            if (slot == null)
            {
                if (!_missingWarned)
                {
                    Debug.LogWarning($"[CellActor] 未拖 {(type == CellType.Bedrock ? "Bedrock" : magicAttr.ToString())} 对应贴图槽（prefab Inspector）", this);
                    _missingWarned = true;
                }
                return;
            }
            _renderer.sprite = slot;
        }
    }
}
