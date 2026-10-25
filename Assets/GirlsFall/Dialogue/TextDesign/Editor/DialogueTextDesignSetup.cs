using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace GirlsFall.Dialogue.Editor {
 public static class DialogueTextDesignSetup {
  public const string PrefabPath="Assets/GirlsFall/Dialogue/Prefabs/DialogueCanvas.prefab";
  public static void Configure(GameObject root){
   var ui=root.GetComponent<DialogueUIController>();var frame=ui.body.transform.parent.parent as RectTransform;
   frame.anchoredPosition=new Vector2(540,30);frame.sizeDelta=new Vector2(1340,310);
   var window=ui.body.transform.parent.gameObject;
   var layout=window.GetComponent<DialogueTextLayout>();if(!layout)layout=window.AddComponent<DialogueTextLayout>();
   layout.body=ui.body;layout.speaker=ui.speaker;layout.nameLabel=ui.speaker.transform.parent as RectTransform;layout.nameBackground=layout.nameLabel.GetComponent<Image>();
   layout.ApplyLayout();CandyDialogueStyle.Apply(root);Preview(root);
  }
  public static void Preview(GameObject root){
   if(Application.isPlaying)return;
   var manager=root.GetComponent<DialogueManager>();var ui=root.GetComponent<DialogueUIController>();
   if(!manager||!ui||!manager.openingDialogue||manager.openingDialogue.lines.Count==0)return;
   var line=manager.openingDialogue.lines[0];ui.body.text=line.text;
   ui.speaker.text=string.IsNullOrWhiteSpace(line.characterName)?manager.openingDialogue.defaultCharacterName:line.characterName;
   ui.body.maxVisibleCharacters=int.MaxValue;ui.group.alpha=1;ui.body.ForceMeshUpdate(true);
  }
  [MenuItem("Tools/Girls Fall/ADV/Apply Text Design")]
  public static void Install(){
   if(Application.isPlaying)throw new Exception("Stop Play first.");
   var root=PrefabUtility.LoadPrefabContents(PrefabPath);
   try{Configure(root);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
   AssetDatabase.SaveAssets();
  }
 }
 [CustomEditor(typeof(DialogueTextLayout))]
 public sealed class DialogueTextLayoutInspector:UnityEditor.Editor {
  public override void OnInspectorGUI(){
   DrawDefaultInspector();var layout=(DialogueTextLayout)target;
   EditorGUILayout.HelpBox("本文位置はウィンドウ左上基準。Text Left/Top Paddingを加えた位置から文字を表示します。本文やmaxVisibleCharactersは上書きしません。停止中にPrefab/Sceneへ保存してください。",MessageType.Info);
   if(GUILayout.Button("配置・文字設定を更新")){layout.ApplyLayout();SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();}
   using(new EditorGUI.DisabledScope(Application.isPlaying)){
    if(GUILayout.Button("会話データの先頭をプレビュー")){
     var ui=layout.GetComponentInParent<DialogueUIController>();if(ui){Undo.RecordObjects(new UnityEngine.Object[]{ui.body,ui.speaker},"Preview dialogue text");DialogueTextDesignSetup.Preview(ui.gameObject);EditorUtility.SetDirty(ui.body);EditorUtility.SetDirty(ui.speaker);SceneView.RepaintAll();}
    }
   }
   if(layout.textWidth<=layout.textLeftPadding+layout.textRightPadding||layout.textHeight<=layout.textTopPadding+layout.textBottomPadding)EditorGUILayout.HelpBox("余白が本文Rect以上です。幅/高さを増やすか余白を減らしてください。",MessageType.Warning);
   else if(layout.body&&layout.body.isTextOverflowing)EditorGUILayout.HelpBox("本文が表示範囲を超えています。自動縮小はしません。会話を分割するか、文字サイズ・幅・高さ・余白を調整してください。",MessageType.Warning);
  }
 }
}