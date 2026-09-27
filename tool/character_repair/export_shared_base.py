import bpy
import json
import os
import shutil

P = '/Users/udeanmbano/StudioProjects/character-prototype/v2'
SOURCE = P + '/delivery/Character01_Editable.blend'
OUT = '/private/tmp/harare-character-repair/repaired'
TEXTURES = OUT + '/Textures'
os.makedirs(TEXTURES, exist_ok=True)

# Ratios target a practical mobile hero-character budget while leaving the editable source untouched.
LODS = {
    'LOD0': {'Male_Foundation': .44, 'Field_Jacket_and_Trousers': .65, 'Sneakers': .75, 'Hair_Cropped_Fade': .65, 'Eyebrows': 1.0, 'Eyes': 1.0, 'Compact_Backpack': 1.0, 'Backpack_Outer_Pocket': 1.0, 'Necklace': .8},
    'LOD1': {'Male_Foundation': .23, 'Field_Jacket_and_Trousers': .40, 'Sneakers': .40, 'Hair_Cropped_Fade': .35, 'Eyebrows': .7, 'Eyes': 1.0, 'Compact_Backpack': .7, 'Backpack_Outer_Pocket': .7, 'Necklace': .35},
    'LOD2': {'Male_Foundation': .11, 'Field_Jacket_and_Trousers': .18, 'Sneakers': .22, 'Hair_Cropped_Fade': .15, 'Eyebrows': .4, 'Eyes': .7, 'Compact_Backpack': .5, 'Backpack_Outer_Pocket': .5, 'Necklace': .2},
}

def activate(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

def remove_shape_keys(obj):
    if obj.data.shape_keys:
        activate(obj)
        # Bake the fitted body, including facial refinements, BEFORE removing keys.
        # Dropping keys without apply_mix exports the unfitted Basis beneath fitted clothes.
        bpy.ops.object.shape_key_remove(all=True, apply_mix=True)

def apply_masks_before_skinning(obj):
    # The MPFB body uses Mask modifiers for helper geometry and garment coverage.
    # Moving each mask above Armature preserves undeformed coordinates and all bone weights.
    for mod in list(obj.modifiers):
        if mod.type != 'MASK':
            continue
        activate(obj)
        while obj.modifiers.find(mod.name) > 0:
            bpy.ops.object.modifier_move_up(modifier=mod.name)
        bpy.ops.object.modifier_apply(modifier=mod.name)

def simplify(obj, ratio):
    remove_shape_keys(obj)
    apply_masks_before_skinning(obj)
    for mod in list(obj.modifiers):
        if mod.type == 'SUBSURF':
            if obj.name == 'Field_Jacket_and_Trousers':
                # Retain the source garment's subdivision before skinning. Its coarse
                # control cage otherwise produces angular crotch/knee deformation.
                mod.levels = 1
                activate(obj)
                while obj.modifiers.find(mod.name) > 0:
                    bpy.ops.object.modifier_move_up(modifier=mod.name)
                bpy.ops.object.modifier_apply(modifier=mod.name)
                ratio *= .45
            else:
                obj.modifiers.remove(mod)
    if ratio >= .999:
        return
    activate(obj)
    mod = obj.modifiers.new('Mobile_LOD', 'DECIMATE')
    mod.decimate_type = 'COLLAPSE'
    mod.ratio = ratio
    while obj.modifiers.find(mod.name) > 0:
        bpy.ops.object.modifier_move_up(modifier=mod.name)
    bpy.ops.object.modifier_apply(modifier=mod.name)

def triangle_count(objects):
    return sum(sum(max(0, len(poly.vertices) - 2) for poly in obj.data.polygons) for obj in objects)

def join_for_runtime(meshes, lod_name):
    # All parts share the same armature, so a joined skinned mesh avoids nine separate skinning components on mobile.
    bpy.ops.object.select_all(action='DESELECT')
    for mesh in meshes:
        mesh.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    meshes[0].name = 'Character01_' + lod_name + '_Mesh'
    return [meshes[0]]

def copy_referenced_textures():
    copied = []
    for image in bpy.data.images:
        if image.source != 'FILE':
            continue
        source = image.filepath_from_user()
        if not source or not os.path.isfile(source):
            continue
        destination = os.path.join(TEXTURES, os.path.basename(source))
        if not os.path.isfile(destination):
            shutil.copy2(source, destination)
        copied.append(os.path.basename(destination))
    return sorted(set(copied))

manifest = {'rig': 'FoundationRig', 'bones': 53, 'lods': {}, 'textures': []}
for lod_name, ratios in LODS.items():
    bpy.ops.wm.open_mainfile(filepath=SOURCE)
    rig = bpy.data.objects['FoundationRig']
    meshes = [bpy.data.objects[name] for name in ratios]
    for mesh in meshes:
        simplify(mesh, ratios[mesh.name])
    meshes = join_for_runtime(meshes, lod_name)
    texture_names = copy_referenced_textures()
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    for mesh in meshes:
        mesh.select_set(True)
    bpy.context.view_layer.objects.active = rig
    destination = OUT + '/Character01_' + lod_name + '.fbx'
    bpy.ops.export_scene.fbx(
        filepath=destination,
        use_selection=True,
        object_types={'ARMATURE', 'MESH'},
        use_mesh_modifiers=False,
        add_leaf_bones=False,
        use_armature_deform_only=True,
        bake_anim=False,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        mesh_smooth_type='FACE',
        path_mode='AUTO',
    )
    manifest['lods'][lod_name] = {
        'file': os.path.basename(destination),
        'triangles': triangle_count(meshes),
        'meshes': {mesh.name: sum(max(0, len(poly.vertices) - 2) for poly in mesh.data.polygons) for mesh in meshes},
    }
    manifest['textures'] = sorted(set(manifest['textures'] + texture_names))
    print(lod_name, manifest['lods'][lod_name])

with open(OUT + '/character01_manifest.json', 'w') as handle:
    json.dump(manifest, handle, indent=2)
print('EXPORTED', json.dumps(manifest, indent=2))
