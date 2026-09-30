"""Clearance checks on the delivered mesh, not just construction parameters."""
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def tree(objects):
    vertices,faces=[],[]
    for obj in objects:
        offset=len(vertices)
        vertices.extend(obj.matrix_world @ v.co for v in obj.data.vertices)
        faces.extend(tuple(offset+i for i in face.vertices) for face in obj.data.polygons)
    return BVHTree.FromPolygons(vertices,faces)


def check(parts,hull,f,report):
    import bpy
    bpy.context.view_layer.update()
    hull_top=max((hull.matrix_world @ v.co).z for v in hull.data.vertices)
    assert hull_top<=4.02*f[2], ('Bow/stern above rail',hull_top)
    ladder_top=max((parts['MastLadder'].matrix_world @ v.co).z for v in parts['MastLadder'].data.vertices)
    assert ladder_top<=16.56*f[2], ('Ladder extends above landing',ladder_top)
    report['bow_stern_below_rail_height']=True
    report['ladder_stiles_end_at_landing']=True
    shields=parts['Shields']
    surroundings=tree([hull,parts['DeckRail'],parts['UpperDeck']])
    assert not surroundings.overlap(tree([shields])), 'Shield intersects hull or deck railing'
    distances=[surroundings.find_nearest(shields.matrix_world @ v.co)[3] for v in shields.data.vertices]
    minimum=min(distances)
    assert minimum>.06,('Shield clearance',minimum)
    report['shield_minimum_vertex_clearance_m']=round(minimum,4)
    report['shields_no_surface_intersections']=True
    def corridor(objects,points,radius):
        surface=tree(objects)
        minimum=100
        for point in points:
            distance=surface.find_nearest(Vector(point))[3]
            minimum=min(minimum,distance)
            assert distance>radius,('Rail opening blocked',point,distance)
        return round(minimum,4)
    points=[((-4.1+i*.065)*f[0],0,(3.1+j*.12)*f[2]) for i in range(21) for j in range(13)]
    report['boarding_rail_portal_clearance_m']=corridor([parts['DeckRail'],shields],points,.27)
    points=[(0,(1.05+i*.05)*f[1],(16.92+j*.1)*f[2]) for i in range(21) for j in range(15)]
    report['lookout_rail_portal_clearance_m']=corridor([parts['Lookout']],points,.27)
    report['boarding_rail_clear_width_m']=round((1.56-.17)*f[1],3)
    report['stern_rudder_pivot_blender_m']=list(parts['Rudder'].location)
    report['helm_wheel_pivot_blender_m']=list(parts['HelmWheel'].location)


def check_finished_ends(parts,hull,f,report):
    import bpy
    import math
    deck=tree([parts['UpperDeck']])
    for y in (-9.6,-9.3,-9.0,9.0,9.3,9.6):
        hit=deck.ray_cast(Vector((0,y*f[1],3.2*f[2])),Vector((0,0,-1)),1.0)[0]
        assert hit is not None, ('Missing end floor',y)
    report['bow_and_stern_floor_samples_present']=True
    rails=tree([parts['DeckRail']])
    for y in (-9.55,9.55):
        hit=rails.ray_cast(Vector((0,y*f[1],4.2*f[2])),Vector((0,0,-1)),.7)[0]
        assert hit is not None, ('Missing end handrail',y)
    report['bow_and_stern_closing_rails_present']=True
    fixed=tree([hull,parts['DeckRail'],parts['UpperDeck']])
    yaw=parts['CannonYaw']
    for angle in range(-90,91,5):
        yaw.rotation_euler.z=math.radians(angle)
        bpy.context.view_layer.update()
        assert not fixed.overlap(tree([yaw,parts['CannonBarrel']])), ('Cannon hits ship',angle)
    yaw.rotation_euler.z=0
    bpy.context.view_layer.update()
    report['raised_forward_cannon_clear_sweep_samples']=37
