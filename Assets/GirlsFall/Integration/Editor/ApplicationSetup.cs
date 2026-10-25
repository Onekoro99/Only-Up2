using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using OnlyUpPrototype;
using GirlsFall.Dialogue;
namespace GirlsFall.Integration.Editor {
 public static class ApplicationSetup {
  public const string TitlePath="Assets/GirlsFall/Scenes/TitleScene.unity";
  public const string GamePath="Assets/CandyKingdom/Scenes/CandyTown_EnclosedWorld.unity";
  public const string LegacyPath="Assets/GirlsFall/Scenes/GirlsFallGameplay.unity";
  static T Find<T>(Scene scene) where T:Component{return scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).FirstOrDefault();}
  public static Behaviour[] Controls(Scene scene){return scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(b=>b&&(b is PrototypePlayerMotor||b is ThirdPersonOrbitCamera||b is FallTracker||b is StageRespawn||b.GetType().Name=="CandyMotionDriver"||b.GetType().Name=="CharacterMovement")).Cast<Behaviour>().ToArray();}
  static void Label(Button b,string text){var label=b.GetComponentInChildren<TMP_Text>(true);label.text=text;label.enableAutoSizing=true;label.fontSizeMin=20;label.fontSizeMax=label.fontSize;}
  static void ConfigureMenu(GameplayMenuController menu,Scene scene,OptionsMenuController template){
   menu.autosave=Find<AutoSaveController>(scene);menu.gameplayControls=Controls(scene);
   if(!menu.optionsButton){menu.optionsButton=menu.quitButton;menu.quitButton=null;menu.optionsButton.name="OptionsButton";}
   Label(menu.optionsButton,"オプション");Label(menu.titleButton,"セーブしてタイトルへ戻る");Label(menu.resumeButton,"ゲームに戻る");
   if(!menu.options){
    var go=UnityEngine.Object.Instantiate(template.gameObject,menu.transform,false);go.name="GameplayOptions";menu.options=go.GetComponent<OptionsMenuController>();
    var overlay=go.GetComponent<Image>();if(!overlay){overlay=go.AddComponent<Image>();overlay.color=Color.clear;}overlay.raycastTarget=true;
   }
   menu.options.gameObject.SetActive(false);EditorUtility.SetDirty(menu);
  }
  public static void Integrate(){
   if(Application.isPlaying)throw new Exception("Stop play before integration.");
   var title=EditorSceneManager.OpenScene(TitlePath,OpenSceneMode.Additive);
   var legacy=EditorSceneManager.OpenScene(LegacyPath,OpenSceneMode.Additive);
   var game=EditorSceneManager.OpenScene(GamePath,OpenSceneMode.Additive);
   var titleController=Find<TitleMenuController>(title);
   var auto=Find<AutoSaveController>(game);
   if(!auto)auto=Find<PrototypePlayerMotor>(game).gameObject.AddComponent<AutoSaveController>();
   var goal=game.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PrototypeGoal>(true)).First(g=>g.isActiveAndEnabled);
   var so=new SerializedObject(auto);so.FindProperty("goal").objectReferenceValue=goal;so.ApplyModifiedPropertiesWithoutUndo();
   var legacyMenu=Find<GameplayMenuController>(legacy);ConfigureMenu(legacyMenu,legacy,titleController.options);
   var menu=Find<GameplayMenuController>(game);
   if(!menu){
    var clone=UnityEngine.Object.Instantiate(legacyMenu.gameObject);SceneManager.MoveGameObjectToScene(clone,game);clone.name="GirlsFallPauseCanvas";menu=clone.GetComponent<GameplayMenuController>();
   }
   ConfigureMenu(menu,game,titleController.options);
   if(!Find<EventSystem>(game)){var events=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));SceneManager.MoveGameObjectToScene(events,game);}
   var dialogue=Find<DialogueManager>(game);
   if(!dialogue){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GirlsFall/Dialogue/Prefabs/DialogueCanvas.prefab");dialogue=((GameObject)PrefabUtility.InstantiatePrefab(prefab,game)).GetComponent<DialogueManager>();}
   dialogue.autosave=auto;dialogue.motor=auto.GetComponent<PrototypePlayerMotor>();dialogue.gameplayControls=Controls(game).Concat(new Behaviour[]{menu}).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(dialogue);
   var oldDialogue=Find<DialogueManager>(legacy);oldDialogue.gameplayControls=Controls(legacy).Concat(new Behaviour[]{legacyMenu}).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(oldDialogue);
   foreach(var scene in new[]{legacy,game}){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
   var list=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(TitlePath,true),new EditorBuildSettingsScene(GamePath,true)};
   list.AddRange(EditorBuildSettings.scenes.Where(s=>s.path!=TitlePath&&s.path!=GamePath));EditorBuildSettings.scenes=list.ToArray();
   EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(TitlePath);
   foreach(var scene in new[]{game,legacy,title})EditorSceneManager.CloseScene(scene,true);
   AssetDatabase.SaveAssets();
  }
 }
 [InitializeOnLoad] public static class ApplicationLaunchTools {
  static ApplicationLaunchTools(){EditorApplication.playModeStateChanged+=Restore;}
  [MenuItem("Tools/Girls Fall/Play Full Game From Title")]
  public static void FullGame(){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ApplicationSetup.TitlePath);EditorApplication.isPlaying=true;}
  [MenuItem("Tools/Girls Fall/Play Current Scene Only")]
  public static void Current(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)return;
   SessionState.SetString("GirlsFallPlayOverride",EditorSceneManager.playModeStartScene?AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene):"");
   SessionState.SetBool("GirlsFallRestorePlayOverride",true);EditorSceneManager.playModeStartScene=null;EditorApplication.isPlaying=true;
  }
  static void Restore(PlayModeStateChange state){
   if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("GirlsFallRestorePlayOverride",false))return;
   var path=SessionState.GetString("GirlsFallPlayOverride","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);SessionState.SetBool("GirlsFallRestorePlayOverride",false);
  }
 }
}