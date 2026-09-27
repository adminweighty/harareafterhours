import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='/private/tmp/harare-character-repair/quaternius/Universal Animation Library[Standard]/Unity/UAL1_Standard_RM.fbx')
rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
scene = bpy.context.scene
print('FPS', scene.render.fps, scene.render.fps_base)
for action in bpy.data.actions:
    if not any(action.name.endswith(n) for n in ('|Walk_Loop', '|Jog_Fwd_Loop', '|Sprint_Loop')):
        continue
    rig.animation_data.action = action
    if action.slots:
        rig.animation_data.action_slot = action.slots[0]
    first, last = action.frame_range
    samples = []
    for frame in (first, last):
        scene.frame_set(int(frame))
        samples.append(rig.matrix_world @ rig.pose.bones['root'].head)
    duration = (last - first) / (scene.render.fps / scene.render.fps_base)
    print('ROOT_STRIDE', action.name, duration, list(samples[1] - samples[0]), (samples[1] - samples[0]).length / duration)
