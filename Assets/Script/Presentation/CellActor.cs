using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// cellActor 占位表现（tech-spec §3/§6）：单预制体复用，按 Cell 数据定位 + 着色。
    /// 只读 Cell、绝不回写（铁律 #2 表现不拥有状态）；M4 换正式美术与动画机。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CellActor : MonoBehaviour
    {
        private static readonly Color RockColor = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color BlueColor = new Color(0.30f, 0.55f, 1.00f);
        private static readonly Color RedColor = new Color(1.00f, 0.30f, 0.30f);
        private static readonly Color YellowColor = new Color(1.00f, 0.90f, 0.30f);

        private SpriteRenderer _renderer;

        /// <summary>预制体美术自带配色时可关掉运行时着色。</summary>
        [SerializeField] private bool _tintByCell = true;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>绑定 Cell 数据并刷新占位外观。</summary>
        public void Bind(Cell cell)
        {
            transform.position = (Vector3)cell.Position; // z=0，深度只属于景观（铁律 #11）
            if (!_tintByCell) return;
            _renderer.color = cell.MagicAttr switch
            {
                CellMagicAttr.Blue => BlueColor,
                CellMagicAttr.Red => RedColor,
                CellMagicAttr.Yellow => YellowColor,
                _ => RockColor,
            };
        }
    }
}
