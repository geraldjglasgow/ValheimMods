"""Painted aged bone: mineral mottling, hairline fractures, wear and soaked blood."""
import math
import numpy as np
import bpy
from workshop import materials

# Uneven fracture paths; geometry and texture use the same locations.
CRACKS=[[(.20,2.20),(.28,2.17),(.32,2.12),(.39,2.09),(.45,2.02)],
        [(.32,2.12),(.29,2.07),(.31,2.00)],
        [(.43,2.31),(.47,2.27),(.52,2.26),(.58,2.19)],
        [(.52,2.26),(.60,2.29),(.64,2.28)],
        [(.34,1.90),(.40,1.94),(.49,1.95),(.55,1.99)],
        [(.67,1.76),(.69,1.83),(.67,1.88)],
        [(.75,2.34),(.73,2.30),(.77,2.25)]]


def segments():
    return [(a,b) for path in CRACKS for a,b in zip(path,path[1:])]


def distance(x,z,a,b):
    dx,dz=b[0]-a[0],b[1]-a[1]
    t=np.clip(((x-a[0])*dx+(z-a[1])*dz)/(dx*dx+dz*dz),0,1)
    return np.sqrt((x-a[0]-t*dx)**2+(z-a[1]-t*dz)**2)


def groove(x,z):
    d=min(float(distance(x,z,a,b)) for a,b in segments())
    return .011*math.exp(-(d/.010)**2)


def material():
    size=512; rng=np.random.default_rng(8802)
    x,z=np.meshgrid(np.linspace(-.10,1.04,size),np.linspace(1.57,2.50,size))
    noise=np.zeros((size,size))
    for count,strength in ((8,.45),(24,.28),(64,.18),(128,.09)):
        grid=rng.random((count,count))
        positions=np.linspace(0,count-1,size)
        low=positions.astype(int); high=np.minimum(low+1,count-1); t=positions-low
        a=grid[low[:,None],low[None,:]]*(1-t[None,:])+grid[low[:,None],high[None,:]]*t[None,:]
        b=grid[high[:,None],low[None,:]]*(1-t[None,:])+grid[high[:,None],high[None,:]]*t[None,:]
        noise+=((a*(1-t[:,None])+b*t[:,None])-.5)*strength
    # Mineral staining gathers near the root and in the broad recessed face.
    root=np.exp(-((x-.12)/.25)**2-((z-2.08)/.25)**2)
    hollow=np.exp(-((x-.53)/.25)**2-((z-2.06)/.14)**2)
    tone=.70+noise*.75-.16*root-.10*hollow
    color=np.stack((tone,tone*.89,tone*.70),axis=-1)
    fissure=np.minimum.reduce([distance(x,z,a,b) for a,b in segments()])
    dark=np.exp(-(fissure/.0018)**2)*.60+np.exp(-(fissure/.006)**2)*.13
    color*=1-dark[:,:,None]
    pores=(rng.random((size,size))>.982)*(noise<.05)
    color[pores]*=.59
    # Irregular blood is painted into the surface: ragged stains, streaks and pin-sized spatter.
    blood=np.zeros((size,size))
    for cx,cz,rx,rz in ((.86,1.87,.12,.09),(.90,2.03,.045,.15),(.79,1.78,.045,.08),(.77,2.18,.028,.04)):
        field=((x-cx)/rx)**2+((z-cz)/rz)**2
        blood=np.maximum(blood,np.clip((1-field+noise*2)*5,0,1))
    for _ in range(30):
        cx,cz=rng.uniform(.52,.95),rng.uniform(1.78,2.29)
        radius=rng.uniform(.002,.009)
        blood=np.maximum(blood,(((x-cx)**2+(z-cz)**2)<radius**2)*rng.uniform(.6,1))
    blood_color=np.stack((.25+noise*.10,.035+noise*.02,.021+noise*.012),axis=-1)
    color=color*(1-blood[:,:,None])+blood_color*blood[:,:,None]
    pixels=np.concatenate((np.clip(color,0,1),np.ones((size,size,1))),axis=-1).astype(np.float32)
    image=bpy.data.images.new('reaver_aged_bone_painted',width=size,height=size)
    image.pixels.foreach_set(pixels.ravel()); image.pack()
    mat=materials.flat('reaver_sculpted_head',(1,1,1),roughness=.88)
    nodes=mat.node_tree.nodes; bsdf=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
    tex=nodes.new('ShaderNodeTexImage'); tex.image=image; tex.interpolation='Closest'
    mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
    return mat


def uv(obj):
    layer=obj.data.uv_layers.active
    for loop in obj.data.loops:
        p=obj.data.vertices[loop.vertex_index].co
        layer.data[loop.index].uv=((p.x+.10)/1.14,(p.z-1.57)/.93)
