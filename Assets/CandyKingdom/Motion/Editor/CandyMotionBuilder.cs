using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace CandyKingdom.Motion.Editor {
public static class CandyMotionBuilder {
 public const string Root="Assets/CandyKingdom/Motion";
 public const string ScenePath="Assets/CandyKingdom/Scenes/CandyTown_MotionTest.unity";
 static GameObject rig;static Transform[] bones;static Vector3[] positions,scales;static Quaternion[] rotations;
 static Dictionary<string,Transform> named;static Dictionary<string,Quaternion> worldBind;
 static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;Folder(Path.GetDirectoryName(p).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(p).Replace('\\','/'),Path.GetFileName(p));}
 static void ResetPose(){for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];bones[i].localScale=scales[i];}}
 static void Rotate(string name,Vector3 euler){var b=named[name];b.rotation=Quaternion.Euler(euler)*b.rotation;}
 static void IK(string upper,string lower,string end,Vector3 target,Vector3 pole){
  Transform a=named[upper],b=named[lower],c=named[end];Vector3 start=a.position;float l1=Vector3.Distance(start,b.position),l2=Vector3.Distance(b.position,c.position);
  Vector3 delta=target-start;float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(l1-l2)+.001f,l1+l2-.002f);Vector3 forward=delta.normalized;
  Vector3 bend=Vector3.ProjectOnPlane(pole-start,forward).normalized;
  float x=(l1*l1-l2*l2+d*d)/(2*d);float h=Mathf.Sqrt(Mathf.Max(0,l1*l1-x*x));Vector3 elbow=start+forward*x+bend*h;
  a.rotation=Quaternion.FromToRotation(b.position-start,elbow-start)*a.rotation;
  b.rotation=Quaternion.FromToRotation(c.position-b.position,target-b.position)*b.rotation;
 }
 static Vector3 RunFoot(float phase,float x){float u=Mathf.Repeat(phase,1);if(u<.5f)return new Vector3(x,.08f,.315f-1.26f*u);float k=(u-.5f)*2;return new Vector3(x,.08f+.12f*Mathf.Sin(k*Mathf.PI),Mathf.Lerp(-.315f,.315f,k));}
 static void Pose(string kind,float t,float duration){
  ResetPose();float phase=t/duration*Mathf.PI*2;float wave=Mathf.Sin(phase),swing=0,lean=4,hipsDrop=0,footLift=.1f;
  Vector3 leftHand=new Vector3(-.37f,1.04f,.12f),rightHand=new Vector3(.37f,1.04f,.12f);
  Vector3 leftFoot=new Vector3(-.12f,.14f,-.035f),rightFoot=new Vector3(.12f,.14f,-.035f);
  if(kind=="Run"){
   swing=Mathf.Cos(phase);hipsDrop=.075f+.018f*(1-Mathf.Cos(phase*2));lean=5;
   leftFoot=RunFoot(t/duration,-.12f);
   rightFoot=RunFoot(t/duration+.5f,.12f);
   leftHand=new Vector3(-.26f,.88f,-.22f*swing);rightHand=new Vector3(.26f,.88f,.22f*swing);
  }else if(kind=="Jump"){
   float prep=1-Mathf.SmoothStep(0,1,t/.10f);hipsDrop=.035f*prep;
   footLift=Mathf.Lerp(.08f,.19f,Mathf.SmoothStep(0,1,t/.18f));
   leftFoot=new Vector3(-.12f,footLift,-.035f);rightFoot=new Vector3(.12f,footLift,-.035f);
   leftHand=new Vector3(-.38f,1.06f,.10f);rightHand=new Vector3(.38f,1.06f,.10f);
  }else if(kind=="LongFall"){
   lean=8+wave*.8f;leftHand=new Vector3(-.10f,.96f,.25f);rightHand=new Vector3(.49f,1.12f,.13f+.015f*wave);
   leftFoot=new Vector3(-.12f,.17f,-.03f);rightFoot=new Vector3(.14f,.15f,-.04f);
   Rotate("Head",new Vector3(-3,0,2+wave));
  }else if(kind=="Landing"){
   float bend=Mathf.Sin(Mathf.Clamp01(t/duration)*Mathf.PI);hipsDrop=.075f*bend;lean=4+3*bend;
   leftFoot=new Vector3(-.12f,.08f,0);rightFoot=new Vector3(.12f,.08f,0);
   leftHand=new Vector3(-.32f,1.00f,.12f);rightHand=new Vector3(.32f,1.00f,.12f);
  }
  named["Hips"].position+=Vector3.up*(-hipsDrop);Rotate("Hips",new Vector3(0,kind=="Run"?2*swing:0,0));
  Rotate("Spine",new Vector3(lean,kind=="Run"?-3*swing:0,kind=="Run"?1.2f*swing:0));
  IK("LeftUpLeg","LeftLeg","LeftFoot",leftFoot,new Vector3(-.12f,.55f,.65f));
  IK("RightUpLeg","RightLeg","RightFoot",rightFoot,new Vector3(.12f,.55f,.65f));
  named["LeftFoot"].rotation=worldBind["LeftFoot"];named["RightFoot"].rotation=worldBind["RightFoot"];
  if(kind=="Run"){Rotate("LeftShoulder",new Vector3(0,2*swing,0));Rotate("RightShoulder",new Vector3(0,-2*swing,0));}
  IK("LeftArm","LeftForeArm","LeftHand",leftHand,new Vector3(-.65f,1.05f,-.32f));
  IK("RightArm","RightForeArm","RightHand",rightHand,new Vector3(.65f,1.05f,-.32f));
 }
 static AnimationClip Clip(string kind,float duration,bool loop){
  var clip=new AnimationClip();clip.name=kind+"_Candy";clip.frameRate=60;
  var paths=new Dictionary<Transform,string>();var curves=new Dictionary<Transform,AnimationCurve[]>();
  foreach(var b in bones){paths[b]=AnimationUtility.CalculateTransformPath(b,rig.transform);var c=new AnimationCurve[7];for(int k=0;k<7;k++)c[k]=new AnimationCurve();curves[b]=c;}
  int frames=Mathf.RoundToInt(duration*60);
  var previous=new Dictionary<Transform,Quaternion>();
  for(int f=0;f<=frames;f++){float t=duration*f/frames;Pose(kind,t,duration);foreach(var b in bones){
   var p=b.localPosition;var q=b.localRotation;Quaternion last;if(previous.TryGetValue(b,out last)&&Quaternion.Dot(last,q)<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);previous[b]=q;
   float[] v={p.x,p.y,p.z,q.x,q.y,q.z,q.w};for(int k=0;k<7;k++)curves[b][k].AddKey(t,v[k]);
  }}
  string[] properties={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
  foreach(var b in bones)for(int k=0;k<7;k++){var c=curves[b][k];for(int i=0;i<c.length;i++){AnimationUtility.SetKeyLeftTangentMode(c,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(c,i,AnimationUtility.TangentMode.Linear);}AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(paths[b],typeof(Transform),properties[k]),c);}
  clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
  var destination=Root+"/Clips/"+clip.name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(destination);if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);AssetDatabase.SaveAssetIfDirty(existing);return existing;}AssetDatabase.CreateAsset(clip,destination);return clip;
 }
 static AnimatorStateTransition Transition(AnimatorState a,AnimatorState b,float blend=.08f){var t=a.AddTransition(b);t.hasExitTime=false;t.hasFixedDuration=true;t.duration=blend;t.canTransitionToSelf=false;return t;}
 static void Condition(AnimatorStateTransition t,AnimatorConditionMode m,string p,float n=0){t.AddCondition(m,n,p);}
 public static void RebuildOwnClips(){rig=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/model_chara.fbx"));rig.hideFlags=HideFlags.HideAndDontSave;rig.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);bones=rig.GetComponentsInChildren<Transform>();positions=new Vector3[bones.Length];scales=new Vector3[bones.Length];rotations=new Quaternion[bones.Length];named=new Dictionary<string,Transform>();worldBind=new Dictionary<string,Quaternion>();for(int i=0;i<bones.Length;i++){var b=bones[i];positions[i]=b.localPosition;scales[i]=b.localScale;rotations[i]=b.localRotation;named[b.name]=b;worldBind[b.name]=b.rotation;}try{Clip("Run",.36f,true);Clip("Jump",.35f,false);Clip("ShortFall",.60f,true);Clip("LongFall",.80f,true);Clip("Landing",.20f,false);}finally{UnityEngine.Object.DestroyImmediate(rig);}}
 [MenuItem("Tools/Candy Kingdom/Create Motion Test")]
 public static void Build(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Stop Play mode and save current scene first.");
  if(AssetDatabase.LoadMainAssetAtPath(ScenePath))throw new Exception("Motion test already exists; refusing overwrite.");
  Folder(Root+"/Clips");Folder(Root+"/Controllers");Folder(Root+"/Backups");
  string stamp=DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
  if(!AssetDatabase.CopyAsset("Assets/Characters/PlayerAnimator.controller",Root+"/Backups/PlayerAnimator_"+stamp+".controller"))throw new Exception("Controller backup failed.");
  if(!AssetDatabase.CopyAsset("Assets/CandyKingdom/Scenes/CandyTown_Test.unity",Root+"/Backups/CandyTown_"+stamp+".unity"))throw new Exception("Scene backup failed.");
  rig=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/model_chara.fbx"));rig.hideFlags=HideFlags.HideAndDontSave;rig.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
  bones=rig.GetComponentsInChildren<Transform>();positions=new Vector3[bones.Length];scales=new Vector3[bones.Length];rotations=new Quaternion[bones.Length];named=new Dictionary<string,Transform>();worldBind=new Dictionary<string,Quaternion>();
  for(int i=0;i<bones.Length;i++){var b=bones[i];positions[i]=b.localPosition;scales[i]=b.localScale;rotations[i]=b.localRotation;named[b.name]=b;worldBind[b.name]=b.rotation;}
  AnimationClip run,jump,shortFall,longFall,landing;
  try{run=Clip("Run",.36f,true);jump=Clip("Jump",.35f,false);shortFall=Clip("ShortFall",.60f,true);longFall=Clip("LongFall",.80f,true);landing=Clip("Landing",.20f,false);}
  finally{UnityEngine.Object.DestroyImmediate(rig);}
  var controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"/Controllers/CandyMotion_Test.controller");
  controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("IsGrounded",AnimatorControllerParameterType.Bool);controller.AddParameter("VerticalSpeed",AnimatorControllerParameterType.Float);controller.AddParameter("LongFalling",AnimatorControllerParameterType.Bool);controller.AddParameter("RunRate",AnimatorControllerParameterType.Float);
  var sm=controller.layers[0].stateMachine;var idle=sm.AddState("Idle",new Vector3(0,0));idle.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Characters/Animation/Fixed/Idle_Fixed.anim");sm.defaultState=idle;
  var r=sm.AddState("Run",new Vector3(240,0));r.motion=run;r.speedParameter="RunRate";r.speedParameterActive=true;
  var j=sm.AddState("Jump",new Vector3(0,150));j.motion=jump;
  var sf=sm.AddState("Short Fall",new Vector3(240,150));sf.motion=shortFall;
  var lf=sm.AddState("Long Fall",new Vector3(480,150));lf.motion=longFall;
  var land=sm.AddState("Landing",new Vector3(240,300));land.motion=landing;
  foreach(var state in new[]{idle,r,j,sf,lf,land})state.writeDefaultValues=true;
  var tr=Transition(idle,r);Condition(tr,AnimatorConditionMode.Greater,"Speed",.1f);Condition(tr,AnimatorConditionMode.If,"IsGrounded");
  tr=Transition(r,idle);Condition(tr,AnimatorConditionMode.Less,"Speed",.1f);Condition(tr,AnimatorConditionMode.If,"IsGrounded");
  foreach(var state in new[]{idle,r,land}){tr=Transition(state,j,.045f);Condition(tr,AnimatorConditionMode.IfNot,"IsGrounded");Condition(tr,AnimatorConditionMode.Greater,"VerticalSpeed",0);tr=Transition(state,sf);Condition(tr,AnimatorConditionMode.IfNot,"IsGrounded");Condition(tr,AnimatorConditionMode.Less,"VerticalSpeed",.001f);}
  tr=Transition(j,sf);Condition(tr,AnimatorConditionMode.IfNot,"IsGrounded");Condition(tr,AnimatorConditionMode.Less,"VerticalSpeed",.001f);
  tr=Transition(sf,lf,.14f);Condition(tr,AnimatorConditionMode.If,"LongFalling");Condition(tr,AnimatorConditionMode.IfNot,"IsGrounded");
  foreach(var state in new[]{j,sf,lf}){tr=Transition(state,land,.045f);Condition(tr,AnimatorConditionMode.If,"IsGrounded");tr=Transition(state,j,.04f);Condition(tr,AnimatorConditionMode.IfNot,"IsGrounded");Condition(tr,AnimatorConditionMode.Greater,"VerticalSpeed",0);}
  tr=Transition(land,r,.06f);tr.hasExitTime=true;tr.exitTime=.9f;Condition(tr,AnimatorConditionMode.If,"IsGrounded");Condition(tr,AnimatorConditionMode.Greater,"Speed",.1f);
  tr=Transition(land,idle,.06f);tr.hasExitTime=true;tr.exitTime=.9f;Condition(tr,AnimatorConditionMode.If,"IsGrounded");Condition(tr,AnimatorConditionMode.Less,"Speed",.1f);
  AssetDatabase.SaveAssetIfDirty(controller);
  if(!AssetDatabase.CopyAsset("Assets/CandyKingdom/Scenes/CandyTown_Test.unity",ScenePath))throw new Exception("Scene copy failed.");
  var scene=EditorSceneManager.OpenScene(ScenePath);var player=GameObject.Find("model_chara");player.GetComponent<Animator>().runtimeAnimatorController=controller;player.AddComponent<CandyMotionDriver>();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  Debug.Log("Candy motion test created with five new clips; original assets unchanged.");
 }
}
}