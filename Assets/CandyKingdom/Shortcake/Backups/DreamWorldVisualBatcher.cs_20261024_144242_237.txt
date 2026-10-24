using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace CandyKingdom.ArtUpgrade.Editor {
public static class DreamWorldVisualBatcher {
 [MenuItem("Tools/Candy Kingdom/Rebuild Dream World Visual Batches")]
 public static void Bake(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().path!=CandyWorldArtBuilder.ScenePath)throw new Exception("Open Dream World in Edit mode.");
  foreach(var name in new[]{"LowestLayer_CookieLandscape","CreamMeadows","CandyTown_HeroArchitecture","PlayableSweetRoute","DistantScenery_NotPlayable"}){
   var root=GameObject.Find(name);if(!root)continue;
   var prior=root.transform.Find("BatchedVisuals");if(prior)UnityEngine.Object.DestroyImmediate(prior.gameObject);
   var groups=new Dictionary<Material,List<CombineInstance>>();var renderers=new List<MeshRenderer>();
   foreach(var mf in root.GetComponentsInChildren<MeshFilter>()){
    var mr=mf.GetComponent<MeshRenderer>();if(!mr||!mf.sharedMesh||mr.sharedMaterials.Length!=1)continue;
    var mat=mr.sharedMaterial;if(!mat)continue;if(!groups.ContainsKey(mat))groups[mat]=new List<CombineInstance>();
    groups[mat].Add(new CombineInstance{mesh=mf.sharedMesh,transform=root.transform.worldToLocalMatrix*mf.transform.localToWorldMatrix});renderers.Add(mr);
   }
   var container=new GameObject("BatchedVisuals");container.transform.SetParent(root.transform,false);
   foreach(var pair in groups){
    string path=CandyWorldArtBuilder.Root+"/Meshes/Batch_"+name+"_"+pair.Key.name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();mesh.name="Batch_"+name+"_"+pair.Key.name;AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
    mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(pair.Value.ToArray(),true,true,false);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
    var g=new GameObject(pair.Key.name);g.transform.SetParent(container.transform,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=pair.Key;g.isStatic=true;
    if(name=="DistantScenery_NotPlayable"){renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;}
   }
   foreach(var mr in renderers)mr.enabled=false;
  }
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
 }
}
}