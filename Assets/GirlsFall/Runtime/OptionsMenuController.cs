using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GirlsFall
{
    public sealed class OptionsMenuController : MonoBehaviour
    {
        public Slider masterSlider, bgmSlider, seSlider, sensitivitySlider;
        public TMP_Text masterValue, bgmValue, seValue, sensitivityValue, feedback;
        public Toggle fullscreen;
        public TMP_Dropdown resolution;
        public Button saveButton, defaultsButton, backButton;
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
