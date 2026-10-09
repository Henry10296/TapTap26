using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>角色运动：移动 + 跳跃，Unity 2D Physics 速度驱动（tech-spec §2）。
    /// 只管运动规则，不做视觉表现（铁律 #6）；gameplay z=0、冻结旋转（铁律 #11）。</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;

        [SerializeField] private float _jumpVelocity = 8f;

        [SerializeField] private LayerMask _groundMask;

        [SerializeField] private float _groundCheckDistance = 0.08f;

        [SerializeField] private Vector2 _groundCheckSize = new(0.9f, 0.08f);

        private Rigidbody2D _rb;
        private BoxCollider2D _body;
        private PlayerActor _actor;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _body = GetComponent<BoxCollider2D>();
            _actor = GetComponent<PlayerActor>();
            _rb.freezeRotation = true;
        }

        private void Update()
        {
            if (_actor?.InputReader == null) return;

            var move = _actor.InputReader.MoveInput;
            // Unity 6 API；若 6000.0.53f1 无 linearVelocity，回退 rb.velocity
            _rb.linearVelocity = new Vector2(move.x * _moveSpeed, _rb.linearVelocity.y);

            if (_actor.InputReader.JumpPressed && IsGrounded())
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpVelocity);
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
            return bottom + Vector2.down * _groundCheckDistance * 0.5f;
        }
    }
}
