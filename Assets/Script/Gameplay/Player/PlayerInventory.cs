using TapCom.Core;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>玩家资源：各属性种子库存（tech-spec §2 增补；截图初值 蓝×2 / 红×2 / 黄×4）。
    /// M1 无限开关（2026-10-09 拍板）：开启时消耗恒通过、计数不动，验证回路不被补给卡住。</summary>
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private bool _infiniteSeeds = true;

        [SerializeField] private int _blueSeeds = 2;

        [SerializeField] private int _redSeeds = 2;

        [SerializeField] private int _yellowSeeds = 4;

        /// <summary>查询某属性种子余量。</summary>
        public int GetCount(SeedMagicType type) => type switch
        {
            SeedMagicType.Blue => _blueSeeds,
            SeedMagicType.Red => _redSeeds,
            SeedMagicType.Yellow => _yellowSeeds,
            _ => 0,
        };

        /// <summary>消耗一颗种子；余量不足时拒绝（不发事件）。日志即运行时自证信号。</summary>
        public bool TryConsume(SeedMagicType type)
        {
            if (_infiniteSeeds)
            {
                Debug.Log($"[PlayerInventory] 无限模式：{type} 消耗放行（计数不动）", this);
                return true;
            }

            var count = GetCount(type);
            if (count <= 0)
            {
                Debug.Log($"[PlayerInventory] 拒绝：{type} 种子已用完", this);
                return false;
            }

            SetCount(type, count - 1);
            Debug.Log($"[PlayerInventory] {type} {count}→{count - 1}", this);
            return true;
        }

        private void SetCount(SeedMagicType type, int value)
        {
            switch (type)
            {
                case SeedMagicType.Blue: _blueSeeds = value; break;
                case SeedMagicType.Red: _redSeeds = value; break;
                case SeedMagicType.Yellow: _yellowSeeds = value; break;
            }
        }
    }
}
