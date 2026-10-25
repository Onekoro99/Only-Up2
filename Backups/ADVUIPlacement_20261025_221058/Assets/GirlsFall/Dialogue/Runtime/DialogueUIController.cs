using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace GirlsFall.Dialogue {
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