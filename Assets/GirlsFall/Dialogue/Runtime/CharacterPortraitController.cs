using System;
using UnityEngine;
using UnityEngine.UI;
namespace GirlsFall.Dialogue {
 [Serializable] public sealed class PortraitExpression {
  [Header("表情の画像と個別補正")]
  [Tooltip("この設定を使う表情の種類です。会話データのExpressionと一致した画像・補正値が選ばれます。")]
  public CharacterExpression expression;
   [Tooltip("この表情で表示する立ち絵Spriteです。差し替え後はアルファ余白を再解析してください。PNGの縦横比は維持されます。")]
   public Sprite sprite;
   [Tooltip("この表情だけの位置補正（1920×1080基準のUI座標）です。Xを増やすと右、Yを増やすと上へ移動します。Yを上げると画面下端に隙間ができる場合があります。")]
   public Vector2 offset;
  [Range(.5f,1.5f)]
  [Tooltip("この表情だけの表示倍率です。1が等倍、1.5は1.5倍、0.5は半分です。共通Scaleに掛け合わせ、見えている部分の幅を調整します。")]
  public float scale=1;
  [HideInInspector] public Rect alphaBounds=new Rect(0,0,1,1);
  [HideInInspector] public Sprite analyzedSprite;
 }
 public sealed class CharacterPortraitController:MonoBehaviour {
  [Header("立ち絵の表示先")]
  [Tooltip("現在の表情を表示するImageの参照です。立ち絵用のExpressionを指定します。位置や大きさは同じGameObjectの配置設定で調整します。")]
  public Image portrait;
  [Tooltip("表情切り替え時に、前の表情を重ねてフェードさせるImageの参照です。PreviousExpressionを指定します。")]
  public Image previousPortrait;
  [Header("表情ごとの設定")]
  [Tooltip("表情の種類ごとにSprite・位置補正・拡大率を登録します。画像を差し替えたら、配置設定のアルファ余白を再解析して保存してください。")]
  public PortraitExpression[] expressions;
  [Min(.01f)]
  [Header("表情フェードとプレビュー")]
  [Tooltip("表情が切り替わるフェード時間（秒）です。大きくするとゆっくり、小さくすると素早く切り替わります。初期値は0.16秒です。")]
  public float fadeDuration=.16f;
   [Tooltip("Play停止中に確認する表情です。会話中の表情は会話データから決まるため、この項目では変わりません。")]
   public CharacterExpression previewExpression;
  float elapsed;bool fading,hasImage,previousVisible;PortraitExpression currentEntry,previousEntry;
  public Sprite CurrentSprite=>portrait?portrait.sprite:null;
  public bool Show(CharacterExpression expression){
   var entry=Find(expression);if(entry==null||!entry.sprite)return false;
   previousVisible=hasImage;previousEntry=currentEntry;previousPortrait.sprite=portrait.sprite;
   CopyRect(portrait.rectTransform,previousPortrait.rectTransform);
   previousPortrait.color=new Color(1,1,1,hasImage?1:0);
   currentEntry=entry;portrait.sprite=entry.sprite;portrait.preserveAspect=true;
   ReapplyLayout();portrait.color=new Color(1,1,1,0);elapsed=0;fading=true;hasImage=true;return true;
  }
  public PortraitExpression Find(CharacterExpression expression){return expressions==null?null:Array.Find(expressions,e=>e!=null&&e.expression==expression);}
  public void ReapplyLayout(){
   var layout=GetComponent<CharacterPortraitLayout>();
   if(layout){if(currentEntry!=null)layout.ApplyImage(portrait,currentEntry);if(previousEntry!=null)layout.ApplyImage(previousPortrait,previousEntry);}
   else if(currentEntry!=null){portrait.rectTransform.anchoredPosition=currentEntry.offset;portrait.rectTransform.localScale=Vector3.one*currentEntry.scale;}
  }
  public void EditorPreview(){
   var entry=Find(previewExpression);if(entry==null||!entry.sprite||!portrait||!previousPortrait)return;
   currentEntry=entry;previousEntry=null;portrait.sprite=entry.sprite;portrait.color=Color.white;previousPortrait.color=Color.clear;ReapplyLayout();
  }
  static void CopyRect(RectTransform a,RectTransform b){b.anchorMin=a.anchorMin;b.anchorMax=a.anchorMax;b.pivot=a.pivot;b.sizeDelta=a.sizeDelta;b.anchoredPosition=a.anchoredPosition;b.localScale=a.localScale;}
  public void Tick(float dt){
   if(!fading)return;elapsed+=Mathf.Max(0,dt);float a=Mathf.Clamp01(elapsed/Mathf.Max(.01f,fadeDuration));
   portrait.color=new Color(1,1,1,a);previousPortrait.color=previousVisible&&previousPortrait.sprite?new Color(1,1,1,1-a):Color.clear;
   if(a>=1){fading=false;previousPortrait.color=Color.clear;}
  }
  void Update(){Tick(Time.unscaledDeltaTime);}
 }
}