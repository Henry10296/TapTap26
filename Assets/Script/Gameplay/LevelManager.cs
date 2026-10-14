using TapCom.Core;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>关卡装载完成事件：Presentation（LevelActorBinder/BackgroundLayerLoader）据此生成表现物。携带源 LevelData（背景层等非 Cell 数据随事件到达）。</summary>
    public readonly struct LevelLoadedEvent
    {
        public readonly string WorldId;
        public readonly WorldGrid World;
        public readonly Vector3Int SpawnPoint;
        public readonly LevelData Data;

        public LevelLoadedEvent(string worldId, WorldGrid world, Vector3Int spawnPoint, LevelData data)
        {
            WorldId = worldId;
            World = world;
            SpawnPoint = spawnPoint;
            Data = data;
        }
    }

    /// <summary>
    /// 关卡管理器（tech-spec §5）：初始化、加载、清理。
    /// 只管数据装载生命周期；表现物归 LevelActorBinder（事件驱动）。
    /// 全构造注入（TASK-011：manager 不摸 Services、不碰 GameObject）。
    /// 计时、目标与胜负判断 M3 补。
    /// </summary>
    public class LevelManager
    {
        private readonly WorldManager _worlds;
        private readonly GameManager _game;
        private readonly EventBus _bus;

        /// <summary>当前已装载的世界 id；未装载为 null。</summary>
        public string CurrentWorldId { get; private set; }

        private LevelData[] _sequence;
        private int _index;

        public LevelManager(WorldManager worlds, GameManager game, EventBus bus)
        {
            _worlds = worlds ?? throw new System.ArgumentNullException(nameof(worlds));
            _game = game ?? throw new System.ArgumentNullException(nameof(game));
            _bus = bus ?? throw new System.ArgumentNullException(nameof(bus));
        }

        /// <summary>注入关卡序列（GameBootstrap 场景装配时调用，人工拖数组）；index 归零。</summary>
        public void SetSequence(LevelData[] levels)
        {
            _sequence = levels;
            _index = 0;
        }

        /// <summary>装载序列中当前关（GameBootstrap 首次装载用；调用方负责已处于 Loading 态，本方法不再切状态）。</summary>
        public bool LoadCurrent()
        {
            if (_sequence == null || _index >= _sequence.Length) return false;
            var data = _sequence[_index];
            Load(data.name, data);
            return true;
        }

        /// <summary>重载当前关（失败重试：Defeat/Victory → Loading 边已有；重载不切场景）。</summary>
        public void Reload()
        {
            if (_sequence == null || _sequence.Length == 0) return;
            if (_game.TrySetState(GameState.Loading)) LoadCurrent();
        }

        /// <summary>下一关（胜利推进：Victory → Loading 边已有）；越界即最后一关通关 → 回标题。</summary>
        public void Advance()
        {
            if (_sequence == null) return;
            _index++;
            if (_index >= _sequence.Length)
            {
                ExitToTitle();
                return;
            }
            if (_game.TrySetState(GameState.Loading)) LoadCurrent();
        }

        /// <summary>退出关卡回标题：卸数据 → TitleScreen（场景切换归 SceneFlow）。</summary>
        public void ExitToTitle()
        {
            Unload();
            _game.TrySetState(GameState.TitleScreen);
        }

        /// <summary>装载关卡：清理旧世界 → 纯数据装载 → 发 LevelLoadedEvent → 推进 Playing（装载完成的事实归本类）。</summary>
        public WorldGrid Load(string worldId, LevelData data)
        {
            Unload();

            var world = LevelLoader.Load(_worlds, worldId, data);
            CurrentWorldId = worldId;
            _bus.Publish(new LevelLoadedEvent(worldId, world, data.SpawnPoint, data));
            _game.TrySetState(GameState.Playing);
            return world;
        }

        /// <summary>清理当前关卡世界数据（表现物由 LevelActorBinder 自行清理）。</summary>
        public bool Unload()
        {
            if (CurrentWorldId == null) return false;
            var id = CurrentWorldId;
            CurrentWorldId = null;
            return _worlds.Destroy(id);
        }
    }
}
