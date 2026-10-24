using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace CandyKingdom.ArtUpgrade.Editor {
public static class CandyWorldArtBuilder {
 public const string Root="Assets/CandyKingdom/ArtUpgrade";
 public const string ScenePath="Assets/CandyKingdom/Scenes/CandyTown_DreamWorld.unity";
 public const string Source="Assets/CandyKingdom/Scenes/CandyTown_MotionTest.unity";
 static Material biscuit,cream,pink,blue,mint,lavender,chocolate,gold,berry;
 static Mesh rounded,disc,torus,whip,island;
 static System.Random random;
 static Transform world,town,far,route;
 static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;string parent=Path.GetDirectoryName(p).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(p));}
 static Material Mat(string name,Color color,float gloss,bool grain=false) {
  var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",gloss);m.SetFloat("_Metallic",0);
  if(grain){var tex=new Texture2D(128,128,TextureFormat.RGB24,false);tex.name=name+"_Grain";var px=new Color[16384];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float v=.90f+.16f*Mathf.PerlinNoise(x*.62f,y*.62f);float pore=Mathf.PerlinNoise(x*.21f+20,y*.21f+20)>.70f?.10f:0;px[y*128+x]=Color.white*(v-pore);}tex.SetPixels(px);tex.Apply();tex.wrapMode=TextureWrapMode.Repeat;AssetDatabase.CreateAsset(tex,Root+"/Textures/"+tex.name+".asset");m.SetTexture("_BaseMap",tex);m.SetTextureScale("_BaseMap",new Vector2(3,3));}
  AssetDatabase.CreateAsset(m,Root+"/Materials/"+name+".mat");return m;
 }
 static Mesh SaveMesh(string name,List<Vector3> v,List<int> t,List<Vector2> uv=null) {var m=new Mesh();m.name=name;m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;m.SetVertices(v);m.SetTriangles(t,0);if(uv!=null)m.SetUVs(0,uv);m.RecalculateNormals();m.RecalculateBounds();string path=Root+"/Meshes/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(m,existing);UnityEngine.Object.DestroyImmediate(m);AssetDatabase.SaveAssetIfDirty(existing);return existing;}AssetDatabase.CreateAsset(m,path);return m;}
 static Mesh Lathe(string name,int rings,int sides,Func<float,float> radius,Func<float,float> height,float ripple=0,float twist=0) {
  var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
  for(int j=0;j<=rings;j++){float u=j/(float)rings;for(int k=0;k<=sides;k++){float a=k/(float)sides*Mathf.PI*2;float r=radius(u)*(1+ripple*Mathf.Cos(7*a+u*twist));float drift=name=="WhippedCream"?.15f*u*u:0;v.Add(new Vector3(Mathf.Cos(a)*r+drift,height(u),Mathf.Sin(a)*r));if(name=="RoundedPastryDisc"&&(u<.1f||u>.9f)){var p=v[v.Count-1];uv.Add(new Vector2(p.x+.5f,p.z+.5f));}else uv.Add(new Vector2(k/(float)sides,u));}}
  for(int j=0;j<rings;j++)for(int k=0;k<sides;k++){int a=j*(sides+1)+k,b=a+sides+1;t.Add(a);t.Add(b);t.Add(a+1);t.Add(a+1);t.Add(b);t.Add(b+1);}
  return SaveMesh(name,v,t,uv);
 }
 static Mesh RoundedBox() {
  var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();Vector3[] ns={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
  foreach(var n in ns){Vector3 u=Mathf.Abs(n.y)>.5f?Vector3.right:Vector3.up;Vector3 w=Vector3.Cross(n,u);int start=v.Count;
   for(int y=0;y<=8;y++)for(int x=0;x<=8;x++){var p=n*.5f+u*(x/8f-.5f)+w*(y/8f-.5f);var q=new Vector3(Mathf.Clamp(p.x,-.39f,.39f),Mathf.Clamp(p.y,-.39f,.39f),Mathf.Clamp(p.z,-.39f,.39f));v.Add(q+(p-q).normalized*.11f);uv.Add(new Vector2(x/8f,y/8f));}
   for(int y=0;y<8;y++)for(int x=0;x<8;x++){int a=start+y*9+x;t.Add(a);t.Add(a+1);t.Add(a+9);t.Add(a+1);t.Add(a+10);t.Add(a+9);}
  }return SaveMesh("RoundedBiscuitBox",v,t,uv);
 }
 static Mesh Torus() {var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();for(int i=0;i<=48;i++){float a=i/48f*Mathf.PI*2;for(int j=0;j<=16;j++){float b=j/16f*Mathf.PI*2;float r=.36f+.14f*Mathf.Cos(b);v.Add(new Vector3(r*Mathf.Cos(a),.14f*Mathf.Sin(b),r*Mathf.Sin(a)));uv.Add(new Vector2(i/48f,j/16f));}}for(int i=0;i<48;i++)for(int j=0;j<16;j++){int a=i*17+j;t.Add(a);t.Add(a+1);t.Add(a+17);t.Add(a+1);t.Add(a+18);t.Add(a+17);}return SaveMesh("SugarTorus",v,t,uv);}
 static GameObject Group(string name,Transform parent,Vector3 position){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;return g;}
 static GameObject MeshObject(string name,Mesh mesh,Vector3 p,Vector3 scale,Material mat,Transform parent,bool solid=false){var g=Group(name,parent,p);g.transform.localScale=scale;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;if(solid)g.AddComponent<MeshCollider>().sharedMesh=mesh;g.isStatic=true;return g;}
 static GameObject Box(string name,Vector3 p,Vector3 scale,Material mat,Transform parent,bool solid=false){return MeshObject(name,rounded,p,scale,mat,parent,solid);}
 static GameObject Ball(string name,Vector3 p,Vector3 scale,Material mat,Transform parent){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;return g;}
 static GameObject Disk(string name,Vector3 p,float diameter,float height,Material mat,Transform parent,bool solid=false){return MeshObject(name,disc,p,new Vector3(diameter,height,diameter),mat,parent,solid);}
 static void Sprinkles(Transform parent,float radius,int count,float y){Material[] colors={pink,blue,mint,gold,lavender};for(int k=0;k<count;k++){float a=(float)random.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)random.NextDouble())*radius;var g=Box("SugarSprinkle",new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r),new Vector3(.045f,.035f,.14f),colors[k%5],parent);g.transform.localRotation=Quaternion.Euler(0,k*137.5f,0);}}
 static void IcingRim(Transform parent,float radius,float y,int count=24){for(int k=0;k<count;k++){float a=k*Mathf.PI*2/count;Ball("CreamScallop",new Vector3(Mathf.Cos(a)*radius,y-.025f*(k%3),Mathf.Sin(a)*radius),new Vector3(.44f,.20f,.44f),cream,parent);}}
 static void Text(string words,Vector3 p,float size,Transform parent,bool upright=false){var g=Group("Sign_"+words,parent,p);g.transform.localRotation=upright?Quaternion.Euler(0,180,0):Quaternion.Euler(90,0,0);var tx=g.AddComponent<TextMesh>();tx.text=words;tx.fontSize=64;tx.characterSize=size/6;tx.anchor=TextAnchor.MiddleCenter;tx.color=new Color(.46f,.23f,.38f);}
 static void Cookie(Transform parent,float diameter,float height,bool solid=true){Disk("BakedCookie",new Vector3(0,-height*.5f,0),diameter,height,biscuit,parent,solid);for(int k=0;k<14;k++){float a=k*2.4f,r=diameter*(.16f+.24f*(k%4)/3f);Ball("ChocolateChip",new Vector3(Mathf.Sin(a)*r,.018f,Mathf.Cos(a)*r),new Vector3(.13f,.06f,.11f),chocolate,parent);}IcingRim(parent,diameter*.46f,-.09f,Mathf.RoundToInt(diameter*10));}
 static void Macaron(Transform parent,float d,bool solid=false){Disk("AlmondShell",new Vector3(0,-.19f,0),d,.38f,pink,parent,solid);Disk("VanillaFilling",new Vector3(0,-.21f,0),d*1.025f,.1f,cream,parent);Disk("PastelCap",new Vector3(0,-.045f,0),d*.97f,.09f,lavender,parent);for(int k=0;k<20;k++){float a=k*Mathf.PI*.1f;Ball("RuffledFeet",new Vector3(Mathf.Sin(a)*d*.47f,-.24f,Mathf.Cos(a)*d*.47f),Vector3.one*.13f,pink,parent);}}
 static void Donut(Transform parent,float d,bool solid=false){MeshObject("BakedDonut",torus,new Vector3(0,-.25f,0),new Vector3(d,.25f/.14f,d),biscuit,parent,solid);MeshObject("StrawberryGlaze",torus,new Vector3(0,-.18f,0),new Vector3(d*.98f,.17f/.14f,d*.98f),pink,parent);if(solid)Disk("VisibleWaferCenter",new Vector3(0,-.06f,0),d*.48f,.12f,chocolate,parent,true);for(int k=0;k<16;k++){float a=k*Mathf.PI/8;var g=Box("SugarSprinkle",new Vector3(Mathf.Sin(a)*d*.36f,-.005f,Mathf.Cos(a)*d*.36f),new Vector3(.05f,.04f,.15f),k%2==0?blue:gold,parent);g.transform.localRotation=Quaternion.Euler(0,k*71,0);}}
 static void Lollipop(Transform parent,Vector3 p,float height){var g=Group("LollipopLamp",parent,p);Box("CreamStick",new Vector3(0,height*.5f,0),new Vector3(.11f,height,.11f),cream,g.transform);var candy=MeshObject("CandyDisc",torus,new Vector3(0,height,0),new Vector3(.95f,1.3f,.95f),pink,g.transform);candy.transform.localRotation=Quaternion.Euler(90,0,0);Ball("CandyCenter",new Vector3(0,height,.02f),new Vector3(.55f,.55f,.28f),blue,g.transform);}
 static void CandyTree(Transform parent,Vector3 p,float scale){var g=Group("CandyTree",parent,p);g.transform.localScale=Vector3.one*scale;Box("WaferTrunk",new Vector3(0,.8f,0),new Vector3(.20f,1.6f,.2f),biscuit,g.transform);Ball("MintCandyCrown",new Vector3(0,1.9f,0),new Vector3(1.5f,1.65f,1.3f),mint,g.transform);Ball("PinkCandyCrown",new Vector3(-.45f,1.55f,0),Vector3.one*.95f,pink,g.transform);Ball("LemonCandyCrown",new Vector3(.42f,1.55f,.12f),Vector3.one*.85f,gold,g.transform);}
 static GameObject House(bool cupcake,Transform parent,Vector3 p){var g=Group(cupcake?"BlueCupcakeCottage":"PinkCookieCottage",parent,p);Material wall=cupcake?blue:pink;
  if(cupcake)Disk("CupcakeWalls",new Vector3(0,1.15f,0),2.9f,2.3f,wall,g.transform,true);else Box("RoundedCookieWalls",new Vector3(0,1.2f,0),new Vector3(2.7f,2.4f,2.8f),wall,g.transform,true);
  Box("BiscuitCornice",new Vector3(0,2.45f,0),new Vector3(3.2f,.38f,3.2f),biscuit,g.transform);
  if(cupcake)MeshObject("SwirledWhipRoof",whip,new Vector3(0,2.55f,0),new Vector3(3.2f,1.7f,3.2f),cream,g.transform);
  else {for(int k=0;k<5;k++)Ball("WhippedRoofRibbon",new Vector3((k-2)*.55f,2.83f,0),new Vector3(.8f,.7f,3.05f),cream,g.transform);MeshObject("CreamSpire",whip,new Vector3(0,3.05f,0),new Vector3(1.4f,1.25f,1.4f),cream,g.transform);}
  Ball("Cherry",new Vector3(cupcake?.2f:0,cupcake?4.2f:4.1f,0),Vector3.one*.5f,berry,g.transform);
  float front=cupcake?-1.42f:-1.43f;
  Box("IcingDoorArch",new Vector3(0,.77f,front),new Vector3(1.05f,1.65f,.15f),cream,g.transform);
  Box("ChocolateDoor",new Vector3(0,.75f,front-.09f),new Vector3(.78f,1.38f,.12f),chocolate,g.transform);
  Ball("SugarDoorHandle",new Vector3(.23f,.7f,front-.17f),Vector3.one*.09f,gold,g.transform);
  for(int side=-1;side<=1;side+=2){var ring=MeshObject("CandyWindowRim",torus,new Vector3(side*.85f,1.72f,front-.03f),new Vector3(.72f,1,.72f),cream,g.transform);ring.transform.localRotation=Quaternion.Euler(90,0,0);Ball("LemonGlass",new Vector3(side*.85f,1.72f,front-.07f),new Vector3(.46f,.46f,.16f),gold,g.transform);}
  for(int k=0;k<10;k++)Box("CookieWallPiping",new Vector3((k-4.5f)*.26f,.18f,front-.01f),new Vector3(.15f,.18f,.08f),cream,g.transform);
  return g;
 }
 public static float GroundHeight(float x,float z){return -2.35f+.18f*Mathf.Sin(x*.12f)*Mathf.Cos(z*.10f)+.13f*Mathf.PerlinNoise(x*.07f+10,z*.07f+10);}
 static Mesh GroundMesh(){var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();int nx=56,nz=70;for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++){float x=-56+i*2,z=-30+j*2;v.Add(new Vector3(x,GroundHeight(x,z),z));uv.Add(new Vector2(i*.12f,j*.12f));}for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){int a=j*(nx+1)+i;t.Add(a);t.Add(a+nx+1);t.Add(a+1);t.Add(a+1);t.Add(a+nx+1);t.Add(a+nx+2);}return SaveMesh("RollingCookieTerrain",v,t,uv);}
 static void Ground(){
  var ground=Group("LowestLayer_CookieLandscape",world,Vector3.zero);
  MeshObject("WalkableRollingCookieGround",GroundMesh(),Vector3.zero,Vector3.one,biscuit,ground.transform,true);
  Box("CookieTerrainUnderside",new Vector3(0,-4.0f,40),new Vector3(112,3,140),biscuit,ground.transform);
  for(int k=0;k<22;k++){float x=(k%2==0?-1:1)*(9+(k%5)*6),z=-16+k*5;float y=GroundHeight(x,z);var hill=Ball("WalkableWhippedHill",new Vector3(x,y-.25f,z),new Vector3(7,1.7f,5),cream,ground.transform);hill.AddComponent<MeshCollider>().sharedMesh=hill.GetComponent<MeshFilter>().sharedMesh;}
  for(int k=0;k<42;k++){float z=-15+k*2.6f,x=8*Mathf.Sin(z*.055f);float y=GroundHeight(x,z);var tile=Box("ChocolateTrail",new Vector3(x,y+.025f,z),new Vector3(2.8f,.09f,2.8f),chocolate,ground.transform);tile.transform.localRotation=Quaternion.Euler(0,Mathf.Cos(z*.055f)*10,0);}
  for(int k=0;k<30;k++){float x=(k%2==0?-1:1)*(9+k%7*4),z=-15+k*3.8f;CandyTree(ground.transform,new Vector3(x,GroundHeight(x,z),z),.8f+(k%3)*.2f);}
  for(int k=0;k<5;k++){float z=-17+k*3.2f,h=-1.8f+k*.45f;var stair=Group("GroundReturnStep_"+(k+1),ground.transform,new Vector3(0,h,z));Cookie(stair.transform,2.8f,.34f);}
  Text("BACK TO TOWN",new Vector3(0,GroundHeight(0,-11)+.035f,-11),.45f,ground.transform);
  for(int k=0;k<50;k++){float x=-38+(float)random.NextDouble()*76,z=-18+(float)random.NextDouble()*110;if(Mathf.Abs(x)<6)continue;var p=new Vector3(x,GroundHeight(x,z),z);if(k%3==0){var g=Group("GroundMacaron",ground.transform,p+Vector3.up*.4f);Macaron(g.transform,1.5f);}else if(k%3==1){var g=Group("GroundDonut",ground.transform,p+Vector3.up*.5f);Donut(g.transform,1.3f);}else Ball("CandyRock",p+Vector3.up*.3f,new Vector3(1.2f,.8f,1),k%2==0?lavender:mint,ground.transform);}
 }
 static void Platform(Transform parent,int variant,float d,int number){
  if(variant==0)Cookie(parent,d,.42f);
  if(variant==1)Macaron(parent,d,true);
  if(variant==2){Box("WaferCore",new Vector3(0,-.23f,0),new Vector3(d,.46f,d),biscuit,parent,true);for(int k=0;k<3;k++)Box("ChocolateWaferLayer",new Vector3(0,-.07f-k*.13f,0),new Vector3(d*1.01f,.045f,d*1.01f),chocolate,parent);for(int k=-3;k<=3;k++)Box("WaferGrid",new Vector3(k*d/8,.007f,0),new Vector3(.03f,.025f,d*.94f),cream,parent);}
  if(variant==3)Donut(parent,d,true);
  if(variant==4){Disk("CakeSponge",new Vector3(0,-.3f,0),d,.6f,biscuit,parent,true);Disk("StrawberryFilling",new Vector3(0,-.32f,0),d*1.005f,.1f,pink,parent);Disk("CreamCakeTop",new Vector3(0,-.04f,0),d*1.015f,.08f,cream,parent);IcingRim(parent,d*.46f,-.06f);for(int k=0;k<5;k++){float a=k*Mathf.PI*.4f;Ball("MiniStrawberry",new Vector3(Mathf.Sin(a)*d*.41f,.045f,Mathf.Cos(a)*d*.41f),new Vector3(.13f,.17f,.13f),berry,parent);}}
  if(variant==5){Box("ChocolateBar",new Vector3(0,-.2f,0),new Vector3(d,.4f,d),chocolate,parent,true);for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)Box("ChocolateSquare",new Vector3(x*d/3,-.005f,z*d/3),new Vector3(d*.29f,.06f,d*.29f),chocolate,parent);}
  if(variant==6){Disk("CandyBonbon",new Vector3(0,-.2f,0),d,.4f,blue,parent,true);for(int k=-2;k<=2;k++){var stripe=Box("CandyRibbon",new Vector3(k*.4f,.008f,0),new Vector3(.12f,.03f,d*.9f),cream,parent);stripe.transform.localRotation=Quaternion.Euler(0,25,0);}Ball("WrapperLeft",new Vector3(-d*.6f,-.2f,0),new Vector3(.5f,.4f,.7f),pink,parent);Ball("WrapperRight",new Vector3(d*.6f,-.2f,0),new Vector3(.5f,.4f,.7f),pink,parent);}
  if(variant==7){MeshObject("WhippedFloatingIsland",island,new Vector3(0,-1,0),new Vector3(d,1,d),lavender,parent);Disk("WalkableCreamCap",new Vector3(0,-.1f,0),d,.2f,cream,parent,true);IcingRim(parent,d*.46f,-.08f);}
  if(number>0)Text(number.ToString("00"),new Vector3(0,.045f,0),.44f,parent);
 }
 static void Cloud(Transform parent,Vector3 p,Vector3 scale){var g=Group("SoftCloud",parent,p);g.transform.localScale=scale;Ball("CloudBody",Vector3.zero,new Vector3(1.6f,.55f,.95f),cream,g.transform);Ball("CloudPuff",new Vector3(-.42f,.14f,.05f),new Vector3(.9f,.7f,.8f),cream,g.transform);Ball("CloudPuff",new Vector3(.4f,.11f,.1f),new Vector3(.9f,.65f,.8f),cream,g.transform);}
 static void Castle(Transform parent,Vector3 p,float scale){var g=Group("DistantSugarCastle",parent,p);g.transform.localScale=Vector3.one*scale;Box("CastleHall",new Vector3(0,2.1f,0),new Vector3(5,4.2f,3),pink,g.transform);for(int i=-1;i<=1;i++)for(int j=-1;j<=1;j++){if(i==0&&j==0)continue;float h=4.5f+(i+j+2)%3;Disk("SugarTower",new Vector3(i*2.4f,h*.5f,j*1.5f),1.35f,h,cream,g.transform);MeshObject("IcingSpire",whip,new Vector3(i*2.4f,h,j*1.5f),new Vector3(2,2.7f,2),i%2==0?blue:lavender,g.transform);Ball("SpireCandy",new Vector3(i*2.4f,h+2.7f,j*1.5f),Vector3.one*.3f,gold,g.transform);}
  MeshObject("CentralSugarSpire",whip,new Vector3(0,4,0),new Vector3(3,5,3),pink,g.transform);
  for(int k=-2;k<=2;k++)Box("CastleWindow",new Vector3(k*.8f,2.6f,-1.55f),new Vector3(.35f,.9f,.08f),gold,g.transform);
 }
 static void Background(){
  for(int k=0;k<11;k++){float side=k%2==0?-1:1;float x=side*(29+k%3*12),z=18+k*10,h=9+k%4*5;var g=Group("DistantFloatingIsland_"+k,far,new Vector3(x,h,z));float d=12+k%3*4;MeshObject("TaperedSugarRock",island,new Vector3(0,-8,0),new Vector3(d,8,d),k%2==0?lavender:pink,g.transform);Disk("IcingIslandTop",new Vector3(0,-.3f,0),d,.6f,cream,g.transform);IcingRim(g.transform,d*.47f,-.25f,40);var house=House(k%2==0,g.transform,new Vector3(-1,0,0));house.transform.localScale=Vector3.one*(1.3f+k%3*.3f);for(int j=0;j<3;j++)CandyTree(g.transform,new Vector3(3-j*3,0,3),1.8f);foreach(var c in g.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);}
  Castle(far,new Vector3(0,24,125),3.5f);
  for(int k=0;k<22;k++)Cloud(far,new Vector3((k%2==0?-1:1)*(15+k%5*11),-5+k%4*10,12+k*7),new Vector3(8+k%3*3,6,5+k%4));
  Material[] bands={pink,gold,mint,blue,lavender};for(int k=0;k<5;k++)for(int j=0;j<36;j++){float u=j/35f;float x=-30+u*60,y=25+Mathf.Sin(u*Mathf.PI)*18+k*.55f;Box("DistantRainbowBridge",new Vector3(x,y,94),new Vector3(1.95f,.42f,.7f),bands[k],far);}
 }
 
 public static void Polish() {
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open Dream World in Edit mode.");
  random=new System.Random(241024);rounded=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/RoundedBiscuitBox.asset");
  biscuit=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/CaramelBiscuit.mat");cream=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/WhippedVanilla.mat");pink=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/StrawberryPastel.mat");blue=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/SkyBlueSugar.mat");mint=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/MintCandy.mat");lavender=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/LavenderMacaron.mat");gold=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/LemonCandy.mat");
  biscuit.SetColor("_BaseColor",new Color(.79f,.57f,.36f));AssetDatabase.SaveAssetIfDirty(biscuit);
  cream.SetColor("_BaseColor",new Color(.95f,.93f,.87f));AssetDatabase.SaveAssetIfDirty(cream);
  disc=Lathe("RoundedPastryDisc",12,64,u=>u<.08f?Mathf.Lerp(0,.5f,u/.08f):u>.92f?Mathf.Lerp(.5f,0,(u-.92f)/.08f):.5f,u=>u<.08f?-.5f:u>.92f?.5f:Mathf.Lerp(-.5f,.5f,(u-.08f)/.84f));
  world=GameObject.Find("DreamCandyWorld").transform;town=GameObject.Find("CandyTown_HeroArchitecture").transform;far=GameObject.Find("DistantScenery_NotPlayable").transform;
  var sky=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/DreamSky.mat");sky.shader=Shader.Find("CandyKingdom/PastelSky");sky.SetColor("_Top",new Color(.32f,.62f,.94f));sky.SetColor("_Horizon",new Color(.84f,.88f,.98f));AssetDatabase.SaveAssetIfDirty(sky);RenderSettings.skybox=sky;
  foreach(Transform cloud in far)if(cloud.name=="SoftCloud"){var scale=cloud.localScale;scale.y=6;cloud.localScale=scale;}
  var castle=GameObject.Find("DistantSugarCastle");castle.transform.localPosition=new Vector3(0,12,140);castle.transform.localScale=Vector3.one*1.6f;
  var light=UnityEngine.Object.FindFirstObjectByType<Light>();if(light)light.intensity=1.05f;
  RenderSettings.ambientSkyColor=new Color(.62f,.72f,.86f);RenderSettings.ambientEquatorColor=new Color(.70f,.67f,.74f);RenderSettings.ambientGroundColor=new Color(.52f,.48f,.58f);
  if(!GameObject.Find("CreamMeadows")){
   var meadow=Group("CreamMeadows",world,Vector3.zero);
   for(int n=0;n<24;n++){float x=(n%2==0?-1:1)*(9+n%5*8),z=-10+(n/2)*9,radius=5+n%3;
    var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();
    for(int j=0;j<=8;j++)for(int k=0;k<=64;k++){float a=k*Mathf.PI*2/64;float r=radius*j/8f*(1+.10f*Mathf.Sin(a*5+n));float px=x+Mathf.Cos(a)*r,pz=z+Mathf.Sin(a)*r;v.Add(new Vector3(px,GroundHeight(px,pz)+.035f*Mathf.Sin(j/8f*Mathf.PI),pz));uv.Add(new Vector2(px*.1f,pz*.1f));}
    for(int j=0;j<8;j++)for(int k=0;k<64;k++){int a=j*65+k;t.Add(a);t.Add(a+1);t.Add(a+65);t.Add(a+1);t.Add(a+66);t.Add(a+65);}
    MeshObject("WalkableCreamMeadow_"+n,SaveMesh("CreamMeadow_"+n,v,t,uv),Vector3.zero,Vector3.one,cream,meadow.transform,true);
   }
  }
  if(!GameObject.Find("DistantCakeTowers")){
   var towers=Group("DistantCakeTowers",far,Vector3.zero);
   for(int i=0;i<4;i++){var g=Group("LayerCakeTower_"+i,towers.transform,new Vector3((i%2==0?-1:1)*(22+i*6),4+i*3,75+i*14));float d=8+i;
    for(int k=0;k<3;k++){Disk("CakeTier",new Vector3(0,k*2.2f,0),d-k,2.2f,k%2==0?pink:blue,g.transform);Disk("CreamTier",new Vector3(0,1.12f+k*2.2f,0),d-k+.1f,.28f,cream,g.transform);IcingRim(g.transform,(d-k)*.47f,1.2f+k*2.2f,28);}
   }
  }
  if(!AssetDatabase.LoadMainAssetAtPath(Root+"/Prefabs/CandyTree.prefab")){var g=Group("CandyTreePrefab",null,Vector3.zero);CandyTree(g.transform,Vector3.zero,1);PrefabUtility.SaveAsPrefabAsset(g,Root+"/Prefabs/CandyTree.prefab");UnityEngine.Object.DestroyImmediate(g);}
  
  var profile=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(Root+"/Materials/DreamWorldVolume.asset");
  if(!profile){
   profile=ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"/Materials/DreamWorldVolume.asset");
   var bloom=profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);bloom.intensity.value=.10f;bloom.threshold.value=1.15f;bloom.scatter.value=.55f;AssetDatabase.AddObjectToAsset(bloom,profile);
   var tone=profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);tone.mode.value=UnityEngine.Rendering.Universal.TonemappingMode.ACES;AssetDatabase.AddObjectToAsset(tone,profile);
   var colors=profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);colors.saturation.value=-3;colors.contrast.value=-4;AssetDatabase.AddObjectToAsset(colors,profile);
   AssetDatabase.SaveAssetIfDirty(profile);
  }
  if(!GameObject.Find("DreamWorld_VisualVolume")){var vg=Group("DreamWorld_VisualVolume",world,Vector3.zero);var volume=vg.AddComponent<UnityEngine.Rendering.Volume>();volume.isGlobal=true;volume.sharedProfile=profile;volume.priority=10;}
  
  UnityEngine.Rendering.Universal.ColorAdjustments grade;
  if(profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out grade)){grade.postExposure.Override(.4f);EditorUtility.SetDirty(grade);AssetDatabase.SaveAssetIfDirty(grade);}
  if(!GameObject.Find("CastleCloudCitadel")){
   island=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/FloatingSugarIsland.asset");
   var citadel=Group("CastleCloudCitadel",far,new Vector3(0,12,140));
   MeshObject("SugarCastleIsland",island,new Vector3(0,-6,0),new Vector3(28,6,28),lavender,citadel.transform);
   Disk("CastleCreamGround",new Vector3(0,-.15f,0),28,.3f,cream,citadel.transform);
   IcingRim(citadel.transform,13.3f,-.2f,44);
  }
  foreach(var t in far.GetComponentsInChildren<Transform>())if(t.name=="CreamScallop"){
   int count=0;foreach(Transform sibling in t.parent)if(sibling.name=="CreamScallop")count++;
   float radius=new Vector2(t.localPosition.x,t.localPosition.z).magnitude;
   float size=Mathf.Max(.44f,Mathf.PI*2*radius/Mathf.Max(1,count)*1.12f);t.localScale=new Vector3(size,size*.45f,size);
  }
  if(!GameObject.Find("ContinuousRainbowBridge")){
   var obsolete=new List<GameObject>();foreach(Transform t in far)if(t.name=="DistantRainbowBridge")obsolete.Add(t.gameObject);foreach(var g in obsolete)UnityEngine.Object.DestroyImmediate(g);
   var bridge=Group("ContinuousRainbowBridge",far,Vector3.zero);Material[] bands={pink,gold,mint,blue,lavender};
   for(int band=0;band<5;band++){var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
    for(int j=0;j<=80;j++){float u=j/80f;var center=new Vector3(-32+u*64,9+Mathf.Sin(u*Mathf.PI)*12+band*.58f,108);var tangent=new Vector3(64,Mathf.Cos(u*Mathf.PI)*Mathf.PI*12,0).normalized;var n=new Vector3(-tangent.y,tangent.x,0)*.30f;
     v.Add(center+n+Vector3.forward*.35f);v.Add(center-n+Vector3.forward*.35f);v.Add(center-n-Vector3.forward*.35f);v.Add(center+n-Vector3.forward*.35f);for(int k=0;k<4;k++)uv.Add(new Vector2(u,k/3f));
    }
    for(int j=0;j<80;j++)for(int k=0;k<4;k++){int a=j*4+k,b=j*4+(k+1)%4;triangles.Add(a);triangles.Add(b);triangles.Add(a+4);triangles.Add(b);triangles.Add(b+4);triangles.Add(a+4);}
    MeshObject("RainbowBand_"+band,SaveMesh("RainbowBand_"+band,v,triangles,uv),Vector3.zero,Vector3.one,bands[band],bridge.transform);
   }
  }
if(Camera.main){var data=Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();if(!data)data=Camera.main.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=true;Camera.main.allowHDR=true;}
EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
 }
[MenuItem("Tools/Candy Kingdom/Create Dream World Art Scene")]
 public static void Build(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Stop Play mode and save current scene first.");
  if(AssetDatabase.LoadMainAssetAtPath(ScenePath))throw new Exception("Dream World scene exists; refusing overwrite.");
  foreach(var part in new[]{"Materials","Textures","Meshes","Prefabs","Backups"})Folder(Root+"/"+part);
  var files=new List<string>(Directory.GetFiles("Assets/Characters","*",SearchOption.AllDirectories));files.AddRange(Directory.GetFiles("Assets/OnlyUpPrototype/Runtime","*.cs"));files.AddRange(Directory.GetFiles("Assets/CandyKingdom/Motion","*.anim",SearchOption.AllDirectories));files.AddRange(Directory.GetFiles("Assets/CandyKingdom/Motion/Runtime","*.cs"));files.Add("Assets/CandyKingdom/Motion/Controllers/CandyMotion_Test.controller");files.Add(Source);
  var hashes=new Dictionary<string,string>();using(var sha=System.Security.Cryptography.SHA256.Create())foreach(var f in files)hashes[f]=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(f)));File.WriteAllText(Root+"/Backups/ProtectedHashes.json",Newtonsoft.Json.JsonConvert.SerializeObject(hashes));
  string stamp=DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");AssetDatabase.CopyAsset(Source,Root+"/Backups/CandyTown_"+stamp+".unity");File.Copy("Assets/CandyKingdom/Editor/CandyTownBuilder.cs",Root+"/Backups/CandyTownBuilder_"+stamp+".cs.txt");
  if(!AssetDatabase.CopyAsset(Source,ScenePath))throw new Exception("Scene copy failed.");
  var scene=EditorSceneManager.OpenScene(ScenePath);random=new System.Random(241024);
  biscuit=Mat("CaramelBiscuit",new Color(.83f,.53f,.27f),.19f,true);cream=Mat("WhippedVanilla",new Color(1,.95f,.83f),.28f);pink=Mat("StrawberryPastel",new Color(.97f,.57f,.73f),.38f);blue=Mat("SkyBlueSugar",new Color(.48f,.80f,.92f),.48f);mint=Mat("MintCandy",new Color(.54f,.83f,.70f),.52f);lavender=Mat("LavenderMacaron",new Color(.76f,.63f,.89f),.24f,true);chocolate=Mat("SmoothChocolate",new Color(.26f,.105f,.075f),.52f,true);gold=Mat("LemonCandy",new Color(.99f,.80f,.38f),.68f);berry=Mat("Raspberry",new Color(.86f,.29f,.43f),.50f);
  rounded=RoundedBox();disc=Lathe("RoundedPastryDisc",12,64,u=>u<.08f?Mathf.Lerp(0,.5f,u/.08f):u>.92f?Mathf.Lerp(.5f,0,(u-.92f)/.08f):.5f,u=>u<.08f?-.5f:u>.92f?.5f:Mathf.Lerp(-.5f,.5f,(u-.08f)/.84f));torus=Torus();whip=Lathe("WhippedCream",28,64,u=>.5f*Mathf.Pow(1-u,.8f),u=>u,.10f,13);island=Lathe("FloatingSugarIsland",20,64,u=>.07f+.43f*Mathf.Pow(u,.45f),u=>u,.045f,5);
  world=Group("DreamCandyWorld",null,Vector3.zero).transform;town=Group("CandyTown_HeroArchitecture",world,Vector3.zero).transform;route=Group("PlayableSweetRoute",world,Vector3.zero).transform;far=Group("DistantScenery_NotPlayable",world,Vector3.zero).transform;
  Ground();EditorSceneManager.SaveScene(scene);
  var old=GameObject.Find("CandyTown_Generated");var sources=new List<Transform>();foreach(Transform child in old.transform)if(child.name.StartsWith("Route_"))sources.Add(child);sources.Sort((a,b)=>string.CompareOrdinal(a.name,b.name));
  for(int k=0;k<sources.Count;k++){var src=sources[k];float d=src.Find("Cookie").localScale.x;var g=Group(src.name,route,src.position);Platform(g.transform,k%8,d,k+1);}
  var start=Group("CandyTown_StartPlaza",town,Vector3.zero);Cookie(start.transform,9,.42f);Sprinkles(start.transform,4.1f,65,.035f);Text("CANDY TOWN",new Vector3(0,.07f,-2.1f),.75f,start.transform);
  var left=House(false,town,new Vector3(-4.6f,0,-.5f));var right=House(true,town,new Vector3(4.6f,0,1.2f));
  PrefabUtility.SaveAsPrefabAsset(left,Root+"/Prefabs/PinkCookieCottage.prefab");PrefabUtility.SaveAsPrefabAsset(right,Root+"/Prefabs/BlueCupcakeCottage.prefab");
  for(int k=0;k<2;k++){var g=Group("TownFoundation",town,new Vector3(k==0?-4.6f:4.6f,0,k==0?-.5f:1.2f));Cookie(g.transform,4.3f,2.5f);}
  for(int k=0;k<10;k++){float side=k%2==0?-1:1;float x=side*(5.8f+k%3*.65f),z=-3+k*.75f;Lollipop(town,new Vector3(x,-.12f,z),1.8f+(k%3)*.15f);var g=Group("MacaronBench",town,new Vector3(x+side*.7f,.05f,z+.7f));Macaron(g.transform,1.35f);g.transform.localScale=new Vector3(1.3f,.7f,.7f);}
  for(int k=0;k<8;k++){float side=k%2==0?-1:1;var p=new Vector3(side*(7+k%3*2),GroundHeight(side*8,k*4),k*4);CandyTree(town,p,1.2f);}
  foreach(Transform child in old.transform)if(child.name!="CandyTown_Start"&&child.name!="CandyTown_Goal"&&child.name!="Area02_CakeGarden_Connection")child.gameObject.SetActive(false);
  for(int k=0;k<8;k++){var g=Group("SweetPlatform_"+k,null,Vector3.zero);Platform(g.transform,k,3,0);PrefabUtility.SaveAsPrefabAsset(g,Root+"/Prefabs/SweetPlatform_"+k+".prefab");UnityEngine.Object.DestroyImmediate(g);}
  var lamp=Group("LollipopLampPrefab",null,Vector3.zero);Lollipop(lamp.transform,Vector3.zero,2);PrefabUtility.SaveAsPrefabAsset(lamp,Root+"/Prefabs/LollipopLamp.prefab");UnityEngine.Object.DestroyImmediate(lamp);
  Background();
  var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.57f,.74f,.98f));sky.SetColor("_GroundColor",new Color(.77f,.76f,.86f));sky.SetFloat("_AtmosphereThickness",.75f);sky.SetFloat("_Exposure",1.25f);sky.SetFloat("_SunSize",.035f);AssetDatabase.CreateAsset(sky,Root+"/Materials/DreamSky.mat");RenderSettings.skybox=sky;
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.75f,.83f,.95f);RenderSettings.ambientEquatorColor=new Color(.78f,.75f,.80f);RenderSettings.ambientGroundColor=new Color(.65f,.57f,.67f);
  RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.80f,.83f,.94f);RenderSettings.fogStartDistance=55;RenderSettings.fogEndDistance=205;
  var light=UnityEngine.Object.FindFirstObjectByType<Light>();if(light){light.transform.rotation=Quaternion.Euler(45,-30,0);light.color=new Color(1,.92f,.83f);light.intensity=1.35f;light.shadows=LightShadows.Soft;light.shadowStrength=.65f;}
  if(Camera.main)Camera.main.clearFlags=CameraClearFlags.Skybox;
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Polish();AssetDatabase.Refresh();Debug.Log("Dream Candy World created: ground, 16 sweet platforms, cottages, clouds and distant castle. Player systems unchanged.");
 }
}
}