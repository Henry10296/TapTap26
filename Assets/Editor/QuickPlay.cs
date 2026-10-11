using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TapCom.EditorTools
{
    /// <summary>
    /// 编辑器快捷启动：热键 Ctrl+Alt+P 直接以 Start.unity 进入 Play 模式，省去手动切场景再点 Play。
    /// 仅编辑器可用，不进包（铁律：资产与场景人工配置，本类不生成任何物体）。
    /// </summary>
    public static class QuickPlay
    {
        private const string StartScenePath = "Assets/Scenes/Start.unity";

        [MenuItem("Tools/快速启动/从Start播放 %&p")]
        public static void PlayFromStart()
        {
            if (EditorApplication.isPlaying) return; // Play 中不动作；先停止再按

            // 当前场景有未保存改动时，OpenScene 走 Unity 自带保存提示，不静默丢弃
            EditorSceneManager.OpenScene(StartScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
