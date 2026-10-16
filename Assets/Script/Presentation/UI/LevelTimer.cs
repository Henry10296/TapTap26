using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// 关卡计时器（TASK-017）：订 LevelLoadedEvent 读 LevelData.TimeLimit（0 = 不限时，计时器隐藏）。
    /// 仅 Playing 态递减（CG/暂停/结算停摆）；剩余比例驱动指针顺时针匀速转一圈（一圈 = 整关限时）。
    /// 归零 Publish FailTriggeredEvent{TimeUp} 只发一次（状态推进归 DefeatPanel：Playing→Defeat 边已有）。
    /// 挂 Canvas 等常驻物体上（本类 SetActive 计时器根）；关卡重载/换关随 LevelLoadedEvent 重新装表。
    /// </summary>
    public class LevelTimer : MonoBehaviour
    {
        [SerializeField] private GameObject _timerRoot; // 计时器根（人工拖 Timer.prefab 实例，显隐用）
        [SerializeField] private RectTransform _hand;   // 指针（clockHand；pivot 须在指针一端，人工调）

        private float _limit;     // 本关限时（秒；0 = 不限时）
        private float _remaining; // 剩余秒数
        private bool _fired;      // TimeUp 只发一次

        private void OnEnable()
        {
            Services.EventBus.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
        }

        private void OnDisable()
        {
            Services.EventBus.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
        }

        private void Start()
        {
            if (_timerRoot == null || _hand == null)
            {
                Debug.LogError("[LevelTimer] 未拖 TimerRoot/Hand 引用（Inspector）");
            }
            Show(false); // 场景搭建若忘了默认隐藏，这里兜底
        }

        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            _limit = e.Data != null ? e.Data.TimeLimit : 0f;
            _remaining = _limit;
            _fired = false;
            Show(_limit > 0f);
            UpdateHand();
        }

        private void Update()
        {
            if (_limit <= 0f || _fired) return;
            // 仅 Playing 计时：CG/暂停/结算停摆（Frozen 态 timeScale=0 双保险，语义门控为准）
            if (Services.Container.Get<GameManager>().Current != GameState.Playing) return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                _fired = true;
                UpdateHand();
                Services.EventBus.Publish(new FailTriggeredEvent { Reason = FailReason.TimeUp });
                return;
            }
            UpdateHand();
        }

        /// <summary>剩余比例 → 指针角度：满时限指 12 点，随时间顺时针扫一圈（3→6→9 点）回 12 点。</summary>
        private void UpdateHand()
        {
            if (_hand == null) return;
            var ratio = _limit > 0f ? _remaining / _limit : 0f;
            _hand.localRotation = Quaternion.Euler(0f, 0f, 360f * ratio);
        }

        private void Show(bool on)
        {
            if (_timerRoot != null) _timerRoot.SetActive(on);
        }
    }
}
