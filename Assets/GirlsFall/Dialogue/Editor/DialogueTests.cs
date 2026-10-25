using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using GirlsFall.Dialogue;
namespace GirlsFall.Dialogue.Editor {
 [InitializeOnLoad] public static class DialogueTests {
  static List<object> results=new List<object>();static string storage;
  static int phase;static double started;static DialogueManager manager;
  static DialogueTests(){EditorApplication.update+=Update;}
  public static void Run(){results.Clear();SessionState.SetInt("GFDialogueTest",1);EditorApplication.isPlaying=true;}
  static void Check(string name,bool value){results.Add(new{name=name,pass=value});}
  static void Private(object o,string n,object v){o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);}
  static void Advance(){Private(manager,"openedFrame",-1);Private(manager,"lastAdvanceFrame",-1);manager.ui.advanceButton.onClick.Invoke();}
  static void Write(){Directory.CreateDirectory("Library/DialogueTests");File.WriteAllText("Library/DialogueTests/Results.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));}
  static void Next(int p){SessionState.SetInt("GFDialogueTest",p);started=EditorApplication.timeSinceStartup;Write();}
  static void Update(){
   phase=SessionState.GetInt("GFDialogueTest",0);if(phase==0||!Application.isPlaying||Time.frameCount<1)return;EditorApplication.QueuePlayerLoopUpdate();
   try{
    if(phase==1){Application.runInBackground=true;
     storage=Path.GetFullPath("Library/DialogueTests/Run_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
     Directory.CreateDirectory(storage);SaveDataManager.UseStorageForTests(storage);
     var settings=GameSettingsManager.Instance;settings.UseStorageForTests(storage);
     settings.Current.width=Screen.width;settings.Current.height=Screen.height;settings.Current.fullscreen=Screen.fullScreen;
     Next(2);SceneManager.LoadScene("TitleScene");return;
    }
    if(EditorApplication.timeSinceStartup-started>90)throw new Exception("Test phase timeout "+phase);
    if(phase==2){
     var title=UnityEngine.Object.FindFirstObjectByType<TitleMenuController>();if(!title)return;
     Check("No save Continue disabled",!title.continueButton.interactable);
     title.newGameButton.onClick.Invoke();Check("New game confirmation opens",title.confirmation.activeSelf);
     title.confirmYes.onClick.Invoke();Next(3);return;
    }
    manager=UnityEngine.Object.FindFirstObjectByType<DialogueManager>();if(!manager)return;
    if(phase==3){
     if(!manager.IsPlaying)return;
     Check("New game starts opening",!manager.autosave.OpeningDialogueCompleted&&manager.ui.presentation.activeSelf);
     Check("ADV suspends controls and physics",Time.timeScale==0&&!manager.motor.enabled&&manager.gameplayControls.All(b=>!b.enabled));
     Check("ADV cursor available",Cursor.visible&&Cursor.lockState==CursorLockMode.None);
     Check("Five assigned expressions",manager.ui.portraits.expressions.Length==5&&manager.ui.portraits.expressions.All(e=>e.sprite));
     foreach(var e in manager.ui.portraits.expressions){
      var path=AssetDatabase.GetAssetPath(e.sprite);var ti=(TextureImporter)AssetImporter.GetAtPath(path);var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);
      Check("Sprite import "+e.expression,ti.textureType==TextureImporterType.Sprite&&ti.spriteImportMode==SpriteImportMode.Single&&ti.alphaIsTransparency&&settings.spriteMeshType==SpriteMeshType.FullRect);
     }
     manager.Tick(.3f);Check("Typewriter reveals partially",manager.ui.body.maxVisibleCharacters>0&&manager.ui.body.maxVisibleCharacters<manager.ui.CharacterCount);
     int index=manager.LineIndex;Advance();manager.ui.advanceButton.onClick.Invoke();
     Check("First click reveals and duplicate frame blocked",manager.LineIndex==index&&manager.ui.body.maxVisibleCharacters>=manager.ui.CharacterCount);
     Advance();Check("Second click advances one line",manager.LineIndex==index+1);
     Check("Character name uses data default",manager.ui.speaker.text=="主人公");
     var rect=manager.ui.portraits.portrait.rectTransform;var size=rect.sizeDelta;var frame=rect.parent.GetComponent<RectTransform>();var frameSize=frame.sizeDelta;var anchor=frame.anchorMin;
     foreach(var e in manager.ui.portraits.expressions){manager.ui.portraits.Show(e.expression);manager.ui.portraits.Tick(.2f);Check("Expression and stable rect "+e.expression,manager.ui.portraits.CurrentSprite==e.sprite&&frame.sizeDelta==frameSize);}
     Check("Portrait anchored lower left",anchor==Vector2.zero);
     Check("Reusable conversation data fields",typeof(DialogueData).GetField("lines")!=null&&typeof(DialogueLine).GetField("text").IsDefined(typeof(TextAreaAttribute),true));
     Check("Save incomplete before finish",SaveDataManager.Instance.TryLoad(out var before,out var err)&&!before.openingDialogueCompleted);
     var frozen=manager.autosave.Capture();manager.ui.skipButton.onClick.Invoke();manager.Tick(1);
     Check("Skip restores gameplay",!manager.IsPlaying&&Time.timeScale==1&&manager.motor.enabled&&!manager.ui.presentation.activeSelf);
     Check("Completion saved",SaveDataManager.Instance.TryLoad(out var done,out err)&&done.openingDialogueCompleted);
     Check("Completion keeps state data",done.pv==before.pv&&done.stage==before.stage&&done.position==frozen.position&&done.rotation==frozen.rotation&&done.horizontalVelocity==frozen.horizontalVelocity&&Mathf.Abs(done.verticalVelocity-frozen.verticalVelocity)<.001f);
     Capture(manager,"Library/DialogueTests/Completed.png",1600,900);
     GameSession.RequestContinue(done);Next(4);SceneManager.LoadScene("GirlsFallGameplay");return;
    }
    if(phase==4){
     if(!manager.autosave.CanSave)return;
     Check("Continue skips completed opening",!manager.IsPlaying&&!manager.AwaitingLanding&&manager.autosave.OpeningDialogueCompleted);
     // Test an existing airborne snapshot without an opening flag: it must keep falling.
     var state=manager.autosave.Capture();state.openingDialogueCompleted=false;state.position=new Vector3(0,3,0);state.grounded=false;state.airborne=true;state.falling=true;state.verticalVelocity=-4;state.pv=321;state.departureHeight=10;state.peakHeight=10;
     SaveDataManager.Instance.Save(state,true);GameSession.RequestContinue(state);Next(5);SceneManager.LoadScene("GirlsFallGameplay");return;
    }
    if(phase==5){
     if(!manager.autosave.CanSave)return;
     Check("Incomplete airborne save defers ADV",manager.AwaitingLanding&&!manager.IsPlaying&&manager.motor.enabled&&manager.motor.VerticalSpeed<0);
     Check("Old fall PV and peak retained",manager.autosave.Capture().pv==321&&manager.autosave.Capture().peakHeight==10);
     for(int i=0;i<100&&!manager.motor.IsGrounded;i++){manager.motor.Simulate(Vector2.zero,false,.02f);manager.motor.GetComponent<OnlyUpPrototype.FallTracker>().Sample(manager.motor.IsGrounded,manager.motor.VerticalSpeed);}
     typeof(DialogueManager).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,null);
     if(!manager.IsPlaying)typeof(DialogueManager).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,null);
     Check("Deferred opening begins after landing",manager.IsPlaying&&manager.motor.IsGrounded);
     manager.Tick(.8f);manager.ui.portraits.Tick(.3f);manager.ui.body.maxVisibleCharacters=9999;
     PortraitLayoutChecks.Run(manager);Capture(manager,"Library/DialogueTests/Opening16x9.png",1600,900);Capture(manager,"Library/DialogueTests/Opening4x3.png",1200,900);
     // Force an actual IO failure, then retry through the real UI button.
     string blocker=Path.Combine(storage,"blocked");File.WriteAllText(blocker,"not a directory");SaveDataManager.UseStorageForTests(blocker);
     manager.ui.skipButton.onClick.Invoke();Check("Failed save keeps controls locked and flag false",manager.SaveFailed&&manager.IsPlaying&&!manager.autosave.OpeningDialogueCompleted&&!manager.motor.enabled);
     Check("Failed save offers retry and return",manager.ui.errorPanel.activeSelf&&manager.ui.retryButton.interactable&&manager.ui.titleButton.interactable);
     SaveDataManager.UseStorageForTests(storage);manager.ui.retryButton.onClick.Invoke();manager.Tick(1);
     bool loadedRetry=SaveDataManager.Instance.TryLoad(out var saved,out var error);Check("Retry completes and preserves PV",!manager.IsPlaying&&loadedRetry&&saved.openingDialogueCompleted&&saved.pv==321);
     // A save with no bool member defaults to incomplete, retaining all other fields.
     string legacy=JsonUtility.ToJson(saved).Replace(",\"openingDialogueCompleted\":true","").Replace("\"openingDialogueCompleted\":true,","");
     var old=JsonUtility.FromJson<PlayerSaveState>(legacy);Check("Legacy save defaults false and stays valid",!old.openingDialogueCompleted&&old.IsValid()&&old.pv==321);
     GameSession.RequestNew();Next(6);SceneManager.LoadScene("GirlsFallGameplay");return;
    }
    if(phase==6){
     if(!manager.IsPlaying)return;
     Check("New game replays opening and resets progress",!manager.autosave.OpeningDialogueCompleted&&manager.autosave.Capture().pv==0);
     // Finish normally, exercising the last-line path, not Skip.
     for(int i=0;i<manager.openingDialogue.lines.Count*2+2&&manager.IsPlaying;i++){Advance();manager.Tick(.001f);}
     manager.Tick(1);
     Check("Normal last line completes",!manager.IsPlaying&&manager.autosave.OpeningDialogueCompleted);
     var clone=UnityEngine.Object.Instantiate(manager.openingDialogue);clone.lines.RemoveAt(0);clone.lines.Reverse();clone.lines.Add(new DialogueLine{characterName="別の人物",text="テスト",expression=CharacterExpression.Normal});
     Check("Data-only replacement event begins",manager.PlayEvent(clone));
     manager.Tick(.5f);Check("Replacement data reflected",manager.ui.body.text==clone.lines[0].text);
     manager.Skip();manager.Tick(1);Check("Reusable event finishes",!manager.IsPlaying&&manager.autosave.OpeningDialogueCompleted);
     UnityEngine.Object.DestroyImmediate(clone);Write();SessionState.SetInt("GFDialogueTest",0);EditorApplication.isPlaying=false;return;
    }
   }catch(Exception ex){results.Add(new{name="Exception",pass=false,error=ex.ToString()});Write();SessionState.SetInt("GFDialogueTest",0);EditorApplication.isPlaying=false;}
  }
  public static void Capture(DialogueManager m,string path,int width,int height){
   Directory.CreateDirectory(Path.GetDirectoryName(path));var canvas=m.ui.GetComponent<Canvas>();var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.gameObject.scene==m.gameObject.scene);
   var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;
   var rt=new RenderTexture(width,height,24);var oldActive=RenderTexture.active;camera.targetTexture=rt;Canvas.ForceUpdateCanvases();
   // Force the same responsive design calculation at this render resolution.
   var layout=m.ui.presentation.GetComponent<ResponsiveTitleLayout>();var cr=m.ui.presentation.GetComponent<RectTransform>();layout.design.localScale=Vector3.one*Mathf.Min(cr.rect.width/1920,cr.rect.height/1080);
   m.ui.portraits.GetComponent<CharacterPortraitLayout>()?.ApplyLayout();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
   var tex=new Texture2D(width,height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
   camera.targetTexture=null;RenderTexture.active=oldActive;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;Canvas.ForceUpdateCanvases();
  }
 }
}