using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace TapCom.Presentation
{
    /// <summary>
    /// 暂停面板（TASK-016 A 线）：显隐跟随 GameState（Paused 显示，其余隐藏，Esc 切换归 GameBootstrap）。
    /// 「继续」→ TrySetState(Playing)；「回标题」→ LevelManager.ExitToTitle（Paused→TitleScreen 边已有）。
    /// 可挂常驻物体，也可直接挂面板本体（订阅在 Awake/OnDestroy，面板自身失活不掉订阅）。
    /// </summary>
    public class PausePanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;             // 暂停面板（人工搭建）
        [SerializeField] private Button _resumeButton;          // 「继续」按钮
        [SerializeField] private Button _backToTitleButton;     // 「回标题」按钮

        // 订阅/按钮接线挂 Awake/OnDestroy（非 OnEnable/OnDisable）：本类会 SetActive 自己所在的物体
        // （挂面板本体的自包含挂法，2026-10-16 用户拍板），OnDisable 解绑会让隐藏后的面板永远收不到事件
        private void Awake()
        {
            Services.EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
            if (_resumeButton != null) _resumeButton.onClick.AddListener(ResumeGame);
            if (_backToTitleButton != null) _backToTitleButton.onClick.AddListener(BackToTitle);
        }

        private void OnDestroy()
        {
            Services.EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(ResumeGame);
            if (_backToTitleButton != null) _backToTitleButton.onClick.RemoveListener(BackToTitle);
        }

        private void Start()
        {
            if (_panel == null)
            {
                Debug.LogError("[PausePanel] 未拖 Panel 引用（Inspector）");
            }
            Show(false); // 场景搭建若忘了默认隐藏，这里兜底
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            Show(e.To == GameState.Paused);
        }

        private void Show(bool on)
        {
            if (_panel != null) _panel.SetActive(on);
        }

        /// <summary>继续游戏（Paused→Playing 边已有；按钮 onClick 调用）。</summary>
        public void ResumeGame()
        {
            Services.EventBus.Publish(new UiClickedEvent()); // 设计表 Play_UI_Click
            Services.Container.Get<GameManager>().TrySetState(GameState.Playing);
        }

        /// <summary>回标题（Paused→TitleScreen 边已有；ExitToTitle 卸数据 + SceneFlow 切场景）。</summary>
        public void BackToTitle()
        {
            Services.EventBus.Publish(new UiClickedEvent()); // 设计表 Play_UI_Click
            Services.Container.Get<LevelManager>().ExitToTitle();
        }
    }
}
