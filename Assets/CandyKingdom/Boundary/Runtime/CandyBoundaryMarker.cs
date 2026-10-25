using UnityEngine;
namespace CandyKingdom.Boundary {
public sealed class CandyBoundaryMarker:MonoBehaviour {
 [Header("生成結果の記録")]
 [Tooltip("外周地形の生成方式のバージョンを記録する値です。数値だけを変えても地形やColliderは再生成されません。")]
 public int generationVersion=1;
 [Tooltip("外周生成時のSeedを記録する整数です。生成結果の識別用で、ここを変更しても配置済みの地形は変わりません。")]
 public int seed=20261025;
 [Tooltip("外周の連続した崖に使うMeshColliderの参照です。隙間・落下対策の確認用で、設定値だけで形状を作り直すものではありません。")]
 public MeshCollider continuousRim;
 [Tooltip("外周の崖高さの目安を記録する値（m）です。変更しても生成済みの地形・Colliderの高さは変わりません。")]
 public float minimumCliffHeight=22f;
 [TextArea]
 [Tooltip("この外周地形の構成や用途を記録する説明文です。ゲームの挙動には影響しません。")]
 public string description="Continuous terrain apron, steep visible sweet cliffs, connected distant landscape. Gameplay scripts unchanged.";
}
}