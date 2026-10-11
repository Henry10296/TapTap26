namespace TapCom.Core
{
    /// <summary>游戏生命周期状态（tech-spec §1：初始化、标题界面、加载、游戏中、暂停、胜利、失败）。</summary>
    public enum GameState
    {
        Initializing,
        TitleScreen,
        Loading,
        Playing,
        Paused,
        Victory,
        Defeat,
    }
}
