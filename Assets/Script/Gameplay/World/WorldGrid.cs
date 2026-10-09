using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>
    /// 运行时格点世界（tech-spec §2 WorldGrid）：格点字典 位置 → Cell。
    /// Magic Circuit（未来执行器）操作环境的唯一入口（铁律 #4）；Player 永不直接改（铁律 #3）。
    /// </summary>
    public class WorldGrid
    {
        private static readonly Vector3Int[] NeighborOffsets =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
        };

        private readonly Dictionary<Vector3Int, Cell> _cells = new Dictionary<Vector3Int, Cell>();

        public int Count => _cells.Count;

        /// <summary>全部 Cell（字典值直出，遍历期间勿改结构）。</summary>
        public IEnumerable<Cell> Cells => _cells.Values;

        /// <summary>占位建格；重复位置抛异常（Invariant 2：哑配置炸在调用时）。</summary>
        public Cell AddCell(Vector3Int position, CellType type, CellGravityAttr gravityAttr, CellMagicAttr magicAttr, bool locked = true)
        {
            if (_cells.ContainsKey(position))
                throw new InvalidOperationException($"[WorldGrid] position already occupied: {position}");
            var cell = new Cell(position, type, gravityAttr, magicAttr, locked);
            _cells[position] = cell;
            return cell;
        }

        /// <summary>移除格点并清理邻接对本格的连接引用（Invariant 3：边集一致性）。</summary>
        public bool RemoveCell(Vector3Int position)
        {
            if (!_cells.Remove(position)) return false;
            foreach (var offset in NeighborOffsets)
            {
                if (_cells.TryGetValue(position + offset, out var neighbor))
                    neighbor.Connections.Remove(position);
            }
            return true;
        }

        /// <summary>直取；位置不存在抛 KeyNotFoundException（调用方哑引用 fail fast）。</summary>
        public Cell GetCell(Vector3Int position)
        {
            return _cells[position];
        }

        public bool TryGetCell(Vector3Int position, out Cell cell)
        {
            return _cells.TryGetValue(position, out cell);
        }

        public bool IsOccupied(Vector3Int position)
        {
            return _cells.ContainsKey(position);
        }

        /// <summary>XY 4 邻接（z 不动，铁律 #11）：空位跳过，返回快照数组。回路规则填表后可扩。</summary>
        public Cell[] GetNeighbors(Vector3Int position)
        {
            var result = new List<Cell>(4);
            foreach (var offset in NeighborOffsets)
            {
                if (_cells.TryGetValue(position + offset, out var neighbor))
                    result.Add(neighbor);
            }
            return result.ToArray();
        }
    }
}
