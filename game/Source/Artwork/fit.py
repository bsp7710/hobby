from modeldata import *
def fitted(name, body):
    refs,w=binding(name); source=(IN/(name+'.mhclo')).read_text('utf8').splitlines()
    ofs=[];scale=np.ones(3);active=False
    for line in source:
        a=line.split()
        if not a:continue
        if a[0] in ['x_scale','y_scale','z_scale']:
            j='xyz'.index(a[0][0]);scale[j]=abs(body[int(a[1]),j]-body[int(a[2]),j])/float(a[3])
        if a[0]=='verts':active=True;continue
        if active and a[0][0].isdigit():
            ofs.append(list(map(float,a[6:9])) if len(a)>=9 else [0,0,0])
            if len(ofs)==len(refs):break
    return (body[refs]*w[:,:,None]).sum(axis=1)+np.array(ofs)*scale,refs,w
