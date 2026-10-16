using TapCom.Core;
using TapCom.MagicCircuit;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>角色运动：移动 + 跳跃，Unity 2D Physics 速度驱动（tech-spec §2）。
    /// 失重场内切漂移分支（TASK-007：W/Space 上、S 下，松手缓停，原型 player.zero L1828）。
    /// 只管运动规则，不做视觉表现（铁律 #6）；gameplay z=0、冻结旋转（铁律 #11）。</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;

        [SerializeField] private float _jumpVelocity = 8f;

        [SerializeField] private LayerMask _groundMask;

        [SerializeField] private Vector2 _groundCheckSize = new(0.9f, 0.08f);

        /// <summary>摔死阈值（TASK-016）：接地瞬间下落速度 ≥ 此值 → Publish FailTriggeredEvent{Fall}。
        /// 换算关系：1 unit = 32 px，18 u/s = 576 px/s（默认值 18f 为设计拍板数）。</summary>
        [SerializeField] private float _fallDeathSpeed = 18f;

        /// <summary>探针盒顶面与自身 collider 底面的净空：杜绝相切命中自己（空中恒 Grounded=true 的根因）。</summary>
        private const float GroundProbeGap = 0.02f;

        private Rigidbody2D _rb;
        private BoxCollider2D _body;
        private PlayerActor _actor;
        private IZeroFieldQuery _zeroQuery;
        private bool _zeroQueryResolved;
        private bool _wasAirborne;
        private float _airborneFallSpeed;
        private bool _fallDead;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _body = GetComponent<BoxCollider2D>();
            _actor = GetComponent<PlayerActor>();
            _rb.freezeRotation = true;
        }

        private void Update()
        {
            UpdateFallDeath();
            if (_fallDead) return; // 摔死当帧起禁用输入（结算流程归 A 线面板，状态推进归订阅方）

            if (_actor?.InputReader == null) return;

            if (!_zeroQueryResolved)
            {
                _zeroQueryResolved = true;
                Services.Container.TryGet(out _zeroQuery);
            }

            if (_zeroQuery != null && _zeroQuery.InZeroField(_body.bounds.center))
            {
                ZeroDrift(Time.deltaTime);
                return;
            }

            var move = _actor.InputReader.MoveInput;
            // Unity 6 API；若 6000.0.53f1 无 linearVelocity，回退 rb.velocity
            _rb.linearVelocity = new Vector2(move.x * _moveSpeed, _rb.linearVelocity.y);

            if (_actor.InputReader.JumpPressed && IsGrounded())
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpVelocity);
        }

        /// <summary>摔死检测（TASK-016）：空中帧记录下落速度（PhysX 落地碰撞会即刻清零 vy，落地帧读不回撞击速度，
        /// 故用「前帧空中速度 + 本帧落地沿」判定）；接地瞬间速度 ≥ 阈值 → 发 FailTriggeredEvent{Fall}。
        /// 只发事件不切状态（白名单推进权归订阅方）；失重场内漂移速度被钳在 ZeroDriftSpeed 之下，天然不触发。</summary>
        private void UpdateFallDeath()
        {
            if (_fallDead) return;
            var grounded = IsGrounded();
            if (grounded)
            {
                if (_wasAirborne && _airborneFallSpeed >= _fallDeathSpeed)
                {
                    _fallDead = true;
                    Debug.Log($"[PlayerMotor] 摔死：落地前下落速度 {_airborneFallSpeed:F1} ≥ 阈值 {_fallDeathSpeed}（TASK-016）", this);
                    Services.EventBus.Publish(new FailTriggeredEvent { Reason = FailReason.Fall });
                }
                _airborneFallSpeed = 0f;
            }
            else
            {
                _airborneFallSpeed = Mathf.Max(0f, -_rb.linearVelocity.y);
            }
            _wasAirborne = !grounded;
        }

        /// <summary>失重漂移：W/Space 上、S 下（MoveInput.y 亦生效），松手指数缓停（原型 L1828）。</summary>
        private void ZeroDrift(float dt)
        {
            var move = _actor.InputReader.MoveInput;
            float targetY;
            if (move.y > 0.1f || _actor.InputReader.JumpPressed) targetY = MagicParams.ZeroDriftSpeed;
            else if (move.y < -0.1f) targetY = -MagicParams.ZeroDriftSpeed;
            else targetY = 0f;

            var velocity = _rb.linearVelocity;
            velocity.x = Mathf.Lerp(velocity.x, move.x * _moveSpeed, Mathf.Min(1f, dt * 8f));
            velocity.y = Mathf.Lerp(velocity.y, targetY, Mathf.Min(1f, dt * 6f));
            _rb.linearVelocity = velocity;
        }

        /// <summary>脚底小盒检测是否着地（只认 Ground 层，防误踩自身/玩家层）。</summary>
        public bool IsGrounded()
        {
            return Physics2D.OverlapBox(GroundProbeCenter(), _groundCheckSize, 0f, _groundMask) != null;
        }

        /// <summary>当前刚体速度（表现层只读，不参与规则计算，铁律 #6）。</summary>
        public Vector2 Velocity => _rb != null ? _rb.linearVelocity : Vector2.zero;

        private void OnDrawGizmosSelected()
        {
            // 调试可视化：地面检测框（真实运行环境验证用）
            if (_body == null) return;
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawWireCube(GroundProbeCenter(), _groundCheckSize);
        }

        private Vector2 GroundProbeCenter()
        {
            var bottom = (Vector2)_body.bounds.center + Vector2.down * _body.bounds.extents.y;
            return bottom + Vector2.down * (GroundProbeGap + _groundCheckSize.y * 0.5f);
        }
    }
}
