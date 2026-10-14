using System.Collections.Generic;
using TapCom.Core;
using TapCom.Gameplay;
using TapCom.MagicCircuit;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// 轨迹预览（TASK-007 目标 7）：按住右键 → 点状抛物线（无落点标记，用户 2026-10-10 拍板）。
    /// 与真实弹道共用 Ballistics.Step + SolveThrowVelocity ⇒ M1 内预览 = 精确弹道（2026-10-13「弹道终于指针」：
    /// 初速解算穿过指针点，与 OnSeedThrown 同一函数同一出点扫描）；
    /// M2 引入阻尼/场差异后本预览退化为「瞄准提示」，须重新标注「预览≠保证」（用户 2026-10-09 原始说明）。
    /// 出点 = 真实投掷同款出生口扫描（Ballistics.TryScanMuzzle 推进至阻挡）；全程被挡则从 ThrowPoint 原点直接画，
    /// 保证任意瞄准角度（含朝下/贴墙/低天花板）都有线可见。
    /// 点列止于指针（2026-10-13 拍板「起点头顶、终点鼠标」；v2 解算后弧线过指针，固定时窗会继续延伸——已改进度越界即收尾）；遇障提前止于障碍面。
    /// </summary>
    public class TrajectoryPreview : MonoBehaviour
    {
        /// <summary>头顶抛点；留空则自动找名为 ThrowPoint 的子物体，再找不到回退自身位置。</summary>
        [SerializeField] private Transform _throwPoint;

        /// <summary>点列数量，Inspector 可调（2026-10-13 需求）；改动重进 Play 生效（Start 建池）。</summary>
        [SerializeField] private int _dotCount = 96;

        private const float StepTime = 0.0125f; // 单点步长：96 点 ≈ 1.2s 预视窗（点数变更视窗随之伸缩）
        private const float DotSize = 0.09f;

        private static readonly Color DotColor = new Color(1f, 1f, 1f, 0.7f);

        private List<GameObject> _dots;
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

            _dots = new List<GameObject>(_dotCount);
            for (var i = 0; i < _dotCount; i++)
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
            var aimWorld = AimWorldPoint();
            var aim = aimWorld - origin;
            var dir = aim.sqrMagnitude < 1e-6f ? Vector2.right : aim.normalized;

            // 出点 = 真实投掷同款出手口扫描（TryScanMuzzle）；起点即被挡则从原点画（任意角度可见，允许带误差）
            var start = Ballistics.TryScanMuzzle(world, origin, dir, out var muzzle) ? muzzle : origin;

            var pos = start;
            // 初速解算穿过指针点：与 OnSeedThrown 同一函数同一出点 ⇒ 预览=真实
            //（预览不含玩家速度继承的采样误差源：继承项在发布时已定，静止瞄准为主）
            var vel = Ballistics.SolveThrowVelocity(start, aimWorld, dir);

            // 线终于指针：弧线到达指针即收尾，末点钉在指针上，不再延伸（拍板「起点头顶、终点鼠标」）
            var toTarget = aimWorld - start;
            var targetDist = toTarget.magnitude;
            var along = toTarget / Mathf.Max(targetDist, 1e-6f);

            var shown = 0;
            for (var i = 0; i < _dotCount; i++)
            {
                var zero = _zeroQuery != null && _zeroQuery.InZeroField(pos);
                var r = Ballistics.Step(world, pos, vel, StepTime, MagicParams.SeedHitboxHalf, zero);
                pos = r.Position;
                vel = r.Velocity;

                if (Vector2.Dot(pos - start, along) >= targetDist)
                {
                    _dots[i].transform.position = new Vector3(aimWorld.x, aimWorld.y, 0f);
                    _dots[i].SetActive(true);
                    shown = i + 1;
                    break;
                }

                _dots[i].transform.position = new Vector3(pos.x, pos.y, 0f);
                _dots[i].SetActive(true);
                shown = i + 1;
                if (r.Hit) break; // 遇障提前止于障碍面（种子真实停点）
            }
            for (var i = shown; i < _dotCount; i++) _dots[i].SetActive(false);
        }

        /// <summary>瞄准点：屏幕鼠标坐标投回 z=0 平面（与 PlayerThrower 同一换算）。</summary>
        private Vector2 AimWorldPoint()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null || _input == null) return Vector2.zero;
            var screen = _input.AimScreenPosition;
            var planeDistance = -_mainCamera.transform.position.z;
            return (Vector2)_mainCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, planeDistance));
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
