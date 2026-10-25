using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace GirlsFall.Dialogue {
 [ExecuteAlways,DisallowMultipleComponent]
 public sealed class DialogueTextLayout:MonoBehaviour {
  [Header("参照コンポーネント")]
  [Tooltip("セリフ本文を表示するTextMeshProの参照です。このコンポーネントへ本文の位置・文字サイズ・余白を反映します。")]
  public TMP_Text body;
  [Tooltip("話者名を表示するTextMeshProの参照です。名前ラベル内の中央に配置します。")]
  public TMP_Text speaker;
  [Tooltip("話者名を載せる角丸ラベルのRectTransformです。名前の位置とラベルの幅・高さをこの枠へ反映します。")]
  public RectTransform nameLabel;
  [Tooltip("名前ラベルの背景Imageです。Name Background Colorで指定した色を反映します。")]
  public Image nameBackground;
  [Header("セリフ本文の文字設定")]
  [Min(8)]
  [Tooltip("本文の文字サイズ（1920×1080基準）です。大きいほど読みやすく大きくなり、小さいほど多くの文字が入ります。初期値38で、自動縮小はしません。")]
  public float fontSize=38;
   [Tooltip("本文Rectの左右位置（基準UI座標）です。ウィンドウ左上から、大きいほど右、小さいほど左へ移動します。文字の開始位置にはLeft Paddingも加わります。")]
   public float textPositionX;
   [Tooltip("本文Rectの上下位置（基準UI座標）です。ウィンドウ左上から、大きいほど上、小さいほど下へ移動します。文字はTop Paddingの分だけ下から始まります。")]
   public float textPositionY;
  [Min(20)]
  [Tooltip("本文Rectの幅（基準UI座標）です。大きいほど1行に多く入り、小さいほど早く折り返します。左右の余白を含み、初期値1364です。")]
  public float textWidth=1364;
  [Min(20)]
  [Tooltip("本文Rectの高さ（基準UI座標）です。大きいほど多くの行を表示できます。上下の余白を含み、初期値304です。")]
  public float textHeight=304;
   [Tooltip("本文の追加行間（TextMeshProの値）です。大きいほど行間が広がり、小さいほど詰まります。初期値10です。")]
   public float lineSpacing=10;
   [Tooltip("本文の追加文字間隔（TextMeshProの文字間値）です。大きいほど文字間が広がり、小さいほど詰まります。0はフォント本来の間隔です。")]
   public float characterSpacing;
   [Tooltip("本文の文字色と透明度です。背景との見分けやすさを調整します。初期値は濃いグレー #383838、不透明です。")]
   public Color textColor=new Color32(56,56,56,255);
  [Header("セリフ本文の余白")]
  [Min(0)]
  [Tooltip("本文の左余白（基準UI座標）です。大きいほど文字の開始位置が右へ、小さいほど左へ移動します。初期値102で、立ち絵との重なりを防ぎます。")]
  public float textLeftPadding=102;
  [Min(0)]
  [Tooltip("本文の右余白（基準UI座標）です。大きいほど早く折り返します。初期値200で、右端の星や雲と文字が重ならない領域を空けます。")]
  public float textRightPadding=200;
  [Min(0)]
  [Tooltip("本文の上余白（基準UI座標）です。大きいほど本文が下へ、小さいほど上へ移動します。名前ラベルの下を空ける初期値は88です。")]
  public float textTopPadding=88;
  [Min(0)]
  [Tooltip("本文の下余白（基準UI座標）です。大きいほど表示できる行数が減り、操作案内の空きが増えます。初期値56です。")]
  public float textBottomPadding=56;
  [Header("キャラクター名ラベル")]
  [Min(8)]
  [Tooltip("話者名の文字サイズ（1920×1080基準）です。大きいほど大きくなります。初期値32で、自動縮小はしません。")]
  public float nameFontSize=32;
   [Tooltip("名前ラベルの左右位置（ウィンドウ内側の左上基準）です。大きいほど右、小さいほど左へ移動します。初期値76です。")]
   public float namePositionX=76;
   [Tooltip("名前ラベルの上下位置（ウィンドウ内側の左上基準）です。大きいほど上、小さいほど下へ移動します。初期値−10です。")]
   public float namePositionY=-10;
  [Min(20)]
  [Tooltip("名前ラベルの幅（基準UI座標）です。大きくすると横長になり、長い名前を入れやすくなります。名前は中央揃えです。初期値250です。")]
  public float nameLabelWidth=250;
  [Min(20)]
  [Tooltip("名前ラベルの高さ（基準UI座標）です。大きいほど上下に広がり、小さいほど薄くなります。初期値52です。本文と重ならない範囲で調整してください。")]
  public float nameLabelHeight=52;
   [Tooltip("話者名の文字色と透明度です。ピンクの背景に読みやすい初期値は白 #FFFFFFです。")]
   public Color nameTextColor=Color.white;
   [Tooltip("名前ラベルの背景色と透明度です。初期値はピンク #FF70A5です。文字色と十分に見分けられる色を指定してください。")]
   public Color nameBackgroundColor=new Color32(255,112,165,255);
  [Header("会話ウィンドウ全体")]
  [Tooltip("ウィンドウ全体の枠です。本文と名前ラベルをまとめて配置します。")] public RectTransform windowFrame;
  [Tooltip("白いウィンドウ内部のImageです。背景色と角丸を反映します。")] public Image windowFill;
  [Tooltip("外側のピンク枠のImageです。枠色と角丸を反映します。")] public Image windowBorder;
  [Tooltip("ウィンドウ左端の位置（1920×1080基準UI座標）。小さくすると左へ移動します。")] public float windowPositionX=500;
  [Tooltip("ウィンドウ下端の位置（基準UI座標）。大きくすると上へ移動します。")] public float windowPositionY=30;
  [Min(200),Tooltip("ウィンドウの基準幅（UI座標）です。自動幅ONでは最小幅として使い、横長画面ではさらに広がります。本文幅は別項目です。")] public float windowWidth=1380;
  [Min(100),Tooltip("ウィンドウ全体の高さ（基準UI座標）。大きくすると上へ広がります。")] public float windowHeight=320;
  [Range(.1f,2),Tooltip("角丸Spriteの密度倍率です。小さいほど角の丸みが大きくなります。")] public float cornerDensity=.4f;
  [Tooltip("ウィンドウの背景色と透明度です。白に近い淡いピンクを使用します。")] public Color windowColor=new Color(1,.975f,.99f,.98f);
  [Tooltip("ウィンドウ外枠の色です。淡いピンクが基準です。")] public Color borderColor=new Color(1,.68f,.83f,1);
  [Tooltip("有効ならウィンドウを立ち絵と同じ画面左下基準で拡縮し、横長画面でも中央寄せの余白を加えません。")] public bool fitWindowToScreen=true;
  [Tooltip("横長画面では右端の余白までウィンドウを広げます。本文Rectの幅は別の設定です。")] public bool autoWindowWidth=true;
  [Min(0),Tooltip("自動拡張時に画面右端へ残す余白（基準UI座標）です。大きいほど右の空きが広がります。")] public float windowRightMargin=40;
  bool dirty=true;
  void OnEnable(){ApplyLayout();}
  void OnValidate(){dirty=true;}
  void OnRectTransformDimensionsChange(){dirty=true;}
  void LateUpdate(){if(dirty)ApplyLayout();}
  public void ApplyLayout(){
   if(!body||!speaker||!nameLabel||!nameBackground)return;
   if(windowFrame){
    var parent=windowFrame.parent as RectTransform;float fit=fitWindowToScreen&&parent?Mathf.Max(.01f,Mathf.Min(parent.rect.width/1920f,parent.rect.height/1080f)):1;
    float actualWidth=autoWindowWidth&&parent?Mathf.Max(windowWidth,parent.rect.width/fit-windowPositionX-windowRightMargin):windowWidth;
    windowFrame.anchorMin=windowFrame.anchorMax=windowFrame.pivot=Vector2.zero;
    windowFrame.localScale=Vector3.one*fit;windowFrame.anchoredPosition=new Vector2(windowPositionX,windowPositionY)*fit;windowFrame.sizeDelta=new Vector2(actualWidth,windowHeight);
   }
   if(windowFill){windowFill.color=windowColor;windowFill.pixelsPerUnitMultiplier=cornerDensity;}
   if(windowBorder){windowBorder.color=borderColor;windowBorder.pixelsPerUnitMultiplier=cornerDensity;}
   var rect=body.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
   rect.anchoredPosition=new Vector2(textPositionX,textPositionY);
   rect.sizeDelta=new Vector2(Mathf.Max(20,textWidth),Mathf.Max(20,textHeight));
   body.fontSize=Mathf.Max(8,fontSize);body.fontStyle=FontStyles.Normal;body.enableAutoSizing=false;
   body.alignment=TextAlignmentOptions.TopLeft;body.lineSpacing=lineSpacing;body.characterSpacing=characterSpacing;
   body.textWrappingMode=TextWrappingModes.Normal;body.overflowMode=TextOverflowModes.Masking;
   body.color=textColor;body.margin=new Vector4(Mathf.Max(0,textLeftPadding),Mathf.Max(0,textTopPadding),Mathf.Max(0,textRightPadding),Mathf.Max(0,textBottomPadding));body.raycastTarget=false;
   nameLabel.anchorMin=nameLabel.anchorMax=nameLabel.pivot=new Vector2(0,1);
   nameLabel.anchoredPosition=new Vector2(namePositionX,namePositionY);nameLabel.sizeDelta=new Vector2(Mathf.Max(20,nameLabelWidth),Mathf.Max(20,nameLabelHeight));
   var name=speaker.rectTransform;name.anchorMin=Vector2.zero;name.anchorMax=Vector2.one;name.pivot=new Vector2(.5f,.5f);name.anchoredPosition=Vector2.zero;name.sizeDelta=Vector2.zero;
   speaker.fontSize=Mathf.Max(8,nameFontSize);speaker.fontStyle=FontStyles.Normal;speaker.enableAutoSizing=false;speaker.alignment=TextAlignmentOptions.Center;speaker.color=nameTextColor;speaker.margin=Vector4.zero;speaker.raycastTarget=false;
   nameBackground.color=nameBackgroundColor;dirty=false;
   // Only typography and rectangles change. Text and maxVisibleCharacters belong to the existing dialogue UI.
  }
 }
}