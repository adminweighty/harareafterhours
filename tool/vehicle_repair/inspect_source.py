import bpy
from collections import Counter
from mathutils import Vector

bpy.ops.wm.open_mainfile(filepath='/Users/udeanmbano/StudioProjects/car-prototype/source/mercedes_gls/uploads_files_2787791_Mercedes+Benz+GLS+580.blend')
for o in bpy.data.objects:
    if o.type != 'MESH':
        continue
    print('MESH', o.name, len(o.data.vertices), list(o.dimensions), list(o.location))
    counts=Counter(p.material_index for p in o.data.polygons)
    for index, material in enumerate(o.data.materials):
        if not material or not counts[index]: continue
        points=[o.matrix_world @ o.data.vertices[v].co for p in o.data.polygons if p.material_index==index for v in p.vertices]
        lo=[round(min(p[a] for p in points),4) for a in range(3)]
        hi=[round(max(p[a] for p in points),4) for a in range(3)]
        print('SLOT',index,material.name,counts[index],lo,hi)
for image in bpy.data.images:
    if image.source=='FILE': print('TEXTURE',image.name,image.filepath)
