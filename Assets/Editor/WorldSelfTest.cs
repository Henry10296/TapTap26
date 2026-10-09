using System;
using TapCom.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TapCom.EditorTools
{
    /// <summary>
    /// TASK-005 数据层自检（design §5 断言清单）：真实断言跑通增删查 / 邻接 / 边集清理 / 多实例隔离。
    /// Console 输出逐条 ok / FAIL + 汇总 PASS n/n。
    /// </summary>
    public static class WorldSelfTest
    {
        private static int _pass;
        private static int _total;

        [MenuItem("Tools/魔法回路/世界自检")]
        public static void Run()
        {
            _pass = 0;
            _total = 0;
            var world = new WorldGrid();

            // ① AddCell + Count
            var c = world.AddCell(new Vector3Int(0, 0, 0), CellType.Rock, CellGravityAttr.Static, CellMagicAttr.Blue);
            Check("AddCell.Count", world.Count == 1);

            // ② 重复位置拒绝
            Check("AddCell.Duplicate.Throws", Throws<InvalidOperationException>(() =>
                world.AddCell(new Vector3Int(0, 0, 0), CellType.Rock, CellGravityAttr.Static, CellMagicAttr.None)));

            // ③ TryGet / IsOccupied 命中
            Check("TryGet.Hit", world.TryGetCell(new Vector3Int(0, 0, 0), out var got) && got == c);
            Check("IsOccupied.Hit", world.IsOccupied(new Vector3Int(0, 0, 0)));

            // ④ RemoveCell 清占位 + 缺位返回 false
            Check("RemoveCell.Clears", world.RemoveCell(new Vector3Int(0, 0, 0))
                && !world.IsOccupied(new Vector3Int(0, 0, 0)) && world.Count == 0);
            Check("RemoveCell.Missing.False", !world.RemoveCell(new Vector3Int(9, 9, 9)));

            // ⑤ 4 邻接命中 / 空位跳过
            world.AddCell(new Vector3Int(0, 0, 0), CellType.Rock, CellGravityAttr.Static, CellMagicAttr.None);
            world.AddCell(new Vector3Int(1, 0, 0), CellType.Rock, CellGravityAttr.Dynamic, CellMagicAttr.Red);
            world.AddCell(new Vector3Int(0, 1, 0), CellType.Rock, CellGravityAttr.Static, CellMagicAttr.Yellow);
            var neighbors = world.GetNeighbors(new Vector3Int(0, 0, 0));
            Check("Neighbors.Count2", neighbors.Length == 2);

            // ⑥ RemoveCell 清理邻接 Connections（边集一致性）
            world.GetCell(new Vector3Int(0, 0, 0)).Connections.Add(new Vector3Int(1, 0, 0));
            world.GetCell(new Vector3Int(1, 0, 0)).Connections.Add(new Vector3Int(0, 0, 0));
            world.RemoveCell(new Vector3Int(1, 0, 0));
            Check("RemoveCell.CleansConnections", world.GetCell(new Vector3Int(0, 0, 0)).Connections.Count == 0);

            // ⑦ 多实例隔离
            var other = new WorldGrid();
            other.AddCell(new Vector3Int(0, 0, 0), CellType.Rock, CellGravityAttr.Static, CellMagicAttr.None);
            Check("InstanceIsolation", world.Count == 2 && other.Count == 1);

            // ⑧ Cell 字段赋值往返（含锁定翻转 = 解锁语义）
            c.State = CellState.Unstable;
            c.MagicState = CellMagicState.Connected;
            c.Locked = false;
            Check("Cell.Fields", c.State == CellState.Unstable
                && c.MagicState == CellMagicState.Connected && !c.Locked);

            // ⑨ z≠0 可存（类型支持，内容不用）
            world.AddCell(new Vector3Int(0, 0, 5), CellType.Rock, CellGravityAttr.Static, CellMagicAttr.None);
            Check("ZAxis.Supported", world.IsOccupied(new Vector3Int(0, 0, 5)));

            // ⑩ WorldManager 生命周期
            var mgr = new WorldManager();
            var w = mgr.Create("L1");
            w.AddCell(new Vector3Int(3, 3, 0), CellType.Rock, CellGravityAttr.Static, CellMagicAttr.None);
            Check("WorldManager.Create", mgr.Count == 1 && mgr.Get("L1").Count == 1);
            Check("WorldManager.Duplicate.Throws", Throws<InvalidOperationException>(() => mgr.Create("L1")));
            Check("WorldManager.TryGet", mgr.TryGet("L1", out _) && !mgr.TryGet("L2", out _));
            mgr.Destroy("L1");
            Check("WorldManager.Destroy", mgr.Count == 0);

            Debug.Log($"[WorldSelfTest] PASS {_pass}/{_total}");
        }

        private static void Check(string name, bool ok)
        {
            _total++;
            if (ok)
            {
                _pass++;
                Debug.Log($"[WorldSelfTest] ok: {name}");
            }
            else
            {
                Debug.LogError($"[WorldSelfTest] FAIL: {name}");
            }
        }

        private static bool Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
                return false;
            }
            catch (T)
            {
                return true;
            }
        }
    }
}
