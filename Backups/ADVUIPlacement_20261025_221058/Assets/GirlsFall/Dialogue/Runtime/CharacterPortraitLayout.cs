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
   [Tooltip("立ち絵を下へ押し下げる補正（基準UI座標）です。大きくすると下端を多めに画面外へ隠し、小さくすると上がります。初期値2で細い隙間を防ぎます。")]
   public float bottomOffset=2;
  [Min(20)]
  [Tooltip("見えている立ち絵の基準幅（1920×1080基準のUI座標）です。大きくすると縦横比を保って全体が大きくなります。初期値570、Scale=1が基準です。")]
  public float width=570;
   [Tooltip("有効にすると画面サイズ・比率に合わせ、会話ウィンドウと同じ基準倍率で立ち絵を縮尺調整します。無効でも左下固定は維持しますが、倍率の自動補正は行いません。")]
   public bool fitToScreen=true;
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
   root.localScale=Vector3.one*Mathf.Max(.1f,scale)*fit;
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