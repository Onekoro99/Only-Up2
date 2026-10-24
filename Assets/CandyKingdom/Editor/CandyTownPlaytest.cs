using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OnlyUpPrototype;
namespace CandyKingdom.Editor {
[InitializeOnLoad]
public static class CandyTownPlaytest {
 static CandyTownPlaytest(){EditorApplication.update+=Poll;}
 static void Poll(){ if(EditorApplication.isPlaying && !EditorApplication.isCompiling && UnityEngine.Time.frameCount>=1 && SessionState.GetBool("CandyTown.Test",false)){SessionState.SetBool("CandyTown.Test",false);Test();}}

 [MenuItem("Tools/Candy Kingdom/Run Route Playtest")]
 public static void Run(){
  if(!UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.StartsWith(CandyTownBuilder.Root+"/Scenes/CandyTown_Test"))throw new Exception("Open CandyTown_Test first.");
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  SessionState.SetBool("CandyTown.Test",true);EditorApplication.isPlaying=true;
 }
 static void Changed(PlayModeStateChange state){
  if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("CandyTown.Test",false)){
   SessionState.SetBool("CandyTown.Test",false);EditorApplication.delayCall+=Test;
  }
 }
 static Vector2 InputFor(Vector3 delta,Camera camera){
  Vector3 f=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
  Vector3 r=Vector3.Cross(Vector3.up,f);
  return new Vector2(Vector3.Dot(delta,r),Vector3.Dot(delta,f));
 }
 static void Test(){
  var lines=new List<string>();int pass=0,fail=0;
  Action<bool,string> check=(ok,name)=>{lines.Add((ok?"PASS ":"FAIL ")+name);if(ok)pass++;else fail++;};
  try{
   var player=GameObject.Find("model_chara");var motor=player.GetComponent<PrototypePlayerMotor>();var animator=player.GetComponent<Animator>();var falls=player.GetComponent<FallTracker>();var orbit=Camera.main.GetComponent<ThirdPersonOrbitCamera>();var respawn=player.GetComponent<StageRespawn>();
   motor.enabled=false;falls.enabled=false;respawn.enabled=false;if(orbit)orbit.enabled=false;
   Vector3 start=player.transform.position;Quaternion rotation=player.transform.rotation;
   motor.Teleport(start,rotation);
   for(int k=0;k<30;k++){motor.Simulate(Vector2.zero,false,.02f);animator.Update(.02f);}
   check(motor.IsGrounded,"Start grounded");
   check(!animator.applyRootMotion,"Root motion disabled");
   var stage=GameObject.Find("CandyTown_Generated").transform;
   var targets=new List<Transform>();foreach(Transform t in stage)if(t.name.StartsWith("Route_"))targets.Add(t);
   targets.Sort((a,b)=>string.CompareOrdinal(a.name,b.name));
   var settings=AssetDatabase.LoadAssetAtPath<CandyTownSettings>(CandyTownBuilder.Root+"/Settings/CandyTownSettings.asset"); check(targets.Count==Mathf.Clamp(settings.platformCount,8,30),"Configured route platform count");
   bool all=true;float previousY=0;
   foreach(var t in targets){
    bool jumped=false,air=false,landed=false;
    for(int k=0;k<500;k++){
     Vector3 delta=t.position-player.transform.position;delta.y=0;float distance=delta.magnitude;
     bool launch=!jumped&&motor.IsGrounded&&distance<2.03f;
     if(launch)jumped=true;
     Vector3 direction=distance>.09f?delta.normalized:Vector3.zero;
     motor.Simulate(InputFor(direction,Camera.main),launch,.02f);animator.Update(.02f);
     falls.Sample(motor.IsGrounded,motor.VerticalSpeed);
     if(!motor.IsGrounded&&jumped)air=true;
     if(air&&motor.IsGrounded){
      landed=Mathf.Abs(player.transform.position.y-(t.position.y+.095812f))<.14f&&Vector3.Distance(new Vector3(player.transform.position.x,0,player.transform.position.z),new Vector3(t.position.x,0,t.position.z))<1.4f;
      break;
     }
     if(player.transform.position.y<previousY-1)break;
    }
    check(landed,"Climb "+t.name+" top="+t.position.y.ToString("F2")+" actualY="+player.transform.position.y.ToString("F2"));
    if(!landed){all=false;break;}previousY=t.position.y;
    for(int k=0;k<5;k++){motor.Simulate(Vector2.zero,false,.02f);animator.Update(.02f);}
   }
   check(all,"Continuous start-to-goal route");
   var goal=GameObject.Find("CandyTown_Goal").GetComponent<PrototypeGoal>();goal.CheckPlayer();
   check(goal.IsReached,"Goal reached");
   // Falling onto a real lower island uses the same motor and collision geometry.
   var recovery=GameObject.Find("RecoveryIsland_7");
   Vector3 drop=recovery.transform.position+new Vector3(-2,6,0);
   motor.Teleport(drop,Quaternion.identity);falls.ResetTracking();bool landedOnLower=false;
   for(int k=0;k<250;k++){motor.Simulate(Vector2.zero,false,.02f);animator.Update(.02f);falls.Sample(motor.IsGrounded,motor.VerticalSpeed);if(motor.IsGrounded){landedOnLower=true;break;}}
   check(landedOnLower,"Fall caught by lower island");
   check(falls.LastReport.Distance>3,"Fall distance recorded");
   motor.Teleport(new Vector3(40,-7,0),Quaternion.identity);respawn.Respawn();
   check(Vector3.Distance(player.transform.position,start)<.01f,"Void respawn returns to start");
   check(UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Length==1,"Single original player animator");
   check(!GameObject.Find("PrototypeStage"),"Original stage inactive in copied scene");
   motor.Teleport(start,rotation);
  }catch(Exception e){check(false,e.ToString());}
  lines.Insert(0,"Candy Town actual CharacterController simulation: "+pass+" passed, "+fail+" failed. dt=0.02 seconds.");
  File.WriteAllLines(Path.Combine(Application.dataPath,"../Library/CandyTownPlaytest.txt"),lines);
  Debug.Log(string.Join("\n",lines));EditorApplication.isPlaying=false;
 }
}
}