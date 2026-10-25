using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace GirlsFall {
 [DefaultExecutionOrder(80)]
 public sealed class GameplayMenuController:MonoBehaviour {
  [Header("ポーズ画面の参照")]
  [Tooltip("ポーズ時・タイトル復帰前に現在の位置や落下状態を保存するAutoSaveControllerです。")]
  public AutoSaveController autosave;
  [Tooltip("Escで表示・非表示を切り替えるポーズパネルです。ゲームに戻る・オプション・タイトル復帰のボタンを含みます。")]
  public GameObject panel;
  [Tooltip("ポーズを閉じ、停止前の移動・カメラ操作と時間の進みを復元するボタンです。")]
  public Button resumeButton;
  [Tooltip("現在の進行を保存してからタイトルへ戻るボタンです。保存に失敗した場合はゲームSceneにとどまります。")]
  public Button titleButton;
  [Tooltip("旧メニュー用のゲーム終了ボタン参照です。現行の3項目メニューでは未使用のため空欄です。設定した場合は進行保存後に終了します。")]
  public Button quitButton;
  [Tooltip("ポーズを維持したまま、タイトルと共通のオプション画面を開くボタンです。")]
  public Button optionsButton;
  [Tooltip("ポーズ中に表示するOptionsMenuControllerです。保存先と設定値はタイトル側のオプションと共有します。")]
  public OptionsMenuController options;
  [Tooltip("進行保存やScene遷移に失敗した理由を表示するTextMeshProです。")]
  public TMP_Text message;
  [Header("停止するゲーム操作")]
  [Tooltip("ポーズ中に停止する操作用コンポーネント一覧です。移動・カメラ等を指定し、再開時には停止前の有効状態へ戻します。")]
  public Behaviour[] gameplayControls=Array.Empty<Behaviour>();
  public bool IsPaused {get;private set;}
  public bool IsBusy=>busy;
  bool busy;float priorTimeScale=1;
  readonly List<Behaviour> pausedControls=new List<Behaviour>();
  readonly List<bool> priorEnabled=new List<bool>();
  void Awake(){
   panel.SetActive(false);if(options)options.gameObject.SetActive(false);
   resumeButton.onClick.AddListener(()=>Pause(false));titleButton.onClick.AddListener(ReturnToTitle);
   if(optionsButton)optionsButton.onClick.AddListener(OpenOptions);
   if(quitButton&&quitButton!=optionsButton)quitButton.onClick.AddListener(()=>{if(SaveProgress())GameSession.ExitApplication();});
  }
  void Update(){
   HandlePauseInput(Input.GetKeyDown(KeyCode.Escape));
   if(!string.IsNullOrEmpty(autosave.Error)){
    if(!IsPaused)Pause(true);message.text=autosave.Error;
   }
  }
  public void HandlePauseInput(bool escape){
   if(!escape||busy)return;
   if(options&&options.gameObject.activeSelf){if(options.SaveSettings())options.gameObject.SetActive(false);return;}
   Pause(!IsPaused);
  }
  public void Pause(bool paused){
   if(busy||paused==IsPaused)return;
   IsPaused=paused;panel.SetActive(paused);
   if(paused){
    priorTimeScale=Time.timeScale;pausedControls.Clear();priorEnabled.Clear();
    foreach(var b in gameplayControls)if(b&&b!=this&&!pausedControls.Contains(b)){pausedControls.Add(b);priorEnabled.Add(b.enabled);b.enabled=false;}
    Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    SaveProgress();
   }else{
    if(options)options.gameObject.SetActive(false);
    Time.timeScale=priorTimeScale;
    for(int i=0;i<pausedControls.Count;i++)if(pausedControls[i])pausedControls[i].enabled=priorEnabled[i];
    // The existing orbit camera captures on its next LateUpdate, discarding warp deltas.
    Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    foreach(var b in pausedControls)if(b is OnlyUpPrototype.ThirdPersonOrbitCamera && b.enabled)StartCoroutine(RecaptureAfterResume((OnlyUpPrototype.ThirdPersonOrbitCamera)b));
   }
  }
  IEnumerator RecaptureAfterResume(OnlyUpPrototype.ThirdPersonOrbitCamera camera){
   yield return null;
   if(!IsPaused&&!busy&&isActiveAndEnabled&&camera&&camera.enabled){camera.enabled=false;camera.enabled=true;}
  }
  public void OpenOptions(){
   if(busy||!options)return;
   if(!IsPaused)Pause(true);
   options.Show();
  }
  bool SaveProgress(){
   if(!autosave||!autosave.CanSave){message.text=autosave?autosave.Error:"セーブ機能が見つかりません。";return false;}
   if(!autosave.SaveNow()){message.text=autosave.Error;return false;}
   message.text="";return true;
  }
  public void ReturnToTitle(){
   if(busy)return;
   if(!IsPaused)Pause(true);
   if(!SaveProgress())return;
   SaveDataManager.Instance.Flush();
   if(!GameSettingsManager.Instance.Save(out string error)){message.text=error;return;}
   busy=true;GameSession.ReturningToTitle();StartCoroutine(LoadTitle());
  }
  IEnumerator LoadTitle(){
   AsyncOperation load=null;
   try{load=SceneManager.LoadSceneAsync(GameSession.TitleScene);}catch(Exception){}
   if(load==null){busy=false;GameSession.Cancel();message.text="タイトル画面を読み込めません。";yield break;}
   while(!load.isDone)yield return null;
  }
  void OnDestroy(){Time.timeScale=1;}
 }
}