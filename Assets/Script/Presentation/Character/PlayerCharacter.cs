using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>Character 动画机（tech-spec §6）：负责 Player 的视觉表现——动画状态与朝向。
    /// 只读 Gameplay 状态驱动 Animator 参数，不参与规则计算（铁律 #6）。
    /// 参数契约（Player.controller）：Speed=float（|横向速度|）、Grounded=bool。</summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerCharacter : MonoBehaviour
    {
        private const string SpeedParam = "Speed";
        private const string GroundedParam = "Grounded";

        private const float FlipThreshold = 0.01f;

        private PlayerActor _actor;
        private Animator _animator;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _actor = GetComponent<PlayerActor>();
            _animator = GetComponent<Animator>();
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_actor?.Motor == null) return;

            var velocity = _actor.Motor.Velocity;
            _animator.SetFloat(SpeedParam, Mathf.Abs(velocity.x));
            _animator.SetBool(GroundedParam, _actor.Motor.IsGrounded());

            // 朝向随移动方向翻转；贴图默认朝向如与预期相反，验收时对调两处赋值
            if (velocity.x > FlipThreshold) _renderer.flipX = false;
            else if (velocity.x < -FlipThreshold) _renderer.flipX = true;
        }
    }
}
