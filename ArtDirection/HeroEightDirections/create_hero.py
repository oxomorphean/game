"""Original chibi model, rig and eight-direction sprite animations. Blender 4.5 LTS.
Run: Blender -b --python create_hero.py -- --output DIR [--preview]
No downloaded model, texture or frame generation service is used.
"""
import argparse
import math
import sys
import json
from pathlib import Path
import bpy
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view

parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True)
parser.add_argument('--preview', action='store_true')
parser.add_argument('--only', default='')
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
out = Path(args.output)
out.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE_NEXT'
scene.render.resolution_x = scene.render.resolution_y = 192
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.image_settings.color_depth = '8'
scene.render.image_settings.compression = 70
scene.render.fps = 16
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
scene.world.color = (.2, .2, .2)

def rgb(h):
    h = h.lstrip('#')
    values = [int(h[i:i+2], 16) / 255 for i in (0, 2, 4)]
    return tuple(v / 12.92 if v < .04045 else ((v + .055) / 1.055) ** 2.4 for v in values)

def material(name, colors, flat=False):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.diffuse_color = (*rgb(colors[-1]), 1)
    n = m.node_tree.nodes
    n.clear()
    output = n.new('ShaderNodeOutputMaterial')
    emit = n.new('ShaderNodeEmission')
    m.node_tree.links.new(emit.outputs[0], output.inputs['Surface'])
    if flat:
        emit.inputs['Color'].default_value = (*rgb(colors[-1]), 1)
    else:
        diffuse = n.new('ShaderNodeBsdfDiffuse')
        diffuse.inputs['Color'].default_value = (.8, .8, .8, 1)
        to_rgb = n.new('ShaderNodeShaderToRGB')
        ramp = n.new('ShaderNodeValToRGB')
        ramp.color_ramp.interpolation = 'CONSTANT'
        ramp.color_ramp.elements.remove(ramp.color_ramp.elements[1])
        for i, (threshold, c) in enumerate(zip((.0, .22, .60), colors)):
            el = ramp.color_ramp.elements[0] if i == 0 else ramp.color_ramp.elements.new(threshold)
            el.position = threshold
            el.color = (*rgb(c), 1)
        m.node_tree.links.new(diffuse.outputs[0], to_rgb.inputs[0])
        m.node_tree.links.new(to_rgb.outputs[0], ramp.inputs[0])
        m.node_tree.links.new(ramp.outputs[0], emit.inputs['Color'])
    return m

ink = material('Ink · midnight blue', ['19283B'], True)
ink.use_backface_culling = True
# Explicit backface rejection also works when a renderer ignores raster culling.
outline_material = bpy.data.materials.new('Outline hull')
outline_material.use_nodes = True
nodes=outline_material.node_tree.nodes;nodes.clear()
g=nodes.new('ShaderNodeNewGeometry');mix=nodes.new('ShaderNodeMixShader')
e=nodes.new('ShaderNodeEmission');e.inputs['Color'].default_value=(*rgb('19283B'),1)
t=nodes.new('ShaderNodeBsdfTransparent');o=nodes.new('ShaderNodeOutputMaterial')
outline_material.node_tree.links.new(g.outputs['Backfacing'],mix.inputs[0])
outline_material.node_tree.links.new(e.outputs[0],mix.inputs[1])
outline_material.node_tree.links.new(t.outputs[0],mix.inputs[2])
outline_material.node_tree.links.new(mix.outputs[0],o.inputs['Surface'])
skin = material('Skin', ['BF7F6C', 'F3B990', 'FFE0B4'])
hair = material('Silver hair', ['768AA4', 'BDCCDF', 'F4F2E7'])
ivory = material('Ivory robe', ['758E9F', 'CADAD9', 'FAF2D6'])
navy = material('Indigo robe', ['1E2D48', '354E70', '5B7997'])
teal = material('Jade cloth', ['164A52', '247C7C', '54B6A0'])
gold = material('Antique gold', ['865932', 'C79B51', 'F5D58A'])
steel = material('Sword steel', ['658297', 'B9D9DB', 'ECFFF3'])
white = material('Eye ivory', ['FFF9E9'], True)
iris = material('Jade iris', ['308C91'], True)
glint = material('Eye highlight', ['FFFFFF'], True)

arm = bpy.data.armatures.new('Cultivator skeleton')
rig = bpy.data.objects.new('Cultivator · 8 directions', arm)
scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
spec = {
    'root': ((0, 0, 0), (0, 0, .35), None),
    'body': ((0, 0, .8), (0, 0, 1.45), 'root'),
    'head': ((0, 0, 1.48), (0, 0, 2.15), 'body'),
    'arm.L': ((-.43, 0, 1.37), (-.54, -.05, .89), 'body'),
    'arm.R': ((.43, 0, 1.37), (.54, -.05, .89), 'body'),
    'leg.L': ((-.19, 0, .78), (-.19, 0, .12), 'root'),
    'leg.R': ((.19, 0, .78), (.19, 0, .12), 'root'),
    'tail': ((0, .25, 2.28), (0, .56, 1.98), 'head'),
}
for name, (head, tail, parent) in spec.items():
    bone = arm.edit_bones.new(name)
    bone.head, bone.tail = head, tail
    if parent:
        bone.parent = arm.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
rig.select_set(False)
for bone in rig.pose.bones:
    bone.rotation_mode = 'XYZ'

def finish(obj, name, mat, bone, outline=True, smooth=True):
    obj.name = name
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.select_set(False)
    obj.parent = rig
    obj.data.materials.append(mat)
    for p in obj.data.polygons:
        p.use_smooth = smooth
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    deform = obj.modifiers.new('Armature', 'ARMATURE')
    deform.object = rig
    if outline:
        shell = bpy.data.objects.new(name + ' · outline', obj.data.copy())
        scene.collection.objects.link(shell)
        shell.parent = rig
        shell.data.materials.clear()
        shell.data.materials.append(outline_material)
        for v in shell.data.vertices:
            v.co += v.normal * .012
        for p in shell.data.polygons:
            p.flip()
        shell.data.update()
        g = shell.vertex_groups.new(name=bone)
        g.add(list(range(len(shell.data.vertices))), 1, 'REPLACE')
        mod = shell.modifiers.new('Armature', 'ARMATURE')
        mod.object = rig
    return obj

def oval(name, loc, size, mat, bone='body', outline=True):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=16, location=loc)
    o = bpy.context.object
    o.scale = size
    return finish(o, name, mat, bone, outline)

def mesh(name, verts, faces, mat, bone='body', outline=True, smooth=False):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    return finish(ob, name, mat, bone, outline, smooth)

def ring_body(name, rings, mat, bone='body', n=12):
    verts = [(rx*math.cos(2*math.pi*i/n), cy + ry*math.sin(2*math.pi*i/n), z)
             for z, rx, ry, cy in rings for i in range(n)]
    faces = []
    for r in range(len(rings)-1):
        for i in range(n):
            j=(i+1)%n; faces.append((r*n+i, r*n+j, (r+1)*n+j, (r+1)*n+i))
    faces += [tuple(reversed(range(n))), tuple((len(rings)-1)*n+i for i in range(n))]
    return mesh(name, verts, faces, mat, bone, smooth=True)

def ribbon(name, points, widths, mat, bone='body'):
    verts=[]
    for (x,y,z),w in zip(points,widths):
        verts += [(x-w,y,z),(x,y-.045,z+.02),(x+w,y,z)]
    faces=[]
    for i in range(len(points)-1):
        for j in range(2):
            a=i*3+j;faces.append((a,a+1,a+4,a+3))
    ob=mesh(name,verts,faces,mat,bone,outline=False)
    solid=ob.modifiers.new('Cloth thickness','SOLIDIFY');solid.thickness=.018
    return ob

oval('Face', (0,-.045,1.96), (.53,.445,.51),skin,'head')
oval('Ear L',(-.51,-.03,1.88),(.11,.08,.14),skin,'head')
oval('Ear R',(.51,-.03,1.88),(.11,.08,.14),skin,'head')
def lock(name,points,widths,depths):
    verts=[]
    for (x,y,z),w,d in zip(points,widths,depths):
        verts.extend([(x-w,y,z),(x,y-d,z+.025),(x+w,y,z),(x,y+d,z-.025)])
    faces=[(3,2,1,0)]
    for j in range(len(points)-1):
        for i in range(4):faces.append((j*4+i,j*4+(i+1)%4,(j+1)*4+(i+1)%4,(j+1)*4+i))
    faces.append(tuple((len(points)-1)*4+i for i in range(4)))
    return mesh(name,verts,faces,hair,'head',False,True)

oval('Hair crown',(0,.10,2.28),(.57,.43,.38),hair,'head')
for i,(x,ex,z) in enumerate([(-.43,-.56,1.96),(-.23,-.31,2.17),(-.02,-.12,2.12),(.20,.32,2.21),(.43,.57,1.96)]):
    lock('Swept fringe '+str(i),[(x*.5,-.15,2.58),(x,-.39,2.40),(ex,-.48,z)],[.14,.16,.005],[.07,.065,.007])
for s in (-1,1):
    for i in range(3):
        lock('Side lock '+str(s)+' '+str(i),[(s*.43,.01+i*.11,2.40),(s*.55,.02+i*.12,2.13),(s*(.58+i*.015),.03+i*.13,1.84+i*.04)],[.11,.095,.005],[.07,.09,.008])
for i in range(5):
    a=(i-2)*.20
    lock('Back lock '+str(i),[(a,.39,2.43),(a*1.1,.48,2.19),(a*1.03,.40,1.84)],[.12,.13,.008],[.06,.07,.005])
oval('Topknot',(0,.29,2.60),(.26,.23,.24),hair,'head')
oval('Topknot jade tie',(0,.11,2.58),(.16,.075,.065),teal,'head')
ribbon('Hair ribbon L',[(-.1,.33,2.59),(-.3,.50,2.22),(-.28,.57,1.99)],[.07,.085,.045],teal,'tail')
ribbon('Hair ribbon R',[(.1,.33,2.59),(.21,.53,2.23),(.38,.6,2.11)],[.065,.075,.03],teal,'tail')
for s in (-1,1):
    oval('Eye socket '+str(s),(s*.207,-.450,2.084),(.165,.028,.129),ink,'head',False)
    oval('Eye white '+str(s),(s*.207,-.477,2.081),(.137,.016,.104),white,'head',False)
    oval('Jade eye '+str(s),(s*.198,-.494,2.085),(.067,.01,.085),iris,'head',False)
    oval('Pupil '+str(s),(s*.198,-.501,2.096),(.029,.008,.057),ink,'head',False)
    oval('Eye glint '+str(s),(s*.198-.018,-.51,2.136),(.024,.007,.026),glint,'head',False)
    ribbon('Brow '+str(s),[(s*.32,-.461,2.24),(s*.19,-.483,2.26),(s*.10,-.454,2.22)],[.011,.016,.006],ink,'head')
oval('Nose',(0,-.49,1.92),(.032,.039,.036),skin,'head',False)
ribbon('Smile',[(-.073,-.456,1.815),(0,-.474,1.799),(.062,-.456,1.824)],[.008,.009,.005],ink,'head')

ring_body('Tunic',[(.67,.31,.21,0),(.91,.26,.21,0),(1.3,.34,.23,0),(1.48,.20,.16,0)],navy)
ring_body('Belt',[(.87,.29,.226,0),(.99,.29,.23,0)],teal)
oval('Jade clasp',(.03,-.24,.94),(.105,.048,.115),gold)
oval('Jade clasp center',(.03,-.285,.94),(.066,.024,.079),teal,outline=False)
ribbon('White lapel',[(-.17,-.158,1.48),(.12,-.257,1.18),(.18,-.231,1.02)],[.085,.06,.045],ivory)
ribbon('Gold lapel border',[(-.26,-.165,1.45),(.052,-.269,1.15),(.13,-.245,1.015)],[.014,.013,.011],gold)
ribbon('Inner collar',[(.17,-.16,1.48),(-.12,-.255,1.17)],[.045,.045],ivory)
for s,side in ((-1,'L'),(1,'R')):
    oval('Trouser '+side,(s*.19,0,.56),(.17,.185,.30),navy,'leg.'+side)
    oval('Boot '+side,(s*.19,-.09,.16),(.16,.26,.16),navy,'leg.'+side)
    oval('Gold boot cuff '+side,(s*.19,-.005,.31),(.173,.19,.035),gold,'leg.'+side)
    oval('Shoulder mantle '+side,(s*.39,.005,1.33),(.265,.24,.19),ivory,'arm.'+side)
    oval('Gold shoulder pin '+side,(s*.42,-.201,1.39),(.067,.036,.063),gold,'arm.'+side)
    oval('Sleeve '+side,(s*.49,-.015,1.10),(.184,.20,.26),navy,'arm.'+side)
    oval('Sleeve cuff '+side,(s*.54,-.04,.92),(.18,.19,.046),gold,'arm.'+side)
    oval('Hand '+side,(s*.55,-.063,.84),(.126,.131,.139),skin,'arm.'+side)
    ribbon('Outer robe '+side,[(s*.20,-.02,1.19),(s*.31,-.10,.80),(s*.40,-.055,.48)],[.12,.15,.15],ivory,'leg.'+side)
    ribbon('Robe gold hem '+side,[(s*.25,-.102,.52),(s*.40,-.09,.49),(s*.54,-.052,.51)],[.014,.018,.013],gold,'leg.'+side)
ribbon('Back coat left',[(-.14,.24,1.31),(-.25,.28,.93),(-.3,.35,.50)],[.16,.16,.17],ivory)
ribbon('Back coat right',[(.14,.24,1.31),(.25,.28,.93),(.3,.35,.50)],[.16,.16,.17],ivory)
ribbon('Jade sash',[(.12,-.245,.94),(.27,-.255,.76),(.17,-.27,.56)],[.085,.095,.05],teal)
ribbon('Jade sash side',[(.14,-.246,.93),(.37,-.18,.82),(.45,-.19,.7)],[.055,.065,.025],teal)
oval('Sword handle',(.60,-.15,.79),(.049,.10,.12),teal,'arm.R')
oval('Pommel',(.56,-.064,.89),(.072,.055,.058),gold,'arm.R')
mesh('Sword guard',[(.45,-.20,.65),(.67,-.32,.66),(.83,-.27,.67),(.70,-.29,.73),(.63,-.22,.76),(.43,-.14,.72)],[(0,1,2,3,4,5)],gold,'arm.R',False)
blade=[(.62,-.28,.68),(.76,-.37,.70),(.86,-.37,.55),(1.05,-.50,.18),(.88,-.45,.25),(.62,-.36,.54),(.74,-.38,.52)]
mesh('Jian blade',blade,[(0,1,6),(1,2,3,6),(3,4,5,6),(5,0,6)],steel,'arm.R',False)

bpy.ops.object.light_add(type='SUN',location=(-4,-6,8))
sun=bpy.context.object
sun.rotation_euler=(Vector((0,0,1))-sun.location).to_track_quat('-Z','Y').to_euler()
sun.data.energy=2.4
sun.data.angle=.4
bpy.ops.object.camera_add(location=(0,-8,10.83))
camera=bpy.context.object
target=Vector((0,0,1.30))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'
camera.data.ortho_scale=4.2
scene.camera=camera
scene.render.engine='BLENDER_EEVEE_NEXT'
if hasattr(scene,'eevee'):
    scene.eevee.taa_render_samples=16
clips={'idle':(8,8,True),'run':(12,18,True),'attack':(8,20,False),'sweep':(10,20,False),'dash':(4,16,True),'hurt':(4,16,False),'down':(6,12,False)}

def pose(clip,t):
    for b in rig.pose.bones:
        b.location=(0,0,0);b.rotation_euler=(0,0,0)
    b=rig.pose.bones;wave=math.sin(t*math.tau)
    b['body'].location.y=.012*wave
    b['tail'].rotation_euler.x=.07*math.sin(t*math.tau-.8)
    if clip=='run':
        b['leg.L'].rotation_euler.x=.65*wave;b['leg.R'].rotation_euler.x=-.65*wave
        b['arm.L'].rotation_euler.x=-.42*wave;b['arm.R'].rotation_euler.x=.32*wave
        b['body'].rotation_euler.x=.13;b['body'].location.y=.025*abs(wave)
        b['head'].rotation_euler.z=.04*wave
    elif clip in ('attack','sweep'):
        impact=(4/7 if clip=='attack' else 5/9)
        v=(-.8*t/.25 if t<.25 else -.8+2.1*(t-.25)/(impact-.25) if t<impact else 1.3*(1-(t-impact)/(1-impact)))
        b['arm.R'].rotation_euler.x=v*1.15;b['arm.R'].rotation_euler.z=v*.90
        b['arm.L'].rotation_euler.x=-v*.35;b['body'].rotation_euler.z=v*.3
        b['head'].rotation_euler.z=-v*.1
        if clip=='sweep':
            b['body'].rotation_euler.z=v*.85;b['arm.R'].rotation_euler.y=-v*.65
    elif clip=='dash':
        b['body'].rotation_euler.x=.42;b['head'].rotation_euler.x=-.15
        b['arm.L'].rotation_euler.x=-.75;b['arm.R'].rotation_euler.x=-.6
        b['leg.L'].rotation_euler.x=.65;b['leg.R'].rotation_euler.x=-.65
        b['tail'].rotation_euler.x=-.40
    elif clip=='hurt':
        b['body'].rotation_euler.x=-.18*math.sin(t*math.pi);b['head'].rotation_euler.x=-.12*math.sin(t*math.pi)
    elif clip=='down':
        fall=min(1,t*1.5)
        b['body'].rotation_euler.x=.5*fall;b['body'].location.y=-.43*fall
        b['leg.L'].rotation_euler.x=1.0*fall;b['leg.R'].rotation_euler.x=1.0*fall
        b['head'].rotation_euler.z=.15*fall;b['arm.L'].rotation_euler.z=.3*fall;b['arm.R'].rotation_euler.z=-.3*fall
    bpy.context.view_layer.update()

for clip,(count,fps,loop) in clips.items():
    rig.animation_data_clear()
    for frame in range(count):
        pose(clip,frame/(count if loop else max(1,count-1)))
        for bone in rig.pose.bones:
            bone.keyframe_insert(data_path='location',frame=frame+1,group=bone.name)
            bone.keyframe_insert(data_path='rotation_euler',frame=frame+1,group=bone.name)
    rig.animation_data.action.name=clip;rig.animation_data.action.use_fake_user=True
rig.animation_data_clear();pose('idle',0)
rig['source']='Original procedural model for Linh Thu Ky; no downloaded assets'
rig['directions']='E, NE, N, NW, W, SW, S, SE'
scene.frame_start=1;scene.frame_end=12
bpy.ops.wm.save_as_mainfile(filepath=str(out/'hero.blend'))
pivot=world_to_camera_view(scene,camera,Vector((0,0,0)))
dirs=['east','north-east','north','north-west','west','south-west','south','south-east']
manifest={'version':1,'frameSize':192,'pixelsPerUnit':100,'pivot':[pivot.x,pivot.y],
          'directions':dirs,'clips':[], 'source':'hero.blend','cameraElevationDegrees':math.degrees(math.atan2(9.53,8))}
if args.preview:
    for d in (6,7,0,2):
        rig.rotation_euler.z=math.radians(d*45+90);pose('idle',0)
        scene.render.filepath=str(out/('preview-'+dirs[d]+'.png'));bpy.ops.render.render(write_still=True)
else:
    for clip,(count,fps,loop) in clips.items():
        manifest['clips'].append({'id':clip,'frames':count,'fps':fps,'loop':loop,'impactPhase':4/7 if clip=='attack' else 5/9 if clip=='sweep' else 0})
        if args.only and args.only != clip:
            continue
        folder=out/'frames'/clip;folder.mkdir(parents=True,exist_ok=True)
        for d in range(8):
            rig.rotation_euler.z=math.radians(d*45+90)
            for f in range(count):
                pose(clip,f/(count if loop else max(1,count-1)))
                scene.render.filepath=str(folder/f'{d:02}-{f:02}.png');bpy.ops.render.render(write_still=True)
        (out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
        print('HERO_CLIP_DONE',clip,flush=True)
print('HERO_EXPORT_DONE',str(out),flush=True)
