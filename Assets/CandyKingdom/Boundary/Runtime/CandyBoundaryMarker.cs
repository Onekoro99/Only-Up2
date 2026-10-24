using UnityEngine;
namespace CandyKingdom.Boundary {
public sealed class CandyBoundaryMarker:MonoBehaviour {
 public int generationVersion=1;
 public int seed=20261025;
 public MeshCollider continuousRim;
 public float minimumCliffHeight=22f;
 [TextArea] public string description="Continuous terrain apron, steep visible sweet cliffs, connected distant landscape. Gameplay scripts unchanged.";
}
}