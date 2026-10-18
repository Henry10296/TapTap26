using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace TapCom.Presentation
{
    /// <summary>
    /// 结算面板（TASK-016 A 线；2026-10-17 用户拍板扩展为胜负共用，挂 GameState.prefab 根）：
    /// 胜利：订 VictoryTriggeredEvent → TrySetState(Victory) → 切 _victorySprite（不拖=保留 Inspector 当前图）→ 显示；「下一关」→ Advance()。
    /// 失败：订 FailTriggeredEvent（挤压/摔落/超时）→ TrySetState(Defeat) → 切 _defeatSprite → 显示；「重开」→ Reload()；失败态隐藏「下一关」（2026-10-18 拍板）。
    /// 对应场景搭建「image（在LevelClear和gameover两张图切换）」：一张 Image 按 _victorySprite/_defeatSprite 切换。
    /// 离开 Victory/Defeat 态即隐藏。订阅/按钮接线挂 Awake/OnDestroy（本类 SetActive 自己所在物体，
    /// 挂面板本体的自包含挂法，2026-10-16 拍板），OnDisable 解绑会让隐藏后的面板永远收不到事件。
    /// </summary>
    public class VictoryPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;         // 结算面板（人工搭建）
        [SerializeField] private Image _resultImage;        // 结算图（LevelClear/gameover 两张切换的那张 Image）
        [SerializeField] private Sprite _victorySprite;     // 胜利图（不拖=保留 Inspector 当前 sprite）
        [SerializeField] private Sprite _defeatSprite;      // 失败图（gameover）
        [SerializeField] private Button _nextLevelButton;   // 「下一关」按钮
        [SerializeField] private Button _resetButton;       // 「重开」按钮（2026-10-17 扩展）

        private void Awake()
        {
            Services.EventBus.Subscribe<VictoryTriggeredEvent>(OnVictory);
            Services.EventBus.Subscribe<FailTriggeredEvent>(OnFail);
            Services.EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
            if (_nextLevelButton != null) _nextLevelButton.onClick.AddListener(NextLevel);
            if (_resetButton != null) _resetButton.onClick.AddListener(ResetLevel);
        }

        private void OnDestroy()
        {
            Services.EventBus.Unsubscribe<VictoryTriggeredEvent>(OnVictory);
            Services.EventBus.Unsubscribe<FailTriggeredEvent>(OnFail);
            Services.EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
            if (_nextLevelButton != null) _nextLevelButton.onClick.RemoveListener(NextLevel);
            if (_resetButton != null) _resetButton.onClick.RemoveListener(ResetLevel);
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
                ApplySprite(_victorySprite);
                SetNextButtonVisible(true);
                Show(true);
            }
        }

        private void OnFail(FailTriggeredEvent e)
        {
            // 失败（挤压/摔落/超时同事件）→ Defeat 态 → 切 gameover 图；迁移被拒则不弹面板
            if (!Services.Container.Get<GameManager>().TrySetState(GameState.Defeat)) return;
            ApplySprite(_defeatSprite);
            SetNextButtonVisible(false);
            Show(true);
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.To != GameState.Victory && e.To != GameState.Defeat) Show(false);
        }

        /// <summary>按结算结果切图（sprite 不拖或 Image 未拖则跳过，不报错）。</summary>
        private void ApplySprite(Sprite sprite)
        {
            if (sprite != null && _resultImage != null) _resultImage.sprite = sprite;
        }

        /// <summary>失败态隐藏「下一关」按钮（2026-10-18 拍板：gameover 只留重开）；胜利态恢复显示。</summary>
        private void SetNextButtonVisible(bool on)
        {
            if (_nextLevelButton != null) _nextLevelButton.gameObject.SetActive(on);
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

        /// <summary>重开当前关（Victory/Defeat→Loading 边已有；2026-10-17 扩展，接 ReSetButton）。</summary>
        public void ResetLevel()
        {
            Services.Container.Get<LevelManager>().Reload();
        }
    }
}
