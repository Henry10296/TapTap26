using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace TapCom.Presentation
{
    /// <summary>
    /// 失败面板（TASK-016 A 线）：订阅 FailTriggeredEvent → TrySetState(Defeat)（Playing→Defeat 边已有）
    /// → 显示（按 FailReason 更新原因文案，_reasonText 可不拖则跳过）；离开 Defeat 态即隐藏。
    /// 「重试」→ LevelManager.Reload()（Defeat→Loading 边已有）；「回标题」→ ExitToTitle（Defeat→TitleScreen 边已有）。
    /// 可挂常驻物体，也可直接挂面板本体（订阅在 Awake/OnDestroy，面板自身失活不掉订阅）。
    /// </summary>
    public class DefeatPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;         // 失败面板（人工搭建）
        [SerializeField] private Text _reasonText;          // 失败原因文案（UnityEngine.UI.Text，可不拖）
        [SerializeField] private Button _retryButton;       // 「重试」按钮
        [SerializeField] private Button _backToTitleButton; // 「回标题」按钮

        // 订阅/按钮接线挂 Awake/OnDestroy（非 OnEnable/OnDisable）：本类会 SetActive 自己所在的物体
        // （挂面板本体的自包含挂法，2026-10-16 用户拍板），OnDisable 解绑会让隐藏后的面板永远收不到事件
        private void Awake()
        {
            Services.EventBus.Subscribe<FailTriggeredEvent>(OnFail);
            Services.EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
            if (_retryButton != null) _retryButton.onClick.AddListener(Retry);
            if (_backToTitleButton != null) _backToTitleButton.onClick.AddListener(BackToTitle);
        }

        private void OnDestroy()
        {
            Services.EventBus.Unsubscribe<FailTriggeredEvent>(OnFail);
            Services.EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
            if (_retryButton != null) _retryButton.onClick.RemoveListener(Retry);
            if (_backToTitleButton != null) _backToTitleButton.onClick.RemoveListener(BackToTitle);
        }

        private void Start()
        {
            if (_panel == null)
            {
                Debug.LogError("[DefeatPanel] 未拖 Panel 引用（Inspector）");
            }
            Show(false); // 场景搭建若忘了默认隐藏，这里兜底
        }

        private void OnFail(FailTriggeredEvent e)
        {
            // 状态推进归本订阅方（事件语义：玩家失败）；迁移被拒（如非 Playing 态）则不弹面板
            if (!Services.Container.Get<GameManager>().TrySetState(GameState.Defeat)) return;
            UpdateReason(e.Reason);
            Show(true);
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.To != GameState.Defeat) Show(false);
        }

        private void UpdateReason(FailReason reason)
        {
            if (_reasonText == null) return;
            _reasonText.text = reason switch
            {
                FailReason.Fall => "失败原因：摔落",
                FailReason.Crush => "失败原因：被挤压",
                _ => "失败原因：时间到",
            };
        }

        private void Show(bool on)
        {
            if (_panel != null) _panel.SetActive(on);
        }

        /// <summary>重试当前关（Defeat→Loading 边已有；Reload 装载完推进归 LevelManager）。</summary>
        public void Retry()
        {
            Services.Container.Get<LevelManager>().Reload();
        }

        /// <summary>回标题（Defeat→TitleScreen 边已有；ExitToTitle 卸数据 + SceneFlow 切场景）。</summary>
        public void BackToTitle()
        {
            Services.Container.Get<LevelManager>().ExitToTitle();
        }
    }
}
