using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GirlsFall
{
    public sealed class TitleMenuController : MonoBehaviour
    {
        public Button continueButton, newGameButton, optionsButton, quitButton;
        public GameObject confirmation;
        public Button confirmYes, confirmNo;
        public OptionsMenuController options;
        public GameObject errorPanel;
        public TMP_Text errorText, statusText;
        public Button errorClose;
        public bool IsBusy { get; private set; }
        public string LastError { get; private set; }

        private void Awake()
        {
            Time.timeScale = 1f; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            GameSession.Ready();
            GameSettingsManager.Instance.Apply(true);
            GameSettingsManager.Instance.TitleMusic(true);
            continueButton.onClick.AddListener(ContinueGame);
            newGameButton.onClick.AddListener(ShowNewGameConfirmation);
            optionsButton.onClick.AddListener(() => { Sound(); options.Show(); });
            quitButton.onClick.AddListener(Quit);
            confirmYes.onClick.AddListener(ConfirmNewGame);
            confirmNo.onClick.AddListener(() => { Sound(); confirmation.SetActive(false); });
            errorClose.onClick.AddListener(() => errorPanel.SetActive(false));
            confirmation.SetActive(false); options.gameObject.SetActive(false); errorPanel.SetActive(false);
            RefreshContinue();
        }
        public void RefreshContinue() { continueButton.interactable = !IsBusy && SaveDataManager.Instance.HasSave; }
        public void ContinueGame()
        {
            if (IsBusy) return; Sound();
            if (!SaveDataManager.Instance.TryLoad(out PlayerSaveState state, out string error)) { ShowError(error); RefreshContinue(); return; }
            GameSession.RequestContinue(state); StartCoroutine(LoadGameplay());
        }
        public void ShowNewGameConfirmation() { if (IsBusy) return; Sound(); confirmation.SetActive(true); confirmNo.Select(); }
        public void ConfirmNewGame()
        {
            if (IsBusy) return; Sound(); confirmation.SetActive(false);
            GameSession.RequestNew(); StartCoroutine(LoadGameplay());
        }
        private IEnumerator LoadGameplay()
        {
            IsBusy = true;
            foreach (Button button in new[] { continueButton, newGameButton, optionsButton, quitButton }) button.interactable = false;
            statusText.text = "読み込み中…";
            AsyncOperation load = null;
            try { load = SceneManager.LoadSceneAsync(GameSession.GameplayScene); }
            catch (Exception) { }
            if (load == null)
            {
                IsBusy = false; GameSession.Cancel(); statusText.text = "";
                foreach (Button button in new[] { newGameButton, optionsButton, quitButton }) button.interactable = true;
                RefreshContinue(); ShowError("ゲームシーンを読み込めません。Build Settingsを確認してください。"); yield break;
            }
            while (!load.isDone) yield return null;
        }
        public void ShowError(string message) { LastError = message; errorText.text = message; errorPanel.SetActive(true); }
        public void Quit()
        {
            if (IsBusy) return; Sound();
            if (!GameSettingsManager.Instance.Save(out string error)) { ShowError(error); return; }
            // There is no AutoSaveController on this scene; never create progress here.
            GameSession.ExitApplication();
        }
        private static void Sound() { GameSettingsManager.Instance.Click(); }
    }
}
