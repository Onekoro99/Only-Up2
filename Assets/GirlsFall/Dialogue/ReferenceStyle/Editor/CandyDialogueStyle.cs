using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
namespace GirlsFall.Dialogue.Editor {
 public static class CandyDialogueStyle {
  static CandyDialogueDecoration Decor(string name,Transform parent,CandyDecorationKind kind){
   var child=parent.Find(name);if(!child){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);child=go.transform;}
   var r=(RectTransform)child;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=Vector2.zero;
   if(!child.GetComponent<CanvasRenderer>())child.gameObject.AddComponent<CanvasRenderer>();
   var art=child.GetComponent<CandyDialogueDecoration>();if(!art)art=child.gameObject.AddComponent<CandyDialogueDecoration>();art.kind=kind;art.raycastTarget=false;art.strength=.85f;return art;
  }
  public static void Apply(GameObject root){
   var ui=root.GetComponent<DialogueUIController>();var layout=root.GetComponentInChildren<DialogueTextLayout>(true);
   var card=ui.body.transform.parent.GetComponent<Image>();var border=card.transform.parent.GetComponent<Image>();
   var presentation=ui.presentation.transform;var portrait=ui.portraits.transform;
   border.transform.SetParent(presentation,false);border.transform.SetAsLastSibling();border.transform.SetSiblingIndex(portrait.GetSiblingIndex());
   layout.windowFrame=border.rectTransform;layout.windowFill=card;layout.windowBorder=border;
   layout.windowPositionX=500;layout.windowPositionY=30;layout.windowWidth=1380;layout.windowHeight=320;layout.cornerDensity=.4f;
   layout.windowColor=new Color(1,.975f,.99f,.98f);layout.borderColor=new Color(1,.68f,.83f,1);
   card.rectTransform.sizeDelta=new Vector2(-16,-16);card.raycastTarget=false;border.raycastTarget=false;
   var mask=card.GetComponent<Mask>();if(!mask)mask=card.gameObject.AddComponent<Mask>();mask.showMaskGraphic=true;
   layout.fontSize=38;layout.textPositionX=layout.textPositionY=0;layout.textWidth=1364;layout.textHeight=304;
   layout.textLeftPadding=102;layout.textRightPadding=200;layout.textTopPadding=88;layout.textBottomPadding=56;layout.lineSpacing=10;
   layout.namePositionX=76;layout.namePositionY=-10;layout.nameFontSize=32;layout.nameLabelWidth=250;layout.nameLabelHeight=52;
   layout.textColor=new Color32(56,56,56,255);layout.nameTextColor=Color.white;layout.nameBackgroundColor=new Color32(255,112,165,255);layout.ApplyLayout();
   layout.nameBackground.pixelsPerUnitMultiplier=.65f;
   var art=Decor("CandyWindowArtwork",card.transform,CandyDecorationKind.Window);art.transform.SetAsFirstSibling();
   Decor("NameFlower",layout.nameLabel,CandyDecorationKind.NameFlower);
   var help=card.transform.Find("Help").GetComponent<TMP_Text>();var hr=help.rectTransform;hr.anchorMin=hr.anchorMax=hr.pivot=Vector2.zero;hr.anchoredPosition=new Vector2(142,14);hr.sizeDelta=new Vector2(600,34);help.fontSize=24;help.color=new Color32(56,56,56,255);help.raycastTarget=false;
   var mouse=Decor("MouseIcon",card.transform,CandyDecorationKind.Mouse);var mr=mouse.rectTransform;mr.anchorMin=mr.anchorMax=mr.pivot=Vector2.zero;mr.anchoredPosition=new Vector2(102,16);mr.sizeDelta=new Vector2(24,36);
   ui.nextIcon.color=new Color32(255,94,148,255);ui.nextIcon.fontSize=40;ui.nextIcon.rectTransform.anchoredPosition=new Vector2(-27,15);ui.nextIcon.rectTransform.sizeDelta=new Vector2(50,46);
   var skip=ui.skipButton;var sr=(RectTransform)skip.transform;sr.sizeDelta=new Vector2(240,64);sr.anchoredPosition=new Vector2(-40,-28);
   var si=skip.GetComponent<Image>();si.color=new Color(1,.94f,.98f,.98f);si.pixelsPerUnitMultiplier=.4f;
   var outline=skip.GetComponent<Outline>();if(!outline)outline=skip.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1,.67f,.83f,.9f);outline.effectDistance=new Vector2(2,2);
   var label=skip.GetComponentInChildren<TMP_Text>(true);label.fontSize=30;label.color=new Color32(232,100,159,255);label.margin=new Vector4(0,0,52,0);
   var arrow=Decor("SkipArrows",skip.transform,CandyDecorationKind.Forward);var ar=arrow.rectTransform;ar.anchorMin=ar.anchorMax=ar.pivot=new Vector2(1,.5f);ar.anchoredPosition=new Vector2(-18,0);ar.sizeDelta=new Vector2(48,30);
   var shadow=border.GetComponent<Shadow>();if(shadow){shadow.effectColor=new Color(.52f,.25f,.42f,.12f);shadow.effectDistance=new Vector2(0,-4);}
  }
  public static void Install(){
   var p=DialogueTextDesignSetup.PrefabPath;var root=PrefabUtility.LoadPrefabContents(p);
   try{Apply(root);DialogueTextDesignSetup.Preview(root);PrefabUtility.SaveAsPrefabAsset(root,p);}finally{PrefabUtility.UnloadPrefabContents(root);}
   AssetDatabase.SaveAssets();
  }
 }
}