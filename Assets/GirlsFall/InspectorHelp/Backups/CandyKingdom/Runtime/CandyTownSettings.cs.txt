using UnityEngine;
namespace CandyKingdom {
[CreateAssetMenu(menuName="Candy Kingdom/Candy Town Settings")]
public sealed class CandyTownSettings : ScriptableObject {
 [Range(8,30)] public int platformCount=16;
 [Min(0.2f)] public float areaHeight=8.8f;
 [Range(2.4f,4f)] public float platformDiameter=3f;
 [Range(0.1f,1f)] public float edgeGap=0.35f;
 [Range(0,1)] public float difficulty=0.5f;
 [Range(0,2)] public float decorationDensity=1f;
 public int seed=241024;
 [Tooltip("Future area connection heights; areas 2-5 are not generated yet.")]
 public float[] laterAreaHeights={10f,12f,14f,16f};
}
}