using TapCom.Gameplay;
using TapCom.MagicCircuit;
using TapCom.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TapCom.Core
{
    /// <summary>
    /// Core 装配入口：RuntimeInitializeOnLoadMethod 自动创建 Core 对象，无需手工搭场景。
    /// 已存在 CoreDriver 则跳过（域重载/重复安装安全，Invariant 5）。
    /// </summary>
    public static class CoreBootstrap
    {
        private const string CoreObjectName = "Core";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Install()
        {
            if (Object.FindFirstObjectByType<CoreDriver>() != null) return;

            var go = new GameObject(CoreObjectName);
            Object.DontDestroyOnLoad(go);
            go.AddComponent<CoreDriver>();
            Debug.Log($"[Core] bootstrap installed ({go.GetInstanceID()})");
        }
    }

    /// <summary>
    /// Core 驱动：装配 Services 门面 + Service 容器与各管理器，每帧驱动 TickManager。
    /// 后续模块（Player/Magic/Level）经 Services 门面取用，不各自 new 单例。
    /// </summary>
    public class CoreDriver : MonoBehaviour
    {
        private TickManager _tick;
        private GameManager _game;
        private float _heartbeatTimer = 1f;
        private int _tickCount;

        private void Awake()
        {
            Services.Initialize();
            var services = Services.Container;
            services.Register(services);
            var bus = new EventBus();
            services.Register(bus);
            _tick = new TickManager();
            services.Register(_tick);
            _game = new GameManager(bus);
            services.Register(_game);
            var worlds = new WorldManager();
            services.Register(worlds);
            var levels = new LevelManager(worlds, _game, bus);
            services.Register(levels);

            // TASK-007：Magic Circuit 模拟器（Tick 驱动 + IZeroFieldQuery 供 PlayerMotor 漂移查询）
            var simulator = new MagicCircuitSimulator(bus, worlds, levels);
            services.Register(simulator);
            services.Register<IZeroFieldQuery>(simulator);
            _tick.Register(simulator);

            // TASK-013：音频模块（订阅游戏事件 → AudioCatalog 映射 → Wwise 后端播声；bank 加载归后端首播前自动完成）
            services.Register(new AudioManager(bus, new WwiseAudioBackend()));

            Debug.Log("[Core] services ready: ServiceContainer, EventBus, TickManager, GameManager, WorldManager, LevelManager, MagicCircuitSimulator, AudioManager, SceneFlow");

            // 启动到标题界面（TASK-008）；「开始游戏」按钮 → Loading（LevelBootstrap 装载完转 Playing）。真实关卡装载 M3 归 LevelManager。
            _game.TrySetState(GameState.TitleScreen);

            // 场景流转（进入主世界 / 退回标题）；须晚于 Services 初始化（OnEnable 订阅 EventBus）
            gameObject.AddComponent<SceneFlow>();
        }

        private void OnDestroy()
        {
            // 容器与事件总线随 Play 轮次重建，防域重载关闭时静态残留。
            Services.Shutdown();
        }

        private void Update()
        {
            _tick.Tick(Time.deltaTime);
            _tickCount++;

            // 心跳：前 5 秒每秒一条，之后每 30 秒一条（证明驱动活着，不刷屏）。
            _heartbeatTimer -= Time.deltaTime;
            if (_heartbeatTimer <= 0f)
            {
                _heartbeatTimer = Time.time < 5f ? 1f : 30f;
                Debug.Log($"[Core] tick alive: t={Time.time:F1}s ({_tickCount} ticks)");
            }

            // 调试键 F1：触发一次非法迁移（Playing -> Initializing），验证状态机拒绝路径。
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                _game.TrySetState(GameState.Initializing);
            }
        }
    }
}
