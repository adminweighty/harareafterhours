"""Repair the supplied SUV without altering its original Blender file."""
import bpy, bmesh, math, json, os
from collections import defaultdict
from mathutils import Vector, Matrix

SOURCE='/Users/udeanmbano/StudioProjects/car-prototype/source/mercedes_gls/uploads_files_2787791_Mercedes+Benz+GLS+580.blend'
OUT='/private/tmp/harare-vehicle-repair/export'
os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=SOURCE)
body=bpy.data.objects['Mercedes Benz GLS 580']
body.data.transform(body.matrix_world)
body.parent=None
body.matrix_world=Matrix.Identity(4)
points=[v.co for v in body.data.vertices]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
scale=5.207/(hi.y-lo.y)
origin=Vector(((hi.x+lo.x)/2,(hi.y+lo.y)/2,lo.z))
for v in body.data.vertices: v.co=(v.co-origin)*scale
body.data.update()
for obj in list(bpy.data.objects):
    if obj!=body: bpy.data.objects.remove(obj,do_unlink=True)
slots=[m.name if m else '' for m in body.data.materials]
tyre_ids={i for i,n in enumerate(slots) if 'tyre' in n.lower()}
centres={}
for side,sgn in [('R',-1),('L',1)]:
    for axle,front in [('F',True),('R',False)]:
        ids={v for p in body.data.polygons if p.material_index in tyre_ids for v in p.vertices
             if body.data.vertices[v].co.x*sgn>0 and (body.data.vertices[v].co.y<0)==front}
        pts=[body.data.vertices[i].co for i in ids]
        mn=Vector(tuple(min(p[i] for p in pts) for i in range(3)))
        mx=Vector(tuple(max(p[i] for p in pts) for i in range(3)))
        centres['Wheel_'+axle+side]=((mn+mx)*.5,(mx.z-mn.z)*.5)
print('WHEELS', {k:(list(v[0]),v[1]) for k,v in centres.items()})

def category(name,p):
    n=name.lower()
    if 'tyre' in n: return 'Tyre'
    if 'windowstint' in n: return 'Glass'
    if 'polar' in n: return 'Paint'
    if 'lights_glass' in n: return 'TailLight' if p.center.y>0 else 'HeadLight'
    if 'color_a07' in n or 'color_a08' in n: return 'TailLight'
    if 'interior' in n: return 'Interior'
    if 'grille' in n or 'chrome' in n or 'color_m0' in n: return 'Metal'
    return 'Trim'

groups=defaultdict(list)
for p in body.data.polygons:
    cat=category(slots[p.material_index],p)
    group='Glass' if cat=='Glass' else 'Body'
    for name,(centre,radius) in centres.items():
        radial=(p.center.y-centre.y)**2+(p.center.z-centre.z)**2
        if abs(p.center.x-centre.x)<.25 and radial<(radius*1.015)**2:
            group=name
            cat='Tyre' if p.material_index in tyre_ids else 'WheelMetal'
            break
    groups[group].append((p,cat))

colors={'Paint':(.34,.40,.44,1),'Glass':(.13,.19,.23,.2),'Tyre':(.025,.028,.032,1),
        'Interior':(.10,.10,.09,1),'Trim':(.055,.06,.065,1),'Metal':(.30,.32,.34,1),
        'WheelMetal':(.48,.50,.52,1),'HeadLight':(.8,.86,.9,1),'TailLight':(.55,.015,.015,1)}
materials={}
for name,color in colors.items():
    m=bpy.data.materials.new('Vehicle_'+name); m.diffuse_color=color; m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=color
    bs.inputs['Metallic'].default_value=.75 if name in ('Metal','WheelMetal') else .1 if name=='Paint' else 0
    bs.inputs['Roughness'].default_value=.3 if name in ('Paint','Metal','WheelMetal','Glass') else .8
    materials[name]=m

meshes=[]
source_uv=body.data.uv_layers.active
for name,polygons in groups.items():
    ids=sorted({v for p,c in polygons for v in p.vertices}); remap={v:i for i,v in enumerate(ids)}
    centre=centres[name][0] if name in centres else Vector()
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata([body.data.vertices[v].co-centre for v in ids],[],[[remap[v] for v in p.vertices] for p,c in polygons])
    cats=list(dict.fromkeys(c for p,c in polygons))
    for c in cats: mesh.materials.append(materials[c])
    uv=mesh.uv_layers.new(name='UVMap')
    for target,(source,cat) in zip(mesh.polygons,polygons):
        target.material_index=cats.index(cat); target.use_smooth=True
        for a,b in zip(target.loop_indices,source.loop_indices):
            if source_uv: uv.data[a].uv=source_uv.data[b].uv
    # The source duplicates vertices around faces. Weld coincident vertices
    # before reduction so continuous door/bonnet panels retain coherent normals.
    bm=bmesh.new(); bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00005)
    for edge in bm.edges:
        if edge.is_manifold: edge.smooth=edge.calc_face_angle()<math.radians(35)
    bm.to_mesh(mesh); bm.free(); mesh.update()
    obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj); obj.location=centre
    meshes.append(obj)
bpy.data.objects.remove(body,do_unlink=True)
manifest={'base':'SharedSUV','source':SOURCE,'length_m':5.207,'wheels':{n:{'centre_blender':list(c),'radius':r} for n,(c,r) in centres.items()},'lods':[]}
silhouette_meshes={}
for lod,ratio in enumerate((.14,.058,.022)):
    copies=[]
    root=bpy.data.objects.new('SharedSUV_LOD'+str(lod),None); bpy.context.collection.objects.link(root)
    for original in meshes:
        obj=original.copy()
        preserve_shell=lod>0 and original.name in silhouette_meshes
        obj.data=(silhouette_meshes[original.name] if preserve_shell else original.data).copy()
        bpy.context.collection.objects.link(obj)
        obj.parent=root
        bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj
        if lod==2 and original.name=='Body':
            bm=bmesh.new(); bm.from_mesh(obj.data)
            interior={i for i,m in enumerate(obj.data.materials) if 'Interior' in m.name}
            bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index in interior],context='FACES')
            unseen=set(bm.verts); tiny=[]
            while unseen:
                seed=unseen.pop(); island=[seed]; queue=[seed]
                while queue:
                    v=queue.pop()
                    for edge in v.link_edges:
                        other=edge.other_vert(v)
                        if other in unseen: unseen.remove(other); queue.append(other); island.append(other)
                extent=Vector(tuple(max(v.co[a] for v in island)-min(v.co[a] for v in island) for a in range(3)))
                if extent.length<.10: tiny.extend(island)
            if tiny: bmesh.ops.delete(bm,geom=tiny,context='VERTS')
            bm.to_mesh(obj.data);bm.free()
        # Aggressive collapse breaks the thin, disconnected source body panels.
        # Keep the verified near shell for every LOD; simplify wheels and remove
        # unseen far interior/tiny details instead of destroying the silhouette.
        if not preserve_shell:
            mod=obj.modifiers.new('Mobile geometry','DECIMATE')
            mod.ratio=max(ratio,.058) if original.name.startswith('Wheel') else ratio
            bpy.ops.object.modifier_apply(modifier=mod.name)
        normal=obj.modifiers.new('Panel weighted normals','WEIGHTED_NORMAL');normal.keep_sharp=True;normal.weight=50
        bpy.ops.object.modifier_apply(modifier=normal.name)
        tri=obj.modifiers.new('Stable triangulation','TRIANGULATE'); bpy.ops.object.modifier_apply(modifier=tri.name)
        if lod==0 and original.name in ('Body','Glass'):
            silhouette_meshes[original.name]=obj.data.copy()
        obj.name=original.name+'_LOD'+str(lod)
        copies.append(obj)
    bpy.ops.object.select_all(action='DESELECT'); root.select_set(True)
    for obj in copies: obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=f'{OUT}/SharedSUV_LOD{lod}.fbx',use_selection=True,object_types={'EMPTY','MESH'},
        use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL')
    triangles=sum(len(o.data.polygons) for o in copies)
    manifest['lods'].append({'lod':lod,'triangles':triangles})
    print('LOD',lod,triangles,[(o.name,len(o.data.polygons)) for o in copies])
    for obj in copies: bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.objects.remove(root,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=OUT+'/SharedSUV_Editable.blend')
with open(OUT+'/manifest.json','w') as f: json.dump(manifest,f,indent=2)
