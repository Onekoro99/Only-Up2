using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OnlyUpPrototype;
using CandyKingdom.Motion;
using CandyKingdom.Shortcake;
namespace CandyKingdom.Shortcake.Editor {
[InitializeOnLoad]
public static class ShortcakeRouteTests {
 static PrototypePlayerMotor motor;static CandyMotionDriver driver;static Animator animator;static StageRespawn respawn;static FallTracker falls;static PrototypeGoal goal;static ShortcakeRouteLayout layout;
 static List<string> lines;static int passed,failed;
 static ShortcakeRouteTests(){EditorApplication.update+=Poll;}
 [MenuItem("Tools/Candy Kingdom/Run Shortcake Route Tests")]
 public static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first.");if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ShortcakeMountainBuilder.ScenePath)throw new Exception("Open two-route scene.");SessionState.SetBool("Shortcake.Test",true);EditorApplication.isPlaying=true;}
 static void Poll(){if(EditorApplication.isPlaying&&Time.frameCount>=1&&SessionState.GetBool("Shortcake.Test",false)){SessionState.SetBool("Shortcake.Test",false);Test();}}
 static void Check(bool ok,string msg){lines.Add((ok?"PASS ":"FAIL ")+msg);if(ok)passed++;else failed++;}
 static Vector2 Input(Vector3 world){var f=Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up).normalized;return new Vector2(Vector3.Dot(world,Vector3.Cross(Vector3.up,f)),Vector3.Dot(world,f));}
 static void Step(Vector3 direction,bool jump=false){motor.Simulate(Input(direction),jump,.02f);driver.Tick(.02f);animator.Update(.02f);falls.Sample(motor.IsGrounded,motor.VerticalSpeed);typeof(StageRespawn).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(respawn,null);}
 static void Place(Vector3 p){motor.Teleport(p,Quaternion.identity);driver.ResetTracking();falls.ResetTracking();animator.Rebind();for(int i=0;i<20;i++)Step(Vector3.zero);}
 static bool Follow(SweetRouteNode node){
  bool jumped=false,air=false;
  for(int k=0;k<650;k++){
   var delta=node.point.position-motor.transform.position;delta.y=0;float distance=delta.magnitude;
   if(!node.jump&&distance<.12f&&motor.IsGrounded&&Mathf.Abs(motor.transform.position.y-node.point.position.y-.095812f)<.28f)return true;
   bool launch=node.jump&&!jumped&&motor.IsGrounded&&distance<node.launchDistance;
   if(launch)jumped=true;Step(distance>.08f?delta.normalized:Vector3.zero,launch);
   if(jumped&&!motor.IsGrounded)air=true;
   if(node.jump&&air&&motor.IsGrounded){var d=motor.transform.position-node.point.position;d.y=0;return Mathf.Abs(motor.transform.position.y-node.point.position.y-.095812f)<.22f&&d.magnitude<1.35f;}
  }return false;
 }
 static bool Route(SweetRouteNode[] nodes,string label){
  foreach(var node in nodes){bool ok=Follow(node);Check(ok,label+" "+node.point.name+" expected="+node.point.position.ToString("F2")+" actual="+motor.transform.position.ToString("F2"));if(!ok)return false;for(int i=0;i<5;i++)Step(Vector3.zero);}return true;
 }
 static void Test(){lines=new List<string>();passed=failed=0;try{
  var player=GameObject.Find("model_chara");motor=player.GetComponent<PrototypePlayerMotor>();driver=player.GetComponent<CandyMotionDriver>();animator=player.GetComponent<Animator>();respawn=player.GetComponent<StageRespawn>();falls=player.GetComponent<FallTracker>();layout=GameObject.Find("ShortcakeAdventureGenerated").GetComponent<ShortcakeRouteLayout>();goal=GameObject.Find("CandyTown_Goal").GetComponent<PrototypeGoal>();
  motor.enabled=false;driver.enabled=false;falls.enabled=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;var orbit=Camera.main.GetComponent<ThirdPersonOrbitCamera>();if(orbit)orbit.enabled=false;int resets=respawn.RespawnCount;
  Place(new Vector3(0,.12f,0));goal.RestoreReached(false);bool a=Route(layout.cookieRoute,"Route A");Check(a,"Original cookie course climb");
  if(a)a=Route(layout.commonCastleRoute,"A shared castle approach");goal.CheckPlayer();Check(a&&goal.IsReached,"Route A start to Sugar Castle");
  Place(new Vector3(0,.12f,0));goal.RestoreReached(false);bool b=Route(layout.cakeRoute,"Route B");Check(b,"Whole shortcake mountain climb");
  if(b)b=Route(layout.summitExit,"Cake summit merge");Check(b,"Cake summit joins cookie endpoint");
  if(b)b=Route(layout.commonCastleRoute,"B shared castle approach");goal.CheckPlayer();Check(b&&goal.IsReached,"Route B start to Sugar Castle");
  Place(layout.cookieRoute[7].point.position+Vector3.up*.12f);bool cross=Route(layout.midRouteSwitch,"Mid-route switch");Check(cross,"Cookie to cake middle crossing");
  if(cross){var reversed=new List<SweetRouteNode>();for(int i=layout.midRouteSwitch.Length-2;i>=0;i--)reversed.Add(new SweetRouteNode(layout.midRouteSwitch[i].point,false));reversed.Add(new SweetRouteNode(layout.cookieRoute[7].point,false));cross=Route(reversed.ToArray(),"Reverse middle crossing");}Check(cross,"Cake to cookie middle crossing");
  Place(new Vector3(-26,15,25));bool landed=motor.IsGrounded;for(int i=0;i<180&&!landed;i++){Step(Vector3.zero);landed=motor.IsGrounded;}
  Check(landed&&motor.transform.position.y<1&&motor.transform.position.y>-4,"Mountain fall caught by bottom landscape");
  Vector3 before=player.transform.position;for(int i=0;i<30;i++)Step(Vector3.back);Check(Vector3.Distance(before,player.transform.position)>1.4f,"Movement remains available after falling");
  Place(new Vector3(-18.8f,11,28));landed=motor.IsGrounded;for(int i=0;i<120&&!landed;i++){Step(Vector3.zero);landed=motor.IsGrounded;}
  Check(landed&&Mathf.Abs(player.transform.position.y-4.6958f)<.22f,"Fall can land on an intermediate cake terrace");
  Place(new Vector3(20,15,145));landed=motor.IsGrounded;for(int i=0;i<180&&!landed;i++){Step(Vector3.zero);landed=motor.IsGrounded;}
  Check(landed&&player.transform.position.y>-4&&player.transform.position.y<0,"Castle approach has extended bottom ground");
  Place(new Vector3(-14,CandyKingdom.ArtUpgrade.Editor.CandyWorldArtBuilder.GroundHeight(-14,0)+.12f,0));bool ground=Route(layout.groundEntrance,"Ground re-entry");Check(ground,"Ground to cake mountain retry without teleport");
  Place(new Vector3(0,CandyKingdom.ArtUpgrade.Editor.CandyWorldArtBuilder.GroundHeight(0,-20)+.12f,-20));var retry=new List<SweetRouteNode>();for(int i=1;i<=5;i++)retry.Add(new SweetRouteNode(GameObject.Find("GroundReturnStep_"+i).transform,true));retry.Add(new SweetRouteNode(GameObject.Find("CandyTown_Start").transform,false));bool cookieRetry=Route(retry.ToArray(),"Ground cookie retry");Check(cookieRetry,"Ground can return to original start and choose either route");
  Check(respawn.RespawnCount==resets,"All normal falls preserve position without respawn");
  var hashes=Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(ShortcakeMountainBuilder.Root+"/Backups/ProtectedHashes.json"));bool unchanged=true;using(var sha=System.Security.Cryptography.SHA256.Create())foreach(var pair in hashes)if(!File.Exists(pair.Key)||BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(pair.Key)))!=pair.Value)unchanged=false;Check(unchanged,"Player assets, controls, animations and source scene hashes unchanged");
  var cc=player.GetComponent<CharacterController>();Check(Mathf.Abs(cc.height-1.889099f)<.0001f&&Mathf.Abs(cc.radius-.26447386f)<.0001f&&Mathf.Abs(cc.stepOffset-.25f)<.0001f,"CharacterController settings retained");
  Check(!animator.applyRootMotion,"Root motion remains disabled");Check(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)=="Assets/CandyKingdom/Motion/Controllers/CandyMotion_Test.controller","Animator controller retained");Check(goal.transform.position.z>130,"Shared goal is at Sugar Castle");bool finite=true;var meshes=new HashSet<Mesh>();foreach(var mf in layout.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh)meshes.Add(mf.sharedMesh);foreach(var mesh in meshes)foreach(var v in mesh.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z))finite=false;Check(finite,"Generated geometry contains no NaN or infinite vertices");
  motor.Teleport(new Vector3(0,.12f,0),Quaternion.identity);
 }catch(Exception e){Check(false,e.ToString());}
 lines.Insert(0,"Shortcake two-route Play tests: "+passed+" passed, "+failed+" failed. Actual CharacterController/Motor/Animator dt=.02; respawn Update exercised.");
 File.WriteAllLines("Library/ShortcakeRouteTests.txt",lines);Debug.Log(string.Join("\n",lines));EditorApplication.isPlaying=false;
 }
}
}