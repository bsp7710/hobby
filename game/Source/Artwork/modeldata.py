from pathlib import Path
import json, math
import numpy as np
ROOT=Path(__file__).resolve().parent
IN=ROOT/'incoming'
OUT=ROOT.parent/'data'
def obj(name):
    positions=[]; tex=[]; faces=[]; group=''
    for line in (IN/(name+'.obj')).open(encoding='utf8'):
        a=line.split()
        if not a: continue
        if a[0]=='v': positions.append(list(map(float,a[1:4])))
        elif a[0]=='vt': tex.append(list(map(float,a[1:3])))
        elif a[0]=='g': group=a[1]
        elif a[0]=='f':
            ids=[v.split('/') for v in a[1:]]
            faces.append((group,tuple(int(v[0])-1 for v in ids),tuple(int(v[1])-1 if len(v)>1 and v[1] else 0 for v in ids)))
    return np.array(positions),np.array(tex),faces
def target(name,shape):
    d=np.zeros(shape)
    path=IN/(name+'.target')
    if not path.exists(): return d
    for line in path.open():
        a=line.split()
        if len(a)==4 and a[0].isdigit(): d[int(a[0])]=[float(v) for v in a[1:]]
    return d
def binding(name):
    refs=[]; weights=[]; active=False
    for line in (IN/(name+'.mhclo')).open(encoding='utf8'):
        a=line.split()
        if not a: continue
        if a[0]=='verts': active=True;continue
        if active and a[0][0].isalpha() and refs: break
        if not active or not a[0].isdigit():continue
        if len(a)==1: refs.append([int(a[0])]*3);weights.append([1.,0.,0.])
        elif len(a)>=9:refs.append([int(v) for v in a[:3]]);weights.append([float(v) for v in a[3:6]])
    return np.array(refs),np.array(weights)
def components(positions, faces):
    p=list(range(len(positions)))
    def root(i):
        while p[i]!=i: p[i]=p[p[i]];i=p[i]
        return i
    for _,ids,_ in faces:
        for its in ids[1:]:p[root(its)]=root(ids[0])
    roots=np.array([root(i) for i in range(len(p))]); unique,ix,cnt=np.unique(roots,return_inverse=True,return_counts=True)
    order=np.argsort(-cnt); index=np.argsort(order)[ix]
    return index
def joints(sk,points):
    return {k:points[v].mean(axis=0) for k,v in sk['joints'].items() if len(v)}
