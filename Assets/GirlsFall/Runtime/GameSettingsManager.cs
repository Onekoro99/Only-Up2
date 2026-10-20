using System;
using System.IO;
using UnityEngine;

namespace GirlsFall
{
    [Serializable]
    public sealed class GameSettings
    {
        public float master = 0.8f, bgm = 0.6f, se = 0.8f;
        public bool fullscreen = true;
        public int width = 1920, height = 1080;
        public float mouseSensitivity = 3f;
    }

    public sealed class GameSettingsManager : MonoBehaviour
    {
        private static GameSettingsManager instance;
        public static GameSettingsManager Instance
        {
            get
            {
                if (!instance) new GameObject("GirlsFallSettings").AddComponent<GameSettingsManager>();
                return instance;
            }
        }
        public GameSettings Current { get; private set; }
        public event Action Changed;
        private AtomicSaveFile file;
        private long revision;
        private AudioSource music, effects;
        private AudioClip tune, click;
        public float BgmOutputVolume => music ? music.volume : 0;
        public float SeOutputVolume => effects ? effects.volume : 0;
        private void Awake()
        {
            if (instance && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject);
            file = new AtomicSaveFile(Path.Combine(Application.persistentDataPath, "girls-fall-options.json"));
            Current = new GameSettings();
            if (file.TryRead(out string json, out revision, out _))
                try { Current = JsonUtility.FromJson<GameSettings>(json) ?? new GameSettings(); } catch (Exception) { }
            music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.playOnAwake = false;
            effects = gameObject.AddComponent<AudioSource>(); effects.playOnAwake = false;
            tune = MakeTone(true); click = MakeTone(false); music.clip = tune;
            Apply(false);
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        public void Apply(bool applyDisplay)
        {
            Current.master = Safe(Current.master, 0.8f, 0f, 1f);
            Current.bgm = Safe(Current.bgm, 0.6f, 0f, 1f);
            Current.se = Safe(Current.se, 0.8f, 0f, 1f);
            Current.mouseSensitivity = Safe(Current.mouseSensitivity, 3f, 0.2f, 10f);
            Current.width = Mathf.Clamp(Current.width, 640, 7680); Current.height = Mathf.Clamp(Current.height, 360, 4320);
            AudioListener.volume = Current.master;
            if (music) music.volume = Current.bgm * 0.18f;
            if (effects) effects.volume = Current.se * 0.3f;
            if (applyDisplay) Screen.SetResolution(Current.width, Current.height, Current.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            ApplyCamera(); Changed?.Invoke();
        }
        public bool Save(out string error)
        {
            try { file.WritePacked(AtomicSaveFile.Pack(++revision, JsonUtility.ToJson(Current))); error = null; return true; }
            catch (Exception) { error = "設定を保存できません。保存先の空き容量・アクセス権を確認してください。"; return false; }
        }
        public void Defaults() { Current = new GameSettings(); Apply(true); }
        public void Click() { if (effects && click) effects.PlayOneShot(click); }
        public void TitleMusic(bool enabled) { if (!music) return; if (enabled) { if (!music.isPlaying) music.Play(); } else music.Stop(); }
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) { ApplyCamera(); TitleMusic(scene.name == "TitleScene"); }
        private void ApplyCamera()
        {
            var camera = UnityEngine.Object.FindFirstObjectByType<OnlyUpPrototype.ThirdPersonOrbitCamera>();
            if (camera) camera.Sensitivity = Current.mouseSensitivity;
        }
        private static float Safe(float value, float fallback, float min, float max) { return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max); }
        private static AudioClip MakeTone(bool loop)
        {
            const int rate = 22050;
            float[] samples = new float[loop ? rate * 8 : rate / 9];
            float[] notes = { 523.25f, 659.25f, 783.99f, 659.25f, 587.33f, 698.46f, 783.99f, 1046.5f };
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / rate;
                float local = loop ? time % 1f : time;
                float envelope = loop ? Mathf.Sin(Mathf.PI * local) * Mathf.Exp(-local * 3f) : Mathf.Exp(-time * 45f) * Mathf.Min(1f, time * 300f);
                float frequency = loop ? notes[Mathf.Min(7, (int)time)] : 1046.5f;
                samples[i] = (Mathf.Sin(time * frequency * Mathf.PI * 2f) + 0.2f * Mathf.Sin(time * frequency * Mathf.PI * 4f)) * envelope * 0.25f;
            }
            AudioClip result = AudioClip.Create(loop ? "PlaceholderCandyBGM" : "CandyClickSE", samples.Length, 1, rate, false);
            result.SetData(samples, 0); return result;
        }
        private void OnApplicationQuit() { Save(out _); }
#if UNITY_EDITOR
        public void UseStorageForTests(string directory)
        {
            file = new AtomicSaveFile(Path.Combine(directory, "girls-fall-options.json")); revision = 0;
            if (file.TryRead(out string json, out revision, out _))
                try { Current = JsonUtility.FromJson<GameSettings>(json) ?? new GameSettings(); } catch (Exception) { }
            Apply(false);
        }
#endif
        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            if (tune) Destroy(tune); if (click) Destroy(click);
            if (instance == this) instance = null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void ResetStatic() { instance = null; }
    }
}
