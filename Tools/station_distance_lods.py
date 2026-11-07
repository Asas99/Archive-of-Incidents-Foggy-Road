"""Generate UV-preserving distant meshes from exported Unity local-space data."""
import bpy
import json
import os
import struct
import time
import numpy as np

ROOT = os.path.join(os.getcwd(), 'Recovery', 'StationDistanceLODs_20261107')
with open(os.path.join(ROOT, 'manifest.json'), encoding='utf-8-sig') as stream:
    entries = json.load(stream)['items']
results = []

for index, entry in enumerate(entries):
    started = time.time()
    with open(os.path.join(ROOT, entry['key'] + '.input'), 'rb') as stream:
        nv, nf = struct.unpack('<ii', stream.read(8))
        attributes = np.frombuffer(stream.read(nv * 32), dtype='<f4').reshape(nv, 8).copy()
        faces = np.frombuffer(stream.read(nf * 16), dtype='<i4').reshape(nf, 4).copy()
    positions, normals, uvs = attributes[:, :3], attributes[:, 3:6], attributes[:, 6:8]
    winding = faces[:, :3].copy()
    # Preserve Unity's original normals/winding convention through Blender.
    samples = winding[::max(1, nf // 1000)]
    cross = np.cross(positions[samples[:, 1]] - positions[samples[:, 0]],
                     positions[samples[:, 2]] - positions[samples[:, 0]])
    invert = np.sum(cross * normals[samples[:, 0]]) < 0
    if invert:
        winding[:, [1, 2]] = winding[:, [2, 1]]
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    mesh = bpy.data.meshes.new(entry['key'])
    mesh.from_pydata(positions.tolist(), [], winding.tolist())
    mesh.update()
    for slot in range(int(faces[:, 3].max()) + 1):
        mesh.materials.append(bpy.data.materials.new('slot_' + str(slot)))
    mesh.polygons.foreach_set('material_index', faces[:, 3].astype(np.int32))
    mesh.polygons.foreach_set('use_smooth', [True] * nf)
    layer = mesh.uv_layers.new(name='UVMap')
    vertex_indices = np.empty(len(mesh.loops), dtype=np.int32)
    mesh.loops.foreach_get('vertex_index', vertex_indices)
    layer.data.foreach_set('uv', uvs[vertex_indices].reshape(-1))
    mesh.normals_split_custom_set_from_vertices(normals.tolist())
    source = bpy.data.objects.new('source', mesh)
    bpy.context.collection.objects.link(source)
    source.hide_render = True
    for label, ratio in [('medium', .25), ('far', .05)]:
        obj = bpy.data.objects.new(label, mesh.copy())
        bpy.context.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        decimate = obj.modifiers.new('Distance simplification', 'DECIMATE')
        decimate.ratio = ratio
        decimate.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=decimate.name)
        transfer = obj.modifiers.new('Preserve source shading', 'DATA_TRANSFER')
        transfer.object = source
        transfer.use_loop_data = True
        transfer.data_types_loops = {'CUSTOM_NORMAL'}
        transfer.loop_mapping = 'POLYINTERP_NEAREST'
        bpy.ops.object.modifier_apply(modifier=transfer.name)
        out = obj.data
        out.calc_loop_triangles()
        output_attributes, output_faces = [], []
        mapping = {}
        for triangle in out.loop_triangles:
            indices = []
            for loop_index in triangle.loops:
                vi = out.loops[loop_index].vertex_index
                normal = out.corner_normals[loop_index].vector
                uv = out.uv_layers.active.data[loop_index].uv
                key = (vi, *tuple(round(v, 7) for v in normal), *tuple(round(v, 7) for v in uv))
                if key not in mapping:
                    mapping[key] = len(output_attributes)
                    output_attributes.append((*out.vertices[vi].co, *normal, *uv))
                indices.append(mapping[key])
            if invert:
                indices[1], indices[2] = indices[2], indices[1]
            output_faces.append((*indices, triangle.material_index))
        path = os.path.join(ROOT, entry['key'] + '_' + label + '.output')
        with open(path, 'wb') as stream:
            stream.write(struct.pack('<ii', len(output_attributes), len(output_faces)))
            stream.write(np.asarray(output_attributes, dtype='<f4').tobytes())
            stream.write(np.asarray(output_faces, dtype='<i4').tobytes())
        results.append({'key': entry['key'], 'level': label, 'triangles': len(output_faces),
                        'source_triangles': nf})
        bpy.data.objects.remove(obj, do_unlink=True)
    with open(os.path.join(ROOT, 'results.json'), 'w', encoding='utf-8') as stream:
        json.dump(results, stream, indent=2)
    print('LOD_PROGRESS', index + 1, len(entries), entry['name'], round(time.time() - started, 1), flush=True)
print('LOD_COMPLETE', len(entries), flush=True)
