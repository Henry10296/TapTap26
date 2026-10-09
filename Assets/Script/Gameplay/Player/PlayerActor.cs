using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>Player 容器：注册并暴露玩家各组件（tech-spec §2 playerActor）。
    /// 组件间不互相直接引用，统一经本类取用。</summary>
    [DisallowMultipleComponent]
    public class PlayerActor : MonoBehaviour
    {
        public PlayerInputReader InputReader { get; private set; }

        public PlayerMotor Motor { get; private set; }

        public PlayerInventory Inventory { get; private set; }

        public PlayerThrower Thrower { get; private set; }

        private void Awake()
        {
            InputReader = GetComponent<PlayerInputReader>();
            Motor = GetComponent<PlayerMotor>();
            Inventory = GetComponent<PlayerInventory>();
            Thrower = GetComponent<PlayerThrower>();
        }
    }
}
