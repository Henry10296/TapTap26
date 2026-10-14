using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// 准备投掷指示（2026-10-13 拍板「瞄准即定色」）：按住右键瞄准时显示当前待投种子贴图。
    /// 颜色源 = PlayerThrower.CurrentSeedType（瞄准开始/每次投出后掷定）——投出的就是显示那颗。
    /// 表现归 Presentation（铁律 #2）；贴图槽人工拖（项目铁律：资产人工配置）。
    /// 挂在 Player.prefab 子物体上（SpriteRenderer 显隐由本类驱动）。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SeedIndicator : MonoBehaviour
    {
        [SerializeField] private Sprite _blueSprite;
        [SerializeField] private Sprite _redSprite;
        [SerializeField] private Sprite _yellowSprite;

        private SpriteRenderer _renderer;
        private PlayerThrower _thrower;
        private PlayerInputReader _input;
        private bool _missingWarned;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _thrower = GetComponentInParent<PlayerThrower>();
            _input = GetComponentInParent<PlayerInputReader>();
        }

        private void Update()
        {
            var show = _thrower != null && _input != null && _input.AimHeld;
            if (_renderer.enabled != show) _renderer.enabled = show;
            if (!show) return;

            _renderer.sprite = SpriteFor(_thrower.CurrentSeedType);
        }

        private Sprite SpriteFor(SeedMagicType type) => type switch
        {
            SeedMagicType.Blue => WarnedSprite(_blueSprite, "Blue"),
            SeedMagicType.Red => WarnedSprite(_redSprite, "Red"),
            _ => WarnedSprite(_yellowSprite, "Yellow"),
        };

        /// <summary>贴图槽没拖：一次性告警并置空贴图（指示器不显示，不糊白块）。</summary>
        private Sprite WarnedSprite(Sprite sprite, string name)
        {
            if (sprite != null) return sprite;
            if (!_missingWarned)
            {
                Debug.LogWarning($"[SeedIndicator] 未拖 {name} 贴图槽（Inspector），指示器不显示", this);
                _missingWarned = true;
            }
            return null;
        }
    }
}
