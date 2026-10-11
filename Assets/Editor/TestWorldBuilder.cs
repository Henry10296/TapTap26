using System.IO;
using TapCom.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TapCom.EditorTools
{
    /// <summary>
    /// M1 测试世界数据生成（TASK-007 §8 第 3 步）：纯代码摆出覆盖三配方与连锁的最小测试面，导出为 LevelData 资产。
    /// 只生成数据资产，**不建任何场景物体**（CLAUDE.md 铁律：游戏物体一律人工配置）——
    /// 装载走正常链：LevelBootstrap（拖资产）→ LevelManager.Load → LevelActorBinder（拖 CellActor 预制体）。
    /// </summary>
    public static class TestWorldBuilder
    {
        private const string AssetPath = "Assets/Levels/TestWorld.asset";

        [MenuItem("Tools/魔法回路/生成M1测试世界数据")]
        public static void Build()
        {
            var dir = Path.GetDirectoryName(AssetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var data = ScriptableObject.CreateInstance<LevelData>();

            // 地面 + 两侧墙（岩石）
            for (var x = -12; x <= 12; x++) Add(data, x, -3, CellMagicAttr.None);
            for (var y = -2; y <= 6; y++)
            {
                Add(data, -12, y, CellMagicAttr.None);
                Add(data, 12, y, CellMagicAttr.None);
            }

            // 左平台（顶面 y=1）+ 右平台（顶面 y=3）
            for (var x = -6; x <= -1; x++) Add(data, x, 0, CellMagicAttr.None);
            for (var x = 2; x <= 7; x++) Add(data, x, 2, CellMagicAttr.None);

            // 失重组：蓝蓝相邻（投红成三连）——左平台上
            Add(data, -4, 1, CellMagicAttr.Blue);
            Add(data, -3, 1, CellMagicAttr.Blue);

            // 爆炸组：红蓝相邻（投黄）——右平台上
            Add(data, 3, 3, CellMagicAttr.Red);
            Add(data, 4, 3, CellMagicAttr.Blue);

            // 吸引组：红红相邻（投黄）——地面上
            Add(data, 8, -2, CellMagicAttr.Red);
            Add(data, 9, -2, CellMagicAttr.Red);

            // 悬空岩（爆炸弹飞 / 解锁重力观察用）+ 地面岩堆（吸引目标）
            Add(data, 0, 4, CellMagicAttr.None);
            Add(data, 0, 5, CellMagicAttr.None);
            for (var x = -9; x <= -7; x++) Add(data, x, -2, CellMagicAttr.None);

            AssetDatabase.DeleteAsset(AssetPath);
            AssetDatabase.CreateAsset(data, AssetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[TestWorldBuilder] 已生成 {AssetPath}（{data.Cells.Count} 格）。\n" +
                      "使用步骤（场景物体人工配置）：\n" +
                      "① 主场景 LevelBootstrap：Level Data 槽拖入本资产，World Id 随意（如 Level_01）\n" +
                      "② 场景空物体挂 LevelActorBinder，拖 CellActor 预制体\n" +
                      "③ Player：加 ThrowPoint 子物体（头顶）+ 挂 TrajectoryPreview 组件\n" +
                      "④ Play → 标题 → 开始 → 装载 → 投掷测试");
        }

        private static void Add(LevelData data, int x, int y, CellMagicAttr magic)
        {
            data.Cells.Add(new CellDef
            {
                Pos = new Vector3Int(x, y, 0),
                Type = CellType.Rock,
                Gravity = CellGravityAttr.Dynamic,
                Magic = magic,
                Locked = true,
            });
        }
    }
}
