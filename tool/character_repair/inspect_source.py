import bpy
import json

bpy.ops.wm.open_mainfile(filepath='/Users/udeanmbano/StudioProjects/character-prototype/v2/delivery/Character01_Editable.blend')
for obj in bpy.data.objects:
    if obj.type == 'MESH':
        print('BASE_MESH', obj.name, len(obj.data.vertices), list(obj.dimensions),
              [(k.name, round(k.value, 3)) for k in obj.data.shape_keys.key_blocks if k.value] if obj.data.shape_keys else [],
              [(m.name, m.type) for m in obj.modifiers])
rig = bpy.data.objects['FoundationRig']
for name in ['pelvis', 'head', 'thigh_l', 'calf_l', 'foot_l', 'ball_l', 'upperarm_l']:
    bone = rig.data.bones[name]
    print('BASE_BONE', name, list(bone.head_local), list(bone.tail_local))
print('SHAPE_REMOVE_API', bpy.ops.object.shape_key_remove.get_rna_type().properties.keys())
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='/private/tmp/harare-character-repair/quaternius/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx')
for obj in bpy.data.objects:
    if obj.type == 'ARMATURE':
        print('ANIMATION_BONES', [b.name for b in obj.data.bones])
print('ANIMATIONS', [(a.name, list(a.frame_range)) for a in bpy.data.actions])
