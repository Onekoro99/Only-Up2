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
        [Header("タイトルメニューのボタン")]
        [Tooltip("最新の進行セーブを読み込むボタンです。セーブが存在しないときは操作できない状態になります。")]
        public Button continueButton;
        [Tooltip("新規ゲームを始めるボタンです。セーブがあれば上書き確認を表示し、なければ直接開始します。オプションは維持します。")]
        public Button newGameButton;
        [Tooltip("タイトル画面のオプションを開くボタンです。ゲーム中のオプションと同じ設定を使用します。")]
        public Button optionsButton;
        [Tooltip("オプション設定を保存して終了するボタンです。EditorではPlayを停止し、タイトルから進行データは上書きしません。")]
        public Button quitButton;
        [Header("新規開始の確認")]
        [Tooltip("既存のセーブを新規ゲームで置き換える確認ダイアログです。はい・いいえのボタンを含みます。")]
        public GameObject confirmation;
        [Tooltip("上書き確認の「はい」ボタンです。新規ゲームとして開始し、進行とADV完了フラグを初期化します。")]
        public Button confirmYes;
        [Tooltip("上書き確認の「いいえ」ボタンです。ダイアログを閉じ、現在の進行データを維持します。")]
        public Button confirmNo;
        [Tooltip("タイトルに表示するオプション画面の制御コンポーネントです。")]
        public OptionsMenuController options;
        [Header("エラーと読込状況")]
        [Tooltip("セーブ読込やScene遷移に失敗した場合に表示するエラーパネルです。")]
        public GameObject errorPanel;
        [Tooltip("読込・設定保存・Scene遷移のエラー内容を表示するテキストです。")]
        public TMP_Text errorText;
        [Tooltip("ゲームSceneを読み込んでいる間の案内を表示するテキストです。")]
        public TMP_Text statusText;
        [Tooltip("エラーパネルだけを閉じてタイトル操作へ戻るボタンです。セーブデータは変更しません。")]
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
        public void ShowNewGameConfirmation() { if (IsBusy) return; if (!SaveDataManager.Instance.HasSave) { ConfirmNewGame(); return; } Sound(); confirmation.SetActive(true); confirmNo.Select(); }
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
