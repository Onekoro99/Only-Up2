using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GirlsFall
{
    public sealed class OptionsMenuController : MonoBehaviour
    {
        [Header("設定用スライダー")]
        [Tooltip("全体音量を調整するスライダーです。値を上げると全体が大きくなり、0で無音になります。値の範囲は0〜1です。")]
        public Slider masterSlider;
        [Tooltip("BGM音量のスライダーです。値を上げるとBGMが大きくなります。実際の音量には全体音量も掛かります。範囲は0〜1です。")]
        public Slider bgmSlider;
        [Tooltip("効果音の音量スライダーです。値を上げると効果音が大きくなります。実際の音量には全体音量も掛かります。範囲は0〜1です。")]
        public Slider seSlider;
        [Tooltip("マウスによるカメラ回転感度のスライダーです。値を上げると少ない移動で大きく回転し、下げるとゆっくりになります。")]
        public Slider sensitivitySlider;
        [Header("現在値とメッセージの表示")]
        [Tooltip("全体音量をパーセントで表示するテキストです。スライダーの現在値に合わせて更新します。")]
        public TMP_Text masterValue;
        [Tooltip("BGM音量のスライダー値をパーセント表示するテキストです。")]
        public TMP_Text bgmValue;
        [Tooltip("効果音の音量スライダー値をパーセント表示するテキストです。")]
        public TMP_Text seValue;
        [Tooltip("マウス感度の現在値を小数で表示するテキストです。")]
        public TMP_Text sensitivityValue;
        [Tooltip("オプション設定の保存成功や、保存失敗のメッセージを表示するテキストです。")]
        public TMP_Text feedback;
        [Header("画面表示の設定")]
        [Tooltip("オンでフルスクリーン、オフでウィンドウ表示を選ぶトグルです。保存または戻る操作時に画面設定へ適用します。")]
        public Toggle fullscreen;
        [Tooltip("表示解像度を選ぶドロップダウンです。大きな解像度ほど細かく表示できますが描画負荷が増えます。保存または戻る操作時に適用します。")]
        public TMP_Dropdown resolution;
        [Header("設定画面のボタン")]
        [Tooltip("現在のオプションを適用して保存するボタンです。進行データは変更しません。")]
        public Button saveButton;
        [Tooltip("音量・解像度・感度などを初期設定へ戻すボタンです。進行データは初期化しません。保存すると再起動後も初期設定が使われます。")]
        public Button defaultsButton;
        [Tooltip("設定を適用・保存してオプション画面を閉じるボタンです。保存に失敗した場合は画面を閉じません。")]
        public Button backButton;
        private readonly List<Vector2Int> sizes = new List<Vector2Int>();
        private bool hooked;
        public void Show()
        {
            gameObject.SetActive(true); Hook(); Populate();
        }
        private void Hook()
        {
            if (hooked) return; hooked = true;
            masterSlider.onValueChanged.AddListener(v => { GameSettingsManager.Instance.Current.master = v; ApplyAudio(); });
            bgmSlider.onValueChanged.AddListener(v => { GameSettingsManager.Instance.Current.bgm = v; ApplyAudio(); });
            seSlider.onValueChanged.AddListener(v => { GameSettingsManager.Instance.Current.se = v; ApplyAudio(); });
            sensitivitySlider.onValueChanged.AddListener(v => { GameSettingsManager.Instance.Current.mouseSensitivity = v; ApplyAudio(); });
            saveButton.onClick.AddListener(Save);
            defaultsButton.onClick.AddListener(() => { GameSettingsManager.Instance.Defaults(); Populate(); });
            backButton.onClick.AddListener(() => { if (SaveSettings()) gameObject.SetActive(false); });
        }
        private void Populate()
        {
            GameSettings s = GameSettingsManager.Instance.Current;
            masterSlider.SetValueWithoutNotify(s.master); bgmSlider.SetValueWithoutNotify(s.bgm); seSlider.SetValueWithoutNotify(s.se);
            sensitivitySlider.SetValueWithoutNotify(s.mouseSensitivity); fullscreen.SetIsOnWithoutNotify(s.fullscreen);
            sizes.Clear();
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(s.width, s.height) })
                if (!sizes.Contains(size)) sizes.Add(size);
            foreach (Resolution available in Screen.resolutions)
            {
                Vector2Int size = new Vector2Int(available.width, available.height);
                if (size.x >= 640 && size.y >= 360 && !sizes.Contains(size)) sizes.Add(size);
            }
            sizes.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            resolution.ClearOptions();
            List<string> labels = new List<string>(); foreach (Vector2Int size in sizes) labels.Add(size.x + " × " + size.y);
            resolution.AddOptions(labels);
            resolution.SetValueWithoutNotify(Mathf.Max(0, sizes.IndexOf(new Vector2Int(s.width, s.height))));
            feedback.text = ""; UpdateValues();
        }
        private void ApplyAudio() { GameSettingsManager.Instance.Apply(false); UpdateValues(); }
        private void UpdateValues()
        {
            masterValue.text = Mathf.RoundToInt(masterSlider.value * 100) + "%";
            bgmValue.text = Mathf.RoundToInt(bgmSlider.value * 100) + "%";
            seValue.text = Mathf.RoundToInt(seSlider.value * 100) + "%";
            sensitivityValue.text = sensitivitySlider.value.ToString("F1");
        }
        public bool SaveSettings()
        {
            var manager = GameSettingsManager.Instance;
            manager.Current.fullscreen = fullscreen.isOn;
            Vector2Int size = sizes[Mathf.Clamp(resolution.value, 0, sizes.Count - 1)];
            manager.Current.width = size.x; manager.Current.height = size.y;
            manager.Apply(true);
            bool saved = manager.Save(out string error);
            feedback.text = saved ? "設定を保存しました。" : error;
            return saved;
        }
        public void Save() { GameSettingsManager.Instance.Click(); SaveSettings(); }
    }
}
