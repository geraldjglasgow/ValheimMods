"""Local preview body from the game; original weights, 1.25x actual prefab size.

The game's mesh/materials are tagged reference and excluded from the shipping kit.
The editable rig uses the game's bone names with an added independent axe control.
"""
import bpy
from mathutils import Matrix, Vector
from workshop import prefab, prefab_parts, reference, scene, unity

SOURCE = 'Characters/Skeleton/Skeleton.prefab'
SCALE = 1.25


def build():
    tree = prefab._Prefab(SOURCE)
    loaded = prefab.load(SOURCE)
    low, high = prefab.bounds(loaded)
    offset = Vector((0, 0, -low.z * SCALE))
    transforms = {tree.name(i): i for i, (k, _) in tree.docs.items()
                  if k == 4 and tree.is_local(i) and tree.name(i) != 'evil_smoke'}
    hips = transforms['Hips']
    ids = []
    def descend(i):
        if tree.name(i) == 'evil_smoke':
            return
        ids.append(i)
        for child in tree.children(i):
            descend(child)
    descend(hips)
    positions = {tree.name(i): tree.world(i).translation * SCALE + offset for i in ids}
    data = bpy.data.armatures.new('BoneReaverRig')
    rig = bpy.data.objects.new('BoneReaverRig', data)
    bpy.context.collection.objects.link(rig)
    scene.select_only([rig])
    bpy.ops.object.mode_set(mode='EDIT')
    root = data.edit_bones.new('root')
    root.head, root.tail = (0,0,0), (0,1,0)
    for i in ids:
        name = tree.name(i)
        bone = data.edit_bones.new(name)
        bone.head = positions[name]
        children = [c for c in tree.children(i) if c in ids]
        target = positions[tree.name(children[0])] if children else bone.head + Vector((0,0,.06))
        bone.tail = target if (target - bone.head).length > .005 else bone.head + Vector((0,0,.06))
        if name in ('Hips', 'Spine2', 'Head'):
            next_name = {'Hips':'Spine','Spine2':'Neck','Head':'Helmet_attach'}[name]
            bone.tail = positions[next_name]
        father = unity.ref(unity.field(tree.docs[i][1], 'm_Father'))[0]
        bone.parent = data.edit_bones.get(tree.name(father)) if father in ids else root
    for name in ('axe', 'daggers', 'eye_glow', 'bag_daggers'):
        b = data.edit_bones.new(name)
        b.head, b.tail, b.parent = (0,0,0), (0,1,0), root
        if name in ('eye_glow','bag_daggers'):
            parent=data.edit_bones['Head' if name=='eye_glow' else 'Hips']
            b.head=parent.head
            b.tail=b.head+Vector((0,.1,0))
            b.parent=parent
    bpy.ops.object.mode_set(mode='OBJECT')
    rig.show_in_front = True
    data.display_type = 'STICK'
    for pb in rig.pose.bones:
        pb.rotation_mode = 'QUATERNION'
    skins = []
    for i in ids + [transforms['Skeleton']]:
        components = tree.components(i)
        if 137 not in components:
            continue
        renderer = components[137][1]
        path = unity.asset_path(unity.ref(unity.field(renderer, 'm_Mesh'))[1])
        obj = next(o for o in prefab.meshes(loaded) if o.data.name.startswith('Skeleton'))
        bpy.context.view_layer.update()
        world = obj.matrix_world.copy()
        obj.parent = None
        obj.data.transform(Matrix.Translation(offset) @ Matrix.Scale(SCALE,4) @ world)
        obj.matrix_world = Matrix.Identity(4)
        obj.name = 'REFERENCE_SkeletonBody'
        # This copy has been posed and resized; never reuse it as raw import data.
        for key in ('reference_source','reference_mesh'):
            if key in obj.data:
                del obj.data[key]
        raw = prefab_parts._vertex_data(unity.read(path))
        weights = prefab_parts._channel(raw, 12)
        indices = prefab_parts._channel(raw, 13)
        names = [tree.name(unity.ref(x)[0]) for x in unity.items(renderer, 'm_Bones')]
        groups = [obj.vertex_groups.new(name=n) for n in names]
        for v, (ws, bs) in enumerate(zip(weights, indices)):
            total = sum(ws)
            for w, b in zip(ws, bs):
                if w > 0:
                    groups[int(b)].add([v], float(w / total), 'REPLACE')
        bind(obj, rig)
        skins.append(obj)
    for o in list(loaded.children_recursive) + [loaded]:
        if o not in skins:
            bpy.data.objects.remove(o, do_unlink=True)
    rig['base_prefab'] = 'Skeleton'
    rig['height_multiplier'] = SCALE
    rig['vanilla_height_m'] = high.z - low.z
    rig['height_m'] = (high.z-low.z) * SCALE
    return rig, skins


def bind(obj, rig, bone=None):
    if bone:
        obj.vertex_groups.new(name=bone).add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    obj.parent = rig
    mod = obj.modifiers.new('Skeleton deformation', 'ARMATURE')
    mod.object = rig
    return obj


def rigid(obj, rig, bone):
    bpy.context.view_layer.update()
    obj.data.transform(obj.matrix_world)
    obj.matrix_world = Matrix.Identity(4)
    return bind(obj, rig, bone)
