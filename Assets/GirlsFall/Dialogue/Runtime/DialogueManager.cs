using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using OnlyUpPrototype;
namespace GirlsFall.Dialogue {
 [DefaultExecutionOrder(60)]
 public sealed class DialogueManager:MonoBehaviour {
  [Header("会話データと連携先")]
  [Tooltip("新規ゲーム・未完了セーブで再生するオープニング会話データです。セリフの編集はこのScriptableObjectで行います。")]
  public DialogueData openingDialogue;
  [Tooltip("セリフウィンドウ・立ち絵・各ボタンをまとめたDialogueUIControllerの参照です。通常は接続済みの参照を維持してください。")]
  public DialogueUIController ui;
  [Tooltip("ADV完了フラグを既存の進行データへ保存するAutoSaveControllerです。位置・PVなども同じセーブに保持します。")]
  public AutoSaveController autosave;
  [Tooltip("ADV開始前の接地確認と、開始地点を床へ落ち着かせる処理に使うプレイヤー移動コンポーネントです。")]
  public PrototypePlayerMotor motor;
   [Header("ADV中の入力と演出")]
   [Tooltip("ADV中に一時停止する操作用コンポーネントの一覧です。移動・カメラ・ポーズ等を指定し、終了後は元の有効状態に戻します。")]
   public Behaviour[] gameplayControls;
  [Min(.01f)]
  [Tooltip("ADV画面の表示・終了フェードにかける時間（秒）です。大きいほどゆっくり、小さいほど素早くなります。初期値0.2秒です。")]
  public float uiFadeSeconds=.2f;
  [Tooltip("有効ならゲーム開始時にセーブの完了フラグを確認し、未完了のオープニングを再生します。無効なら自動開始せず、別イベントからの再生は利用できます。")]
  public bool playOpeningOnStart=true;
  public bool IsPlaying {get;private set;}
  public bool AwaitingLanding {get;private set;}
  public bool SaveFailed {get;private set;}
  public int LineIndex {get;private set;}
  public bool IsTyping=>IsPlaying&&!finishing&&revealed<ui.CharacterCount;
  DialogueData active;Action onFinished;bool opening,finishing,preparing,locked;
  float lineTime,revealed,fade,savedTimeScale;int openedFrame,lastAdvanceFrame=-1;
  readonly List<Behaviour> suspended=new List<Behaviour>();
  readonly List<bool> enabledStates=new List<bool>();
  CursorLockMode previousLock;bool previousVisible;
  void Awake(){
   ui.Hide();
   ui.advanceButton.onClick.AddListener(Advance);
   ui.skipButton.onClick.AddListener(Skip);
   ui.retryButton.onClick.AddListener(RetrySave);
   ui.titleButton.onClick.AddListener(ReturnToTitleWithoutCompletion);
  }
  void Start(){
   if(!playOpeningOnStart||!autosave||!autosave.CanSave||autosave.OpeningDialogueCompleted)return;
   if(!openingDialogue){Debug.LogWarning("Opening dialogue is not assigned.",this);return;}
   if(autosave.LoadedState!=null&&(autosave.LoadedState.airborne||autosave.LoadedState.falling)){
    AwaitingLanding=true;return; // Keep restored position/velocity; never teleport an old fall snapshot.
   }
   PrepareOpening();
  }
  void PrepareOpening(){
   LockGameplay();preparing=true;opening=true;active=openingDialogue;
  }
  void Update(){
   if(AwaitingLanding&&motor.IsGrounded){AwaitingLanding=false;PrepareOpening();}
   if(preparing){
    // Settle the new spawn onto its existing collider without accepting player input.
    if(!motor.IsGrounded){float old=Time.timeScale;Time.timeScale=1;motor.Simulate(Vector2.zero,false,.02f);Time.timeScale=old;return;}
    motor.GetComponent<FallTracker>().Sample(motor.IsGrounded,motor.VerticalSpeed);
    preparing=false;Begin(active,true,null);
   }
   if(!IsPlaying)return;
   Tick(Time.unscaledDeltaTime);
   if(!finishing&&!SaveFailed&&(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)))Advance();
  }
  void LockGameplay(){
   if(locked)return;locked=true;savedTimeScale=Time.timeScale;previousLock=Cursor.lockState;previousVisible=Cursor.visible;
   suspended.Clear();enabledStates.Clear();
   foreach(var b in gameplayControls)if(b&&b!=this&&!suspended.Contains(b)){suspended.Add(b);enabledStates.Add(b.enabled);b.enabled=false;}
   if(autosave)autosave.DialoguePaused=true;
   Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
  }
  void UnlockGameplay(){
   if(!locked)return;locked=false;Time.timeScale=savedTimeScale;
   if(autosave)autosave.DialoguePaused=false;
   for(int i=0;i<suspended.Count;i++)if(suspended[i])suspended[i].enabled=enabledStates[i];
   Cursor.lockState=previousLock;Cursor.visible=previousVisible;
  }
  public bool PlayEvent(DialogueData data,Action completed=null){
   if(IsPlaying||preparing||!data)return false;
   AwaitingLanding=false;Begin(data,false,completed);return true;
  }
  void Begin(DialogueData data,bool isOpening,Action completed){
   LockGameplay();active=data;opening=isOpening;onFinished=completed;
   IsPlaying=true;finishing=false;SaveFailed=false;fade=0;LineIndex=0;openedFrame=Time.frameCount;
   ui.advanceButton.interactable=ui.skipButton.interactable=true;ui.Show();
   if(active.lines==null||active.lines.Count==0){Finish();return;}ShowLine();
  }
  void ShowLine(){
   var line=active.lines[LineIndex];lineTime=0;revealed=0;
   ui.SetLine(string.IsNullOrWhiteSpace(line.characterName)?active.defaultCharacterName:line.characterName,line.text,line.expression);
  }
  public void Tick(float dt){
   if(!IsPlaying)return;dt=Mathf.Max(0,dt);
   if(finishing){fade=Mathf.Max(0,fade-dt/uiFadeSeconds);ui.group.alpha=fade;if(fade<=0)Complete();return;}
   fade=Mathf.Min(1,fade+dt/uiFadeSeconds);ui.group.alpha=fade;
   if(SaveFailed)return;
   var line=active.lines[LineIndex];lineTime+=dt;
   if(lineTime<line.delayBefore)return;
   float speed=line.charactersPerSecond>0?line.charactersPerSecond:active.defaultCharactersPerSecond;
   revealed=Mathf.Min(ui.CharacterCount,revealed+speed*dt);ui.Reveal(Mathf.FloorToInt(revealed));
  }
  public void Advance(){
   if(!IsPlaying||finishing||SaveFailed||Time.frameCount==openedFrame||lastAdvanceFrame==Time.frameCount)return;
   lastAdvanceFrame=Time.frameCount;
   if(IsTyping){revealed=ui.CharacterCount;ui.Reveal(ui.CharacterCount);lineTime=float.MaxValue;return;}
   LineIndex++;if(LineIndex>=active.lines.Count){Finish();return;}ShowLine();
  }
  public void Skip(){if(IsPlaying&&!finishing&&!SaveFailed)Finish();}
  void Finish(){
   if(opening&&autosave&&!autosave.TryCompleteOpeningDialogue()){
    SaveFailed=true;ui.Error("会話の完了を保存できませんでした。\n"+autosave.Error+"\n再試行できます。保存せずタイトルへ戻った場合は、次回この会話が再表示されます。");return;
   }
   SaveFailed=false;ui.errorPanel.SetActive(false);finishing=true;fade=1;
   ui.group.interactable=false;ui.group.alpha=1;
  }
  public void RetrySave(){if(SaveFailed)Finish();}
  public void ReturnToTitleWithoutCompletion(){
   if(!SaveFailed)return;
   SaveDataManager.Instance.Flush();GameSession.ReturningToTitle();
   SceneManager.LoadScene(GameSession.TitleScene);
  }
  void Complete(){
   ui.Hide();IsPlaying=false;UnlockGameplay();var callback=onFinished;onFinished=null;callback?.Invoke();
  }
  void OnDestroy(){UnlockGameplay();}
 }
}