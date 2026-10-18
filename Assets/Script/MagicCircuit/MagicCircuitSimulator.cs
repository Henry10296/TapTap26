using System.Collections.Generic;
using TapCom.Core;
using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.MagicCircuit
{
    /// <summary>飞行种子（纯数据非 GameObject；落定即出列成格，tech-spec §4 规则 2/3）。</summary>
    public sealed class SeedProjectile
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public SeedMagicType Type;

        /// <summary>贴合点被占的诊断日志只打一次（防 120Hz 刷屏）。</summary>
        public bool SnapBlockedLogged;
    }

    /// <summary>解锁运动格（已出字典；落定/加固再 AddCell 回字典——Invariant：字典内格恒 Locked）。
    /// Id：全期唯一身份，Removed/Added 事件经 MoverId 传给表现层做本体绑定（同一 CellActor 跟随运动，不换皮）。</summary>
    public sealed class MovingCell
    {
        public int Id;

        public Vector2 Position;
        public Vector2 Velocity;
        public CellMagicAttr Magic;

        /// <summary>吸引态：向 settled 团表面空格寻位（吸引场过期自动转普通运动格）。</summary>
        public bool Attracted;

        /// <summary>解锁宽限：期间不参与贴合（防解锁即回贴，原型 grace 0.22s）。</summary>
        public float Grace;
    }

    /// <summary>持续魔法场（M1：Zero 失重 / Attract 吸引；爆炸无持续场）。</summary>
    public sealed class MagicField
    {
        public SpellType Type;
        public Vector2 Center;
        public float Radius;
        public float Remaining;

        /// <summary>吸引场：已加固格集合（锚团，只增不减；原型 settled 语义）。</summary>
        public readonly HashSet<Vector3Int> Settled = new HashSet<Vector3Int>();
    }

    /// <summary>蓄能中的配方组（0.5s；任一块消失即取消——格点世界「分离」唯一来源是被移除）。</summary>
    public sealed class PendingCast
    {
        public Vector3Int[] Cells;
        public SpellType Spell;
        public float Remaining;
    }

    /// <summary>
    /// Magic Circuit 模拟器（tech-spec §4 执行器，TASK-007）：
    /// 投掷冲量飞行 → 贴合成格 → 三连配方触发 → 失重/爆炸/吸引 → 解锁格运动回落 → 物理传链连锁。
    /// 固定步 1/120 内部累加器（TickManager 变步长不进模拟）；效果修改一律经本类落地（铁律 #4）。
    /// 触发模型（2026-10-09 拍板）：新落定块为 source、两步邻域 L 形三连、精确色数查 RecipeTable、
    /// 多组随机选一、每组一次（fired 集）、蓄能 0.5s。
    /// 基岩免疫（TASK-016）：Bedrock 地形格全路径不可动不可消（IsImmovable 闸），且被配方枚举显式排除（无色兜底）。
    /// </summary>
    public class MagicCircuitSimulator : ITickable, IZeroFieldQuery
    {
        private static readonly Vector3Int[] NeighborOffsets =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
        };

        private readonly EventBus _bus;
        private readonly WorldManager _worlds;
        private readonly LevelManager _levels;
        private readonly List<SeedProjectile> _seeds = new List<SeedProjectile>();
        private readonly List<MovingCell> _movers = new List<MovingCell>();
        private readonly List<MagicField> _fields = new List<MagicField>();
        private readonly List<PendingCast> _pending = new List<PendingCast>();
        private readonly HashSet<string> _fired = new HashSet<string>();
        private readonly System.Random _random = new System.Random();
        private readonly List<Cell> _snapshotBuffer = new List<Cell>();
        private int _nextMoverId = 1;
        private float _accumulator;
        private bool _worldMissingLogged;

        public int Priority => 100;

        /// <summary>表现拉取：飞行种子快照（只读，勿持有引用跨帧）。</summary>
        public IReadOnlyList<SeedProjectile> FlyingSeeds => _seeds;

        /// <summary>表现拉取：运动格快照。</summary>
        public IReadOnlyList<MovingCell> MovingCells => _movers;

        /// <summary>表现拉取：蓄能中的配方组（高亮用）。</summary>
        public IReadOnlyList<PendingCast> PendingCasts => _pending;

        public MagicCircuitSimulator(EventBus bus, WorldManager worlds, LevelManager levels)
        {
            _bus = bus;
            _worlds = worlds;
            _levels = levels;
            _bus.Subscribe<SeedThrownEvent>(OnSeedThrown);
            _bus.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
        }

        /// <summary>关卡装载即清全部上一局模拟状态（2026-10-18 拍板）：_fired 位置键跨关残留会烧掉重开关卡后的
        /// 同位置配方（「颜色方块不稳定触发」根因）；种子/运动格/魔法场/蓄能同属按关生命周期，一并清。</summary>
        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            _seeds.Clear();
            _movers.Clear();
            _fields.Clear();
            _pending.Clear();
            _fired.Clear();
            Debug.Log("[MagicCircuit] level loaded：上一局模拟状态全清（飞行种子/运动格/魔法场/蓄能/已触发登记）");
        }

        /// <summary>失重场查询（IZeroFieldQuery；PlayerMotor 漂移判定用）。</summary>
        public bool InZeroField(Vector2 worldPos)
        {
            foreach (var field in _fields)
            {
                if (field.Type != SpellType.Zero) continue;
                if ((worldPos - field.Center).sqrMagnitude <= field.Radius * field.Radius) return true;
            }
            return false;
        }

        public void Tick(float deltaTime)
        {
            _accumulator += deltaTime;
            var steps = 0;
            while (_accumulator >= MagicParams.TickDt && steps < MagicParams.MaxStepsPerFrame)
            {
                _accumulator -= MagicParams.TickDt;
                steps++;
                StepOnce(MagicParams.TickDt);
            }
            if (steps >= MagicParams.MaxStepsPerFrame) _accumulator = 0f;
        }

        private WorldGrid World
        {
            get
            {
                var id = _levels.CurrentWorldId;
                if (id != null && _worlds.TryGet(id, out var world)) return world;
                if (!_worldMissingLogged)
                {
                    Debug.LogWarning("[MagicCircuit] 无已装载世界（LevelManager 尚未 Load），模拟挂起");
                    _worldMissingLogged = true;
                }
                return null;
            }
        }

        // —— 主循环 ——

        private void StepOnce(float dt)
        {
            var world = World;
            if (world == null) return;

            for (var i = _fields.Count - 1; i >= 0; i--)
            {
                _fields[i].Remaining -= dt;
                if (_fields[i].Remaining <= 0f) _fields.RemoveAt(i);
            }

            CaptureIntoAttraction(world);
            StepSeeds(world, dt);
            StepMovers(world, dt);
            StepPendingCasts(world, dt);
        }

        /// <summary>吸引场捕获：范围内字典格补登记为吸引目标（原型 L1817）。</summary>
        private void CaptureIntoAttraction(WorldGrid world)
        {
            var field = LatestAttractField();
            if (field == null) return;

            _snapshotBuffer.Clear();
            foreach (var cell in world.Cells)
            {
                if (field.Settled.Contains(cell.Position)) continue;
                if (IsImmovable(cell)) continue; // 始终固定/基岩（TASK-016）：免疫吸引捕获
                if ((CellCenter(cell.Position) - field.Center).sqrMagnitude > field.Radius * field.Radius) continue;
                _snapshotBuffer.Add(cell);
            }
            // 快照后解锁：RemoveCell 会改字典，不能边遍历边删
            for (var i = 0; i < _snapshotBuffer.Count; i++)
            {
                var cell = _snapshotBuffer[i];
                Debug.Log($"[MagicCircuit] attract field captures ({cell.Position.x},{cell.Position.y})（引力场捕获：场内存活期落定即吸走）");
                Unlock(world, cell.Position, cell.MagicAttr, Vector2.zero, attracted: true);
            }
        }

        private void StepSeeds(WorldGrid world, float dt)
        {
            for (var i = _seeds.Count - 1; i >= 0; i--)
            {
                var seed = _seeds[i];
                var r = Ballistics.Step(world, seed.Position, seed.Velocity, dt, MagicParams.SeedHitboxHalf, InZeroField(seed.Position));
                seed.Position = r.Position;
                seed.Velocity = r.Velocity;

                if (seed.Position.y < MagicParams.OutOfBoundsY)
                {
                    Debug.Log($"[MagicCircuit] seed lost (out of bounds) type={seed.Type}");
                    _seeds.RemoveAt(i);
                    continue;
                }

                if (!r.Hit) continue;
                var target = SnapTarget(r);
                if (world.IsOccupied(target))
                {
                    // 贴合面被占：轴向速度已清零，滑停重试（原型反弹的 M1 简化）；诊断日志每种子一次
                    if (!seed.SnapBlockedLogged)
                    {
                        seed.SnapBlockedLogged = true;
                        Debug.Log($"[MagicCircuit] seed snap blocked: 贴合点 {target} 被占，种子滑停等待（at {seed.Position}）");
                    }
                    continue;
                }

                world.AddCell(target, CellType.Rock, CellGravityAttr.Dynamic, SeedAttr(seed.Type), locked: true);
                PublishChanged(CellChangeType.Added, target, SeedAttr(seed.Type));
                Debug.Log($"[MagicCircuit] seed landed ({target.x},{target.y}) type={seed.Type}");
                _seeds.RemoveAt(i);
                EvaluateRecipes(world, target);
            }
        }

        private void StepMovers(WorldGrid world, float dt)
        {
            for (var i = _movers.Count - 1; i >= 0; i--)
            {
                var mover = _movers[i];
                mover.Grace = Mathf.Max(0f, mover.Grace - dt);

                if (mover.Position.y < MagicParams.OutOfBoundsY)
                {
                    Debug.Log("[MagicCircuit] moving cell lost (out of bounds)");
                    _movers.RemoveAt(i);
                    continue;
                }

                var arrived = false;
                var target = default(Vector3Int?);
                if (mover.Attracted)
                {
                    arrived = TrySeekAttractSlot(world, mover, dt, out var slot);
                    if (arrived) target = slot;
                }

                var r = Ballistics.Step(world, mover.Position, mover.Velocity, dt, 0.5f, InZeroField(mover.Position) || mover.Attracted);
                mover.Position = r.Position;
                mover.Velocity = r.Velocity;

                if (!arrived && r.Hit && mover.Grace <= 0f) target = SnapTarget(r);
                else if (r.Hit && mover.Grace > 0f) Debug.Log($"[MagicCircuit] mover hit during grace at ({mover.Position.x:F2},{mover.Position.y:F2})（宽限内忽略贴合，沿面滑落）");

                if (target.HasValue && (arrived || mover.Grace <= 0f) && world.IsOccupied(target.Value))
                {
                    Debug.Log($"[MagicCircuit] mover snap blocked: {target.Value} occupied（贴合点被占，滑停重试）");
                    target = null;
                }
                if (target.HasValue)
                {
                    world.AddCell(target.Value, CellType.Rock, CellGravityAttr.Dynamic, mover.Magic, locked: true);
                    PublishChanged(CellChangeType.Added, target.Value, mover.Magic, mover.Id);
                    if (mover.Attracted) LatestAttractField()?.Settled.Add(target.Value);
                    Debug.Log($"[MagicCircuit] cell re-attached ({target.Value.x},{target.Value.y}) id={mover.Id}");
                    _movers.RemoveAt(i);
                }
            }
        }

        private void StepPendingCasts(WorldGrid world, float dt)
        {
            for (var i = _pending.Count - 1; i >= 0; i--)
            {
                var pending = _pending[i];

                var valid = true;
                foreach (var pos in pending.Cells)
                {
                    if (world.TryGetCell(pos, out var cell) && cell.MagicAttr != CellMagicAttr.None) continue;
                    valid = false;
                    break;
                }
                if (valid) valid = ConnectedTriple(pending.Cells);

                if (!valid)
                {
                    Debug.Log("[MagicCircuit] charge cancelled（三连已缺失）");
                    _pending.RemoveAt(i);
                    continue;
                }

                pending.Remaining -= dt;
                if (pending.Remaining > 1e-4f) continue;
                _pending.RemoveAt(i);
                Cast(world, pending.Spell, pending.Cells);
            }
        }

        // —— 投掷 ——

        private void OnSeedThrown(SeedThrownEvent e)
        {
            var world = World;
            if (world == null) return;

            var dir = e.Direction.sqrMagnitude < 1e-6f ? Vector2.right : e.Direction.normalized;

            // 出手口扫描：沿方向推进至阻挡，起点被挡则拒投（判定共用 Ballistics.TryScanMuzzle，与 PlayerThrower 预检同源）
            if (!Ballistics.TryScanMuzzle(world, e.Origin, dir, out var muzzle))
            {
                Debug.Log("[MagicCircuit] throw blocked: muzzle occluded（出手位置被挡，拒投）");
                return;
            }

            // 弹道解算穿过指针点（2026-10-13「弹道终于指针」：固定速度解低弧角，超射程抬速），预览同源（TrajectoryPreview）
            // TASK-016：速度取事件携带值（PlayerThrower 序列化字段），<=0 回退常量兼容旧发布方
            var velocity = Ballistics.SolveThrowVelocity(muzzle, e.Target, dir, e.Speed > 0f ? e.Speed : MagicParams.ThrowSpeed)
                + new Vector2(e.PlayerVelocity.x * MagicParams.PlayerVxInheritRatio, 0f);
            _seeds.Add(new SeedProjectile { Position = muzzle, Velocity = velocity, Type = e.Type });
            Debug.Log($"[MagicCircuit] seed spawned type={e.Type} at {muzzle} vel={velocity}");
        }

        // —— 三连配方 ——

        /// <summary>触发判定（原型 detectRecipes L1641-1664）：source + 两步邻域枚举 L 形三连，精确色数，随机选一。</summary>
        private void EvaluateRecipes(WorldGrid world, Vector3Int source)
        {
            _snapshotBuffer.Clear();
            foreach (var cell in world.Cells)
            {
                if (cell.MagicAttr == CellMagicAttr.None) continue;
                if (cell.Type == CellType.Bedrock) continue; // 基岩无色不触发配方（TASK-016；误配彩色也兜底排除）
                _snapshotBuffer.Add(cell);
            }
            if (_snapshotBuffer.Count == 0) return;

            var near = new List<Vector3Int>();
            foreach (var cell in _snapshotBuffer)
                if (cell.Position != source && AreAdjacent(cell.Position, source)) near.Add(cell.Position);

            var local = new List<Vector3Int>(near);
            foreach (var n in near)
            foreach (var cell in _snapshotBuffer)
                if (cell.Position != source && !local.Contains(cell.Position) && AreAdjacent(cell.Position, n))
                    local.Add(cell.Position);

            List<PendingCast> candidates = null;
            for (var i = 0; i < local.Count; i++)
            for (var j = i + 1; j < local.Count; j++)
            {
                var tri = new[] { source, local[i], local[j] };
                if (!ConnectedTriple(tri)) continue;
                CountColors(world, tri, out var red, out var blue, out var yellow);
                if (!RecipeTable.TryMatch(red, blue, yellow, out var spell)) continue;
                var key = FiredKey(tri);
                if (_fired.Contains(key)) continue;
                (candidates ??= new List<PendingCast>()).Add(new PendingCast { Cells = tri, Spell = spell, Remaining = MagicParams.ChargeTime });
            }

            if (candidates == null || candidates.Count == 0) return;
            var chosen = candidates[_random.Next(candidates.Count)];
            _fired.Add(FiredKey(chosen.Cells));
            _pending.Add(chosen);
            Debug.Log($"[MagicCircuit] recipe {chosen.Spell} charging {MagicParams.ChargeTime}s（{Describe(chosen.Cells)}）");
        }

        private void Cast(WorldGrid world, SpellType spell, Vector3Int[] tri)
        {
            var center = (CellCenter(tri[0]) + CellCenter(tri[1]) + CellCenter(tri[2])) / 3f;
            _bus.Publish(new SpellCastEvent { Type = spell, Center = center });

            if (spell == SpellType.Zero)
            {
                // 范围内全部消失（含触发三块自己，原型 L1681）+ 失重场
                _snapshotBuffer.Clear();
                foreach (var cell in world.Cells)
                    if ((CellCenter(cell.Position) - center).sqrMagnitude <= MagicParams.ZeroFieldRadius * MagicParams.ZeroFieldRadius)
                        _snapshotBuffer.Add(cell);
                foreach (var cell in _snapshotBuffer) RemoveDictCell(world, cell.Position);
                _fields.Add(new MagicField { Type = SpellType.Zero, Center = center, Radius = MagicParams.ZeroFieldRadius, Remaining = MagicParams.FieldDuration });
                Debug.Log($"[MagicCircuit] cast zero：消失 {_snapshotBuffer.Count} 格，失重场 r={MagicParams.ZeroFieldRadius}格 {MagicParams.FieldDuration}s");
            }
            else if (spell == SpellType.Blast)
            {
                foreach (var pos in tri) RemoveDictCell(world, pos);
                _snapshotBuffer.Clear();
                foreach (var cell in world.Cells)
                    if ((CellCenter(cell.Position) - center).sqrMagnitude <= MagicParams.BlastRadius * MagicParams.BlastRadius)
                        _snapshotBuffer.Add(cell);
                foreach (var cell in _snapshotBuffer)
                {
                    var d = Vector2.Distance(CellCenter(cell.Position), center);
                    var dir = (CellCenter(cell.Position) - center) / Mathf.Max(d, 1e-4f);
                    var v = dir * (MagicParams.BlastImpulseBase + MagicParams.BlastImpulseFalloff * (1f - d / MagicParams.BlastRadius));
                    v.y += MagicParams.BlastUpKick;
                    Unlock(world, cell.Position, cell.MagicAttr, v, attracted: false);
                }
                Debug.Log($"[MagicCircuit] cast blast：三块消失，弹飞 {_snapshotBuffer.Count} 格（r={MagicParams.BlastRadius}格）");
            }
            else if (spell == SpellType.Attract)
            {
                var field = new MagicField { Type = SpellType.Attract, Center = center, Radius = MagicParams.AttractRadius, Remaining = MagicParams.FieldDuration };
                foreach (var pos in tri) field.Settled.Add(pos);
                _snapshotBuffer.Clear();
                foreach (var cell in world.Cells)
                {
                    if (field.Settled.Contains(cell.Position)) continue;
                    if ((CellCenter(cell.Position) - center).sqrMagnitude > field.Radius * field.Radius) continue;
                    _snapshotBuffer.Add(cell);
                }
                foreach (var cell in _snapshotBuffer) Unlock(world, cell.Position, cell.MagicAttr, Vector2.zero, attracted: true);
                _fields.Add(field);
                Debug.Log($"[MagicCircuit] cast attract：锚团 {tri.Length} 块，捕获 {_snapshotBuffer.Count} 格（r={MagicParams.AttractRadius}格 {MagicParams.FieldDuration}s）");

                // GDD 引力配方：三块不销毁、消除颜色（转为岩石方块），保留作吸附锚团
                foreach (var pos in tri)
                {
                    if (!world.TryGetCell(pos, out var cell)) continue;
                    cell.MagicAttr = CellMagicAttr.None;
                    PublishChanged(CellChangeType.Changed, pos, cell.MagicAttr);
                }
            }
            else if (spell == SpellType.Static)
            {
                // 静止配方（2026-10-18 拍板改版）：变化直径内全部格子原地转为基岩（含触发三块）。
                // 地形语义全走既有 Bedrock 闸：免疫解锁/消除/吸引（IsImmovable），无色不再参与配方（EvaluateRecipes 排除）。
                // 已固化/基岩跳过：幂等；构造外转基岩须手动补 Frozen（Cell 构造只在 new 时置位）。
                _snapshotBuffer.Clear();
                foreach (var cell in world.Cells)
                {
                    if (IsImmovable(cell)) continue;
                    if ((CellCenter(cell.Position) - center).sqrMagnitude <= MagicParams.StaticRadius * MagicParams.StaticRadius)
                        _snapshotBuffer.Add(cell);
                }
                foreach (var cell in _snapshotBuffer)
                {
                    cell.Type = CellType.Bedrock;
                    cell.MagicAttr = CellMagicAttr.None;
                    cell.Frozen = true;
                    PublishChanged(CellChangeType.Changed, cell.Position, cell.MagicAttr, cellType: cell.Type);
                }
                Debug.Log($"[MagicCircuit] cast static：{_snapshotBuffer.Count} 格转为基岩（r={MagicParams.StaticRadius}格）");
            }
        }

        // —— 解锁 / 加固 ——

        /// <summary>不可动判定（TASK-016）：Frozen（静止魔法固化）或 Bedrock（基岩地形，构造即 Frozen——
        /// 这里按类型再兜一道，防数据被误改后基岩仍保持免疫）。</summary>
        private static bool IsImmovable(Cell cell) => cell.Frozen || cell.Type == CellType.Bedrock;

        /// <summary>解锁 = 出字典进运动列表（tech-spec §4 规则 6；Invariant：字典内格恒 Locked）。
        /// 始终固定（Frozen，静止魔法）与基岩（Bedrock，TASK-016）免疫解锁。</summary>
        private void Unlock(WorldGrid world, Vector3Int pos, CellMagicAttr magic, Vector2 velocity, bool attracted)
        {
            if (!world.TryGetCell(pos, out var cell)) return;
            if (IsImmovable(cell))
            {
                Debug.Log($"[MagicCircuit] cell frozen/bedrock, unlock ignored ({pos.x},{pos.y})");
                return;
            }
            if (!world.RemoveCell(pos)) return;

            var mover = new MovingCell
            {
                Id = _nextMoverId++,
                Position = CellCenter(pos),
                Velocity = velocity,
                Magic = magic,
                Attracted = attracted,
                Grace = MagicParams.DetachGraceTime,
            };
            _movers.Add(mover);
            PublishChanged(CellChangeType.Removed, pos, magic, mover.Id);
            Debug.Log($"[MagicCircuit] cell unlocked ({pos.x},{pos.y}) id={mover.Id}");
        }

        private void RemoveDictCell(WorldGrid world, Vector3Int pos)
        {
            if (!world.TryGetCell(pos, out var cell)) return;
            if (IsImmovable(cell))
            {
                Debug.Log($"[MagicCircuit] cell frozen/bedrock, remove ignored ({pos.x},{pos.y})");
                return;
            }
            var attr = cell.MagicAttr;
            world.RemoveCell(pos);
            PublishChanged(CellChangeType.Removed, pos, attr);
        }

        // —— 吸引寻位 ——

        private MagicField LatestAttractField()
        {
            for (var i = _fields.Count - 1; i >= 0; i--)
                if (_fields[i].Type == SpellType.Attract)
                    return _fields[i];
            return null;
        }

        /// <summary>朝最近 settled 团表面空格转向（原型 attractForce L1716-1723）；到位返回 true。</summary>
        private bool TrySeekAttractSlot(WorldGrid world, MovingCell mover, float dt, out Vector3Int slot)
        {
            slot = default;
            var field = LatestAttractField();
            if (field == null)
            {
                mover.Attracted = false; // 场过期 → 转普通运动格
                return false;
            }

            var best = Vector3Int.zero;
            var bestDist = float.MaxValue;
            var found = false;
            foreach (var settled in field.Settled)
            foreach (var offset in NeighborOffsets)
            {
                var candidate = settled + offset;
                if (world.IsOccupied(candidate)) continue;
                var d = (CellCenter(candidate) - mover.Position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = candidate; found = true; }
            }
            if (!found) return false;
            slot = best;

            var toSlot = CellCenter(slot) - mover.Position;
            var dist = toSlot.magnitude;
            if (dist <= MagicParams.AttractArriveDistance) return true;

            var speed = Mathf.Min(MagicParams.AttractSeekSpeed, dist * 6f);
            var desired = toSlot / Mathf.Max(dist, 1e-4f) * speed;
            mover.Velocity += (desired - mover.Velocity) * Mathf.Min(1f, dt * 10f);
            return false;
        }

        // —— 工具 ——

        private static Vector3Int SnapTarget(Ballistics.Result r)
        {
            return r.HitCell + new Vector3Int(Mathf.RoundToInt(r.HitNormal.x), Mathf.RoundToInt(r.HitNormal.y), 0);
        }

        private static Vector2 CellCenter(Vector3Int pos) => new Vector2(pos.x + 0.5f, pos.y + 0.5f);

        private static bool OverlapsCell(WorldGrid world, Vector2 center, float half)
        {
            var min = Vector2Int.FloorToInt(center - new Vector2(half, half));
            var max = Vector2Int.FloorToInt(center + new Vector2(half - 1e-4f, half - 1e-4f));
            for (var y = min.y; y <= max.y; y++)
            for (var x = min.x; x <= max.x; x++)
                if (world.TryGetCell(new Vector3Int(x, y, 0), out _)) return true;
            return false;
        }

        private static bool AreAdjacent(Vector3Int a, Vector3Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;

        /// <summary>L 形三连（原型 connectedTriple L1639）：任一成员与其余两员均 4 向相邻。</summary>
        private static bool ConnectedTriple(Vector3Int[] tri)
        {
            for (var i = 0; i < tri.Length; i++)
            {
                var count = 0;
                for (var j = 0; j < tri.Length; j++)
                    if (j != i && AreAdjacent(tri[i], tri[j])) count++;
                if (count == 2) return true;
            }
            return false;
        }

        private void CountColors(WorldGrid world, Vector3Int[] tri, out int red, out int blue, out int yellow)
        {
            red = blue = yellow = 0;
            foreach (var pos in tri)
            {
                if (!world.TryGetCell(pos, out var cell)) continue;
                switch (cell.MagicAttr)
                {
                    case CellMagicAttr.Red: red++; break;
                    case CellMagicAttr.Blue: blue++; break;
                    case CellMagicAttr.Yellow: yellow++; break;
                }
            }
        }

        private static string FiredKey(Vector3Int[] tri)
        {
            var sorted = (Vector3Int[])tri.Clone();
            System.Array.Sort(sorted, (a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y != b.y ? a.y.CompareTo(b.y) : a.z.CompareTo(b.z));
            return $"{sorted[0].x},{sorted[0].y};{sorted[1].x},{sorted[1].y};{sorted[2].x},{sorted[2].y}";
        }

        private static string Describe(Vector3Int[] tri) => FiredKey(tri);

        private static CellMagicAttr SeedAttr(SeedMagicType type) => type switch
        {
            SeedMagicType.Blue => CellMagicAttr.Blue,
            SeedMagicType.Red => CellMagicAttr.Red,
            SeedMagicType.Yellow => CellMagicAttr.Yellow,
            _ => CellMagicAttr.None,
        };

        private void PublishChanged(CellChangeType type, Vector3Int pos, CellMagicAttr attr, int moverId = 0, CellType cellType = CellType.Rock)
        {
            _bus.Publish(new CellWorldChangedEvent { Type = type, Position = pos, Attr = attr, MoverId = moverId, CellType = cellType });
        }
    }
}
