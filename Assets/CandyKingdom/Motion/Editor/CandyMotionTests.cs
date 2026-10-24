using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OnlyUpPrototype;
namespace CandyKingdom.Motion.Editor {
[InitializeOnLoad]
public static class CandyMotionTests {
 static PrototypePlayerMotor motor;static CandyMotionDriver driver;static Animator animator;
 static List<string> log;static int passed,failed;
 static CandyMotionTests(){EditorApplication.update+=Poll;}
 [MenuItem("Tools/Candy Kingdom/Run Motion Tests")]
 public static void Run(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode first.");
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=CandyMotionBuilder.ScenePath)throw new Exception("Open motion test scene.");
  SessionState.SetBool("CandyMotion.Test",true);EditorApplication.isPlaying=true;
 }
 static void Poll(){if(EditorApplication.isPlaying&&Time.frameCount>=1&&SessionState.GetBool("CandyMotion.Test",false)){SessionState.SetBool("CandyMotion.Test",false);Test();}}
 static void Check(bool ok,string message){log.Add((ok?"PASS ":"FAIL ")+message);if(ok)passed++;else failed++;}
 static bool State(string name){return animator.GetCurrentAnimatorStateInfo(0).IsName(name)||animator.IsInTransition(0)&&animator.GetNextAnimatorStateInfo(0).IsName(name);}
 static void Step(Vector2 input,bool jump=false){motor.Simulate(input,jump,.02f);driver.Tick(.02f);animator.Update(.02f);}
 static void Settle(Vector3 p){motor.Teleport(p,Quaternion.identity);driver.ResetTracking();animator.Rebind();for(int i=0;i<25;i++)Step(Vector2.zero);}
 static void Test(){
  log=new List<string>();passed=failed=0;
  try{
   var player=GameObject.Find("model_chara");motor=player.GetComponent<PrototypePlayerMotor>();driver=player.GetComponent<CandyMotionDriver>();animator=player.GetComponent<Animator>();
   motor.enabled=false;driver.enabled=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   foreach(var c in player.GetComponents<MonoBehaviour>())if(c is StageRespawn||c is FallTracker)c.enabled=false;
   var camera=Camera.main.GetComponent<ThirdPersonOrbitCamera>();if(camera)camera.enabled=false;
   Vector3 start=player.transform.position;Settle(start);
   Check(State("Idle"),"Idle on ground");
   for(int i=0;i<12;i++)Step(Vector2.up);
   Check(State("Run"),"Idle to Run");
   Check(motor.HorizontalVelocity.magnitude>3.4f,"Motor movement speed preserved");
   Check(!animator.applyRootMotion,"Root motion remains disabled");
   Settle(start);Step(Vector2.zero,true);
   for(int i=0;i<5;i++)Step(Vector2.zero);
   Check(State("Jump")&&motor.VerticalSpeed>0,"Jump during ascent");
   bool shortSeen=false,longSeen=false,landingSeen=false;
   for(int i=0;i<70;i++){Step(Vector2.zero);shortSeen|=State("Short Fall");longSeen|=State("Long Fall");landingSeen|=State("Landing");}
   Check(shortSeen,"Jump to Short Fall");Check(!longSeen,"Normal jump does not use Long Fall");Check(landingSeen,"Landing on ground contact");Check(State("Idle"),"Landing returns to Idle");
   Settle(start);motor.Teleport(new Vector3(0,12,0),Quaternion.identity);driver.ResetTracking();
   bool fallDirect=false;longSeen=false;landingSeen=false;
   for(int i=0;i<100;i++){Step(Vector2.zero);if(i==8)fallDirect=State("Short Fall");longSeen|=State("Long Fall");landingSeen|=State("Landing");}
   Check(fallDirect,"Descending without Jump enters Short Fall");Check(longSeen,"High fall enters Long Fall");Check(landingSeen,"High fall lands");Check(State("Idle"),"High fall returns to Idle");
   Settle(start);bool walkedOff=false;for(int i=0;i<100;i++){Step(Vector2.down);if(!motor.IsGrounded){walkedOff=true;break;}}for(int i=0;i<8;i++)Step(Vector2.down);Check(walkedOff&&motor.VerticalSpeed<0&&State("Short Fall"),"Walking off a ledge directly enters Short Fall");
   Settle(start);Step(Vector2.zero,true);bool didLand=false;
   for(int i=0;i<60;i++){Step(Vector2.zero);if(motor.IsGrounded){didLand=true;break;}}
   Step(Vector2.zero,true);for(int i=0;i<4;i++)Step(Vector2.zero);
   Check(didLand&&motor.VerticalSpeed>0&&State("Jump"),"Immediate repeated jump overrides Landing");
   Settle(start);Step(Vector2.zero,true);
   for(int i=0;i<60;i++){Step(Vector2.zero);if(motor.IsGrounded)break;}
   for(int i=0;i<18;i++)Step(Vector2.up);
   Check(State("Run"),"Landing returns to Run while moving");
   Check(motor.HorizontalVelocity.magnitude>3.4f,"Landing does not lock movement");
   Settle(start);driver.LongFallDelay=.2f;motor.Teleport(new Vector3(0,8,0),Quaternion.identity);driver.ResetTracking();
   for(int i=0;i<17;i++)Step(Vector2.zero);
   Check(State("Long Fall"),"Inspector fall delay is honored");driver.LongFallDelay=.65f;
   var controller=animator.runtimeAnimatorController;int clips=0;
   foreach(var clip in controller.animationClips){if(clip.name=="Idle_Fixed")continue;clips++;bool valid=true;foreach(var b in AnimationUtility.GetCurveBindings(clip))if(b.path!=""&&!player.transform.Find(b.path))valid=false;Check(valid,clip.name+" paths valid");Check(AnimationUtility.GetObjectReferenceCurveBindings(clip).Length==0,clip.name+" has no unmatched object curves");}
   Check(clips==5,"Five independent new clips");
   Check(animator.runtimeAnimatorController!=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Characters/PlayerAnimator.controller"),"Dedicated controller isolated from original");
   motor.Teleport(start,Quaternion.identity);
  }catch(Exception e){Check(false,e.ToString());}
  log.Insert(0,"Candy motion Play mode tests: "+passed+" passed, "+failed+" failed; actual Motor + Animator dt=0.02.");
  File.WriteAllLines("Library/CandyMotionTests.txt",log);Debug.Log(string.Join("\n",log));EditorApplication.isPlaying=false;
 }
}
}