using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OnlyUpPrototype;
namespace CameraUpgrade.Editor {
[InitializeOnLoad] public static class CameraUpgradeTests {
 static List<object> results=new List<object>();
 static ThirdPersonOrbitCamera orbit; static PrototypePlayerMotor motor;
 static CameraUpgradeTests(){EditorApplication.update+=Update;}
 public static void Run(){SessionState.SetInt("CameraUpgradeTest",1);EditorApplication.isPlaying=true;}
 static void Check(string name,bool pass){results.Add(new{name=name,pass=pass});}
 static void Pose(float yaw,float pitch){orbit.ApplyLook(new Vector2(Mathf.DeltaAngle(orbit.Yaw,yaw)/orbit.Sensitivity,(orbit.Pitch-pitch)/orbit.Sensitivity));orbit.SnapToTarget();}
 static void Settle(int n=100){for(int i=0;i<n;i++)orbit.Tick(.02f);}
 static void Input(Vector2 look,float wheel=0,bool esc=false,bool click=false,bool reset=false,bool focus=true,bool ui=false){orbit.ProcessInput(look,wheel,esc,click,reset,focus,ui);}
 static void Save(){Directory.CreateDirectory("Library/CameraUpgradeTests");File.WriteAllText("Library/CameraUpgradeTests/Results.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));}
 static void Update(){
  int phase=SessionState.GetInt("CameraUpgradeTest",0);
  if(phase==0||!Application.isPlaying||Time.frameCount<1)return;
  try{
   if(phase==2){
    if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="TitleScene")return;
    var title=UnityEngine.Object.FindFirstObjectByType<GirlsFall.TitleMenuController>();if(!title)return;
    Check("Title cursor visible and unlocked",Cursor.visible&&Cursor.lockState==CursorLockMode.None);
    Check("Title Continue disabled without test save",!title.continueButton.interactable);
    title.optionsButton.onClick.Invoke();
    Check("Options button opens real menu",title.options.gameObject.activeSelf);
    Check("Options cursor remains usable",Cursor.visible&&Cursor.lockState==CursorLockMode.None);
    Check("Title creates no progress save",!File.Exists(Path.Combine(Path.GetFullPath("Library/CameraUpgradeTests/Storage"),"girls-fall-progress.json")));
    Save();SessionState.SetInt("CameraUpgradeTest",0);EditorApplication.isPlaying=false;return;
   }
   SessionState.SetInt("CameraUpgradeTest",0);
   orbit=UnityEngine.Object.FindFirstObjectByType<ThirdPersonOrbitCamera>();motor=UnityEngine.Object.FindFirstObjectByType<PrototypePlayerMotor>();
   motor.enabled=false;
   foreach(var b in motor.GetComponents<MonoBehaviour>())if(b!=orbit)b.enabled=false;
   motor.Teleport(new Vector3(25,45,25),Quaternion.identity);orbit.ResetZoom();Pose(0,0);
   orbit.CaptureCursor();float y=orbit.Yaw,p=orbit.Pitch;
   Input(new Vector2(2,-1));
   Check("Mouse rotates without RMB",Mathf.Abs(Mathf.DeltaAngle(y,orbit.Yaw)-6)<.01f&&Mathf.Abs(orbit.Pitch-p-3)<.01f);
   Input(Vector2.zero,0,true);y=orbit.Yaw;Input(new Vector2(9,9));
   Check("Escape releases and blocks look",!orbit.IsCursorCaptured&&Cursor.visible&&Cursor.lockState==CursorLockMode.None&&orbit.Yaw==y);
   Input(Vector2.zero,0,false,true,false,true,true);Check("UI click does not recapture",!orbit.IsCursorCaptured);
   Input(new Vector2(100,100),0,false,true);Check("Game click recaptures without warp rotation",orbit.IsCursorCaptured&&orbit.Yaw==y);
   Input(Vector2.zero,0,false,false,false,false);Check("Focus loss releases cursor",!orbit.IsCursorCaptured);
   orbit.CaptureCursor();orbit.SetInputSuspended(true);Input(new Vector2(2,2));Check("Menu suspension blocks look",!orbit.IsCursorCaptured&&orbit.Yaw==y);orbit.SetInputSuspended(false);
   orbit.CaptureCursor();Time.timeScale=0;Input(new Vector2(2,2));Check("Paused game releases cursor",!orbit.IsCursorCaptured);Time.timeScale=1;
   orbit.enabled=false;Check("Disabled camera releases cursor",Cursor.visible&&Cursor.lockState==CursorLockMode.None);orbit.enabled=true;Input(Vector2.zero);Check("Resume auto captures",orbit.IsCursorCaptured);
   orbit.ResetZoom();Pose(0,0);orbit.ApplyZoom(.1f);
   Check("Wheel up sets nearer target independently",Mathf.Abs(orbit.TargetDistance-2)<.01f&&Mathf.Abs(orbit.CurrentDistance-3)<.01f);
   orbit.Tick(.02f);Check("Zoom interpolates",orbit.CurrentDistance>2&&orbit.CurrentDistance<3);Settle();
   Check("Zoom settles to target",Mathf.Abs(orbit.CurrentDistance-2)<.01f);
   orbit.ApplyZoom(-.2f);Check("Wheel down zooms out",Mathf.Abs(orbit.TargetDistance-4)<.01f);
   orbit.ApplyZoom(100);Check("Minimum zoom clamp",orbit.TargetDistance==1.5f);
   orbit.ApplyZoom(-100);Check("Maximum zoom clamp",orbit.TargetDistance==7);
   orbit.CaptureCursor();y=orbit.Yaw;Input(new Vector2(1,0),.1f);Check("Simultaneous orbit and zoom",orbit.Yaw!=y&&orbit.TargetDistance<7);
   Input(Vector2.zero,0,false,false,true);Check("Middle click resets distance",orbit.TargetDistance==3);
   Pose(0,5.73917f);
   Check("Default eye height and distance",Mathf.Abs(orbit.transform.position.y-motor.transform.position.y-1.5f)<.01f&&Mathf.Abs(orbit.CurrentDistance-3)<.01f);
   var cam=orbit.GetComponent<Camera>();var feet=cam.WorldToViewportPoint(motor.transform.position-Vector3.up*.0758f);var head=cam.WorldToViewportPoint(motor.transform.position+Vector3.up*1.8133f);
   Check("Default framing fits full body",feet.z>0&&feet.y>0&&head.y<1&&Mathf.Abs(head.x-.5f)<.02f&&head.y-feet.y>.4f);
   Pose(0,-100);Check("Minimum pitch clamp",Mathf.Abs(orbit.Pitch+35)<.01f);
   Pose(0,100);Check("Maximum pitch clamp",Mathf.Abs(orbit.Pitch-70)<.01f);
   orbit.SetZoomDistance(7);Pose(0,0);
   var wall=new GameObject("Camera test wall");var box=wall.AddComponent<BoxCollider>();box.size=new Vector3(5,5,.4f);wall.transform.position=new Vector3(25,46.2f,22.8f);Physics.SyncTransforms();orbit.Tick(.02f);
   float compressed=orbit.CurrentDistance;Check("Wall compresses distance preserving preference",compressed<2&&orbit.TargetDistance==7&&orbit.IsObstructed);
   orbit.SetZoomDistance(4);Settle();box.enabled=false;Physics.SyncTransforms();orbit.Tick(.02f);
   Check("Obstacle recovery is smooth",orbit.CurrentDistance>compressed&&orbit.CurrentDistance<4&&orbit.TargetDistance==4);Settle();
   Check("Obstacle recovery restores requested zoom",Mathf.Abs(orbit.CurrentDistance-4)<.01f);
   var child=new GameObject("Self collider test");child.transform.SetParent(motor.transform);child.transform.localPosition=new Vector3(0,1.2f,-.8f);child.AddComponent<SphereCollider>().radius=.6f;Physics.SyncTransforms();orbit.SnapToTarget();
   Check("Player child collider ignored",!orbit.IsObstructed&&Mathf.Abs(orbit.CurrentDistance-4)<.01f);UnityEngine.Object.DestroyImmediate(child);
   box.enabled=true;box.size=new Vector3(5,5,.2f);wall.transform.position=new Vector3(25,46.2f,24.6f);Physics.SyncTransforms();orbit.Tick(.02f);
   Check("Extreme obstruction hides body temporarily",orbit.IsPlayerHidden);
   UnityEngine.Object.DestroyImmediate(wall);Physics.SyncTransforms();orbit.SnapToTarget();Check("Body visibility restored",!orbit.IsPlayerHidden);
   motor.Teleport(new Vector3(0,.12f,0),Quaternion.identity);orbit.ResetZoom();Pose(90,5.73917f);
   for(int i=0;i<50;i++){motor.Simulate(Vector2.up,false,.02f);orbit.Tick(.02f);}
   Check("W remains camera relative",motor.transform.position.x>3&&Mathf.Abs(motor.transform.position.z)<.1f);
   motor.Teleport(new Vector3(0,.12f,0),Quaternion.identity);Pose(0,5.73917f);
   for(int i=0;i<10;i++)motor.Simulate(Vector2.zero,false,.02f);
   bool rose=false,fell=false,tracked=true;float maxY=motor.transform.position.y;
   for(int i=0;i<100;i++){motor.Simulate(Vector2.zero,i==0,.02f);orbit.Tick(.02f);rose|=motor.VerticalSpeed>0;fell|=motor.VerticalSpeed<-.1f;maxY=Mathf.Max(maxY,motor.transform.position.y);var v=cam.WorldToViewportPoint(motor.transform.position+Vector3.up*1.2f);tracked&=v.z>0&&Mathf.Abs(v.x-.5f)<.01f&&Mathf.Abs(v.y-.5f)<.01f&&orbit.CurrentDistance<=7.001f;}
   Check("Jump and fall keep player centered",rose&&fell&&tracked&&maxY>.9f);
   motor.Teleport(new Vector3(-10.8f,1.42f,18.8f),Quaternion.identity);orbit.SetZoomDistance(7);Pose(180,0);
   Check("Real cake collision",orbit.IsObstructed&&orbit.CurrentDistance<7&&orbit.TargetDistance==7);
   motor.Teleport(new Vector3(-4.6f,.12f,-2.5f),Quaternion.identity);Pose(180,0);
   Check("Real house collision",orbit.IsObstructed&&orbit.CurrentDistance<7);
   motor.Teleport(new Vector3(0,.12f,0),Quaternion.identity);orbit.ResetZoom();Pose(0,5.73917f);
   var storage=Path.GetFullPath("Library/CameraUpgradeTests/Storage");Directory.CreateDirectory(storage);
   GirlsFall.SaveDataManager.UseStorageForTests(storage);
   var settings=GirlsFall.GameSettingsManager.Instance;settings.UseStorageForTests(storage);settings.Current.mouseSensitivity=2.2f;settings.Apply(false);
   Check("Existing settings sensitivity integration",Mathf.Abs(orbit.Sensitivity-2.2f)<.01f);
   settings.Current.width=Screen.width;settings.Current.height=Screen.height;settings.Current.fullscreen=Screen.fullScreen;
   Save();SessionState.SetInt("CameraUpgradeTest",2);UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
  }catch(Exception ex){results.Add(new{name="Exception",pass=false,error=ex.ToString()});Save();SessionState.SetInt("CameraUpgradeTest",0);EditorApplication.isPlaying=false;}
 }
}
}