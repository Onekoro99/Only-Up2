using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace CameraUpgrade.Editor {
public static class CameraUpgradeSetup {
 public const string Root="Assets/CameraUpgrade";
 public const string ScenePath="Assets/CandyKingdom/Scenes/CandyTown_EnclosedWorld.unity";
 public const string ScriptPath="Assets/OnlyUpPrototype/Runtime/ThirdPersonOrbitCamera.cs";
 static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;string parent=Path.GetDirectoryName(p).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(p));}
 static string PathOf(Transform t){return t.parent?PathOf(t.parent)+"/"+t.name+"#"+t.GetSiblingIndex():t.name+"#"+t.GetSiblingIndex();}
 static float[] V(Vector3 p){return new[]{p.x,p.y,p.z};}
 public static string WorldFingerprint(){
  var list=new List<object>();var scene=SceneManager.GetActiveScene();foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true)){
   if(t.GetComponent<Camera>())continue;
   var cols=new List<object>();foreach(var c in t.GetComponents<Collider>()){string shape="";var box=c as BoxCollider;var mesh=c as MeshCollider;var cc=c as CharacterController;var capsule=c as CapsuleCollider;
    if(box)shape="box:"+box.center.ToString("F6")+box.size.ToString("F6");
    if(mesh)shape="mesh:"+AssetDatabase.GetAssetPath(mesh.sharedMesh)+":"+mesh.convex+":"+mesh.cookingOptions;
    if(cc)shape="cc:"+cc.center.ToString("F6")+":"+cc.height+":"+cc.radius+":"+cc.skinWidth+":"+cc.stepOffset+":"+cc.slopeLimit+":"+cc.minMoveDistance;
    if(capsule)shape="capsule:"+capsule.center.ToString("F6")+":"+capsule.radius+":"+capsule.height+":"+capsule.direction;
    cols.Add(new{type=c.GetType().FullName,enabled=c.enabled,trigger=c.isTrigger,shape=shape});
   }
   var components=new List<string>();foreach(var c in t.GetComponents<Component>())components.Add(c?c.GetType().FullName:"missing");
   list.Add(new{path=PathOf(t),position=V(t.localPosition),rotation=new[]{t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w},scale=V(t.localScale),active=t.gameObject.activeSelf,layer=t.gameObject.layer,tag=t.tag,components=components,colliders=cols});
  }
  return Newtonsoft.Json.JsonConvert.SerializeObject(list);
 }
 [MenuItem("Tools/Camera Upgrade/Backup Current Camera")]
 public static void Backup(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Stop Play and save scene first.");
  Folder(Root+"/Backups");string stamp=DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
  if(!AssetDatabase.CopyAsset(SceneManager.GetActiveScene().path,Root+"/Backups/Scene_"+stamp+".unity"))throw new Exception("Scene backup failed.");
  File.Copy(ScriptPath,Root+"/Backups/ThirdPersonOrbitCamera_"+stamp+".cs.txt");
  File.WriteAllText(Root+"/Backups/WorldFingerprint.json",WorldFingerprint());
  var hashes=new Dictionary<string,string>();using(var sha=System.Security.Cryptography.SHA256.Create())foreach(var file in Directory.GetFiles("Assets","*",SearchOption.AllDirectories)){
   string p=file.Replace('\\','/');if(p.StartsWith(Root+"/")||p==ScriptPath||p==SceneManager.GetActiveScene().path)continue;
   hashes[p]=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file)));
  }
  using(var sha=System.Security.Cryptography.SHA256.Create())hashes["ProjectSettings/InputManager.asset"]=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes("ProjectSettings/InputManager.asset")));
  File.WriteAllText(Root+"/Backups/ProtectedAssets.json",Newtonsoft.Json.JsonConvert.SerializeObject(hashes));
 }
 [MenuItem("Tools/Camera Upgrade/Apply Camera Defaults")]
 public static void ApplyDefaults(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first.");
  var camera=Camera.main;var orbit=camera.GetComponent<OnlyUpPrototype.ThirdPersonOrbitCamera>();if(!orbit)throw new Exception("Orbit camera missing.");
  var so=new SerializedObject(orbit);var values=new Dictionary<string,float>{{"distance",3},{"minCameraDistance",1.5f},{"maxCameraDistance",7},{"cameraHeight",1.5f},{"targetHeight",1.2f},{"sensitivity",3},{"zoomSpeed",10},{"zoomSmoothness",12},{"followSmoothness",20},{"rotationSmoothness",18},{"returnSmoothness",10},{"minPitch",-35},{"maxPitch",70},{"pitch",Mathf.Asin(.3f/3)*Mathf.Rad2Deg},{"yaw",0},{"collisionRadius",.22f}};
  foreach(var pair in values){var p=so.FindProperty(pair.Key);if(p==null)throw new Exception("Camera field missing: "+pair.Key);p.floatValue=pair.Value;}
  so.ApplyModifiedPropertiesWithoutUndo();orbit.SnapToTarget();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  if(WorldFingerprint()!=File.ReadAllText(Root+"/Backups/WorldFingerprint.json"))throw new Exception("Non-camera scene data changed.");
 }
}
}