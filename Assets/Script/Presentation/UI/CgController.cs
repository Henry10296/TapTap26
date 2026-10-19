using System.Collections.Generic;
using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TapCom.Presentation
{
    /// <summary>
    /// 开场演出 UI（TASK-016 A 线，C0 契约）：Cg 态下接管 CG 图显示与点击推进。
    /// 流程：进 Cg → 有图且本会话该关未播过 → 显示 CG 图，等左键/空格 → Publish(IntroStartedEvent)（相机开运镜）
    /// → 收 IntroFinishedEvent → TrySetState(Cg→Playing)；无图或已播过 → 进 Cg 直接发 IntroStartedEvent。
    /// 「会话内一次」：static HashSet 按 worldId 记录，重开/重载不再显示图，但仍走 IntroStartedEvent 流程。
    /// 防重入：IntroStarted 之后（运镜中）再点无效。可挂常驻物体，也可直接挂面板本体
/// （订阅在 Awake/OnDestroy，本类隐藏宿主不掉订阅）。
    /// 运镜与 IntroFinishedEvent 发布归相机（B 线）；LevelManager.Load 推进 Cg 也归 B 线。
    /// </summary>
    public class CgController : MonoBehaviour
    {
        [SerializeField] private GameObject _cgPanel; // 全屏 CG 面板（含 Image，人工搭建）
        [SerializeField] private Image _cgImage;      // 显示 Data.CgImage 的 Image

        /// <summary>本会话已播过 CG 图的关卡（Play 轮次内常驻；域重载随 Play 重建）。</summary>
        private static readonly HashSet<string> _shownWorlds = new HashSet<string>();

        private LevelData _data;        // 最近装载关卡（LevelLoadedEvent 缓存）
        private string _worldId;        // 最近装载关卡 id（会话内一次判重键）
        private bool _waitingForInput;  // CG 图已显示、等点击/空格；发 IntroStarted 后即 false（运镜中再点无效）

        // 订阅挂 Awake/OnDestroy（非 OnEnable/OnDisable）：本类会 SetActive 自己所在的物体
        // （挂面板本体的自包含挂法，2026-10-16 用户拍板），OnDisable 解绑会让隐藏后的面板永远收不到事件
        private void Awake()
        {
            Services.EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
            Services.EventBus.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
            Services.EventBus.Subscribe<IntroFinishedEvent>(OnIntroFinished);
        }

        private void OnDestroy()
        {
            Services.EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
            Services.EventBus.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
            Services.EventBus.Unsubscribe<IntroFinishedEvent>(OnIntroFinished);
        }

        private void Start()
        {
            if (_cgPanel == null || _cgImage == null)
            {
                Debug.LogError("[CgController] 未拖 CG 面板 / Image 引用（Inspector）");
            }
            HideImage(); // 场景搭建若忘了默认隐藏，这里兜底
        }

        private void Update()
        {
            if (!_waitingForInput) return;

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if ((mouse != null && mouse.leftButton.wasPressedThisFrame)
                || (keyboard != null && keyboard.spaceKey.wasPressedThisFrame))
            {
                StartCameraIntro();
            }
        }

        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            _worldId = e.WorldId;
            _data = e.Data;
        }

        private void OnStateChanged(GameStateChangedEvent e)
        {
            if (e.To == GameState.Cg)
            {
                BeginIntro();
            }
            else if (e.From == GameState.Cg)
            {
                EndIntro(); // 离开 Cg（正常推进 Playing 或异常跳转）都收尾
            }
        }

        private void OnIntroFinished(IntroFinishedEvent e)
        {
            // 防串扰：仅演出期（Cg 态）收到的运镜完成才推进；收尾隐藏由 OnStateChanged(From=Cg) 完成
            var game = Services.Container.Get<GameManager>();
            if (game.Current != GameState.Cg) return;
            game.TrySetState(GameState.Playing);
        }

        /// <summary>进 Cg：有图且本会话未播过 → 显示图等输入；否则直接开运镜（图不再显示，流程仍走 IntroStarted）。</summary>
        private void BeginIntro()
        {
            Debug.Log($"[CgController] TEMP-DIAG BeginIntro: dataNull={_data == null}, cgImageNull={_data == null || _data.CgImage == null}, worldId=[{_worldId}], shownCount={_shownWorlds.Count}");
            var fresh = _data != null && _data.CgImage != null
                        && !string.IsNullOrEmpty(_worldId) && !_shownWorlds.Contains(_worldId);
            if (!fresh)
            {
                if (_data == null)
                {
                    Debug.LogError("[CgController] 进 Cg 但无关卡数据（LevelLoadedEvent 未先于 Cg 到达？）");
                }
                StartCameraIntro();
                return;
            }

            _shownWorlds.Add(_worldId);
            ShowImage(_data.CgImage);
            _waitingForInput = true;
        }

        /// <summary>发 IntroStartedEvent（相机开运镜）并立即解锁进 Playing（2026-10-15 用户拍板「点完 CG 立刻开始玩」：
        /// 运镜降级为纯镜头表现，不再冻结世界等它跑完）。此后点击无效；运镜完成事件到达时
        /// OnIntroFinished 因已非 Cg 态自动空转，契约不变。</summary>
        private void StartCameraIntro()
        {
            _waitingForInput = false;
            HideImage();
            Services.EventBus.Publish(new IntroStartedEvent());
            Services.Container.Get<GameManager>().TrySetState(GameState.Playing);
        }

        private void EndIntro()
        {
            _waitingForInput = false;
            HideImage();
        }

        private void ShowImage(Sprite sprite)
        {
            if (_cgPanel == null || _cgImage == null) return;
            _cgImage.sprite = sprite;
            _cgPanel.SetActive(true);
        }

        private void HideImage()
        {
            if (_cgPanel != null) _cgPanel.SetActive(false);
        }
    }
}
