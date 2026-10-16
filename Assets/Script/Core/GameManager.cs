using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapCom.Core
{
    /// <summary>状态迁移事件：每次成功迁移发布一条，订阅方只读。</summary>
    public readonly struct GameStateChangedEvent
    {
        public readonly GameState From;
        public readonly GameState To;

        public GameStateChangedEvent(GameState from, GameState to)
        {
            From = from;
            To = to;
        }
    }

    /// <summary>非法状态迁移异常（GameManager 内部构造）。</summary>
    public class GameTransitionException : Exception
    {
        public GameState From { get; }
        public GameState To { get; }

        public GameTransitionException(GameState from, GameState to)
            : base($"illegal state transition: {from} -> {to}")
        {
            From = from;
            To = to;
        }
    }

    /// <summary>
    /// 管理整个游戏生命周期：启动、初始化、状态切换、结束（tech-spec §1）。
    /// 只协调生命周期，不拥有任何玩法逻辑。
    /// </summary>
    public class GameManager
    {
        /// <summary>状态迁移白名单：表外迁移一律拒绝（Invariant 4：状态机之外无暗门）。</summary>
        public static readonly IReadOnlyDictionary<GameState, GameState[]> AllowedTransitions =
            new Dictionary<GameState, GameState[]>
            {
                { GameState.Initializing, new[] { GameState.TitleScreen, GameState.Loading } },
                { GameState.TitleScreen, new[] { GameState.Loading } },
                { GameState.Loading, new[] { GameState.Playing, GameState.Cg } },
                { GameState.Cg, new[] { GameState.Playing } },
                { GameState.Playing, new[] { GameState.Paused, GameState.Victory, GameState.Defeat, GameState.Loading, GameState.TitleScreen } },
                { GameState.Paused, new[] { GameState.Playing, GameState.TitleScreen } },
                { GameState.Victory, new[] { GameState.Loading, GameState.TitleScreen } },
                { GameState.Defeat, new[] { GameState.Loading, GameState.TitleScreen } },
            };

        private readonly EventBus _bus;

        public GameState Current { get; private set; }

        public GameManager(EventBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            Current = GameState.Initializing;
        }

        /// <summary>
        /// 尝试迁移状态。合法：迁移 + info 日志 + 发布 GameStateChangedEvent；
        /// 非法：error 日志 + return false（不向调用方抛出）。
        /// </summary>
        public bool TrySetState(GameState next)
        {
            if (next == Current)
            {
                Debug.LogError($"[Core] rejected state transition {Current} -> {next} (same state)");
                return false;
            }
            if (!AllowedTransitions.TryGetValue(Current, out var allowed) || Array.IndexOf(allowed, next) < 0)
            {
                Debug.LogError($"[Core] rejected state transition {Current} -> {next}");
                return false;
            }
            var from = Current;
            Current = next;
            // TASK-016：静止态（CG 演出/暂停/结算）冻结世界——Time.timeScale=0 使 Unity 物理
            // 与 TickManager（CoreDriver 按 Time.deltaTime 驱动，CoreBootstrap.cs:82）同时停摆；
            // 相机与 UI 侧一律用 unscaled 时间（C 线契约）。
            Time.timeScale = IsFrozenState(next) ? 0f : 1f;
            Debug.Log($"[Core] GameState: {from} -> {next}");
            _bus.Publish(new GameStateChangedEvent(from, next));
            return true;
        }

        /// <summary>该状态下世界是否静止（TASK-016）。</summary>
        public static bool IsFrozenState(GameState state)
        {
            return state == GameState.Cg || state == GameState.Paused
                || state == GameState.Victory || state == GameState.Defeat;
        }
    }
}
