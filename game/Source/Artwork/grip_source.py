from make_cast import *
def unit(x):return x/max(np.linalg.norm(x),1e-10)
def align(a,b):
    a=unit(a);b=unit(b);v=np.cross(a,b);c=np.dot(a,b)
    K=np.array([[0,-v[2],v[1]],[v[2],0,-v[0]],[-v[1],v[0],0]])
    return np.eye(3)+K+K@K/max(1e-8,1+c)
def make_grip(z):
    ar=Artist(0);m=ar.m;js=ar.j
    def tip(n,end='head'):return js[allbones[n][end]]
    wrist=tip('wrist.L');widthAxis=unit(tip('finger2-1.L')-tip('finger5-1.L'))
    forward=np.mean([tip('finger%d-1.L'%k) for k in range(2,6)],axis=0)-wrist
    forward=unit(forward-widthAxis*np.dot(widthAxis,forward))
    df=unit(np.array([-.69,.125,.72]));dw=np.array([0.,1.,0]);dw=unit(dw-df*np.dot(dw,df))
    R=np.column_stack([df,dw,np.cross(df,dw)])@np.column_stack([forward,widthAxis,np.cross(forward,widthAxis)]).T
    scale=.11;wp=np.array([.098,-.059,-.018])
    def palm(points):return ((points-wrist)@R.T)*scale+wp
    tr={};sw=np.zeros(len(m))
    for finger in range(1,6):
        n='finger%d-1.L'%finger;head=palm(tip(n));phi=math.atan2(head[2],head[0])
        for phalanx in range(1,4):
            n='finger%d-%d.L'%(finger,phalanx);a=tip(n);b=tip(n,'tail');lm=np.linalg.norm(b-a)*scale
            if finger==1:
                direction=[np.array([-.12,.92,-.38]),np.array([-.52,.68,-.50]),np.array([-.78,.48,-.36])][phalanx-1]
                nxt=head+unit(direction)*lm
            else:
                phi+=lm/[.045,.042,.040][phalanx-1]
                radius=[.049,.044,.041][phalanx-1]
                nxt=np.array([radius*math.cos(phi),head[1]-(.002 if phalanx>1 else .001),radius*math.sin(phi)])
            rot=align(R@(b-a),nxt-head)@R;tr[n]=(rot,a,head);head=nxt
    out=np.zeros_like(m)
    for b,arr in raw.items():
        data=np.array(arr);idx=data[:,0].astype(int);weights=data[:,1]
        if b in tr:
            rr,h,th=tr[b];pos=(m[idx]-h)@rr.T*scale+th
        else:pos=palm(m[idx])
        out[idx]+=pos*weights[:,None]
        if b.endswith('.L') and (b.startswith('wrist') or b.startswith('finger') or b.startswith('metacarpal')):sw[idx]+=weights
    faces=[f for f in bodyfaces if sw[list(f[1])].mean()>.48]
    art=Artist(0);art.parts=[]
    inv=out/art.scale;inv[:,1]-=8.17;inv[:,2]+=.30
    art.package(inv,bodyuv,faces,art.fixed(len(inv),'root'),'skin.jpg')
    name,v,idx=art.parts[0];buf=BytesIO();buf.write(struct.pack('<ii',len(v),len(idx)));buf.write(v.tobytes());buf.write(idx.tobytes());z.writestr('hand.mesh',buf.getvalue())
