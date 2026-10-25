import bpy, math, random, os, json
from mathutils import Vector, noise
random.seed(318)
base=r'C:/Users/9shun/UnityProjects/Only-Up2'
out=base+'/Assets/GirlsFall/Environment/CakeSample'
for d in ['Models','Textures']: os.makedirs(out+'/'+d,exist_ok=True)
old=bpy.data.collections.get('GF_CakeSample')
if old and not old.objects: bpy.data.collections.remove(old)
if bpy.data.collections.get('GF_CakeSample'): raise RuntimeError('Sample already exists')
for m in list(bpy.data.materials):
 if m.name.startswith('GF_') and m.users==0: bpy.data.materials.remove(m)
col=bpy.data.collections.new('GF_CakeSample');bpy.context.scene.collection.children.link(col)
mats=[]
def mat(name,color,rough):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;mats.append(m);return m
sponge=mat('GF_Sponge',(0.83,.54,.26),.78)
cream=mat('GF_VanillaCream',(1,.91,.80),.36)
berry=mat('GF_Strawberry',(.81,.055,.10),.31)
leaf=mat('GF_MintLeaf',(.18,.40,.18),.65)
seed=mat('GF_GoldenSeed',(.95,.65,.26),.48)
choc=mat('GF_Chocolate',(.19,.065,.035),.29)
def tex(name,normal=False):
 n=512;im=bpy.data.images.new(name,width=n,height=n,alpha=False);pixels=[]
 heights=[];colors=[]
 for y in range(n):
  for x in range(n):
   a=noise.noise(Vector((x*.15,y*.15,4.3)));b=noise.noise(Vector((x*.61,y*.61,1.7)))
   pore=max(0,(a-.30)*2.4);heights.append(-pore*.7+b*.07);v=a*.085+b*.025-pore*.08
   colors.extend((max(0,min(1,.88+v)),max(0,min(1,.66+v)),max(0,min(1,.39+v*.8)),1))
 if normal:
  for y in range(n):
   for x in range(n):
    dx=(heights[y*n+(x+1)%n]-heights[y*n+(x-1)%n])*.55
    dy=(heights[((y+1)%n)*n+x]-heights[((y-1)%n)*n+x])*.55
    v=Vector((-dx,-dy,1)).normalized();pixels.extend((v.x*.5+.5,v.y*.5+.5,v.z*.5+.5,1))
 else: pixels=colors
 im.pixels.foreach_set(pixels);im.filepath_raw=out+'/Textures/'+name+'.png';im.file_format='PNG'
 if normal: im.colorspace_settings.name='Non-Color'
 im.save();im.pack();return im
albedo=tex('CakeSponge_BaseColor');normal=tex('CakeSponge_Normal',True)
nodes=sponge.node_tree.nodes;links=sponge.node_tree.links;p=next(n for n in nodes if n.type=='BSDF_PRINCIPLED');t=nodes.new('ShaderNodeTexImage');t.image=albedo;links.new(t.outputs['Color'],p.inputs['Base Color']);t=nodes.new('ShaderNodeTexImage');t.image=normal;nm=nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.35;links.new(t.outputs['Color'],nm.inputs['Color']);links.new(nm.outputs['Normal'],p.inputs['Normal'])
objects=[]
def mesh(name,verts,faces,material,uv=None):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();ob=bpy.data.objects.new(name,me);col.objects.link(ob);ob.data.materials.append(material)
 for p in me.polygons:p.use_smooth=True
 layer=me.uv_layers.new(name='UVMap')
 for poly in me.polygons:
  for li in poly.loop_indices:
   co=me.vertices[me.loops[li].vertex_index].co
   layer.data[li].uv=((math.atan2(co.y,co.x)/(2*math.pi))%1,co.z*2)
 objects.append(ob);return ob
def ringmesh(name,profile,material,n=96):
 verts=[]
 for j,(z,r) in enumerate(profile):
  for i in range(n):
   a=2*math.pi*i/n;rr=r*(1+.004*math.sin(a*7)+.002*math.cos(a*13+j*.5));verts.append((rr*math.cos(a),rr*math.sin(a),z))
 faces=[]
 for j in range(len(profile)-1):
  for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 faces.extend([tuple(reversed(range(n))),tuple((len(profile)-1)*n+i for i in range(n))])
 return mesh(name,verts,faces,material)
ringmesh('Lower_BakedSponge',[(0,1.43),(.012,1.475),(.04,1.485),(.205,1.485),(.235,1.46)],sponge)
ringmesh('Berry_ConfitRibbon',[(.236,1.458),(.248,1.482),(.29,1.48),(.302,1.457)],berry)
ringmesh('Vanilla_Filling',[(.21,1.455),(.232,1.489),(.25,1.493),(.267,1.485),(.28,1.46)],cream)
ringmesh('Upper_BakedSponge',[(.295,1.455),(.314,1.48),(.50,1.48),(.529,1.455)],sponge)
ringmesh('Flat_Walkable_Icing',[(.515,1.46),(.55,1.50),(.58,1.505),(.60,1.475),(.60,.001)],cream)
# Rounded drips with variable length, no collision.
n=192;verts=[];faces=[]
for j in range(6):
 t=j/5
 for i in range(n):
  a=2*math.pi*i/n;wave=(.5+.5*math.cos(a*13+.5))**7;wave2=(.5+.5*math.cos(a*19-1))**10
  bottom=.493-.115*wave-.055*wave2;z=.565*(1-t)+bottom*t
  r=1.487+.019*math.sin(math.pi*t)-.014*t**5
  verts.append((r*math.cos(a),r*math.sin(a),z))
for j in range(5):
 for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
mesh('Soft_Icing_Drips',verts,faces,cream)
def whip(cx,cy,scale=1):
 n=32;nr=18;v=[];f=[]
 for j in range(nr):
  t=j/(nr-1);z=.60+t*.32*scale;r=.145*scale*(1-t)**.7+.003
  for i in range(n):
   a=2*math.pi*i/n;rr=r*(1+.16*math.cos(6*a-t*7));v.append((cx+rr*math.cos(a)+.04*scale*t*t,cy+rr*math.sin(a),z))
 for j in range(nr-1):
  for i in range(n):f.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 mesh('Piped_WhippedCream',v,f,cream)
def strawberry(cx,cy,sc):
 n=32;nr=18;v=[];f=[]
 for j in range(nr):
  t=j/(nr-1);r=.14*sc*(math.sin(math.pi*(.12+.88*t)))**.8;z=.61+.32*sc*t
  for i in range(n):
   a=2*math.pi*i/n;d=.003*math.cos(a*8+j*2);v.append((cx+(r+d)*math.cos(a),cy+(r+d)*math.sin(a),z))
 for j in range(nr-1):
  for i in range(n):f.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 mesh('Fresh_Strawberry',v,f,berry)
 for row in range(1,6):
  t=row/7;r=.14*sc*math.sin(math.pi*(.12+.88*t))**.8
  for i in range(9):
   a=2*math.pi*(i+(row%2)*.5)/9;center=Vector((cx+(r+.002)*math.cos(a),cy+(r+.002)*math.sin(a),.61+.32*sc*t));tangent=Vector((-math.sin(a),math.cos(a),0));up=Vector((0,0,1));rad=Vector((math.cos(a),math.sin(a),0))
   pts=[tuple(center+tangent*.006*sc),tuple(center+up*.011*sc),tuple(center-tangent*.006*sc),tuple(center-up*.011*sc),tuple(center+rad*.004)]
   mesh('Berry_Seed',pts,[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],seed)
 for k in range(5):
  a=2*math.pi*k/5;z=.91*sc+.61-.32*sc+.32*sc # corrected below
  z=.61+.30*sc
  pts=[(cx,cy,z),(cx+.055*sc*math.cos(a-.45),cy+.055*sc*math.sin(a-.45),z+.01),(cx+.12*sc*math.cos(a),cy+.12*sc*math.sin(a),z-.035),(cx+.055*sc*math.cos(a+.45),cy+.055*sc*math.sin(a+.45),z+.01)]
  mesh('Strawberry_Calyx',pts,[(0,1,2),(0,2,3)],leaf)
for k in range(8):
 a=2*math.pi*k/8+.18
 whip(1.20*math.cos(a),1.20*math.sin(a),.85+random.random()*.17)
 if k%2==0:
  b=a+.30;strawberry(1.19*math.cos(b),1.19*math.sin(b),.82+random.random()*.16)
# Thin chocolate ribbons on rim, center remains clear.
for k in range(3):
 a=k*2.094+.5;v=[];f=[]
 for j in range(20):
  t=j/19;ang=a+(t-.5)*.38;r=1.25;z=.617+.055*math.sin(math.pi*t)
  for side in [-1,1]:v.append(((r+side*.025)*math.cos(ang),(r+side*.025)*math.sin(ang),z))
 for j in range(19):f.append((j*2,j*2+1,j*2+3,j*2+2))
 mesh('Chocolate_Ribbon',v,f,choc)
# Join into one six-material mesh.
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();hi=bpy.context.object;hi.name='GF_StrawberryCake_LOD0'
# Weld coincident vertices and fix all normals.
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.00001);bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
lo=hi.copy();lo.data=hi.data.copy();col.objects.link(lo);lo.name='GF_StrawberryCake_LOD1'
bpy.context.view_layer.objects.active=lo;hi.select_set(False);lo.select_set(True);mod=lo.modifiers.new('DistanceReduction','DECIMATE');mod.ratio=.38;bpy.ops.object.modifier_apply(modifier=mod.name)
lo.hide_set(True);hi.hide_set(False)
for o in [hi,lo]:
 o.data.calc_loop_triangles()
bpy.ops.object.select_all(action='DESELECT')
hi.select_set(True);lo.hide_set(False);lo.select_set(True);bpy.context.view_layer.objects.active=hi
fbx=out+'/Models/GF_StrawberryCake.fbx'
bpy.ops.export_scene.fbx(filepath=fbx,use_selection=True,object_types={'MESH'},global_scale=1,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,path_mode='STRIP',use_mesh_modifiers=True)
lo.hide_set(True)
os.makedirs(base+'/ArtSource/Blender',exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=base+'/ArtSource/Blender/GF_StrawberryCake.blend',copy=True)
print(json.dumps({'fbx':fbx,'objects':[{'name':o.name,'triangles':len(o.data.loop_triangles),'dimensions':list(o.dimensions),'materials':len(o.data.materials)} for o in [hi,lo]],'textures':[albedo.filepath_raw,normal.filepath_raw]}))
