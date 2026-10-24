using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OnlyUpPrototype;
using CandyKingdom.Motion;
namespace CandyKingdom.ArtUpgrade.Editor {
[InitializeOnLoad]
public static class CandyWorldArtTests {
 static PrototypePlayerMotor motor;static CandyMotionDriver driver;static Animator animator;static StageRespawn respawn;static FallTracker falls;
 static List<string> lines;static int passed,failed;
 static CandyWorldArtTests(){EditorApplication.update+=Poll;}
 [MenuItem("Tools/Candy Kingdom/Run Dream World Tests")]
 public static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first.");if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=CandyWorldArtBuilder.ScenePath)throw new Exception("Open Dream World scene.");SessionState.SetBool("DreamWorld.Test",true);EditorApplication.isPlaying=true;}
 static void Poll(){if(EditorApplication.isPlaying&&Time.frameCount>=1&&SessionState.GetBool("DreamWorld.Test",false)){SessionState.SetBool("DreamWorld.Test",false);Test();}}
 static void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);if(ok)passed++;else failed++;}
 static Vector2 Input(Vector3 world){var f=Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up).normalized;return new Vector2(Vector3.Dot(world,Vector3.Cross(Vector3.up,f)),Vector3.Dot(world,f));}
 static void Step(Vector3 direction,bool jump=false){motor.Simulate(Input(direction),jump,.02f);driver.Tick(.02f);animator.Update(.02f);falls.Sample(motor.IsGrounded,motor.VerticalSpeed);typeof(StageRespawn).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(respawn,null);}
 static void Place(Vector3 p){motor.Teleport(p,Quaternion.identity);driver.ResetTracking();falls.ResetTracking();for(int i=0;i<20;i++)Step(Vector3.zero);}
 static bool JumpTo(Transform target){bool launched=false,air=false;for(int k=0;k<500;k++){var d=target.position-motor.transform.position;d.y=0;float distance=d.magnitude;bool jump=!launched&&motor.IsGrounded&&distance<2.03f;if(jump)launched=true;Step(distance>.09f?d.normalized:Vector3.zero,jump);if(launched&&!motor.IsGrounded)air=true;if(air&&motor.IsGrounded)return Mathf.Abs(motor.transform.position.y-(target.position.y+.095812f))<.21f&&new Vector2(d.x,d.z).magnitude<1.3f;}return false;}
 static void Test(){lines=new List<string>();passed=failed=0;try{
  var player=GameObject.Find("model_chara");motor=player.GetComponent<PrototypePlayerMotor>();driver=player.GetComponent<CandyMotionDriver>();animator=player.GetComponent<Animator>();respawn=player.GetComponent<StageRespawn>();falls=player.GetComponent<FallTracker>();
  motor.enabled=false;driver.enabled=false;falls.enabled=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;var orbit=Camera.main.GetComponent<ThirdPersonOrbitCamera>();if(orbit)orbit.enabled=false;
  int originalResets=respawn.RespawnCount;
  motor.Teleport(new Vector3(20,15,20),Quaternion.identity);driver.ResetTracking();falls.ResetTracking();bool landed=false,longSeen=false;
  for(int i=0;i<180;i++){Step(Vector3.zero);longSeen|=animator.GetCurrentAnimatorStateInfo(0).IsName("Long Fall");if(motor.IsGrounded){landed=true;break;}}
  Check(landed,"High fall lands on lowest terrain");Check(respawn.RespawnCount==originalResets,"Normal fall does not respawn");Check(motor.transform.position.y>-4,"Ground catches player before -5m kill plane");Check(longSeen,"Existing Long Fall animation still plays");Check(falls.LastReport.Distance>10,"Existing fall distance tracking preserved");
  var before=player.transform.position;for(int i=0;i<30;i++)Step(Vector3.forward);Check(Vector3.Distance(before,player.transform.position)>1.5f,"Player can move after ground landing");
  foreach(float x in new[]{-40f,0f,40f})foreach(float z in new[]{0f,35f,90f})Check(Physics.Raycast(new Vector3(x,25,z),Vector3.down,40,~0,QueryTriggerInteraction.Ignore),"Ground coverage "+x+","+z);
  Place(new Vector3(0,CandyWorldArtBuilder.GroundHeight(0,-20)+.12f,-20));
  bool returnRoute=true;for(int i=1;i<=5;i++){var target=GameObject.Find("GroundReturnStep_"+i).transform;bool ok=JumpTo(target);Check(ok,"Ground return step "+i);if(!ok){returnRoute=false;break;}}
  if(returnRoute){for(int i=0;i<75;i++)Step(Vector3.forward);for(int i=0;i<10;i++)Step(Vector3.zero);}
  Check(returnRoute&&motor.IsGrounded&&Mathf.Abs(player.transform.position.y-.095812f)<.2f,"Can return from ground to town");
  Place(new Vector3(0,.12f,0));bool route=true;
  for(int i=1;i<=16;i++){var target=GameObject.Find("Route_"+i.ToString("00")).transform;bool ok=JumpTo(target);Check(ok,"Sweet route "+i+" actualY="+player.transform.position.y.ToString("F2"));if(!ok){route=false;break;}for(int k=0;k<5;k++)Step(Vector3.zero);}
  Check(route,"All 16 redesigned platforms reachable");var goal=GameObject.Find("CandyTown_Goal").GetComponent<PrototypeGoal>();goal.CheckPlayer();Check(goal.IsReached,"Original goal reference still works");
  Check(respawn.RespawnCount==originalResets,"No normal fall or route test caused automatic respawn");
  var protectedFiles=Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(CandyWorldArtBuilder.Root+"/Backups/ProtectedHashes.json"));bool unchanged=true;using(var sha=System.Security.Cryptography.SHA256.Create())foreach(var pair in protectedFiles)if(!File.Exists(pair.Key)||BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(pair.Key)))!=pair.Value)unchanged=false;Check(unchanged,"Protected assets and gameplay scripts hashes unchanged");
  Check(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)=="Assets/CandyKingdom/Motion/Controllers/CandyMotion_Test.controller","Animator controller unchanged");
  Check(Mathf.Abs(player.GetComponent<CharacterController>().height-1.889099f)<.0001f,"CharacterController height unchanged");
  Check(!animator.applyRootMotion,"Root motion remains disabled");Check(GameObject.Find("DistantScenery_NotPlayable").GetComponentsInChildren<Collider>().Length==0,"Distant scenery has no gameplay colliders");
  motor.Teleport(new Vector3(0,.12f,0),Quaternion.identity);
 }catch(Exception e){Check(false,e.ToString());}
 lines.Insert(0,"Dream World Play tests: "+passed+" passed, "+failed+" failed; actual CharacterController/Motor/Animator, dt=.02; original respawn Update exercised.");
 File.WriteAllLines("Library/DreamWorldTests.txt",lines);Debug.Log(string.Join("\n",lines));EditorApplication.isPlaying=false;
 }
}
}