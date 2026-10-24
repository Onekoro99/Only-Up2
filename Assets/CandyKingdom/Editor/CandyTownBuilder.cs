using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using OnlyUpPrototype;

namespace CandyKingdom.Editor {
public static class CandyTownBuilder {
 public const string Root="Assets/CandyKingdom";
 public const string ScenePath=Root+"/Scenes/CandyTown_Test.unity";
 public static string LastBuildReport;
 static Transform stage;
 static Material pink,blue,white,cookie,chocolate,lavender,gold;
 static System.Random random;
 static void Folder(string p) { if(AssetDatabase.IsValidFolder(p))return; string parent=Path.GetDirectoryName(p).Replace('\\','/'); Folder(parent); AssetDatabase.CreateFolder(parent,Path.GetFileName(p)); }
 static Material Mat(string name,Color color) {
  string path=Root+"/Materials/"+name+".mat";
  Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color; m.SetFloat("_Smoothness",0.35f);AssetDatabase.CreateAsset(m,path);}
  return m;
 }
 static GameObject Shape(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,Transform parent,bool solid=false) {
  var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;
  var c=g.GetComponent<Collider>(); UnityEngine.Object.DestroyImmediate(c);
  if(solid) {if(type==PrimitiveType.Cube)g.AddComponent<BoxCollider>();else {var mesh=g.AddComponent<MeshCollider>();mesh.sharedMesh=g.GetComponent<MeshFilter>().sharedMesh;}}
  g.isStatic=true;return g;
 }
 static GameObject Cube(string n,Vector3 p,Vector3 s,Material m,Transform t,bool solid=false){return Shape(n,PrimitiveType.Cube,p,s,m,t,solid);}
 static void Set(UnityEngine.Object o,string key,UnityEngine.Object value){var so=new SerializedObject(o);so.FindProperty(key).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static void Label(string value,Vector3 p,float size,Transform parent) {
  var g=new GameObject("Sign_"+value);g.transform.SetParent(parent);g.transform.position=p;g.transform.rotation=Quaternion.Euler(90,0,0);
  var text=g.AddComponent<TextMesh>();text.text=value;text.fontSize=64;text.characterSize=size/6f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.25f,.13f,.20f);
 }
 static void Lollipop(Vector3 p,float height,Transform parent) {
  Shape("CandyStick",PrimitiveType.Cylinder,p+Vector3.up*height*.5f,new Vector3(.10f,height*.5f,.10f),white,parent);
  Shape("CandySwirl",PrimitiveType.Cylinder,p+Vector3.up*height,new Vector3(.9f,.13f,.9f),pink,parent).transform.rotation=Quaternion.Euler(90,0,0);
  Shape("CandyCenter",PrimitiveType.Sphere,p+Vector3.up*height+Vector3.back*.15f,Vector3.one*.48f,blue,parent);
 }
 static void House(Vector3 p,int index) {
  var g=new GameObject("CandyHouse_"+index);g.transform.SetParent(stage);
  Cube("BiscuitWalls",p+Vector3.up*1.1f,new Vector3(2.2f,2.2f,2.3f),index%2==0?pink:blue,g.transform,true);
  Cube("WaferRoof",p+Vector3.up*2.4f,new Vector3(2.8f,.5f,2.8f),cookie,g.transform);
  for(int i=0;i<5;i++)Shape("IcingRoof",PrimitiveType.Sphere,p+new Vector3((i-2)*.5f,2.8f,0),new Vector3(.8f,.6f,2.5f),white,g.transform);
  Cube("ChocolateDoor",p+new Vector3(0,.65f,-1.17f),new Vector3(.65f,1.3f,.08f),chocolate,g.transform);
  for(int side=-1;side<=1;side+=2)Shape("CandyWindow",PrimitiveType.Sphere,p+new Vector3(side*.7f,1.5f,-1.17f),new Vector3(.42f,.42f,.12f),gold,g.transform);
 }
 static void Platform(string name,Vector3 top,float diameter,int index) {
  var g=new GameObject(name);g.transform.SetParent(stage);g.transform.position=top;
  Shape("Cookie",PrimitiveType.Cylinder,top-Vector3.up*.21f,new Vector3(diameter,.21f,diameter),cookie,g.transform,true);
  // Thin frosting is decorative; it stays below the authoritative flat collision surface.
  Shape("Frosting",PrimitiveType.Cylinder,top-Vector3.up*.025f,new Vector3(diameter*.95f,.022f,diameter*.95f),index%2==0?pink:blue,g.transform);
  for(int k=0;k<10;k++){float a=k*Mathf.PI*.2f;var p=top+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*diameter*.43f;Shape("SugarPearl",PrimitiveType.Sphere,p-Vector3.up*.01f,Vector3.one*.12f,white,g.transform);}
  Label(index.ToString("00"),top+Vector3.up*.012f,.65f,g.transform);
 }
 [MenuItem("Tools/Candy Kingdom/Create Candy Town Test Scene")]
 public static void Build() {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode first.");
  if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Current scene has unsaved changes; save it first.");
  if(AssetDatabase.LoadMainAssetAtPath(ScenePath))throw new InvalidOperationException("Test scene already exists. Use Generate New Revision to preserve it.");
  Generate(ScenePath);
 }
 [MenuItem("Tools/Candy Kingdom/Generate New Revision")]
 public static void Revision(){Generate(AssetDatabase.GenerateUniqueAssetPath(ScenePath));}
 public static void Generate(string destination) {
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play mode and save current scene first.");
  Folder(Root+"/Scenes");Folder(Root+"/Materials");Folder(Root+"/Settings");Folder(Root+"/Backups");
  var config=AssetDatabase.LoadAssetAtPath<CandyTownSettings>(Root+"/Settings/CandyTownSettings.asset");
  if(!config){config=ScriptableObject.CreateInstance<CandyTownSettings>();AssetDatabase.CreateAsset(config,Root+"/Settings/CandyTownSettings.asset");}
  string source="Assets/OnlyUpPrototype/Scenes/OnlyUp_Prototype.unity";
  string stamp=DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
  if(!AssetDatabase.CopyAsset(source,Root+"/Backups/Prototype_"+stamp+".unity"))throw new Exception("Scene backup failed.");
  if(!AssetDatabase.CopyAsset("Assets/Characters/PlayerAnimator.controller",Root+"/Backups/PlayerAnimator_"+stamp+".controller"))throw new Exception("Animator backup failed.");
  if(!AssetDatabase.CopyAsset(source,destination))throw new Exception("Scene copy failed.");
  var scene=EditorSceneManager.OpenScene(destination);
  var player=GameObject.Find("model_chara");
  if(!player||!player.GetComponent<PrototypePlayerMotor>())throw new Exception("Existing player motor missing.");
  var motor=new SerializedObject(player.GetComponent<PrototypePlayerMotor>());
  float speed=motor.FindProperty("moveSpeed").floatValue,jump=motor.FindProperty("jumpSpeed").floatValue,gravity=Mathf.Abs(motor.FindProperty("gravity").floatValue);
  int count=Mathf.Clamp(config.platformCount,8,30);
  float rise=config.areaHeight/count;
  float peak=jump*jump/(2*gravity);
  if(rise<=0||rise>peak*.65f)throw new Exception("Requested rise exceeds safe jump height. Reduce areaHeight or increase platformCount.");
  float diameter=Mathf.Clamp(config.platformDiameter,2.4f,4f);
  float safeFlight=(jump+Mathf.Sqrt(jump*jump-2*gravity*rise))/gravity;
  float gap=Mathf.Lerp(config.edgeGap*.65f,config.edgeGap,Mathf.Clamp01(config.difficulty));
  if(gap<.05f||gap>speed*safeFlight*.45f)throw new Exception("Edge gap exceeds conservative jump allowance.");
  var old=GameObject.Find("PrototypeStage");if(old)old.SetActive(false);
  var testGround=GameObject.Find("TestGround");if(testGround)testGround.SetActive(false);
  stage=new GameObject("CandyTown_Generated").transform;
  random=new System.Random(config.seed);
  pink=Mat("Strawberry",new Color(1,.64f,.78f));blue=Mat("Soda",new Color(.58f,.85f,.97f));white=Mat("Vanilla",new Color(1,.97f,.91f));cookie=Mat("Cookie",new Color(.83f,.57f,.31f));chocolate=Mat("Chocolate",new Color(.28f,.12f,.09f));lavender=Mat("Lavender",new Color(.77f,.68f,.95f));gold=Mat("Lemon",new Color(1,.87f,.42f));
  Platform("StartIsland",Vector3.zero,9,0);
  Label("CANDY TOWN",new Vector3(0,.02f,-2),.6f,stage);
  var spawn=new GameObject("CandyTown_Start");spawn.transform.SetParent(stage);spawn.transform.position=player.transform.position;
  Set(player.GetComponent<StageRespawn>(),"startPoint",spawn.transform);
  var points=new Vector3[count];Vector3 previous=Vector3.zero;
  float spacing=diameter+gap;
  for(int i=0;i<count;i++){
   float dx=i==0?0:((i/4)%2==0?.5f:-.5f);
   float dz=i==0?4.5f+diameter*.5f+gap:Mathf.Sqrt(spacing*spacing-dx*dx);
   var p=new Vector3(previous.x+dx,rise*(i+1),previous.z+dz);points[i]=p;
   float size=diameter*(1f-.08f*config.difficulty*(i/(float)count));
   Platform("Route_"+(i+1).ToString("00"),p,size,i+1);
   if(i%4==3) {
    var lower=p+new Vector3(0,-2.4f,-3.1f);
    Cube("RecoveryIsland_"+i,lower-Vector3.up*.25f,new Vector3(8,.5f,7),lavender,stage,true);
    // Recovery steps reconnect to the previous section rather than granting a new checkpoint.
    for(int r=0;r<3;r++)Platform("RecoveryStep_"+i+"_"+r,lower+new Vector3(3.8f-r*1.15f,(r+1)*.5f,2.9f+r*.4f),1.8f,0);
   }
   if(i%4==0)Label("NEXT",p+new Vector3(0,.025f,.65f),.3f,stage);
   previous=p;
  }
  Vector3 end=points[count-1];
  var exit=new GameObject("Area02_CakeGarden_Connection");exit.transform.SetParent(stage);exit.transform.position=end;
  Label("AREA 1 GOAL",end+new Vector3(0,.03f,0),.28f,stage);
  var goalObject=new GameObject("CandyTown_Goal");goalObject.transform.SetParent(stage);goalObject.transform.position=end+Vector3.up*.4f;
  var goal=goalObject.AddComponent<PrototypeGoal>();Set(goal,"player",player.transform);
  foreach(var hud in UnityEngine.Object.FindObjectsByType<PrototypeHUD>(FindObjectsSortMode.None))Set(hud,"goal",goal);
  House(new Vector3(-4.6f,0,-.5f),0);House(new Vector3(4.6f,0,1.2f),1);
  int ornaments=Mathf.RoundToInt(30*Mathf.Clamp(config.decorationDensity,0,2));
  for(int i=0;i<ornaments;i++){
   int n=i%count;var p=points[n]+new Vector3((i%2==0?-1:1)*(3.5f+(float)random.NextDouble()*2),-1.5f,(float)random.NextDouble()*2-1);
   Shape("FloatingSugarCloud",PrimitiveType.Sphere,p,new Vector3(2.5f,.65f,1.8f),white,stage);
   Lollipop(p+Vector3.up*.25f,1.5f+(float)random.NextDouble(),stage);
  }
  for(int i=0;i<7;i++)Shape("SkyCloud",PrimitiveType.Sphere,new Vector3((i%2==0?-1:1)*14,4+i*.7f,i*9),new Vector3(9,2.2f,5),white,stage);
  RenderSettings.fog=true;RenderSettings.fogColor=new Color(.84f,.88f,.98f);RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=50;RenderSettings.fogEndDistance=130;
  RenderSettings.ambientLight=new Color(.74f,.76f,.85f);
  if(Camera.main){Camera.main.backgroundColor=new Color(.79f,.87f,.98f);Camera.main.clearFlags=CameraClearFlags.SolidColor;}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  AssetDatabase.SaveAssetIfDirty(config);
  LastBuildReport="Scene: "+destination+"; platforms="+count+"; rise="+rise+"; gap="+gap+"; theoretical peak="+peak+"; speed="+speed+"; jump="+jump+"; seed="+config.seed;
  Debug.Log("Candy Town built. "+LastBuildReport);
 }
}
}