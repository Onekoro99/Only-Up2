using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using TMPro;
namespace GirlsFall.Dialogue.Editor {
 [InitializeOnLoad] public static class DialogueTextDesignTests {
  static List<object> results=new List<object>();static double began;
  static DialogueTextDesignTests(){EditorApplication.update+=Update;}
  public static void Run(){results.Clear();SessionState.SetInt("GFTextDesignTest",1);EditorApplication.isPlaying=true;}
  static void Check(string name,bool pass){results.Add(new{name=name,pass=pass});}
  static void Write(){Directory.CreateDirectory("Library/DialogueTextDesign");File.WriteAllText("Library/DialogueTextDesign/Results.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));}
  static void Update(){
   int phase=SessionState.GetInt("GFTextDesignTest",0);if(phase==0||!Application.isPlaying||Time.frameCount<1)return;
   Application.runInBackground=true;EditorApplication.QueuePlayerLoopUpdate();
   try{
    if(phase==1){
     string storage=Path.GetFullPath("Library/DialogueTextDesign/Storage_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));Directory.CreateDirectory(storage);
     SaveDataManager.UseStorageForTests(storage);var settings=GameSettingsManager.Instance;settings.UseStorageForTests(storage);settings.Current.width=Screen.width;settings.Current.height=Screen.height;settings.Current.fullscreen=Screen.fullScreen;
     GameSession.RequestNew();SessionState.SetInt("GFTextDesignTest",2);began=EditorApplication.timeSinceStartup;SceneManager.LoadScene(GameSession.GameplayScene);return;
    }
    if(EditorApplication.timeSinceStartup-began>120)throw new Exception("Text test scene timeout");
    var m=UnityEngine.Object.FindFirstObjectByType<DialogueManager>();if(!m||!m.IsPlaying)return;
    m.Tick(1);var ui=m.ui;var layout=ui.GetComponentInChildren<DialogueTextLayout>(true);var body=ui.body;var canvas=ui.GetComponent<Canvas>();
    Check("Existing Japanese font",AssetDatabase.GetAssetPath(body.font)=="Assets/GirlsFall/Fonts/GirlsFallJapaneseSDF.asset");
    Check("Initial typography",body.fontSize==38&&body.lineSpacing==10&&body.characterSpacing==0&&body.fontStyle==FontStyles.Normal&&!body.enableAutoSizing&&body.alignment==TextAlignmentOptions.TopLeft&&body.textWrappingMode==TextWrappingModes.Normal);
    Check("Initial body and name colors",ColorUtility.ToHtmlStringRGBA(body.color)=="383838FF"&&ColorUtility.ToHtmlStringRGBA(ui.speaker.color)=="FFFFFFFF"&&ColorUtility.ToHtmlStringRGBA(layout.nameBackground.color)=="FF70A5FF");
    var design=ui.presentation.transform.Find("ResponsiveDesign") as RectTransform;
    var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.gameObject.scene==m.gameObject.scene);
    var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;
    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1280,720),new Vector2Int(1200,900),new Vector2Int(1920,900)}){
     var rt=new RenderTexture(size.x,size.y,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();
     var parent=ui.presentation.GetComponent<RectTransform>();design.localScale=Vector3.one*Mathf.Min(parent.rect.width/1920,parent.rect.height/1080);
     var portraitLayout=ui.portraits.GetComponent<CharacterPortraitLayout>();portraitLayout.ApplyLayout();layout.ApplyLayout();Canvas.ForceUpdateCanvases();
     foreach(var line in m.openingDialogue.lines){
      ui.SetLine(m.openingDialogue.defaultCharacterName,line.text,line.expression);ui.Reveal(int.MaxValue);ui.portraits.Tick(1);layout.ApplyLayout();Canvas.ForceUpdateCanvases();body.ForceMeshUpdate(true);
      ui.ApplySkipLayout();Canvas.ForceUpdateCanvases();
      var skipCorners=new Vector3[4];((RectTransform)ui.skipButton.transform).GetWorldCorners(skipCorners);var skipTopRight=RectTransformUtility.WorldToScreenPoint(camera,skipCorners[2]);float factor=Mathf.Min(size.x/1920f,size.y/1080f);
      Check(size+" "+line.expression+" Skip fixed to screen top right",Mathf.Abs(size.x-skipTopRight.x-20*factor)<.6f&&Mathf.Abs(size.y-skipTopRight.y-20*factor)<.6f&&ui.skipButton.transform.parent==ui.presentation.transform);
      var entry=ui.portraits.Find(line.expression);var imageRect=ui.portraits.portrait.rectTransform;var opaqueBottom=RectTransformUtility.WorldToScreenPoint(camera,imageRect.TransformPoint(new Vector3(entry.alphaBounds.x*imageRect.rect.width,entry.alphaBounds.y*imageRect.rect.height,0)));var opaqueTop=RectTransformUtility.WorldToScreenPoint(camera,imageRect.TransformPoint(new Vector3(entry.alphaBounds.x*imageRect.rect.width,entry.alphaBounds.yMax*imageRect.rect.height,0)));
      Check(size+" "+line.expression+" Portrait source pixels fit vertically",opaqueTop.y<=size.y-portraitLayout.headTopMargin*factor+.6f&&Mathf.Abs(opaqueBottom.y)<.6f);
      Check(size+" "+line.expression+" Original portrait size preserved",portraitLayout.width==570&&portraitLayout.scale==1&&Mathf.Abs(portraitLayout.transform.localScale.x/factor*canvas.scaleFactor-1)<.02f);
      Check(size+" "+line.expression+" no overflow",!body.isTextOverflowing);
      var visible=body.textInfo.characterInfo.Take(body.textInfo.characterCount).Where(c=>c.isVisible).ToArray();var rect=body.rectTransform.rect;
      Check(size+" "+line.expression+" ink inside text bounds",visible.All(c=>c.bottomLeft.x>=rect.xMin+body.margin.x-.5f&&c.topRight.x<=rect.xMax-body.margin.z+.5f&&c.topRight.y<=rect.yMax-body.margin.y+.5f&&c.bottomLeft.y>=rect.yMin+body.margin.w-.5f));
      var point=body.rectTransform.TransformPoint(new Vector3(body.margin.x,-body.margin.y,0));float designX=RectTransformUtility.WorldToScreenPoint(camera,point).x/Mathf.Min(size.x/1920f,size.y/1080f);
      Check(size+" "+line.expression+" text moved left",Mathf.Abs(designX-610)<.2f&&designX<676);
      var screen=RectTransformUtility.WorldToScreenPoint(camera,point);var portraitRight=RectTransformUtility.WorldToScreenPoint(camera,portraitLayout.transform.TransformPoint(new Vector3(portraitLayout.width,0,0)));
      Check(size+" "+line.expression+" text clear of portrait",screen.x>portraitRight.x+1);
      if(line==m.openingDialogue.lines[0]){
       var oldActive=RenderTexture.active;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,size.x,size.y),0,0);tex.Apply();
       Directory.CreateDirectory("Library/DialogueTextDesign");File.WriteAllBytes("Library/DialogueTextDesign/"+size.x+"x"+size.y+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=oldActive;
      }
     }
     camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);
    }
    canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;Canvas.ForceUpdateCanvases();ui.portraits.GetComponent<CharacterPortraitLayout>().ApplyLayout();
    string saved=JsonUtility.ToJson(layout),unchangedText=body.text;ui.Reveal(5);int count=ui.CharacterCount;
    layout.textPositionX=12;layout.textPositionY=-9;layout.ApplyLayout();Check("Text position controls",body.rectTransform.anchoredPosition==new Vector2(12,-9));
    layout.textWidth=1200;layout.textHeight=290;layout.ApplyLayout();Check("Text width/height controls",body.rectTransform.sizeDelta==new Vector2(1200,290));
    layout.fontSize=42;layout.lineSpacing=14;layout.characterSpacing=1.5f;layout.textColor=Color.blue;layout.ApplyLayout();Check("Typography controls",body.fontSize==42&&body.lineSpacing==14&&body.characterSpacing==1.5f&&body.color==Color.blue);
    layout.textLeftPadding=80;layout.textRightPadding=70;layout.textTopPadding=90;layout.textBottomPadding=40;layout.ApplyLayout();Check("Four padding controls",body.margin==new Vector4(80,90,70,40));
    layout.namePositionX=45;layout.namePositionY=-20;layout.nameLabelWidth=280;layout.nameLabelHeight=56;layout.nameFontSize=30;layout.nameTextColor=Color.yellow;layout.nameBackgroundColor=Color.cyan;layout.ApplyLayout();
    Check("Name position/size controls",layout.nameLabel.anchoredPosition==new Vector2(45,-20)&&layout.nameLabel.sizeDelta==new Vector2(280,56)&&ui.speaker.fontSize==30);
    Check("Name color controls",ui.speaker.color==Color.yellow&&layout.nameBackground.color==Color.cyan);
    Check("Applying layout preserves text and typewriter state",body.text==unchangedText&&body.maxVisibleCharacters==5&&ui.CharacterCount==count);
    JsonUtility.FromJsonOverwrite(saved,layout);layout.ApplyLayout();
    var first=m.openingDialogue.lines[0];ui.SetLine(m.openingDialogue.defaultCharacterName,first.text,first.expression);ui.Reveal(2);body.ForceMeshUpdate(true);
    var lines=body.textInfo.characterInfo.Take(body.textInfo.characterCount).Select(c=>c.lineNumber).ToArray();var position=body.rectTransform.anchoredPosition;
    ui.Reveal(int.MaxValue);body.ForceMeshUpdate(true);
    Check("Typewriter preserves wrapping and position",lines.SequenceEqual(body.textInfo.characterInfo.Take(body.textInfo.characterCount).Select(c=>c.lineNumber))&&body.rectTransform.anchoredPosition==position);
    var source=AssetDatabase.LoadAssetAtPath<GameObject>(DialogueTextDesignSetup.PrefabPath).GetComponentInChildren<DialogueTextLayout>(true);Check("Settings persist in prefab",source.fontSize==38&&source.textLeftPadding==102&&source.namePositionX==76&&source.body.fontSize==38);
    ui.SetLine(m.openingDialogue.defaultCharacterName,string.Join("\n",Enumerable.Repeat(first.text,8)),first.expression);ui.Reveal(int.MaxValue);body.ForceMeshUpdate(true);Check("Long text clipped without automatic shrinking",body.isTextOverflowing&&body.overflowMode==TextOverflowModes.Masking&&!body.enableAutoSizing&&body.fontSize==38);
    ui.SetLine(m.openingDialogue.defaultCharacterName,first.text,first.expression);m.ui.skipButton.onClick.Invoke();m.Tick(1);
    Check("Existing skip and gameplay restore",!m.IsPlaying&&m.motor.enabled&&Time.timeScale==1);
    Check("Completion persists with updated UI",SaveDataManager.Instance.TryLoad(out var state,out var error)&&state.openingDialogueCompleted);
    Write();SessionState.SetInt("GFTextDesignTest",0);EditorApplication.isPlaying=false;
   }catch(Exception ex){results.Add(new{name="Exception",pass=false,error=ex.ToString()});Write();SessionState.SetInt("GFTextDesignTest",0);EditorApplication.isPlaying=false;}
  }
 }
}