"""Build three clothed game meshes from the CC0 MakeHuman topology and fitting data."""
from fit import *
from PIL import Image,ImageFilter,ImageEnhance,ImageDraw
from collections import defaultdict
from io import BytesIO
import zipfile,struct
base,bodyuv,bodyfaces=obj('base')
bodyfaces=[f for f in bodyfaces if f[0]=='body']
sk=json.loads((IN/'default.mhskel').read_text('utf8'))
allbones=sk['bones']
wanted=['root','spine03','spine01','neck01','head']
for side in ['.L','.R']:
    wanted += [x+side for x in ['clavicle','upperarm01','lowerarm01','wrist','upperleg01','lowerleg01','foot']]
def mapped(n):
    while n and n not in wanted:n=allbones[n]['parent']
    return wanted.index(n) if n else -1
raw=json.loads((IN/'default_weights.mhw').read_text('utf8'))['weights']
baseweights=np.zeros((len(base),len(wanted)))
for b,arr in raw.items():
    for i,w in arr:baseweights[i,mapped(b)]+=w
baseweights[:,0]+=(baseweights.sum(1)<.0001)
baseweights/=baseweights.sum(1)[:,None]
def tar(name):return target(name,base.shape)
blink=tar('eye-left-closure')+tar('eye-right-closure')
speak=tar('mouth-open')*.20+tar('mouth-corner-puller')*.075
def output_texture(z,name,bm,ext='PNG'):
    buf=BytesIO();bm.save(buf,ext,quality=93,subsampling=0);z.writestr(name,buf.getvalue())
def textures(z):
    skin=Image.open(IN/'young_lightskinned_female_diffuse3.jpg').convert('RGB')
    # Preserve painted structure of the face while suppressing photographic skin noise.
    skin=Image.blend(skin,skin.filter(ImageFilter.GaussianBlur(2.3)),.80)
    skin=ImageEnhance.Color(skin).enhance(.80)
    output_texture(z,'skin.jpg',skin,'JPEG')
    # Symmetrical sclera and an illustrated limbal ring drawn in eye-local coordinates.
    yy,xx=np.mgrid[-1:1:384j,-1:1:384j]
    rr=np.sqrt((xx*1.01)**2+(yy*.90)**2);angle=np.arctan2(yy,xx)
    eye=np.zeros((384,384,3));sclera=np.clip(1-np.abs(xx)*.12-np.maximum(0,-yy)*.13,0,1)
    eye[:]=np.array([229,223,215])*sclera[:,:,None]
    iris=np.clip((.489-rr)*120,0,1)
    spoke=(np.sin(angle*51+rr*27)*.5+np.cos(angle*113-rr*9)*.3+np.sin(angle*37)*.3)*6
    richness=np.maximum(0,1-np.abs(rr-.34)*7)*24+np.maximum(0,yy)*16+spoke
    color=np.array([63,51,44])+richness[:,:,None]*np.array([1.0,.75,.48])
    dark=np.clip((rr-.405)*12,0,.80)+np.clip((.242-rr)*25,0,1)*.83
    color=color*(1-dark[:,:,None])+np.array([21,22,26])*dark[:,:,None]
    eye=eye*(1-iris[:,:,None])+color*iris[:,:,None]
    glint=np.exp(-(((xx+.14)/.061)**2+((yy+.18)/.051)**2)*1.45)+np.exp(-(((xx-.14)/.034)**2+((yy-.13)/.030)**2))
    eye+=(np.array([235,237,224])-eye)*glint[:,:,None]
    output_texture(z,'eye.jpg',Image.fromarray(np.clip(eye,0,255).astype('uint8')),'JPEG')
    for file,name in [('eyebrow001.png','brow.png'),('eyelashes01.png','lash.png')]:
        im=Image.open(IN/file).convert('RGBA')
        ar=np.array(im);ar[:,:,:3]=np.array([58,42,41]) if name=='brow.png' else np.array([45,37,36])
        ar[:,:,3]=np.clip(ar[:,:,3].astype(float)*(1.05 if name=='brow.png' else .91),0,255)
        output_texture(z,name,Image.fromarray(ar))
    output_texture(z,'shoes.jpg',Image.open(IN/'shoes01_diffuse.jpg').convert('RGB'),'JPEG')
    rng=np.random.default_rng(41)
    v,u=np.mgrid[0:512,0:256];x=u/255;y=v/511
    weave=(np.cos(u*np.pi/2)*2.5+np.cos(v*np.pi/2)*2.8+np.cos((u-v)*np.pi/4)*1.9+rng.normal(0,2.5,x.shape))
    for i,(suit,plaid,h) in enumerate([((52,61,76),(69,73,86),(49,43,46)),((86,56,62),(81,64,67),(79,49,43)),((48,70,64),(58,71,68),(43,44,49))]):
        emblem=Image.new('RGB',(128,128),suit);pen=ImageDraw.Draw(emblem)
        pen.polygon([(18,17),(110,17),(103,76),(88,96),(64,111),(39,96),(24,76)],fill=(62,73,71),outline=(191,169,114),width=6)
        pen.polygon([(38,68),(54,38),(61,52),(68,34),(91,70)],outline=(206,187,128),width=4)
        pen.line((41,78,86,78),fill=(190,173,124),width=4);pen.line((53,86,75,86),fill=(181,164,113),width=3)
        output_texture(z,'crest%d.jpg'%i,emblem,'JPEG')
        w=np.clip(np.array(suit)[None,None,:]+weave[:,:,None],0,255).astype('uint8')
        output_texture(z,'jacket%d.jpg'%i,Image.fromarray(w),'JPEG')
        band=np.minimum(u%68,68-u%68);row=np.minimum(v%86,86-v%86)
        thin=(band<1.7)|(row<1.7); plaidColor=np.array(plaid)+weave[:,:,None]-10*((band<12)|(row<13))[:,:,None]+24*thin[:,:,None]+11*((abs(band-19)<1)|(abs(row-24)<1))[:,:,None]
        output_texture(z,'skirt%d.jpg'%i,Image.fromarray(np.clip(plaidColor,0,255).astype('uint8')),'JPEG')
        flow=x+np.sin(5*y+x*3)*.012+np.sin(14*y+x*24)*.003
        strand=np.sin(flow*183)*4+np.sin(flow*516+.8)*4+np.cos(flow*81)*5+3*np.sin(flow*1432)+np.sin(flow*35)*5
        gloss=11*np.exp(-((y-.44-.075*np.sin(x*4))/.12)**2)
        col=np.clip(np.array(h)+((strand+gloss-6*y)[:,:,None]),0,255).astype('uint8')
        output_texture(z,'hair%d.jpg'%i,Image.fromarray(col),'JPEG')
        # Soft filament edges survive mip filtering in close-ups.
        border=np.minimum(x,1-x);centre=.47+.055*np.sin(6.9*y+2)
        envelope=np.maximum(0,.48-(y**2.9)*.25)
        drift=.013*np.sin(x*284+y*12)+.008*np.cos(x*610-y*21)
        coverage=np.clip((envelope-abs(x-centre)+drift)*34,0,1)
        tips=np.clip((1-y+np.cos(x*62+y*3)*.035)*41,0,1)
        strandColor=np.clip(col.astype(float)+np.cos(x*67+y*5)[:,:,None]*5,0,255).astype('uint8')
        output_texture(z,'fringe%d.png'%i,Image.fromarray(np.dstack([strandColor,np.uint8(coverage*tips*255)])))

    output_texture(z,'ivory.jpg',Image.fromarray(np.clip(np.array([218,216,203])+weave[:,:,None]*.70,0,255).astype('uint8')),'JPEG')
    output_texture(z,'ribbon.jpg',Image.fromarray(np.clip(np.array([116,67,66])+weave[:,:,None]+15*((u%48)<2)[:,:,None],0,255).astype('uint8')),'JPEG')
    output_texture(z,'porcelain.jpg',Image.new('RGB',(64,64),(177,161,151)),'JPEG')
    output_texture(z,'gold.jpg',Image.fromarray(np.clip(np.array([190,157,93])+weave[:,:,None]*.4,0,255).astype('uint8')),'JPEG')
def shaped(i):
    a=[.95,.79,.89][i]
    return base+tar('asian-female-young')*a+tar('caucasian-female-young')*(1-a)+(tar('l-eye-scale-incr')+tar('r-eye-scale-incr'))*[.45,.53,.40][i]+tar('chin-width-decr')*[.28,.16,.32][i]+tar('nose-scale-horiz-decr')*[.25,.35,.17][i]+tar('mouth-scale-horiz-incr')*[.07,.13,.02][i]+tar('chin-height-decr')*[.29,.20,.15][i]+tar('nose-scale-depth-decr')*[.17,.14,.13][i]+tar('nose-point-width-decr')*.11+(tar('l-eye-height2-incr')+tar('r-eye-height2-incr'))*[.20,.26,.17][i]
class Artist:
    def __init__(self,i):
        self.i=i;self.m=shaped(i)+tar("mouth-corner-puller")*[.25,.34,.18][i];self.scale=[.109,.1067,.111][i];self.parts=[];self.j=joints(sk,self.m)
    def point(self,v):
        p=np.array(v,copy=True,dtype=float);p[...,1]+=8.17;p[...,2]-=.30
        return p*self.scale
    def package(self,pts,uv,faces,weights,material,expr=None,tint=None):
        if not faces:return
        p=self.point(pts);nn=np.zeros_like(p);lookup={};index=[];ui=[];ids=[];smooth=[]
        for _,vs,ts in faces:
            for j in range(1,len(vs)-1):
                a,b,c=vs[0],vs[j],vs[j+1];n=np.cross(p[b]-p[a],p[c]-p[a]);nn[[a,b,c]]+=n
                for vi,ti in [(a,ts[0]),(b,ts[j]),(c,ts[j+1])]:
                    key=(vi,ti)
                    if key not in lookup:lookup[key]=len(ids);ids.append(vi);ui.append(ti)
                    index.append(lookup[key])
        ids=np.array(ids);uv=np.array(uv)[ui].copy();uv[:,1]=1-uv[:,1];n=nn[ids];n/=np.maximum(1e-12,np.linalg.norm(n,axis=1))[:,None]
        w=np.maximum(0,weights[ids]);ii=np.argsort(w,axis=1)[:,-2:][:,::-1];t=np.take_along_axis(w,ii,axis=1);amt=np.rint(t[:,0]/np.maximum(1e-12,t.sum(1))*255).astype('uint8')
        dtype=np.dtype([('p','<f4',(3,)),('n','<i2',(3,)),('uv','<u2',(2,)),('joint','u1',(2,)),('weight','u1'),('color','u1',(3,)),('blink','<i2',(3,)),('speak','<i2',(3,))])
        v=np.zeros(len(ids),dtype=dtype);v['p']=p[ids];v['n']=np.rint(np.clip(n,-1,1)*32767);v['uv']=np.rint(np.clip(uv,0,1)*65535);v['joint']=ii;v['weight']=amt
        v['color']=255 if tint is None else np.clip(tint[ids],0,255)
        if expr is not None:
            for name,d in zip(['blink','speak'],expr):v[name]=np.clip(np.rint(d[ids]*self.scale*100000),-32767,32767)
        self.parts.append((material,v,np.array(index,dtype='<u2')))
    def fixed(self,count,bone):
        w=np.zeros((count,len(wanted)));w[:,wanted.index(bone)]=1;return w
    def fringe(self):
        # Individually curved clumps follow the local forehead surface and narrow into free tips.
        m=self.m; surface=m[np.unique(np.concatenate([f[1] for f in bodyfaces]))]
        surf=surface[(surface[:,1]>5.38)&(surface[:,2]>.46)]
        vertices=[];uv=[];fs=[];tones=[];role=self.i
        rng=np.random.default_rng(503+role)
        for strand in range(17):
            f=(strand+.5)/17;end=(f-.5)*1.43
            root=end*.42+[-.22,.34,.20][role]
            tipy=([5.94,6.14,6.18][role]+.14*abs(end)+.14*math.sin(f*4.8+role)+rng.uniform(-.111,.099))
            if role==1:tipy-=.24*max(0,-end)
            if role==2:tipy+=.24*(1-abs(end))
            for j in range(11):
                t=j/10;xc=root*(1-t)+end*t+.052*math.sin(t*math.pi)*(1 if role!=1 else -1)
                y=6.69+(tipy-6.69)*t
                sel=(abs(surf[:,0]-xc)<.18)&(abs(surf[:,1]-y)<.14)
                if np.any(sel):z=np.max(surf[sel,2])
                else:z=.88
                z+=.047+.04*math.sin(t*math.pi)+.004*math.cos(strand*3)
                width=(.059+.030*math.sin(math.pi*t))*(.09+.91*(1-t**6))
                for l in range(5):
                    k=(l-2)/2;vertices.append((xc+k*width,y+.014*(1-k*k),z+.034*(1-k*k)))
                    uv.append(((k+1)/2,1-.98*t));tones.append([233+(strand%4)*5]*3)
                if j:
                    start=len(vertices)-10
                    for l in range(4):
                        ids=tuple(start+h for h in [l,l+1,l+6,l+5]);fs.append(('',ids,ids))
        self.package(vertices,uv,fs,self.fixed(len(vertices),'head'),'fringe%d.png'%role,tint=np.array(tones))
    def extras(self):
        # Inset shirt and two sculpted collar leaves with contrasting ribbon at the throat.
        verts=[];uv=[];fs=[]
        def quad(a,b,c,d):
            n=len(verts);verts.extend([a,b,c,d]);uv.extend([[0,0],[1,0],[1,1],[0,1]]);fs.append(('',tuple(range(n,n+4)),tuple(range(n,n+4))))
        m=self.m
        idx=np.unique(np.concatenate([f[1] for f in bodyfaces]))
        def front(x,y):
            pp=m[idx];r=(np.abs(pp[:,0]-x)<.22)&(np.abs(pp[:,1]-y)<.19)
            return (np.max(pp[r,2]) if np.any(r) else 1.)+.095
        def p(x,y,extra=0):return (x,y,front(x,y)+extra)
        for k in range(7):
            frac=k/7;frac2=(k+1)/7; ya=4.58-2.02*frac;yb=4.58-2.02*frac2
            xa=.38*(1-frac)+.035;xb=.38*(1-frac2)+.035
            quad(p(-xa,ya),p(xa,ya),p(xb,yb),p(-xb,yb))
        for s in [-1,1]:
            quad(p(s*.46,4.55,.076),p(s*.08,4.37,.092),p(s*.18,3.98,.135),p(s*.58,4.29,.068))
            quad(p(s*.49,4.57,.04),p(s*.10,4.37,.044),p(s*.20,4.02,.06),p(s*.57,4.33,.045))
        self.package(verts,uv,fs,self.fixed(len(verts),'spine01'),'ivory.jpg')
        verts.clear();uv.clear();fs.clear()
        for s in [-1,1]:
            quad(p(s*.035,4.15,.11),p(s*.30,4.26,.125),p(s*.33,4.02,.13),p(s*.05,4.09,.13))
            quad(p(s*.02,4.12,.13),p(s*.13,4.10,.14),p(s*.22,3.69,.13),p(s*.09,3.73,.12))
        self.package(verts,uv,fs,self.fixed(len(verts),'spine01'),'ribbon.jpg')
        verts.clear();uv.clear();fs.clear()
        # Bound lapels run over the shirt edges. The prefect badge sits over the breast pocket.
        for side in [-1,1]:
            for k in range(7):
                a=k/7;b=(k+1)/7
                def edge(t,outer):
                    w=(.44*(1-t)+.034)+(outer*(.18*(1-t)+.035))
                    return p(side*w,4.48-1.78*t,.142+outer*.018)
                quad(edge(a,0),edge(a,1),edge(b,1),edge(b,0))
        self.package(verts,uv,fs,self.fixed(len(verts),'spine01'),'jacket%d.jpg'%self.i)
        verts.clear();uv.clear();fs.clear()
        # Texture coordinates follow the gold embroidered edge of a square sewn patch.
        quad(p(.35,3.10,.16),p(.70,3.10,.16),p(.70,3.46,.16),p(.35,3.46,.16))
        self.package(verts,uv,fs,self.fixed(len(verts),'spine01'),'crest%d.jpg'%self.i)
        verts.clear();uv.clear();fs.clear()
        for y in [2.27,1.71,1.15]:
            rad=.054; center=(0,y,front(0,y)+.16)
            for j in range(10):
                a=j*math.tau/10;b=(j+1)*math.tau/10;quad(center,(rad*math.cos(a),y+rad*math.sin(a),center[2]),(rad*math.cos(b),y+rad*math.sin(b),center[2]),center)
        self.package(verts,uv,fs,self.fixed(len(verts),'spine03'),'gold.jpg')
    def build(self):
        m=self.m
        # Only exposed anatomy is exported. The rest of the character is permanently dressed.
        face=[];hands=[];legs=[]
        for f in bodyfaces:
            v=m[list(f[1])].mean(0);x,y,z=v;x=abs(x)
            if y>4.05 and (y>4.62 or x<.52):face.append(f)
            elif x>3.45 and y<1.43:hands.append(f)
            elif y< -2.78:legs.append(f)
        # Tint cheeks and lips in model space to keep the paint stable under animation.
        x,y,z=m.T; blush=np.exp(-((abs(x)-.46)/.22)**2-((y-5.47)/.15)**2)*np.clip((z-.65)*3,0,1)
        tint=255-np.stack([blush*0,blush*22,blush*16],axis=1)
        self.package(m,bodyuv,face+hands+legs,baseweights,'skin.jpg',(blink,speak),tint)
        for name,mat in [('high-poly','eye.jpg'),('teeth_base','porcelain.jpg'),('eyebrow001','brow.png'),('eyelashes01','lash.png'),(['long01','bob01','ponytail01'][self.i],'hair%d.jpg'%self.i),('female_elegantsuit01','jacket%d.jpg'%self.i),('shoes01','shoes.jpg')]:
            _,uv,fs=obj(name);p,refs,ww=fitted(name,m);comp=components(p,fs);w=np.einsum('ij,ijk->ik',ww,baseweights[refs]);ex=None
            if name=='high-poly':
                fs=[f for f in fs if comp[f[1][0]]<2];w=self.fixed(len(p),'head')
                uv=np.zeros((len(p),2))
                for g in [0,1]:
                    ar=p[comp==g];cx=(ar[:,0].min()+ar[:,0].max())/2;cy=(ar[:,1].min()+ar[:,1].max())/2
                    uv[comp==g,0]=np.clip((ar[:,0]-cx)/.271+.5,.001,.999)
                    uv[comp==g,1]=np.clip((ar[:,1]-cy)/.268+.5,.001,.999)
                fs=[(g,vs,vs) for g,vs,ts in fs]
            if name=='teeth_base':
                p[:,2]-=.075
                # Only anterior teeth visible in restrained speech are submitted to the renderer.
                fs=[f for f in fs if p[list(f[1])].mean(0)[2]>.95]
                w=self.fixed(len(p),'head');ex=(np.zeros_like(p),np.einsum('ij,ijk->ik',ww,speak[refs]))
            if name=='eyelashes01' or name=='eyebrow001':
                w=self.fixed(len(p),'head');ex=(np.einsum('ij,ijk->ik',ww,blink[refs]),np.zeros_like(p))
            if mat.startswith('hair'):
                w=self.fixed(len(p),'head')
                if name=='bob01':
                    # Part the inner curtain above the eye so a three-quarter conversation stays readable.
                    xp,yp,zp=p.T.copy()
                    lift=np.exp(-((xp+.08)/.64)**4)*np.clip((zp-.79)/.41,0,1)*np.clip((6.12-yp)/.70,0,1)
                    p[:,1]+=.69*lift;p[:,0]-=.20*lift
                if name=='long01':
                    # The tips settle toward the coat as the head turns.
                    amount=np.clip((4.15-p[:,1])*.22,0,.73);w[:,wanted.index('head')]-=amount;w[:,wanted.index('spine01')]+=amount
            if name=='female_elegantsuit01':
                skirt=[f for f in fs if comp[f[1][0]]==1]
                # Add sewn-in pleats to the fitted skirt while retaining the original waist.
                a=np.arctan2(p[:,0],p[:,2]-.18);h=np.clip((.6-p[:,1])/3.5,0,1);fold=(.5+.5*np.cos(a*22))**3*h*.145
                ps=p.copy();ps[:,0]+=np.sin(a)*fold;ps[:,2]+=np.cos(a)*fold
                self.package(ps,uv,skirt,w,'skirt%d.jpg'%self.i)
                fs=[f for f in fs if comp[f[1][0]]==0]
            self.package(p,uv,fs,w,mat,ex)
        self.fringe()
        self.extras()
    def binary(self):
        b=BytesIO();b.write(b'ECR2');b.write(struct.pack('<i',len(wanted)))
        for n in wanted:
            name=n.encode();b.write(bytes([len(name)]));b.write(name);b.write(struct.pack('<i3f',mapped(allbones[n]['parent']),*self.point(self.j[allbones[n]['head']])))
        b.write(struct.pack('<i',len(self.parts)))
        for nm,v,idx in self.parts:
            name=nm.encode();b.write(bytes([len(name)]));b.write(name);b.write(struct.pack('<ii',len(v),len(idx)));b.write(v.tobytes());b.write(idx.tobytes())
        return b.getvalue()
