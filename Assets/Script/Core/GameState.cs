namespace TapCom.Core
{
    /// <summary>游戏生命周期状态（tech-spec §1：初始化、标题界面、加载、游戏中、暂停、胜利、失败）。</summary>
    public enum GameState
    {
        Initializing,
        TitleScreen,
        Loading,
        /// <summary>关卡开场演出（TASK-015）：开场 CG 图（可缺）+ 运镜到终点再回玩家；游戏静止，结束进 Playing。</summary>
        Cg,
        Playing,
        Paused,
        Victory,
        Defeat,
    }
}
