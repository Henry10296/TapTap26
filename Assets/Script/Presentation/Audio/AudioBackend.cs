using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>音频播放后端收口（TASK-013）：AudioManager 只认此接口。
    /// Invariant：换后端 = 新增实现 + CoreBootstrap 装配行换一个词，AudioManager/Catalog 不动。</summary>
    public interface IAudioBackend
    {
        /// <summary>播一个声音 ID。false = 引擎未就绪 / Post 失败；调用方不关心后端细节。</summary>
        bool Play(string eventId);
    }

    /// <summary>
    /// Wwise 后端（集成 2024.1.17）：首次播放前同步加载 Main bank
    /// （产物位于 Assets/StreamingAssets/Audio/GeneratedSoundBanks/Windows/，与
    /// AkWwiseInitializationSettings 的 m_BasePath: Audio\GeneratedSoundBanks 对齐，无需任何场景配置）。
    /// 播放经自建常驻发射器（AkGameObj 自动 RegisterGameObj，OnEnable 同步 + 引擎晚初始化兜底委托双保险）；
    /// Post 到未注册 ID（含 null→AK_INVALID_GAME_OBJECT）会被 monitor 以 Unknown/Dead game object 拒声——2026-10-12 运行时实证。
    /// 不依赖 Wwise Picker / .wproj：声音 ID 为字符串，引擎内做 FNV hash 与 bank 匹配。
    /// </summary>
    public class WwiseAudioBackend : IAudioBackend
    {
        private const string BankName = "Main";

        private bool _bankLoaded;

        private GameObject _emitter;

        /// <summary>常驻发射器：运行时功能物体（同 CoreBootstrap "Core" 物体先例），跨场景不销毁。</summary>
        private void EnsureEmitter()
        {
            if (_emitter != null)
            {
                return;
            }
            _emitter = new GameObject("WwiseAudioBackendEmitter");
            Object.DontDestroyOnLoad(_emitter);
            _emitter.AddComponent<AkGameObj>();
        }

        public bool Play(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || !AkSoundEngine.IsInitialized())
            {
                return false;
            }

            if (!_bankLoaded)
            {
                // 同步加载时序安全：AudioManager 只在游戏事件（Playing 等）触发 Post，
                // 必然晚于 AkInitializer 的引擎初始化（场景期 OnEnable）。
                // 用 SDK 层重载（返回 AKRESULT）；AkBankManager.LoadBank 返回的是 bank handle ID 非错误码。
                var result = AkSoundEngine.LoadBank(BankName, out _);
                if (result != AKRESULT.AK_Success)
                {
                    Debug.LogError($"[Audio] LoadBank({BankName}) failed: {result}");
                    return false;
                }
                _bankLoaded = true;
                Debug.Log($"[Audio] bank loaded: {BankName}");
            }

            EnsureEmitter();
            var playingId = AkSoundEngine.PostEvent(eventId, _emitter);
            if (playingId == AkSoundEngine.AK_INVALID_PLAYING_ID)
            {
                Debug.LogError($"[Audio] PostEvent failed: {eventId}（bank 里没有该 Event？）");
                return false;
            }

            Debug.Log($"[Audio] play {eventId} (playingId={playingId})");
            return true;
        }
    }
}
