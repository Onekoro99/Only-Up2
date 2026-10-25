using UnityEngine;
namespace CandyKingdom {
[CreateAssetMenu(menuName="Candy Kingdom/Candy Town Settings")]
public sealed class CandyTownSettings : ScriptableObject {
 [Range(8,30)]
 [Header("ルート生成の設定")]
 [Tooltip("キャンディタウン生成時の足場数です。多くすると同じ総高さでも1段の上昇が小さくなります。範囲8〜30、初期値16です。生成済みSceneは自動更新しません。")]
 public int platformCount=16;
 [Min(0.2f)]
 [Tooltip("キャンディタウンの攻略ルートが上昇する総高さ（m）です。同じ足場数なら大きいほど各段差が高くなり、難しくなります。初期値8.8mで、再生成時に反映します。")]
 public float areaHeight=8.8f;
 [Range(2.4f,4f)]
 [Tooltip("生成する基本足場の直径（m）です。大きいほど着地面が広く、小さいほど狭くなります。範囲2.4〜4m、初期値3mです。再生成時に反映します。")]
 public float platformDiameter=3f;
 [Range(0.1f,1f)]
 [Tooltip("生成する足場の端同士の基準間隔（m）です。大きいほど離れ、小さいほど近づきます。実際の間隔にはDifficultyも反映します。初期値0.35mです。")]
 public float edgeGap=0.35f;
 [Range(0,1)]
 [Tooltip("生成時の難しさ（0〜1）です。大きいほど足場間隔が広がり、後半の足場も少し小さくなります。小さいほど易しくなります。初期値0.5です。")]
 public float difficulty=0.5f;
 [Range(0,2)]
 [Header("装飾と乱数")]
 [Tooltip("生成する追加装飾の量です。大きいほど装飾数と描画負荷が増え、小さいほど減ります。0で追加装飾なし、1が基準量、上限2です。")]
 public float decorationDensity=1f;
 [Tooltip("生成の乱数Seedです。同じ設定・Seedなら同じ配置を再現できます。数値の大小に難易度の意味はなく、別の整数で配置が変わります。再生成時に使用します。")]
 public int seed=241024;
 
 [Header("後続エリアの予定")]
 [Tooltip("後続エリアへの接続高さの予定値（m）の一覧です。この設定だけでエリア2〜5は生成されません。生成済み足場を移動する設定でもありません。")]
 public float[] laterAreaHeights={10f,12f,14f,16f};
}
}