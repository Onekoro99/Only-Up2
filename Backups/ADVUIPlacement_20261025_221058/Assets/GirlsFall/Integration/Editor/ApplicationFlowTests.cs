using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using OnlyUpPrototype;
using GirlsFall.Dialogue;
namespace GirlsFall.Integration.Editor {
 [InitializeOnLoad] public static class ApplicationFlowTests {
  static List<object> results=new List<object>();
  static string storage,quitProgress;static PlayerSaveState expected;static double began;
  static ApplicationFlowTests(){EditorApplication.update+=Update;}
  public static void Run(){results.Clear();SessionState.SetInt("GFIntegrationTest",1);EditorApplication.isPlaying=true;}
  static void Check(string name,bool pass){results.Add(new{name=name,pass=pass});}
  static void Write(){Directory.CreateDirectory("Library/GirlsFallIntegration");File.WriteAllText("Library/GirlsFallIntegration/Results.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));}
  static void Next(int phase){SessionState.SetInt("GFIntegrationTest",phase);began=EditorApplication.timeSinceStartup;Write();}
  static void ReadyState(DialogueManager m){m.motor.GetComponent<FallTracker>().Sample(m.motor.IsGrounded,m.motor.VerticalSpeed);}
  static bool Same(PlayerSaveState a,PlayerSaveState b){return a!=null&&b!=null&&a.position==b.position&&Quaternion.Angle(a.rotation,b.rotation)<.01f&&a.horizontalVelocity==b.horizontalVelocity&&Mathf.Abs(a.verticalVelocity-b.verticalVelocity)<.001f&&a.pv==b.pv&&a.stage==b.stage&&Math.Abs(a.playSeconds-b.playSeconds)<.001&&a.falling==b.falling&&a.airborne==b.airborne&&a.grounded==b.grounded&&a.openingDialogueCompleted==b.openingDialogueCompleted&&Mathf.Abs(a.departureHeight-b.departureHeight)<.001&&Mathf.Abs(a.peakHeight-b.peakHeight)<.001;}
  static void Advance(DialogueManager m){typeof(DialogueManager).GetField("openedFrame",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(m,-1);typeof(DialogueManager).GetField("lastAdvanceFrame",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(m,-1);m.ui.advanceButton.onClick.Invoke();}
  static void Update(){
   int phase=SessionState.GetInt("GFIntegrationTest",0);if(phase==0)return;
   if(phase==99&&!Application.isPlaying){
    results=Newtonsoft.Json.JsonConvert.DeserializeObject<List<object>>(File.ReadAllText("Library/GirlsFallIntegration/Results.json"));
    Check("Title Quit stops Play Mode",true);Check("Title Quit does not overwrite progress",File.ReadAllText(SessionState.GetString("GFIntegrationQuitPath",""))==SessionState.GetString("GFIntegrationQuitContents",""));
    Write();SessionState.SetInt("GFIntegrationTest",0);return;
   }
   if(!Application.isPlaying||Time.frameCount<1)return;
   Application.runInBackground=true;EditorApplication.QueuePlayerLoopUpdate();
   try{
    if(phase==1){
     storage=Path.GetFullPath("Library/GirlsFallIntegration/Run_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));Directory.CreateDirectory(storage);
     SaveDataManager.UseStorageForTests(storage);var settings=GameSettingsManager.Instance;settings.UseStorageForTests(storage);
     settings.Current.width=Screen.width;settings.Current.height=Screen.height;settings.Current.fullscreen=Screen.fullScreen;
     Check("Build starts at Title",EditorBuildSettings.scenes.First(s=>s.enabled).path==ApplicationSetup.TitlePath);
     Check("Canonical map in build",EditorBuildSettings.scenes.Any(s=>s.path==ApplicationSetup.GamePath&&s.enabled));
     Next(2);SceneManager.LoadScene("TitleScene");return;
    }
    if(EditorApplication.timeSinceStartup-began>120)throw new Exception("Phase timeout "+phase);
    if(phase==2){
     var title=UnityEngine.Object.FindFirstObjectByType<TitleMenuController>();if(!title)return;
     Check("Title scene and four menu buttons",SceneManager.GetActiveScene().name=="TitleScene"&&new[]{title.continueButton,title.newGameButton,title.optionsButton,title.quitButton}.All(b=>b));
     Check("No save Continue disabled",!title.continueButton.interactable);
     Check("Title cursor available",Cursor.visible&&Cursor.lockState==CursorLockMode.None);
     title.optionsButton.onClick.Invoke();Check("Title options opens",title.options.gameObject.activeSelf);
     title.options.masterSlider.value=.63f;title.options.sensitivitySlider.value=2.7f;title.options.backButton.onClick.Invoke();
     Check("Title options saves",!title.options.gameObject.activeSelf&&File.Exists(Path.Combine(storage,"girls-fall-options.json")));
     title.newGameButton.onClick.Invoke();Check("No save starts without overwrite confirmation",!title.confirmation.activeSelf&&title.IsBusy);
     Next(3);return;
    }
    var m=UnityEngine.Object.FindFirstObjectByType<DialogueManager>();
    if(phase==3){
     if(!m||!m.IsPlaying)return;var menu=UnityEngine.Object.FindFirstObjectByType<GameplayMenuController>();var orbit=UnityEngine.Object.FindFirstObjectByType<ThirdPersonOrbitCamera>();
     Check("New game loads candy map",SceneManager.GetActiveScene().name==GameSession.GameplayScene&&GameObject.Find("BoundaryWorldGenerated"));
     Check("One player/camera/save/eventsystem",UnityEngine.Object.FindObjectsByType<PrototypePlayerMotor>(FindObjectsSortMode.None).Length==1&&UnityEngine.Object.FindObjectsByType<AutoSaveController>(FindObjectsSortMode.None).Length==1&&UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length==1);
     Check("Opening controls locked",!m.motor.enabled&&!orbit.enabled&&!menu.enabled&&Time.timeScale==0);
     var l=m.ui.portraits.GetComponent<CharacterPortraitLayout>();l.ApplyLayout();Check("Portrait layout preserved",l.positionX==0&&l.positionY==0&&l.width==570&&l.bottomOffset==2&&((RectTransform)l.transform).anchorMin==Vector2.zero);
     Check("Opening data and five sprites preserved",m.openingDialogue.lines.Count==5&&m.ui.portraits.expressions.All(e=>e.sprite&&e.analyzedSprite==e.sprite));
     m.Tick(.4f);Check("Typewriter runs",m.ui.body.maxVisibleCharacters>0);
     int index=m.LineIndex;Advance(m);m.ui.advanceButton.onClick.Invoke();Check("Click reveals without double advance",m.LineIndex==index&&m.ui.body.maxVisibleCharacters==m.ui.CharacterCount);Advance(m);Check("Next dialogue works",m.LineIndex==index+1);m.Tick(3);
     GirlsFall.Dialogue.Editor.PortraitLayoutChecks.Run(m);m.ui.portraits.Show(m.openingDialogue.lines[m.LineIndex].expression);m.ui.portraits.Tick(1);
     GirlsFall.Dialogue.Editor.DialogueTests.Capture(m,"Library/GirlsFallIntegration/OpeningCandy.png",1600,900);
     m.ui.skipButton.onClick.Invoke();m.Tick(1);Check("Skip completes opening and restores controls",!m.IsPlaying&&m.motor.enabled&&orbit.enabled&&menu.enabled&&Time.timeScale==1);
     Check("Fresh progress and opening saved",SaveDataManager.Instance.TryLoad(out var fresh,out var error)&&fresh.sceneName==GameSession.GameplayScene&&fresh.pv==0&&fresh.stage==1&&fresh.openingDialogueCompleted);
     Check("New game kept options",Mathf.Abs(GameSettingsManager.Instance.Current.master-.63f)<.01&&Mathf.Abs(orbit.Sensitivity-2.7f)<.01);
     orbit.CaptureCursor();float yaw=orbit.Yaw;orbit.ProcessInput(new Vector2(1,1),.1f,false,false,false,true,false);Check("Free mouse and wheel camera remains functional",orbit.Yaw!=yaw&&orbit.TargetDistance<orbit.DefaultDistance);
     m.motor.Teleport(new Vector3(0,.12f,0),Quaternion.identity);for(int i=0;i<12;i++){m.motor.Simulate(Vector2.zero,false,.02f);ReadyState(m);}
     var start=m.motor.transform.position;for(int i=0;i<10;i++){m.motor.Simulate(Vector2.up,false,.02f);ReadyState(m);}
     Check("Movement still works",Vector3.Distance(start,m.motor.transform.position)>.4f);
     for(int i=0;i<12;i++){m.motor.Simulate(Vector2.zero,false,.02f);ReadyState(m);}
     float initialY=m.motor.transform.position.y;bool jumped=false,fell=false;
     for(int i=0;i<80;i++){m.motor.Simulate(Vector2.zero,i==0,.02f);ReadyState(m);jumped|=m.motor.transform.position.y>initialY+.5f;fell|=m.motor.VerticalSpeed<-.1f&&!m.motor.IsGrounded;}
     Check("Jump and fall still work",jumped&&fell);
     menu.HandlePauseInput(true);Check("Esc pauses movement and camera",menu.IsPaused&&menu.panel.activeSelf&&Time.timeScale==0&&!m.motor.enabled&&!orbit.enabled&&Cursor.visible);
     menu.optionsButton.onClick.Invoke();Check("Pause opens shared options UI",menu.options.gameObject.activeSelf&&menu.options.masterSlider&&menu.options.resolution&&menu.options.fullscreen);
     menu.options.sensitivitySlider.value=4.1f;menu.options.backButton.onClick.Invoke();Check("Pause options saves shared sensitivity",!menu.options.gameObject.activeSelf&&Mathf.Abs(orbit.Sensitivity-4.1f)<.01);
     menu.resumeButton.onClick.Invoke();Check("Resume restores previous controls",!menu.IsPaused&&Time.timeScale==1&&m.motor.enabled&&orbit.enabled);
     var high=m.autosave.Capture();high.stage=3;high.pv=777;high.playSeconds=123.5;high.position=new Vector3(0,7,22);high.rotation=Quaternion.Euler(0,95,0);high.horizontalVelocity=new Vector3(1,0,2);high.verticalVelocity=-4.5f;high.grounded=false;high.airborne=true;high.falling=true;high.departureHeight=15;high.peakHeight=15;high.openingDialogueCompleted=true;
     m.autosave.Restore(high);menu.HandlePauseInput(true);expected=m.autosave.Capture();menu.titleButton.onClick.Invoke();
     Check("Return starts after synchronous save",menu.IsBusy&&SaveDataManager.Instance.TryLoad(out var saved,out error)&&Same(saved,expected));Next(4);return;
    }
    if(phase==4){
     var title=UnityEngine.Object.FindFirstObjectByType<TitleMenuController>();if(!title)return;
     Check("Return displays Title with Continue",title.continueButton.interactable&&Time.timeScale==1&&Cursor.visible);
     Check("Latest fall snapshot retained on Title",SaveDataManager.Instance.TryLoad(out var state,out var error)&&Same(state,expected));
     Check("Options preserved across scenes",Mathf.Abs(GameSettingsManager.Instance.Current.mouseSensitivity-4.1f)<.01);
     title.continueButton.onClick.Invoke();Next(5);return;
    }
    if(phase==5){
     if(!m||!m.autosave.CanSave)return;var menu=UnityEngine.Object.FindFirstObjectByType<GameplayMenuController>();
     Check("Continue restores complete snapshot",Same(m.autosave.LoadedState,expected));
     Check("Continue skips completed opening",!m.IsPlaying&&m.autosave.OpeningDialogueCompleted);
     Check("Fall continues without high-position rollback",m.motor.VerticalSpeed<0&&m.motor.transform.position.y<=expected.position.y+.01f);
     Check("One persistent settings manager",UnityEngine.Object.FindObjectsByType<GameSettingsManager>(FindObjectsSortMode.None).Length==1);
     Check("New scene UI refs valid",menu.options&&menu.optionsButton&&menu.autosave==m.autosave&&m.ui.portraits&&m.motor);
     menu.Pause(true);string block=Path.Combine(storage,"blocked");File.WriteAllText(block,"not a directory");SaveDataManager.UseStorageForTests(block);
     menu.titleButton.onClick.Invoke();Check("Save failure aborts scene transition",SceneManager.GetActiveScene().name==GameSession.GameplayScene&&!menu.IsBusy&&!string.IsNullOrEmpty(menu.message.text)&&menu.IsPaused);
     SaveDataManager.UseStorageForTests(storage);menu.titleButton.onClick.Invoke();Check("Retry can return after storage recovers",menu.IsBusy);Next(6);return;
    }
    if(phase==6){
     var title=UnityEngine.Object.FindFirstObjectByType<TitleMenuController>();if(!title)return;
     title.newGameButton.onClick.Invoke();Check("Existing save asks overwrite confirmation",title.confirmation.activeSelf&&!title.IsBusy);
     title.confirmNo.onClick.Invoke();Check("Cancel keeps current progress",!title.confirmation.activeSelf&&SaveDataManager.Instance.TryLoad(out var saved,out var error)&&saved.pv==777);
     title.newGameButton.onClick.Invoke();title.confirmYes.onClick.Invoke();Next(7);return;
    }
    if(phase==7){
     if(!m||!m.IsPlaying)return;
     var fresh=m.autosave.Capture();Check("Confirmed new game resets progress",fresh.stage==1&&fresh.pv==0&&fresh.playSeconds<10&&!fresh.openingDialogueCompleted&&!fresh.falling&&m.motor.transform.position.magnitude<1);
     Check("Confirmed new game keeps settings",Mathf.Abs(GameSettingsManager.Instance.Current.mouseSensitivity-4.1f)<.01);
     for(int i=0;i<14&&m.IsPlaying;i++){Advance(m);m.Tick(.1f);}m.Tick(1);Check("Normal ADV finish resumes gameplay",!m.IsPlaying&&m.autosave.OpeningDialogueCompleted&&m.motor.enabled);
     m.autosave.SaveNow();GameSession.Ready();Next(8);SceneManager.LoadScene(GameSession.GameplayScene);return;
    }
    if(phase==8){
     if(!m||!m.autosave.CanSave)return;
     Check("Direct gameplay scene startup loads safely",m.autosave.LoadedState!=null&&!m.IsPlaying&&m.motor.enabled);
     var legacy=m.autosave.Capture();legacy.sceneName=GameSession.LegacyGameplayScene;legacy.pv=55;legacy.openingDialogueCompleted=true;
     Check("Legacy save still validates",legacy.IsValid());SaveDataManager.Instance.Save(legacy,true);GameSession.RequestContinue(legacy);Next(9);SceneManager.LoadScene(GameSession.GameplayScene);return;
    }
    if(phase==9){
     if(!m||!m.autosave.CanSave)return;
     Check("Legacy save restores in canonical map",m.autosave.LoadedState.sceneName==GameSession.LegacyGameplayScene&&m.autosave.Capture().pv==55);
     Check("Next save writes canonical scene id",m.autosave.Capture().sceneName==GameSession.GameplayScene);
     var menu=UnityEngine.Object.FindFirstObjectByType<GameplayMenuController>();menu.ReturnToTitle();Next(10);return;
    }
    if(phase==10){
     var title=UnityEngine.Object.FindFirstObjectByType<TitleMenuController>();if(!title)return;
     File.WriteAllText(SaveDataManager.Instance.SavePath,"corrupt test save");title.RefreshContinue();title.continueButton.onClick.Invoke();
     Check("Corrupt Continue displays error without transition",title.errorPanel.activeSelf&&!title.IsBusy&&SceneManager.GetActiveScene().name=="TitleScene");
     Check("Settings still has no duplicate after repeated transitions",UnityEngine.Object.FindObjectsByType<GameSettingsManager>(FindObjectsSortMode.None).Length==1);
     Check("Gameplay objects unloaded on Title",!UnityEngine.Object.FindFirstObjectByType<AutoSaveController>()&&!UnityEngine.Object.FindFirstObjectByType<PrototypePlayerMotor>());
     quitProgress=File.ReadAllText(SaveDataManager.Instance.SavePath);SessionState.SetString("GFIntegrationQuitPath",SaveDataManager.Instance.SavePath);SessionState.SetString("GFIntegrationQuitContents",quitProgress);Next(99);title.quitButton.onClick.Invoke();return;
    }
   }catch(Exception e){results.Add(new{name="Exception",pass=false,error=e.ToString()});Write();SessionState.SetInt("GFIntegrationTest",0);EditorApplication.isPlaying=false;}
  }
 }
}