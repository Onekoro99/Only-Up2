using System;
using System.Collections.Generic;
using UnityEngine;
namespace GirlsFall.Dialogue {
 [Serializable] public sealed class DialogueLine {
   [Header("セリフの内容と演出")]
   [Tooltip("このセリフの話者名です。空欄なら会話データのDefault Character Nameを表示します。")]
   public string characterName="";
  [TextArea(3,8)]
  [Tooltip("この行で表示するセリフ本文です。改行も表示に反映されます。長すぎる場合は会話を分割するか、表示範囲を調整してください。")]
  public string text;
  [Tooltip("このセリフ中に表示する立ち絵の表情です。同じ種類のSpriteと個別補正が立ち絵設定から選ばれます。")]
  public CharacterExpression expression;
  [Min(0)]
  [Tooltip("このセリフだけの文字送り速度（文字/秒）です。0なら会話の既定速度を使用します。正の値では大きいほど速く、小さいほどゆっくり表示します。")]
  public float charactersPerSecond;
  [Min(0)]
  [Tooltip("この行の本文を表示し始めるまでの待ち時間（秒）です。大きくすると長く待ち、0なら待たずに文字送りを始めます。")]
  public float delayBefore;
 }
 [CreateAssetMenu(fileName="Dialogue",menuName="Girls Fall/Dialogue/Conversation")]
 public sealed class DialogueData:ScriptableObject {
  [Header("会話の基本設定")]
  [Tooltip("各セリフのCharacter Nameが空欄のときに表示する話者名です。初期値は主人公です。")]
  public string defaultCharacterName="主人公";
  [Min(1)]
  [Tooltip("会話全体の文字送り速度（文字/秒）です。大きいほど速く、小さいほどゆっくり表示します。各行の速度が0のときに使います。初期値32です。")]
  public float defaultCharactersPerSecond=32;
  [Header("セリフ一覧")]
  [Tooltip("上から順に表示するセリフの一覧です。要素を追加・削除・並べ替えると会話順が変わります。本文・話者名・表情・速度・待ち時間を行ごとに編集します。")]
  public List<DialogueLine> lines=new List<DialogueLine>();
 }
}