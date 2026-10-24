using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using OnlyUpPrototype;
using CandyKingdom.Motion;
namespace CandyKingdom.Boundary.Editor {
[InitializeOnLoad]
public static class CandyBoundaryTests {
 static PrototypePlayerMotor motor;static CandyMotionDriver driver;static Animator animator;static StageRespawn respawn;static FallTracker falls;static ThirdPersonOrbitCamera orbit;
 static List<string> lines;static int passed,failed;static MeshCollider rim;
 static CandyBoundaryTests(){EditorApplication.update+=Poll;}
 [MenuItem("Tools/Candy Kingdom/Run Enclosed World Tests")]
 public static void Run(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first.");
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=CandyBoundaryBuilder.ScenePath)throw new Exception("Open enclosed world scene.");
  SessionState.SetBool("CandyBoundary.Test",true);EditorApplication.isPlaying=true;
 }
 static void Poll(){if(EditorApplication.isPlaying&&Time.frameCount>=1&&SessionState.GetBool("CandyBoundary.Test",false)){SessionState.SetBool("CandyBoundary.Test",false);Test();}}
 static void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);if(ok)passed++;else failed++;}
 static Vector2 Input(Vector3 world){var f=Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up).normalized;return new Vector2(Vector3.Dot(world,Vector3.Cross(Vector3.up,f)),Vector3.Dot(world,f));}
 static void Step(Vector3 direction,bool jump=false){motor.Simulate(Input(direction),jump,.02f);driver.Tick(.02f);animator.Update(.02f);falls.Sample(motor.IsGrounded,motor.VerticalSpeed);typeof(StageRespawn).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(respawn,null);}
 static void Place(Vector3 p){motor.Teleport(p,Quaternion.identity);driver.ResetTracking();falls.ResetTracking();for(int i=0;i<35;i++)Step(Vector3.zero);}
 static void PlaceOnTerrain(Vector3 p){RaycastHit hit;if(Physics.Raycast(new Vector3(p.x,70,p.z),Vector3.down,out hit,90,~0,QueryTriggerInteraction.Ignore))p.y=hit.point.y+.13f;Place(p);}
 static void CameraPose(float yaw,float pitch){typeof(ThirdPersonOrbitCamera).GetField("yaw",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(orbit,yaw);typeof(ThirdPersonOrbitCamera).GetField("pitch",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(orbit,pitch);orbit.SnapToTarget();}
 static bool CameraClear(){foreach(var c in Physics.OverlapSphere(Camera.main.transform.position,.20f,~0,QueryTriggerInteraction.Ignore))if(c.transform!=motor.transform&&!c.transform.IsChildOf(motor.transform))return false;return true;}
 static void Capture360(string name,Vector3 location){
  Place(location);var ghost=new GameObject("BoundaryCaptureCamera");var camera=ghost.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
  var data=ghost.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=true;
  var rt=new RenderTexture(320,180,24);var sheet=new Texture2D(1280,720,TextureFormat.RGB24,false);
  try{for(int i=0;i<16;i++){CameraPose(i*22.5f,-8);camera.transform.SetPositionAndRotation(Camera.main.transform.position,Camera.main.transform.rotation);camera.targetTexture=rt;camera.aspect=16f/9;camera.Render();RenderTexture.active=rt;sheet.ReadPixels(new Rect(0,0,320,180),(i%4)*320,(3-i/4)*180);}sheet.Apply();File.WriteAllBytes("Library/"+name+".png",sheet.EncodeToPNG());}
  finally{RenderTexture.active=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(sheet);UnityEngine.Object.DestroyImmediate(ghost);}
 }
 static void Test(){
  lines=new List<string>();passed=failed=0;
  try{
   var player=GameObject.Find("model_chara");motor=player.GetComponent<PrototypePlayerMotor>();driver=player.GetComponent<CandyMotionDriver>();animator=player.GetComponent<Animator>();respawn=player.GetComponent<StageRespawn>();falls=player.GetComponent<FallTracker>();orbit=Camera.main.GetComponent<ThirdPersonOrbitCamera>();
   motor.enabled=false;driver.enabled=false;falls.enabled=false;orbit.enabled=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;rim=GameObject.Find("BoundaryWorldGenerated").GetComponent<CandyBoundaryMarker>().continuousRim;int resets=respawn.RespawnCount;
   int samples=0,missing=0;for(float x=-54;x<=54;x+=4)for(float z=-28;z<=164;z+=4){var p=new Vector3(x,0,z);if(!CandyBoundaryBuilder.Inside(p))continue;samples++;RaycastHit hit;if(!Physics.Raycast(new Vector3(x,80,z),Vector3.down,out hit,90,~0,QueryTriggerInteraction.Ignore))missing++;}
   Check(missing==0,"Interior ground ray coverage "+samples+" samples, missing="+missing);
   int seamMissing=0;for(int k=0;k<720;k++){float a=k*Mathf.PI*2/720;var p=CandyBoundaryBuilder.Outline(a,0);RaycastHit hit;if(!rim.Raycast(new Ray(new Vector3(p.x,80,p.z),Vector3.down),out hit,100))seamMissing++;}
   Check(seamMissing==0,"Continuous rim apron 720 seam samples, missing="+seamMissing);
   var near=rim.sharedMesh;var far=AssetDatabase.LoadAssetAtPath<Mesh>(CandyBoundaryBuilder.Root+"/Meshes/ConnectedOuterLandscape.asset");var nv=near.vertices;var fv=far.vertices;bool aligned=true,finite=true;for(int k=0;k<CandyBoundaryBuilder.Segments;k++)if(Vector3.Distance(nv[13*CandyBoundaryBuilder.Segments+k],fv[k])>.0001f)aligned=false;
   foreach(var m in new[]{near,far})foreach(var v in m.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z))finite=false;
   Check(aligned,"Physical rim and distant landscape share the exact join");Check(finite,"Terrain vertices are finite");
   float minimum=999;for(int k=0;k<256;k++)minimum=Mathf.Min(minimum,CandyBoundaryBuilder.WallTop(k*Mathf.PI*2/256));
   Check(minimum>12+1.225f+.25f,"Lowest wall top "+minimum.ToString("F2")+" exceeds highest route + jump + step");
   foreach(var direction in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right}){Place(new Vector3(0,.12f,0));bool safe=true;for(int i=0;i<400;i++){Step(direction);if(player.transform.position.y<-4||!CandyBoundaryBuilder.Inside(player.transform.position))safe=false;}Check(safe,"Start cardinal movement "+direction+" remains supported and enclosed");}
   for(int k=0;k<16;k++){float a=k*Mathf.PI*2/16;var p=CandyBoundaryBuilder.Outline(a,-4);PlaceOnTerrain(p);bool safe=true;var outward=CandyBoundaryBuilder.Outward(a);for(int i=0;i<240;i++){Step(outward,motor.IsGrounded);if(!CandyBoundaryBuilder.Inside(player.transform.position)||player.transform.position.y<-4)safe=false;}Check(safe,"Boundary outward movement + repeated jumps "+(k*22.5f)+" deg");}
   Place(new Vector3(0,12.12f,152));bool highSafe=true;for(int i=0;i<300;i++){Step(Vector3.forward,motor.IsGrounded);if(!CandyBoundaryBuilder.Inside(player.transform.position)||player.transform.position.y<-4)highSafe=false;}Check(highSafe,"Highest castle-side ledge jump cannot escape");
   for(int k=0;k<16;k++){float a=k*Mathf.PI*2/16;var p=CandyBoundaryBuilder.Outline(a,-.8f);PlaceOnTerrain(p);var n=CandyBoundaryBuilder.Outward(a);float yaw=Mathf.Atan2(-n.x,-n.z)*Mathf.Rad2Deg;CameraPose(yaw,22);Check(CameraClear()&&orbit.CurrentDistance<5.45f,"Orbit camera collides with visible rim "+(k*22.5f)+" deg");}
   foreach(var location in new[]{new Vector3(0,.12f,0),new Vector3(0,12.12f,135),new Vector3(-13,8.92f,26)}){int open=0;for(int i=0;i<72;i++){float a=i*Mathf.PI*2/72;RaycastHit hit;if(!rim.Raycast(new Ray(location+Vector3.up*1.1f,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))),out hit,240))open++;}Check(open==0,"360-degree horizon terrain coverage from "+location+", open="+open);}
   foreach(var location in new[]{new Vector3(36,16,5),new Vector3(-35,16,45),new Vector3(20,20,145)}){Place(location);bool landed=motor.IsGrounded;for(int i=0;i<180&&!landed;i++){Step(Vector3.zero);landed=motor.IsGrounded;}Check(landed&&player.transform.position.y>-4,"High fall lands on lower sweets ground "+location);}
   Check(respawn.RespawnCount==resets,"No boundary movement, jump or normal fall caused respawn");
   var hashes=Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(CandyBoundaryBuilder.Root+"/Backups/ProtectedHashes.json"));bool unchanged=true;using(var sha=System.Security.Cryptography.SHA256.Create())foreach(var pair in hashes)if(!File.Exists(pair.Key)||BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(pair.Key)))!=pair.Value)unchanged=false;Check(unchanged,"Protected models, controls, animation, input and source scene unchanged");
   Check(GameObject.Find("BoundaryWorldGenerated").GetComponentsInChildren<Collider>().Length==1,"One visible terrain collider; no invisible box wall or decorative colliders");
   Capture360("Boundary360_Start",new Vector3(0,.12f,0));Capture360("Boundary360_Castle",new Vector3(0,12.12f,135));CameraPose(0,22);
  }catch(Exception e){Check(false,e.ToString());}
  lines.Insert(0,"Enclosed world tests: "+passed+" passed, "+failed+" failed; actual Motor/CharacterController/Animator/Orbit camera; dt=.02.");
  File.WriteAllLines("Library/CandyBoundaryTests.txt",lines);Debug.Log(string.Join("\n",lines));
  // Reuse the existing full A/B traversal suite against this actual scene.
  try{typeof(CandyKingdom.Shortcake.Editor.ShortcakeRouteTests).GetMethod("Test",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);File.Copy("Library/ShortcakeRouteTests.txt","Library/EnclosedWorldRouteTests.txt",true);}catch(Exception e){File.AppendAllText("Library/CandyBoundaryTests.txt","\nFAIL Existing route suite: "+e);EditorApplication.isPlaying=false;}
 }
}
}