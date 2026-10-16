using TapCom.Core;
using UnityEngine;
namespace TapCom.Gameplay
{
    /// <summary>玩家资源：各属性种子库存（tech-spec §2 增补）。
    /// TASK-019：每关初始数量由 LevelData 自配，PlayerSpawner 生成玩家后 ApplyLevelSeeds 注入
    /// （旧实例连同扣减一起销毁，重载天然干净；字段序列化值仅 Inspector 占位）。
    /// 无限开关保留做调试（2026-10-09 拍板：开启时消耗恒通过、计数不动），默认关。
    /// 种子耗尽判定不做（TASK-016 拍板暂不，只拒绝投掷）。</summary>
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private bool _infiniteSeeds = false;

        [SerializeField] private int _blueSeeds = 3;

        [SerializeField] private int _redSeeds = 3;

        [SerializeField] private int _yellowSeeds = 3;

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

        /// <summary>按关卡数据覆盖三色初始库存并全量发布（TASK-019；PlayerSpawner 生成玩家后调用，0 = 该色 0 颗）。</summary>
        public void ApplyLevelSeeds(LevelData data)
        {
            if (data == null) return;
            _blueSeeds = data.BlueSeeds;
            _redSeeds = data.RedSeeds;
            _yellowSeeds = data.YellowSeeds;
            PublishCount(SeedMagicType.Blue);
            PublishCount(SeedMagicType.Red);
            PublishCount(SeedMagicType.Yellow);
            Debug.Log($"[PlayerInventory] 关卡种子：Blue {data.BlueSeeds} / Red {data.RedSeeds} / Yellow {data.YellowSeeds}", this);
        }

        private void Start()
        {
            // TASK-018：HUD 常驻场景而本组件随玩家每关重建，初始库存全量发布一次（订阅方拿到即绑）
            PublishCount(SeedMagicType.Blue);
            PublishCount(SeedMagicType.Red);
            PublishCount(SeedMagicType.Yellow);
        }

        private void SetCount(SeedMagicType type, int value)
        {
            switch (type)
            {
                case SeedMagicType.Blue: _blueSeeds = value; break;
                case SeedMagicType.Red: _redSeeds = value; break;
                case SeedMagicType.Yellow: _yellowSeeds = value; break;
            }
            PublishCount(type);
        }

        /// <summary>库存变化唯一发布口（TASK-018：SeedHud 据此写数量文本）。</summary>
        private void PublishCount(SeedMagicType type)
        {
            Services.EventBus.Publish(new SeedCountChangedEvent { Type = type, Count = GetCount(type) });
        }
    }
}
