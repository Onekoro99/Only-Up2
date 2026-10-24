using System;
using UnityEngine;
namespace CandyKingdom.Shortcake {
[Serializable] public sealed class SweetRouteNode {
 public Transform point;
 public bool jump;
 public float launchDistance=2.03f;
 public SweetRouteNode(Transform p,bool j,float distance=2.03f){point=p;jump=j;launchDistance=distance;}
}
public sealed class ShortcakeRouteLayout:MonoBehaviour {
 public SweetRouteNode[] cookieRoute;
 public SweetRouteNode[] cakeRoute;
 public SweetRouteNode[] summitExit;
 public SweetRouteNode[] commonCastleRoute;
 public SweetRouteNode[] midRouteSwitch;
 public SweetRouteNode[] groundEntrance;
 public Transform castleGoal;
 public int decorationSeed=241024;
}
}