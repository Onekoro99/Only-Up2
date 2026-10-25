using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace GirlsFall.Dialogue.Editor {
 public static class PortraitLayoutChecks {
  static List<object> results;
  static void Check(string name,bool value){results.Add(new{name=name,pass=value});}
  public static void Run(DialogueManager manager){
   results=new List<object>();var pc=manager.ui.portraits;var layout=pc.GetComponent<CharacterPortraitLayout>();var frame=(RectTransform)pc.transform;float savedBottom=layout.bottomOffset;
   var canvas=manager.ui.GetComponent<Canvas>();var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.gameObject.scene==manager.gameObject.scene);
   var savedMode=canvas.renderMode;var savedCamera=canvas.worldCamera;
   canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;
   foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1280,720),new Vector2Int(1200,900)}){
    var rt=new RenderTexture(resolution.x,resolution.y,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();layout.ApplyLayout();Canvas.ForceUpdateCanvases();
    var cameraRect=canvas.GetComponent<RectTransform>();var design=manager.ui.presentation.GetComponent<ResponsiveTitleLayout>();design.design.localScale=Vector3.one*Mathf.Min(cameraRect.rect.width/1920,cameraRect.rect.height/1080);
    var commonSize=frame.sizeDelta;
    foreach(var entry in pc.expressions){
     pc.Show(entry.expression);pc.Tick(1);layout.ApplyLayout();Canvas.ForceUpdateCanvases();
     var image=pc.portrait.rectTransform;float bottom=image.anchoredPosition.y+entry.alphaBounds.y*image.rect.height;
     float left=image.anchoredPosition.x+entry.alphaBounds.x*image.rect.width;
     var screenBottom=RectTransformUtility.WorldToScreenPoint(camera,image.TransformPoint(new Vector3(entry.alphaBounds.x*image.rect.width,entry.alphaBounds.y*image.rect.height,0)));
     Check(resolution+" "+entry.expression+" bottom/left",Mathf.Abs(bottom+layout.bottomOffset)<.05f&&Mathf.Abs(left)<.05f&&screenBottom.y<=.5f&&screenBottom.y>-6&&Mathf.Abs(screenBottom.x)<.5f);
     Check(resolution+" "+entry.expression+" common frame and aspect",frame.sizeDelta==commonSize&&Mathf.Abs(image.rect.width/image.rect.height-entry.sprite.rect.width/entry.sprite.rect.height)<.001f);
     if(resolution.x==1600||entry.expression==CharacterExpression.Surprised){
      var oldActive=RenderTexture.active;camera.Render();RenderTexture.active=rt;
      var tex=new Texture2D(resolution.x,resolution.y,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,resolution.x,resolution.y),0,0);tex.Apply();
      Directory.CreateDirectory("Library/PortraitLayout");File.WriteAllBytes("Library/PortraitLayout/"+resolution.x+"x"+resolution.y+"_"+entry.expression+".png",tex.EncodeToPNG());
      UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=oldActive;
     }
    }
    camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);
   }
   canvas.renderMode=savedMode;canvas.worldCamera=savedCamera;Canvas.ForceUpdateCanvases();
   pc.Show(CharacterExpression.Normal);pc.Tick(1);layout.ApplyLayout();var initial=frame.anchoredPosition;float initialScale=frame.localScale.x;float initialWidth=pc.portrait.rectTransform.rect.width;
   layout.positionX=24;layout.positionY=-10;layout.ApplyLayout();Check("Common position controls",frame.anchoredPosition.x>initial.x&&frame.anchoredPosition.y<initial.y);
   layout.positionX=layout.positionY=0;layout.scale=1.2f;layout.ApplyLayout();Check("Common Scale",frame.localScale.x>initialScale*1.19f);
   layout.scale=1;layout.width=640;layout.ApplyLayout();Check("Width changes displayed size",pc.portrait.rectTransform.rect.width>initialWidth);
   layout.width=570;layout.bottomOffset=8;layout.ApplyLayout();var normal=pc.Find(CharacterExpression.Normal);Check("Bottom Offset",Mathf.Abs(pc.portrait.rectTransform.anchoredPosition.y+normal.alphaBounds.y*pc.portrait.rectTransform.rect.height+8)<.01f);
   layout.bottomOffset=savedBottom;normal.offset=new Vector2(12,-5);normal.scale=1.1f;layout.ApplyLayout();Check("Expression offset and scale",Mathf.Abs(pc.portrait.rectTransform.anchoredPosition.x+normal.alphaBounds.x*pc.portrait.rectTransform.rect.width-12)<.01f&&pc.portrait.rectTransform.rect.width>570);
   normal.offset=Vector2.zero;normal.scale=1;layout.ApplyLayout();
   pc.Show(CharacterExpression.Angry);pc.Tick(.04f);Check("Expression fade interpolates",pc.portrait.color.a>0&&pc.portrait.color.a<1&&pc.previousPortrait.color.a>0);
   pc.Tick(1);pc.Show(CharacterExpression.Surprised);pc.Tick(1);layout.ApplyLayout();
   Check("Portrait draws above dialogue and below errors",frame.GetSiblingIndex()>manager.ui.presentation.transform.Find("ResponsiveDesign").GetSiblingIndex()&&frame.GetSiblingIndex()<manager.ui.errorPanel.transform.GetSiblingIndex());
   Directory.CreateDirectory("Library/PortraitLayout");File.WriteAllText("Library/PortraitLayout/Results.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
   File.WriteAllText("Library/PortraitLayout/AlphaBounds.json",Newtonsoft.Json.JsonConvert.SerializeObject(pc.expressions.Select(e=>new{expression=e.expression.ToString(),sprite=AssetDatabase.GetAssetPath(e.sprite),bounds=new[]{e.alphaBounds.x,e.alphaBounds.y,e.alphaBounds.width,e.alphaBounds.height},offset=new[]{e.offset.x,e.offset.y},scale=e.scale}),Newtonsoft.Json.Formatting.Indented));
  }
 }
}