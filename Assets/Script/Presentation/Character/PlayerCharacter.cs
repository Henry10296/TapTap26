using TapCom.Gameplay;
using UnityEngine;
using TapCom.Core;//东暮添加用于动画机


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
        private Camera _mainCamera;//用于瞄准翻转
        private void Update()
        {
            if (_actor?.Motor == null) return;

            var velocity = _actor.Motor.Velocity;
            _animator.SetFloat(SpeedParam, Mathf.Abs(velocity.x));
            _animator.SetBool(GroundedParam, _actor.Motor.IsGrounded());
            _animator.SetFloat("VerticalSpeed", velocity.y);//用于下落动画

            var input = _actor.InputReader;//用于投掷动画
            var playing = Services.Container.Get<GameManager>().Current
              == GameState.Playing;

            bool aiming = playing && input != null && input.AimHeld;
            _animator.SetBool("Aiming", aiming);

            if (playing && input != null && input.ThrowPressed)
            {
                _animator.SetTrigger("Throw");
            }

            // 朝向随移动方向翻转；贴图默认朝向如与预期相反，验收时对调两处赋值
            //f (velocity.x > FlipThreshold) _renderer.flipX = false;
            //else if (velocity.x < -FlipThreshold) _renderer.flipX = true;
            //替换为以下，用于瞄准翻转
            if (aiming)
            {
                if (_mainCamera == null)
                    _mainCamera = Camera.main;

                if (_mainCamera != null)
                {
                    var mouseScreen = input.AimScreenPosition;
                    var aimWorld = _mainCamera.ScreenToWorldPoint(
                        new Vector3(
                            mouseScreen.x,
                            mouseScreen.y,
                            -_mainCamera.transform.position.z
                        )
                    );

                    float directionX = aimWorld.x - transform.position.x;

                    // 鼠标接近角色中线时保持原朝向，避免抖动
                    if (Mathf.Abs(directionX) > FlipThreshold)
                        _renderer.flipX = directionX < 0f;
                }
            }
            else
            {
                if (velocity.x > FlipThreshold)
                    _renderer.flipX = false;
                else if (velocity.x < -FlipThreshold)
                    _renderer.flipX = true;
            }
        }

        private EventBus _bus;//用于跳跃
        private void OnEnable()
        {
            _bus = Services.EventBus;
            _bus.Subscribe<PlayerJumpedEvent>(OnPlayerJumped);
        }

        private void OnDisable()
        {
            _bus?.Unsubscribe<PlayerJumpedEvent>(OnPlayerJumped);
        }

        private void OnPlayerJumped(PlayerJumpedEvent e)
        {
            _animator.SetTrigger("Jump");
        }
    }
}
