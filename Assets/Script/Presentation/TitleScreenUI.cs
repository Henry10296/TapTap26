using TapCom.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapCom.Presentation
{
    /// <summary>
    /// 开始界面控制器（TASK-008 v2：场景由用户手搭，本脚本只做接线）：
    /// 挂在 Canvas 上，拖 Panel / 开始按钮 / 退出按钮三个引用。
    /// 显示/隐藏跟随 GameState（TitleScreen 显示，其余隐藏）；「开始」→ Loading（LevelBootstrap 装载完转 Playing）。
    /// </summary>
    public class TitleScreenUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;

        private void OnEnable()
        {
            Services.EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
            if (_startButton != null) _startButton.onClick.AddListener(OnStartClicked);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnDisable()
        {
            Services.EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
            if (_startButton != null) _startButton.onClick.RemoveListener(OnStartClicked);
            if (_quitButton != null) _quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private void Start()
        {
            if (_panel == null)
            {
                Debug.LogError("[TitleScreenUI] 未拖 Panel 引用");
                return;
            }
            // CoreDriver 在 BeforeSceneLoad 已切到 TitleScreen（早于本组件），这里补一次显示判断
            Show(Services.Container.Get<GameManager>().Current == GameState.TitleScreen);
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            Show(e.To == GameState.TitleScreen);
        }

        private void Show(bool on)
        {
            if (_panel != null) _panel.SetActive(on);
        }

        private void OnStartClicked()
        {
            Services.Container.Get<GameManager>().TrySetState(GameState.Loading);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
