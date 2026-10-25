using System;
using UnityEngine;
namespace CandyKingdom.Shortcake {
[Serializable] public sealed class SweetRouteNode {
 [Header("ルート検証の目標")]
 [Tooltip("自動ルート検証で目標にする足場・地点のTransformです。この参照の差し替えだけでは地形を動かしません。")]
 public Transform point;
 [Tooltip("この目標へ進む自動テストでジャンプを使うか指定します。手動操作のジャンプを禁止・許可する設定ではありません。")]
 public bool jump;
 [Tooltip("自動テストが目標へ何mまで近づいたらジャンプするかの距離です。大きいほど手前で、小さいほど近づいてから跳びます。通常のジャンプ力は変わりません。初期値2.03mです。")]
 public float launchDistance=2.03f;
 public SweetRouteNode(Transform p,bool j,float distance=2.03f){point=p;jump=j;launchDistance=distance;}
}
public sealed class ShortcakeRouteLayout:MonoBehaviour {
 [Header("ルート検証用の目標一覧")]
 [Tooltip("クッキールートの到達順を記録する検証用の目標一覧です。上から順に自動テストでたどります。並べ替えても実際の足場は移動しません。")]
 public SweetRouteNode[] cookieRoute;
 [Tooltip("ケーキルートの到達順を記録する検証用の目標一覧です。足場参照とジャンプ条件を保持し、通常の移動操作を変える設定ではありません。")]
 public SweetRouteNode[] cakeRoute;
 [Tooltip("ケーキ頂上から共通ルートへ抜ける目標一覧です。自動到達テスト用の順番で、地形の生成・移動は行いません。")]
 public SweetRouteNode[] summitExit;
 [Tooltip("合流後からお城へ進む共通ルートの目標一覧です。自動テストの通過順を指定します。")]
 public SweetRouteNode[] commonCastleRoute;
 [Tooltip("途中で別ルートへ乗り換える接続箇所の目標一覧です。検証用の参照で、ここを編集しても橋や足場は自動で移動しません。")]
 public SweetRouteNode[] midRouteSwitch;
 [Tooltip("下層の地面からルートへ戻る入口の目標一覧です。自動到達テスト用の順番を保持します。")]
 public SweetRouteNode[] groundEntrance;
 [Header("ゴールと装飾の記録")]
 [Tooltip("最終ゴールとなるお城側のTransformです。ルートの到達検証に使用し、ゴール判定コンポーネントとは別の参照です。")]
 public Transform castleGoal;
 [Tooltip("装飾生成のSeedを記録する整数です。数値の大小に難易度の意味はありません。この記録値の変更だけでは装飾は再配置されません。")]
 public int decorationSeed=241024;
}
}