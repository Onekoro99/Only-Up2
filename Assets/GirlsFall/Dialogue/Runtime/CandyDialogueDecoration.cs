using UnityEngine;
using UnityEngine.UI;
namespace GirlsFall.Dialogue {
 public enum CandyDecorationKind { Window, NameFlower, Mouse, Forward }
 [RequireComponent(typeof(CanvasRenderer))]
 public sealed class CandyDialogueDecoration:MaskableGraphic {
  [Tooltip("描画する飾りの種類です。背景の星と雲、名前ラベルの花、操作案内のマウス、スキップ矢印を選びます。")] public CandyDecorationKind kind;
  [Range(0,1),Tooltip("飾りの濃さです。大きいほど濃く、0で見えなくなります。文字やボタンの機能は変わりません。")] public float strength=.85f;
  Vector2 origin;VertexHelper mesh;
  Color Tone(Color c){c*=color;c.a*=strength;return c;}
  void Polygon(Vector2[] points,Color color){
   int at=mesh.currentVertCount;Vector2 center=Vector2.zero;foreach(var p in points)center+=p;center/=points.Length;
   mesh.AddVert(center+origin,Tone(color),Vector2.zero);foreach(var p in points)mesh.AddVert(p+origin,Tone(color),Vector2.zero);
   for(int i=0;i<points.Length;i++)mesh.AddTriangle(at,at+1+i,at+1+(i+1)%points.Length);
  }
  void Ellipse(Vector2 c,float rx,float ry,Color color){var p=new Vector2[32];for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/p.Length;p[i]=c+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry);}Polygon(p,color);}
  void Line(Vector2 a,Vector2 b,float width,Color color){var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Polygon(new[]{a+n,b+n,b-n,a-n},color);}
  void Star(Vector2 c,float size,Color color,float rotation=0){var p=new Vector2[10];for(int i=0;i<10;i++){float a=(90+rotation+i*36)*Mathf.Deg2Rad;float radius=i%2==0?size:size*.47f;p[i]=c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}Polygon(p,color);}
  void Flower(Vector2 c,float radius,Color color){for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Ellipse(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*.6f,radius*.48f,radius*.48f,color);}Ellipse(c,radius*.23f,radius*.23f,new Color(1,.94f,.66f,.95f));}
  Vector2 Curve(float t,float w,float h){float u=1-t;return u*u*u*new Vector2(w-35,h-4)+3*u*u*t*new Vector2(w-46,h*.65f)+3*u*t*t*new Vector2(w-72,h*.35f)+t*t*t*new Vector2(w-160,30);}
  protected override void OnPopulateMesh(VertexHelper vh){
   vh.Clear();mesh=vh;var r=rectTransform.rect;origin=r.min;float w=r.width,h=r.height;
   var pink=new Color(1,.60f,.79f,.35f);var blue=new Color(.63f,.88f,.96f,.45f);
   if(kind==CandyDecorationKind.Window){
    Polygon(new[]{new Vector2(0,0),new Vector2(w,0),new Vector2(w,h),new Vector2(0,h)},new Color(1,.85f,.94f,.07f));
    Ellipse(new Vector2(w-85,14),65,36,blue);Ellipse(new Vector2(w-128,24),35,27,blue);Ellipse(new Vector2(w-42,17),28,25,blue);
    Ellipse(new Vector2(20,4),50,22,new Color(.8f,.94f,1,.16f));
    var previous=Curve(0,w,h);for(int i=1;i<=32;i++){var next=Curve(i/32f,w,h);Line(previous,next,2,new Color(1,.85f,.58f,.42f));previous=next;}
    Flower(Curve(.03f,w,h),20,new Color(1,.72f,.84f,.32f));
    var colors=new[]{new Color(.57f,.88f,.8f,.55f),new Color(1,.83f,.46f,.55f),new Color(.76f,.64f,.91f,.5f),new Color(1,.55f,.75f,.5f)};
    for(int i=0;i<4;i++)Star(Curve(.24f+i*.2f,w,h),i==1?23:18,colors[i],i*17);
    Star(new Vector2(22,h-115),20,new Color(1,.88f,.54f,.2f),-16);
    Flower(new Vector2(52,h-90),10,new Color(.76f,.91f,.92f,.22f));
    Star(new Vector2(w-182,24),10,pink,12);
   }else if(kind==CandyDecorationKind.NameFlower){
    Flower(new Vector2(10,h*.5f),18,Color.white);Ellipse(new Vector2(26,h*.70f),3,3,new Color(1,.9f,.97f,1));
   }else if(kind==CandyDecorationKind.Forward){
    Color c=new Color(1,.36f,.64f,1);Polygon(new[]{new Vector2(2,1),new Vector2(w*.48f,h*.5f),new Vector2(2,h-1)},c);Polygon(new[]{new Vector2(w*.52f,1),new Vector2(w-1,h*.5f),new Vector2(w*.52f,h-1)},c);
   }else{
    Color c=new Color(.22f,.22f,.22f,1);float radius=w*.46f;var points=new Vector2[50];
    for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/(points.Length-1);float cy=a<Mathf.PI?h-radius:radius;points[i]=new Vector2(w*.5f+Mathf.Cos(a)*radius,cy+Mathf.Sin(a)*radius);}
    for(int i=1;i<points.Length;i++)Line(points[i-1],points[i],2,c);Line(points[points.Length-1],points[0],2,c);
    Line(new Vector2(w*.5f,h*.54f),new Vector2(w*.5f,h-1),2,c);Line(new Vector2(1,h*.54f),new Vector2(w-1,h*.54f),2,c);
    Polygon(new[]{new Vector2(3,h*.57f),new Vector2(w*.45f,h*.57f),new Vector2(w*.45f,h-4),new Vector2(3,h-9)},new Color(1,.63f,.8f,.75f));
   }
  }
#if UNITY_EDITOR
  protected override void OnValidate(){base.OnValidate();SetVerticesDirty();}
#endif
 }
}