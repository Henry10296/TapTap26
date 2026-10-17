using System.Collections.Generic;
using TapCom.Core;
using TapCom.MagicCircuit;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>
    /// 魔法范围指示（2026-10-17 用户拍板「触发时显示魔法范围」；同日误删后按用户澄清恢复——范围显示保留）：
    /// 订阅 SpellCastEvent（MagicCircuitSimulator 发布，音效/震屏同源）在效果中心画半径圆圈。
    /// 半径取 MagicParams 配方表（虚空 2.5 / 爆炸 3.5 / 引力 4.5 / 静止 2.5）；
    /// 场类配方（虚空/引力）显示 FieldDuration=5s（场存在期），瞬时配方（爆炸/静止）短闪 _flashDuration。
    /// 圆圈 = 点阵复刻 TrajectoryPreview 白图先例（运行时 Sprite.Create，SpriteRenderer 默认材质，URP 下零材质风险）；
    /// 点数/点径/颜色/时长全可调。挂场景常驻物体（人工 1 步，节点缩放保持 1）。
    /// </summary>
    public class SpellRangeIndicator : MonoBehaviour
    {
        [SerializeField] private int _dotCount = 64;                          // 圆周点数
        [SerializeField] private float _dotSize = 0.12f;                      // 单点尺寸（世界单位）
        [SerializeField] private Color _color = new Color(1f, 1f, 1f, 0.6f);  // 点颜色
        [SerializeField] private float _flashDuration = 0.35f;                // 瞬时配方（爆炸/静止）显示时长
        [SerializeField] private int _sortingOrder = 10;                      // 压在场景层之上保证可见

        private readonly List<SpriteRenderer> _dots = new List<SpriteRenderer>();
        private GameObject _ringRoot;
        private float _remaining;

        private void Start()
        {
            // Texture2D.whiteTexture 4×4 白图，pixelsPerUnit=4 ⇒ 1 单位见方（与 TrajectoryPreview 同款）
            var square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            _ringRoot = new GameObject("SpellRangeRing");
            _ringRoot.transform.SetParent(transform, false);
            for (var i = 0; i < _dotCount; i++)
            {
                var go = new GameObject($"dot{i}", typeof(SpriteRenderer));
                go.transform.SetParent(_ringRoot.transform, false);
                go.transform.localScale = new Vector3(_dotSize, _dotSize, 1f);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = square;
                sr.color = _color;
                sr.sortingOrder = _sortingOrder;
                _dots.Add(sr);
            }
            _ringRoot.SetActive(false); // 初始隐藏，首播才显形
            Services.EventBus.Subscribe<SpellCastEvent>(OnSpellCast);
        }

        private void OnDestroy()
        {
            if (Services.Container == null) return;
            Services.EventBus.Unsubscribe<SpellCastEvent>(OnSpellCast);
            if (_ringRoot != null) Destroy(_ringRoot);
        }

        private void OnSpellCast(SpellCastEvent e)
        {
            var radius = e.Type switch
            {
                SpellType.Zero => MagicParams.ZeroFieldRadius,
                SpellType.Blast => MagicParams.BlastRadius,
                SpellType.Attract => MagicParams.AttractRadius,
                _ => MagicParams.StaticRadius,
            };
            var step = Mathf.PI * 2f / _dots.Count;
            for (var i = 0; i < _dots.Count; i++)
            {
                var angle = i * step;
                _dots[i].transform.position = e.Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            // 场类配方（虚空/引力）跟场持续期；瞬时配方（爆炸/静止）短闪
            _remaining = (e.Type == SpellType.Zero || e.Type == SpellType.Attract)
                ? MagicParams.FieldDuration
                : _flashDuration;
            _ringRoot.SetActive(true);
        }

        private void Update()
        {
            if (_ringRoot == null || !_ringRoot.activeSelf) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) _ringRoot.SetActive(false);
        }
    }
}
