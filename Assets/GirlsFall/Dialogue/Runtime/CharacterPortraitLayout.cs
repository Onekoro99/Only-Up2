using UnityEngine;
using UnityEngine.UI;
namespace GirlsFall.Dialogue {
 [ExecuteAlways,RequireComponent(typeof(CharacterPortraitController))]
 public sealed class CharacterPortraitLayout:MonoBehaviour {
   [Header("立ち絵の位置とサイズ")]
   [Tooltip("立ち絵の共通の左右位置（1920×1080基準のUI座標）です。大きくすると右、小さくすると左へ移動します。0で左端に合わせます。")]
   public float positionX;
   [Tooltip("立ち絵の共通の上下位置（1920×1080基準のUI座標）です。大きくすると上、小さくすると下へ移動します。0が下端基準で、正の値では下に隙間ができます。")]
   public float positionY;
  [Min(.1f)]
  [Tooltip("立ち絵全体の表示倍率です。1が標準、1.5は1.5倍、0.5は半分です。左下を基準に拡大・縮小し、表情別Scaleも掛け合わせます。")]
  public float scale=1;
   [Tooltip("立ち絵を下へ押し下げる補正（基準UI座標）です。大きくすると下端を多めに画面外へ隠し、小さくすると上がります。0で不透明部分の下端を画面下端に合わせます。")]
   public float bottomOffset=0;
  [Min(20)]
  [Tooltip("見えている立ち絵の基準幅（1920×1080基準のUI座標）です。大きくすると縦横比を保って全体が大きくなります。初期値570、Scale=1が基準です。")]
  public float width=570;
   [Tooltip("有効にすると画面サイズ・比率に合わせ、会話ウィンドウと同じ基準倍率で立ち絵を縮尺調整します。無効でも左下固定は維持しますが、倍率の自動補正は行いません。")]
   public bool fitToScreen=true;
  [Header("立ち絵の画面内への収まり")]
  [Tooltip("有効なら全表情の髪の上端が画面を越えない共通倍率に制限します。通常の大きさは維持し、はみ出す場合だけ必要な分を縮小します。元画像に描かれていない部分は復元できません。")]
  public bool keepHeadOnScreen=true;
  [Min(0),Tooltip("髪の上端に確保する最小余白（1920×1080基準のUI座標）です。大きいほど上端に余裕を取り、必要な場合だけ全表情を同じ倍率で縮小します。")]
  public float headTopMargin=8;
  CharacterPortraitController controller;
  void LateUpdate(){ApplyLayout();}
  void OnEnable(){controller=GetComponent<CharacterPortraitController>();}
  public void ApplyLayout(){
   if(!controller)controller=GetComponent<CharacterPortraitController>();
   var root=transform as RectTransform;if(!root)return;
   var parent=root.parent as RectTransform;float fit=1;
   if(parent&&fitToScreen)fit=Mathf.Max(.01f,Mathf.Min(parent.rect.width/1920f,parent.rect.height/1080f));
   root.anchorMin=root.anchorMax=root.pivot=Vector2.zero;
   root.anchoredPosition=new Vector2(positionX,positionY)*fit;
   root.sizeDelta=new Vector2(Mathf.Max(20,width),Mathf.Max(20,width)/ReferenceAspect());
   float actualScale=Mathf.Max(.1f,scale);
   if(keepHeadOnScreen&&parent&&parent.rect.height>1&&controller.expressions!=null){
    float top=0;
    foreach(var entry in controller.expressions){
     if(entry==null||!entry.sprite)continue;
     Rect bounds=entry.analyzedSprite==entry.sprite&&entry.alphaBounds.width>0?entry.alphaBounds:new Rect(0,0,1,1);
     float h=Mathf.Max(20,width)*Mathf.Max(.1f,entry.scale)/bounds.width*entry.sprite.rect.height/entry.sprite.rect.width;
     top=Mathf.Max(top,bounds.height*h-bottomOffset+entry.offset.y);
    }
    if(top>0)actualScale=Mathf.Min(actualScale,Mathf.Max(.01f,(parent.rect.height/fit-positionY-Mathf.Max(0,headTopMargin))/top));
   }
   root.localScale=Vector3.one*actualScale*fit;
   if(!Application.IsPlaying(gameObject))controller.EditorPreview();else controller.ReapplyLayout();
  }
  float ReferenceAspect(){
   var e=controller.Find(CharacterExpression.Normal);return e!=null&&e.sprite?e.sprite.rect.width/e.sprite.rect.height:1.17f;
  }
  public void ApplyImage(Image image,PortraitExpression entry){
   if(!image||entry==null||!entry.sprite)return;
   Rect bounds=entry.analyzedSprite==entry.sprite&&entry.alphaBounds.width>0?entry.alphaBounds:new Rect(0,0,1,1);
   float w=Mathf.Max(20,width)*Mathf.Max(.1f,entry.scale)/bounds.width;
   float h=w*entry.sprite.rect.height/entry.sprite.rect.width;
   var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;
   rect.sizeDelta=new Vector2(w,h);rect.localScale=Vector3.one;
   rect.anchoredPosition=new Vector2(-bounds.x*w,-bounds.y*h-bottomOffset)+entry.offset;
   image.preserveAspect=true;
  }
 }
}