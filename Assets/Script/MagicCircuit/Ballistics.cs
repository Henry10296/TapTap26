using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.MagicCircuit
{
    /// <summary>
    /// 弹道单步推进（TASK-007 design §4.1）：模拟器（种子/运动格）与 TrajectoryPreview 共用的唯一实现。
    /// 单步 = 重力（失重场内免）+ 水平阻尼 exp(-0.32t) + 限速 + 逐轴移动碰撞（先 x 后 y，原型 moveGroup 同序）。
    /// 预览与真实同源 ⇒ M1 内预览 = 精确弹道；M2 引入阻尼/场差异后须重新评估并标注「预览≠保证」。
    /// 坐标契约：cell (x,y) 占世界方块 [x,x+1)×[y,y+1)。
    /// </summary>
    public static class Ballistics
    {
        /// <summary>一步推进结果。</summary>
        public struct Result
        {
            public Vector2 Position;
            public Vector2 Velocity;

            /// <summary>本步是否命中格（Position 已钳回撞击面）。</summary>
            public bool Hit;

            /// <summary>命中格坐标（Hit 时有效）。</summary>
            public Vector3Int HitCell;

            /// <summary>命中法线：从命中格指向来向（贴合目标 = HitCell + 取整法线）。</summary>
            public Vector2 HitNormal;
        }

        /// <summary>推进一步。half = 运动体半宽；zeroField = true 时本步无重力。</summary>
        public static Result Step(WorldGrid world, Vector2 position, Vector2 velocity, float dt, float half, bool zeroField)
        {
            if (!zeroField) velocity.y -= MagicParams.Gravity * dt;
            velocity.x *= Mathf.Exp(-MagicParams.HorizontalDampingRate * dt);
            velocity.x = Mathf.Clamp(velocity.x, -MagicParams.VelCapX, MagicParams.VelCapX);
            velocity.y = Mathf.Clamp(velocity.y, -MagicParams.VelCapY, MagicParams.VelCapY);

            var result = new Result { Position = position, Velocity = velocity };
            MoveAxis(world, ref result, dt, half, axisX: true);
            MoveAxis(world, ref result, dt, half, axisX: false);
            return result;
        }

        /// <summary>
        /// 点中心 ± half 是否与任何格重叠（出手口扫描共用：真实投掷与预览起点同一判定）。
        /// 坐标契约同 Step：cell (x,y) 占 [x,x+1)²。
        /// </summary>
        public static bool OverlapsCell(WorldGrid world, Vector2 center, float half)
        {
            var min = Vector2Int.FloorToInt(center - new Vector2(half, half));
            var max = Vector2Int.FloorToInt(center + new Vector2(half - 1e-4f, half - 1e-4f));
            for (var y = min.y; y <= max.y; y++)
            for (var x = min.x; x <= max.x; x++)
                if (world.TryGetCell(new Vector3Int(x, y, 0), out _)) return true;
            return false;
        }

        private static void MoveAxis(WorldGrid world, ref Result result, float dt, float half, bool axisX)
        {
            var v = axisX ? result.Velocity.x : result.Velocity.y;
            var delta = v * dt;
            if (Mathf.Abs(delta) < 1e-6f) return;
            var sign = Mathf.Sign(delta);

            var p = result.Position;
            if (axisX) p.x += delta;
            else p.y += delta;

            // 运动方块覆盖的格范围（cell (x,y) 占 [x,x+1)²）
            var min = Vector2Int.FloorToInt(p - new Vector2(half, half));
            var max = Vector2Int.FloorToInt(p + new Vector2(half - 1e-4f, half - 1e-4f));
            for (var y = min.y; y <= max.y; y++)
            for (var x = min.x; x <= max.x; x++)
            {
                var cellPos = new Vector3Int(x, y, 0);
                if (!world.TryGetCell(cellPos, out _)) continue;

                // 命中：钳回撞击面，轴向速度清零
                if (axisX)
                {
                    p.x = sign > 0 ? x - half : x + 1 + half;
                    result.Velocity.x = 0f;
                    result.HitNormal = new Vector2(-sign, 0f);
                }
                else
                {
                    p.y = sign > 0 ? y - half : y + 1 + half;
                    result.Velocity.y = 0f;
                    result.HitNormal = new Vector2(0f, -sign);
                }

                result.Position = p;
                result.Hit = true;
                result.HitCell = cellPos;
                return;
            }

            result.Position = p;
        }
    }
}
