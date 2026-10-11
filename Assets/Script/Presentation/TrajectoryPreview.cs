using System.Collections.Generic;
using TapCom.Core;
using TapCom.Gameplay;
using TapCom.MagicCircuit;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// 轨迹预览（TASK-007 目标 7）：按住右键 → 点状抛物线（无落点标记，用户 2026-10-10 拍板）。
    /// 与真实弹道共用 Ballistics.Step ⇒ M1 内预览 = 精确弹道；
    /// M2 引入阻尼/场差异后本预览退化为「瞄准提示」，须重新标注「预览≠保证」（用户 2026-10-09 原始说明）。
    /// 出点 = 真实投掷同款出生口扫描（Ballistics.OverlapsCell 推进至阻挡）；全程被挡则从 ThrowPoint 原点直接画，
    /// 保证任意瞄准角度（含朝下/贴墙/低天花板）都有线可见。
    /// </summary>
    public class TrajectoryPreview : MonoBehaviour
    {
        /// <summary>头顶抛点；留空则自动找名为 ThrowPoint 的子物体，再找不到回退自身位置。</summary>
        [SerializeField] private Transform _throwPoint;

        private const int DotCount = 96;        // 密点列：间距 ≈0.16 格，读感为连续曲线（用户「要顺滑」）
        private const float StepTime = 0.0125f; // 96×0.0125 = 1.2s 预视窗（原 32×0.035≈1.12s 基本不变）
        private const float DotSize = 0.09f;

        private static readonly Color DotColor = new Color(1f, 1f, 1f, 0.7f);

        private readonly List<GameObject> _dots = new List<GameObject>(DotCount);
        private Sprite _square;
        private PlayerInputReader _input;
        private Camera _mainCamera;
        private LevelManager _levels;
        private WorldManager _worlds;
        private IZeroFieldQuery _zeroQuery;

        private void Start()
        {
            _square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            _input = GetComponentInParent<PlayerInputReader>();
            _levels = Services.Container.Get<LevelManager>();
            _worlds = Services.Container.Get<WorldManager>();
            Services.Container.TryGet(out _zeroQuery);
            if (_throwPoint == null) _throwPoint = transform.Find("ThrowPoint");

            for (var i = 0; i < DotCount; i++)
            {
                var dot = NewSquare(Vector3.one * DotSize, DotColor);
                dot.SetActive(false);
                _dots.Add(dot);
            }
        }

        private void Update()
        {
            // out var 不能藏在 && 链里（短路时未必赋值 → CS0165），改提前返回式（语义等价）
            if (_input == null || !_input.AimHeld
                || _levels.CurrentWorldId == null
                || !_worlds.TryGet(_levels.CurrentWorldId, out var world))
            {
                SetVisible(false);
                return;
            }

            var origin = _throwPoint != null ? (Vector2)_throwPoint.position : (Vector2)transform.position;
            var dir = AimDirection(origin);

            // 出点 = 真实投掷同款出生口扫描：沿方向推进至阻挡；起点即被挡则从原点画（任意角度可见，允许带误差）
            var start = origin;
            for (var d = 0f; d <= MagicParams.MuzzleScanDistance + 1e-4f; d += MagicParams.MuzzleScanStep)
            {
                var p = origin + dir * d;
                if (Ballistics.OverlapsCell(world, p, MagicParams.SeedHitboxHalf)) break;
                start = p;
            }

            var pos = start;
            var vel = dir * MagicParams.ThrowSpeed; // 预览不含玩家速度继承的采样误差源：与投掷公式一致（继承项在发布时已定，静止瞄准为主）

            var shown = 0;
            for (var i = 0; i < DotCount; i++)
            {
                var zero = _zeroQuery != null && _zeroQuery.InZeroField(pos);
                var r = Ballistics.Step(world, pos, vel, StepTime, MagicParams.SeedHitboxHalf, zero);
                pos = r.Position;
                vel = r.Velocity;
                _dots[i].transform.position = new Vector3(pos.x, pos.y, 0f);
                _dots[i].SetActive(true);
                shown = i + 1;
                if (r.Hit) break; // 遇障停（同原型：点列止于障碍面）
            }
            for (var i = shown; i < DotCount; i++) _dots[i].SetActive(false);
        }

        /// <summary>瞄准方向：屏幕鼠标点投回 z=0 平面（与 PlayerThrower 同一换算）。</summary>
        private Vector2 AimDirection(Vector2 origin)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null || _input == null) return Vector2.right;
            var screen = _input.AimScreenPosition;
            var planeDistance = -_mainCamera.transform.position.z;
            var aimWorld = _mainCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, planeDistance));
            var dir = (Vector2)aimWorld - origin;
            return dir.sqrMagnitude < 1e-6f ? Vector2.right : dir.normalized;
        }

        private void SetVisible(bool visible)
        {
            foreach (var dot in _dots) dot.SetActive(visible);
        }

        private GameObject NewSquare(Vector3 scale, Color color)
        {
            var go = new GameObject("preview-dot");
            go.transform.SetParent(transform, false);
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _square;
            renderer.color = color;
            return go;
        }
    }
}
