using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace GirlsFall.Dialogue {
 [ExecuteAlways]
 public sealed class DialogueUIController:MonoBehaviour {
  [Header("ADVの表示先")]
  [Tooltip("ADV画面の透明度とUI入力をまとめて制御するCanvasGroupです。表示・終了フェードや操作の切り替えに使います。")]
  public CanvasGroup group;
  [Tooltip("立ち絵とセリフウィンドウをまとめた表示ルートです。ADV開始時に表示し、終了時に非表示にします。")]
  public GameObject presentation;
  [Tooltip("ADV完了の保存に失敗した場合に表示するエラーパネルです。再試行・タイトルへ戻るボタンを含みます。")]
  public GameObject errorPanel;
  [Tooltip("会話データの話者名を表示するTextMeshProです。本文用ではなく名前ラベルのテキストを指定します。")]
  public TMP_Text speaker;
  [Tooltip("セリフ本文のTextMeshProです。全文を設定し、maxVisibleCharactersで文字送りを行います。")]
  public TMP_Text body;
  [Tooltip("全文表示後に現れる、次へ進めることを示すアイコン用テキストです。文字送り中は非表示です。")]
  public TMP_Text nextIcon;
  [Tooltip("ADV完了を保存できない理由と対処を表示するTextMeshProです。")]
  public TMP_Text errorText;
  [Header("セリフ送りと保存エラーのボタン")]
  [Tooltip("クリックによるセリフ送りを受け付けるボタンです。文字送り中は全文表示、全文表示後は次の行へ進みます。")]
  public Button advanceButton;
  [Tooltip("会話をスキップするボタンです。通常終了と同じ完了フラグの保存を行い、成功後にゲームへ戻ります。")]
  public Button skipButton;
  [Tooltip("ADV完了の保存に失敗したとき、同じ完了保存を再試行するボタンです。")]
  public Button retryButton;
  [Tooltip("ADV完了を保存できない場合に、完了扱いにせずタイトルへ戻るボタンです。次回は会話が再表示されます。")]
  public Button titleButton;
  [Tooltip("表情に応じた立ち絵Spriteの選択とフェードを担当するCharacterPortraitControllerです。")]
  public CharacterPortraitController portraits;
  [Header("SKIPボタンの右上配置")]
  [Min(0),Tooltip("画面右端からボタン右端までの余白（1920×1080基準のUI座標）です。大きいほど左へ移動します。初期値20です。")]
  public float skipRightMargin=20;
  [Min(0),Tooltip("画面上端からボタン上端までの余白（1920×1080基準のUI座標）です。大きいほど下へ移動します。初期値20です。")]
  public float skipTopMargin=20;
  [Min(1),Tooltip("SKIPボタンの幅（1920×1080基準のUI座標）です。右端を固定して、大きいほど左へ広がります。初期値240で従来の幅を維持します。")]
  public float skipWidth=240;
  [Min(1),Tooltip("SKIPボタンの高さ（1920×1080基準のUI座標）です。上端を固定して、大きいほど下へ広がります。初期値64で従来の高さを維持します。")]
  public float skipHeight=64;
  void OnEnable(){ApplySkipLayout();}
  void LateUpdate(){ApplySkipLayout();}
  public void ApplySkipLayout(){
   if(!skipButton||!presentation)return;
   var rect=skipButton.transform as RectTransform;var parent=rect.parent as RectTransform;if(!parent)return;
   float fit=parent.rect.width>1&&parent.rect.height>1?Mathf.Max(.01f,Mathf.Min(parent.rect.width/1920f,parent.rect.height/1080f)):1;
   rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-Mathf.Max(0,skipRightMargin),-Mathf.Max(0,skipTopMargin))*fit;
   rect.sizeDelta=new Vector2(Mathf.Max(1,skipWidth),Mathf.Max(1,skipHeight));rect.localScale=Vector3.one*fit;
  }
  public int CharacterCount {get;private set;}
  public void Show(){
   presentation.SetActive(true);errorPanel.SetActive(false);
   group.alpha=0;group.interactable=true;group.blocksRaycasts=true;
   UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
  }
  public void SetLine(string name,string text,CharacterExpression expression){
   speaker.text=name;body.text=text??"";body.maxVisibleCharacters=0;
   body.ForceMeshUpdate();CharacterCount=body.textInfo.characterCount;
   portraits.Show(expression);nextIcon.gameObject.SetActive(false);
  }
  public void Reveal(int count){body.maxVisibleCharacters=count;nextIcon.gameObject.SetActive(count>=CharacterCount);}
  public void Error(string text){errorText.text=text;errorPanel.SetActive(true);advanceButton.interactable=false;skipButton.interactable=false;}
  public void Hide(){group.alpha=0;group.interactable=false;group.blocksRaycasts=false;presentation.SetActive(false);}
 }
}