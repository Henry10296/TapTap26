using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace TapCom.Presentation
{
    /// <summary>
    /// 胜利面板（TASK-016 A 线）：订阅 VictoryTriggeredEvent → TrySetState(Victory)（Playing→Victory 边已有）
    /// → 显示；离开 Victory 态即隐藏。「下一关」→ LevelManager.Advance()（Victory→Loading 边已有；
    /// 最后一关 Advance 内部自动回标题）。可挂常驻物体，也可直接挂面板本体（订阅在 Awake/OnDestroy，面板自身失活不掉订阅）。
    /// </summary>
    public class VictoryPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;         // 胜利面板（人工搭建）
        [SerializeField] private Button _nextLevelButton;   // 「下一关」按钮

        // 订阅/按钮接线挂 Awake/OnDestroy（非 OnEnable/OnDisable）：本类会 SetActive 自己所在的物体
        // （挂面板本体的自包含挂法，2026-10-16 用户拍板），OnDisable 解绑会让隐藏后的面板永远收不到事件
        private void Awake()
        {
            Services.EventBus.Subscribe<VictoryTriggeredEvent>(OnVictory);
            Services.EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
            if (_nextLevelButton != null) _nextLevelButton.onClick.AddListener(NextLevel);
        }

        private void OnDestroy()
        {
            Services.EventBus.Unsubscribe<VictoryTriggeredEvent>(OnVictory);
            Services.EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
            if (_nextLevelButton != null) _nextLevelButton.onClick.RemoveListener(NextLevel);
        }

        private void Start()
        {
            if (_panel == null)
            {
                Debug.LogError("[VictoryPanel] 未拖 Panel 引用（Inspector）");
            }
            Show(false); // 场景搭建若忘了默认隐藏，这里兜底
        }

        private void OnVictory(VictoryTriggeredEvent e)
        {
            // 状态推进归本订阅方（事件语义：到达终点）；迁移被拒（如非 Playing 态）则不弹面板
            if (Services.Container.Get<GameManager>().TrySetState(GameState.Victory))
            {
                Show(true);
            }
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.To != GameState.Victory) Show(false);
        }

        private void Show(bool on)
        {
            if (_panel != null) _panel.SetActive(on);
        }

        /// <summary>下一关（Victory→Loading 边已有；最后一关 Advance 自动回标题）。</summary>
        public void NextLevel()
        {
            Services.Container.Get<LevelManager>().Advance();
        }
    }
}
