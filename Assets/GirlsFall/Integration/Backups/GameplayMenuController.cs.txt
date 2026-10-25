using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GirlsFall
{
    public sealed class GameplayMenuController : MonoBehaviour
    {
        public AutoSaveController autosave;
        public GameObject panel;
        public Button resumeButton, titleButton, quitButton;
        public TMP_Text message;
        private bool busy;
        private OnlyUpPrototype.ThirdPersonOrbitCamera orbit;
        private void Awake()
        {
            orbit = UnityEngine.Object.FindFirstObjectByType<OnlyUpPrototype.ThirdPersonOrbitCamera>();
            panel.SetActive(false);
            resumeButton.onClick.AddListener(() => Pause(false));
            titleButton.onClick.AddListener(ReturnToTitle);
            quitButton.onClick.AddListener(() => { if (!autosave.CanSave || autosave.SaveNow()) GameSession.ExitApplication(); else message.text = autosave.Error; });
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !busy) Pause(!panel.activeSelf);
            if (!string.IsNullOrEmpty(autosave.Error)) { if (!panel.activeSelf) Pause(true); message.text = autosave.Error; }
        }
        public void Pause(bool paused)
        {
            panel.SetActive(paused); Time.timeScale = paused ? 0f : 1f;
            if (orbit) orbit.enabled = !paused;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (paused) autosave.SaveNow();
        }
        public void ReturnToTitle()
        {
            if (busy) return;
            if (autosave.CanSave && !autosave.SaveNow()) { message.text = autosave.Error; return; }
            SaveDataManager.Instance.Flush(); GameSettingsManager.Instance.Save(out _);
            busy = true; GameSession.ReturningToTitle(); StartCoroutine(LoadTitle());
        }
        private IEnumerator LoadTitle()
        {
            AsyncOperation load = null;
            try { load = SceneManager.LoadSceneAsync(GameSession.TitleScene); } catch (Exception) { }
            if (load == null) { busy = false; GameSession.Cancel(); message.text = "タイトル画面を読み込めません。"; yield break; }
            while (!load.isDone) yield return null;
        }
        private void OnDestroy() { Time.timeScale = 1f; }
    }
}
