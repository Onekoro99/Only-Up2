using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace GirlsFall.Dialogue.Editor {
 public static class PortraitLayoutSetup {
  public const string PrefabPath="Assets/GirlsFall/Dialogue/Prefabs/DialogueCanvas.prefab";
  public static Rect Analyze(Sprite sprite){
   if(!sprite)throw new ArgumentNullException("sprite");
   var path=AssetDatabase.GetAssetPath(sprite);var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
   try{
    if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path),false))throw new Exception("Cannot decode "+path);
    var pixels=texture.GetPixels32();int width=texture.width,height=texture.height;
    var seen=new bool[pixels.Length];var queue=new int[pixels.Length];
    int largest=0,bestX=0,bestY=0,bestRight=width-1,bestTop=height-1;
    for(int start=0;start<pixels.Length;start++){
     if(seen[start]||pixels[start].a<32)continue;
     int read=0,write=1,minX=width,minY=height,maxX=0,maxY=0;queue[0]=start;seen[start]=true;
     while(read<write){
      int n=queue[read++],x=n%width,y=n/width;minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);
      if(x>0)Add(n-1,pixels,seen,queue,ref write);
      if(x+1<width)Add(n+1,pixels,seen,queue,ref write);
      if(y>0)Add(n-width,pixels,seen,queue,ref write);
      if(y+1<height)Add(n+width,pixels,seen,queue,ref write);
     }
     if(write>largest){largest=write;bestX=minX;bestY=minY;bestRight=maxX;bestTop=maxY;}
    }
    if(largest<100)throw new Exception("Portrait main opaque component was not found: "+path);
    return new Rect((float)bestX/width,(float)bestY/height,(float)(bestRight-bestX+1)/width,(float)(bestTop-bestY+1)/height);
   }finally{UnityEngine.Object.DestroyImmediate(texture);}
  }
  static void Add(int n,Color32[] pixels,bool[] seen,int[] queue,ref int write){if(!seen[n]&&pixels[n].a>=32){seen[n]=true;queue[write++]=n;}}
  public static void AnalyzeAll(CharacterPortraitController controller){
   Undo.RecordObject(controller,"Analyze portrait alpha");
   foreach(var entry in controller.expressions){
    if(!entry.sprite)continue;entry.alphaBounds=Analyze(entry.sprite);entry.analyzedSprite=entry.sprite;
   }
   EditorUtility.SetDirty(controller);var layout=controller.GetComponent<CharacterPortraitLayout>();if(layout)layout.ApplyLayout();
  }
  public static void Configure(GameObject root){
   var controller=root.GetComponentInChildren<CharacterPortraitController>(true);
   var frame=controller.transform as RectTransform;var presentation=root.GetComponent<DialogueUIController>().presentation.transform;
   frame.SetParent(presentation,false);
   var error=root.GetComponent<DialogueUIController>().errorPanel.transform;frame.SetSiblingIndex(error.GetSiblingIndex());
   var mask=frame.GetComponent<RectMask2D>();if(mask)UnityEngine.Object.DestroyImmediate(mask);
   var layout=frame.GetComponent<CharacterPortraitLayout>();if(!layout)layout=frame.gameObject.AddComponent<CharacterPortraitLayout>();
   layout.positionX=layout.positionY=0;layout.width=570;layout.scale=1;layout.bottomOffset=0;layout.fitToScreen=true;layout.keepHeadOnScreen=true;layout.headTopMargin=8;
   AnalyzeAll(controller);controller.previewExpression=CharacterExpression.Normal;layout.ApplyLayout();
   root.GetComponent<DialogueUIController>().group.alpha=1;
  }
  [MenuItem("Tools/Girls Fall/ADV/Install Portrait Layout")]
  public static void Install(){
   var root=PrefabUtility.LoadPrefabContents(PrefabPath);
   try{Configure(root);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
   AssetDatabase.SaveAssets();
  }
 }
 [CustomEditor(typeof(CharacterPortraitLayout))]
 public sealed class CharacterPortraitLayoutInspector:UnityEditor.Editor{
  public override void OnInspectorGUI(){
   DrawDefaultInspector();
   var layout=(CharacterPortraitLayout)target;var controller=layout.GetComponent<CharacterPortraitController>();
   EditorGUI.BeginChangeCheck();float fade=EditorGUILayout.FloatField(new GUIContent("Fade Duration","表情が切り替わるフェード時間（秒）です。大きくするとゆっくり、小さくすると素早く切り替わります。初期値は0.16秒です。 CharacterPortraitControllerの値と連動します。"),controller.fadeDuration);
   if(EditorGUI.EndChangeCheck()){Undo.RecordObject(controller,"Change portrait fade");controller.fadeDuration=Mathf.Max(.01f,fade);EditorUtility.SetDirty(controller);PrefabUtility.RecordPrefabInstancePropertyModifications(controller);}
   EditorGUILayout.HelpBox("位置・幅は1920×1080基準。Bottom Offsetは正で下へ補正します。画像差し替え後はアルファを再解析してください。PNG本体やRead/Write設定は変更しません。",MessageType.Info);
   if(GUILayout.Button("指定Spriteのアルファ余白を再解析"))PortraitLayoutSetup.AnalyzeAll(controller);
   if(GUILayout.Button("Scene / Gameビューの配置を更新")){layout.ApplyLayout();SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();}
  }
 }
}