using TapCom.Core;
using TMPro;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// 种子 HUD（TASK-018）：订 SeedCountChangedEvent 写各色数量文本；
    /// 订 SeedTypeChangedEvent 把 ChoiceMark 平移到所选种子图标旁（图标竖排一列，纯位置跟随，_offset 可调）。
    /// 初始绑定归发布方（PlayerInventory/PlayerThrower 初始全量发布，随玩家每关重建自动刷新），本类纯订阅。
    /// 挂 Hud.prefab 根上，拖 3 文本 + 3 图标 + ChoiceMark。
    /// </summary>
    public class SeedHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text _blueCount;       // Blue 图标下数量文本
        [SerializeField] private TMP_Text _redCount;        // Red 图标下数量文本
        [SerializeField] private TMP_Text _yellowCount;     // Yellow 图标下数量文本
        [SerializeField] private RectTransform _blueIcon;   // 蓝图标（指针跟随目标，与 ChoiceMark 同父坐标系）
        [SerializeField] private RectTransform _redIcon;
        [SerializeField] private RectTransform _yellowIcon;
        [SerializeField] private RectTransform _choiceMark; // 指针（ChoiceMark）
        [SerializeField] private Vector2 _offset = new Vector2(0f, -95f); // 指针相对所选图标的偏移（人工调）

        private void OnEnable()
        {
            Services.EventBus.Subscribe<SeedCountChangedEvent>(OnCountChanged);
            Services.EventBus.Subscribe<SeedTypeChangedEvent>(OnTypeChanged);
        }

        private void OnDisable()
        {
            Services.EventBus.Unsubscribe<SeedCountChangedEvent>(OnCountChanged);
            Services.EventBus.Unsubscribe<SeedTypeChangedEvent>(OnTypeChanged);
        }

        private void Start()
        {
            if (_choiceMark == null || _blueIcon == null || _redIcon == null || _yellowIcon == null
                || _blueCount == null || _redCount == null || _yellowCount == null)
            {
                Debug.LogError("[SeedHud] 引用未拖齐（3 文本 + 3 图标 + ChoiceMark）", this);
            }
        }

        private void OnCountChanged(SeedCountChangedEvent e)
        {
            var text = e.Type switch
            {
                SeedMagicType.Blue => _blueCount,
                SeedMagicType.Red => _redCount,
                _ => _yellowCount,
            };
            if (text != null) text.text = e.Count.ToString();
        }

        private void OnTypeChanged(SeedTypeChangedEvent e)
        {
            if (_choiceMark == null) return;
            var icon = e.Type switch
            {
                SeedMagicType.Blue => _blueIcon,
                SeedMagicType.Red => _redIcon,
                _ => _yellowIcon,
            };
            if (icon != null) _choiceMark.anchoredPosition = icon.anchoredPosition + _offset;
        }
    }
}
