using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Uncalled
{
    // All four poses (field, conversation, seated and holding an object) use the same rig.
    public sealed class CharacterCompany : IDisposable
    {
        struct Affine
        {
            public float a,b,c,d,e,f,g,h,i,tx,ty,tz;
            public static Affine Turn(Pt p,float rx,float ry,float rz)
            {
                const double rad = Math.PI / 180;
                float x=(float)Math.Sin(rx*rad), X=(float)Math.Cos(rx*rad);
                float y=(float)Math.Sin(ry*rad), Y=(float)Math.Cos(ry*rad);
                float z=(float)Math.Sin(rz*rad), Z=(float)Math.Cos(rz*rad);
                var m = new Affine();
                m.a=Z*Y; m.b=Z*y*x-z*X; m.c=Z*y*X+z*x;
                m.d=z*Y; m.e=z*y*x+Z*X; m.f=z*y*X-Z*x;
                m.g=-y; m.h=Y*x; m.i=Y*X;
                m.tx=p.x-(m.a*p.x+m.b*p.y+m.c*p.z);
                m.ty=p.y-(m.d*p.x+m.e*p.y+m.f*p.z);
                m.tz=p.z-(m.g*p.x+m.h*p.y+m.i*p.z);
                return m;
            }
            public static Affine Compose(Affine p,Affine q)
            {
                var o=new Affine();
                o.a=p.a*q.a+p.b*q.d+p.c*q.g; o.b=p.a*q.b+p.b*q.e+p.c*q.h; o.c=p.a*q.c+p.b*q.f+p.c*q.i;
                o.d=p.d*q.a+p.e*q.d+p.f*q.g; o.e=p.d*q.b+p.e*q.e+p.f*q.h; o.f=p.d*q.c+p.e*q.f+p.f*q.i;
                o.g=p.g*q.a+p.h*q.d+p.i*q.g; o.h=p.g*q.b+p.h*q.e+p.i*q.h; o.i=p.g*q.c+p.h*q.f+p.i*q.i;
                o.tx=p.a*q.tx+p.b*q.ty+p.c*q.tz+p.tx; o.ty=p.d*q.tx+p.e*q.ty+p.f*q.tz+p.ty; o.tz=p.g*q.tx+p.h*q.ty+p.i*q.tz+p.tz;
                return o;
            }
        }
        struct BindVertex
        {
            public float x,y,z,nx,ny,nz,bx,by,bz,sx,sy,sz,blend;
            public int j,k;
        }
        sealed class Surface : IDisposable
        {
            public uint tex;
            public float material;
            public BindVertex[] rest;
            public Vertex[] visible;
            ushort[] indices;
            GCHandle vertexHandle, indexHandle;
            public Surface(BinaryReader r,uint texture,float kind)
            {
                tex=texture; material=kind;
                int count=r.ReadInt32(), length=r.ReadInt32();
                if(count<1||count>65535||length<3||length>500000) throw new InvalidDataException("Invalid character mesh.");
                visible=new Vertex[count]; rest=new BindVertex[count]; indices=new ushort[length];
                for (int k=0;k<count;k++)
                {
                    var v=new BindVertex();
                    v.x=r.ReadSingle(); v.y=r.ReadSingle(); v.z=r.ReadSingle();
                    v.nx=r.ReadInt16()/32767f;v.ny=r.ReadInt16()/32767f;v.nz=r.ReadInt16()/32767f;
                    visible[k].u=r.ReadUInt16()/65535f;visible[k].v=r.ReadUInt16()/65535f;
                    v.j=r.ReadByte();v.k=r.ReadByte();v.blend=1-r.ReadByte()/255f;
                    uint red=r.ReadByte(),green=r.ReadByte(),blue=r.ReadByte();visible[k].color=red|(green<<8)|(blue<<16)|0xff000000u;
                    v.bx=r.ReadInt16()*.00001f;v.by=r.ReadInt16()*.00001f;v.bz=r.ReadInt16()*.00001f;
                    v.sx=r.ReadInt16()*.00001f;v.sy=r.ReadInt16()*.00001f;v.sz=r.ReadInt16()*.00001f;
                    rest[k]=v;
                }
                Buffer.BlockCopy(r.ReadBytes(length*2),0,indices,0,length*2);
                vertexHandle=GCHandle.Alloc(visible,GCHandleType.Pinned);indexHandle=GCHandle.Alloc(indices,GCHandleType.Pinned);
            }
            public void Pose(Affine[] bones,float blink,float speech)
            {
                for(int i=0;i<rest.Length;i++)
                {
                    BindVertex p=rest[i];Affine a=bones[p.j];
                    float x=p.x+p.bx*blink+p.sx*speech, y=p.y+p.by*blink+p.sy*speech,z=p.z+p.bz*blink+p.sz*speech;
                    float xx=a.a*x+a.b*y+a.c*z+a.tx, yy=a.d*x+a.e*y+a.f*z+a.ty,zz=a.g*x+a.h*y+a.i*z+a.tz;
                    float nx=a.a*p.nx+a.b*p.ny+a.c*p.nz,ny=a.d*p.nx+a.e*p.ny+a.f*p.nz,nz=a.g*p.nx+a.h*p.ny+a.i*p.nz;
                    if(p.blend>.001f){
                        Affine b=bones[p.k];float w=p.blend;
                        xx+=(b.a*x+b.b*y+b.c*z+b.tx-xx)*w;yy+=(b.d*x+b.e*y+b.f*z+b.ty-yy)*w;zz+=(b.g*x+b.h*y+b.i*z+b.tz-zz)*w;
                        nx+=(b.a*p.nx+b.b*p.ny+b.c*p.nz-nx)*w;ny+=(b.d*p.nx+b.e*p.ny+b.f*p.nz-ny)*w;nz+=(b.g*p.nx+b.h*p.ny+b.i*p.nz-nz)*w;
                    }
                    visible[i].x=xx; visible[i].y=yy; visible[i].z=zz;visible[i].nx=nx;visible[i].ny=ny;visible[i].nz=nz;
                }
            }
            public void Draw()
            {
                GL.CharacterMaterial(material);GL.glBindTexture(3553,tex);
                IntPtr p=vertexHandle.AddrOfPinnedObject();
                GL.glInterleavedArrays(0x2A29,36,p);GL.glEnableClientState(0x8075);GL.glNormalPointer(5126,36,IntPtr.Add(p,24));
                GL.glDrawElements(4,indices.Length,5123,indexHandle.AddrOfPinnedObject());
            }
            public void Dispose(){if(vertexHandle.IsAllocated)vertexHandle.Free();if(indexHandle.IsAllocated)indexHandle.Free();}
        }
        sealed class ActorSkin
        {
            public string[] names; public Pt[] pivot; public int[] parent; public Affine[] bones;
            public Surface[] parts;public int frame=int.MinValue, stance=-1; public float lastPhase;
        }
        ActorSkin[] roles=new ActorSkin[3];
        Surface hand;
        Dictionary<string,uint> textures=new Dictionary<string,uint>();
        uint campus;
        public CharacterCompany(uint atlas)
        {
            campus=atlas;
            using(Stream data=Assembly.GetExecutingAssembly().GetManifestResourceStream("company.cast"))
            using(var z=new ZipArchive(data,ZipArchiveMode.Read))
            {
                for(int n=0;n<roles.Length;n++){
                    var actor=new ActorSkin();roles[n]=actor;
                    using(Stream member=z.GetEntry("role"+n+".rig").Open())
                    using(var r=new BinaryReader(member)){
                        if(r.ReadUInt32()!=0x32524345)throw new InvalidDataException("Invalid rig.");
                        int count=r.ReadInt32();actor.names=new string[count];actor.pivot=new Pt[count];actor.parent=new int[count];actor.bones=new Affine[count];
                        for(int k=0;k<count;k++){actor.names[k]=r.ReadString();actor.parent[k]=r.ReadInt32();actor.pivot[k]=new Pt(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());}
                        actor.parts=new Surface[r.ReadInt32()];
                        for(int k=0;k<actor.parts.Length;k++){
                            string name=r.ReadString();uint tex;
                            if(!textures.TryGetValue(name,out tex)){
                                using(var s=z.GetEntry(name).Open())using(var copy=new MemoryStream()){s.CopyTo(copy);copy.Position=0;using(var bm=new Bitmap(copy))tex=GL.Texture(bm,true);}
                                textures[name]=tex;
                            }
                            float material=(name.StartsWith("hair")||name.StartsWith("fringe"))?3:name=="skin.jpg"?1:name=="eye.jpg"?2:name.EndsWith(".png")?4:5;
                            actor.parts[k]=new Surface(r,tex,material);
                        }
                    }
                }
                using(var hs=z.GetEntry("hand.mesh").Open())using(var br=new BinaryReader(hs)){
                    hand=new Surface(br,textures["skin.jpg"],1);
                    hand.Pose(new[]{Affine.Turn(new Pt(0,0,0),0,0,0)},0,0);
                }
            }
            GL.glBindTexture(3553,campus);
        }
        public void Hand(){hand.Draw();GL.CharacterMaterial(0);GL.glBindTexture(3553,campus);}
        static float Sin(double a){return (float)Math.Sin(a);}
        public void Draw(int role,float x,float z,float yaw,float phase,float time,bool walk,int pose,Mesh held)
        {
            var a=roles[role];bool seated=role==2&&pose>=2&&pose<8;
            int moment=pose>=5&&pose<8?(pose-5)%3:-1;
            float gait=walk?Sin(phase):0;
            int frame=(int)(time*30),stance=pose*2+(walk?1:0);
            bool update=frame!=a.frame||stance!=a.stance||Math.Abs(a.lastPhase-phase)>.16f;
            if(update){
                a.frame=frame;a.stance=stance;a.lastPhase=phase;
                float breathe=Sin(time*1.56+role),listen=Sin(time*.65+role*1.9);
                for(int j=0;j<a.names.Length;j++){
                    string name=a.names[j];float ax=0,ay=0,az=0;
                    if(name=="root"){az=walk?gait*1.8f:.8f*listen;}
                    if(name=="spine03"){ay=(walk?gait*3:listen*1.6f);ax=seated?6:1.6f+breathe*.6f;}
                    if(name=="spine01"){ay=walk?-gait*5:listen;ax=-2+breathe*.8f;}
                    if(name=="neck01"){ay=listen*(walk?2:5);az=pose>0?2+role*.8f:0;}
                    if(name=="head"){ax=1.8f*Sin(time*1.2+role)+(pose>=2?(role==2?8:3):0);ay=listen*(pose>0?3:5);}
                    float side=name.EndsWith(".L")?1:-1;
                    bool holds=side==1;
                    if(name.StartsWith("clavicle")){az=side*(-2+breathe*.55f);}
                    if(name.StartsWith("upperarm")){az=-side*(holds&&pose>0?27:37);ax=walk?side*gait*18:2.5f; if(holds)ax-=pose>=2?(role==1?45:27):12;if(seated&&!holds)ax-=24;}
                    if(name.StartsWith("lowerarm")){ax=holds?(pose>=2?-56:pose>0?-43:-20):-9-Math.Max(0,side*gait)*12;az=holds?-side*9:side*1.3f;}
                    if(name.StartsWith("wrist")){ay=holds?-10:2;ax=holds?9:-3;}
                    if(name.StartsWith("upperleg")){az=-side*4.8f;ax=seated?-72:side*gait*23;}
                    if(name.StartsWith("lowerleg")){ax=seated?81:3+Math.Max(0,-side*gait)*33;}
                    if(name.StartsWith("foot")){ax=seated?-6:-2-Math.Max(0,-side*gait)*12;}
                    if(moment>=0){
                        if(name=="head"){
                            ax+=role==2?(moment==1?8:-2):role==1&&moment==1?2:-2;
                            az+=(role==0?1:role==2?-1:0)*(moment==2?5:2);
                            ay+=moment==0?-4:moment==2?4:0;
                        }
                        if(holds&&name.StartsWith("upperarm")){
                            ax=role==0?(moment==1?-48:-28):role==1?(moment==1?-52:-26):-33;
                            az=role==1&&moment==1?-38:-30;
                        }
                        if(holds&&name.StartsWith("lowerarm"))ax=role==0?(moment==1?-55:-28):role==1?(moment==1?-71:-43):-28;
                        if(!holds&&name.StartsWith("upperarm")&&(role==2||role==1&&moment==1)){ax=-36;az=31;}
                        if(!holds&&name.StartsWith("lowerarm")&&(role==2||role==1&&moment==1))ax=role==2?-35:-52;
                    }
                    if(pose==8)
                    {
                        if(holds && name.StartsWith("upperarm")){ax=-94; ay=-24; az=42;}
                        if(holds && name.StartsWith("lowerarm")){ax=8; ay=-20; az=-12;}
                        if(holds && name.StartsWith("wrist")){ax=-97; ay=-19; az=-55;}
                        if(name=="head"){ay=-17; ax=10; az=-6;}
                    }
                    Affine local=Affine.Turn(a.pivot[j],ax,ay,az);
                    a.bones[j]=a.parent[j]>=0?Affine.Compose(a.bones[a.parent[j]],local):local;
                }
                float cycle=(time+role*.83f)%4.63f;
                float blink=Math.Max(0,1-Math.Abs(cycle-.095f)/.095f);
                float speech=pose==0||pose>=8?0:Math.Max(0,Sin(time*10.7+role)*.41f+.15f)*Math.Max(0,Sin(time*2.6+role)*1.2f+.30f);
                foreach(var s in a.parts)s.Pose(a.bones,blink,speech);
            }
            GL.glPushMatrix();GL.glTranslatef(x,(seated?-.28f:0)+Math.Abs(gait)*.012f,z);GL.glRotatef(yaw*57.29578f,0,1,0);
            GL.glEnable(0x0BC0);GL.glAlphaFunc(0x0204,.055f); GL.glEnable(3042);GL.glBlendFunc(770,771);
            foreach(var s in a.parts)s.Draw();
            GL.glDisable(3042);GL.glDisable(0x0BC0);GL.CharacterMaterial(0);GL.glBindTexture(3553,campus);
            int hand=Array.IndexOf(a.names,"wrist.L");
            if(held!=null && hand>=0){
                Pt p=a.pivot[hand];Affine h=a.bones[hand];p.y-=.041f;p.z+=.033f;
                GL.glPushMatrix();GL.glTranslatef(h.a*p.x+h.b*p.y+h.c*p.z+h.tx,h.d*p.x+h.e*p.y+h.f*p.z+h.ty,h.g*p.x+h.h*p.y+h.i*p.z+h.tz);
                GL.glRotatef(role==2?-20:0,1,0,0);held.Draw();GL.glPopMatrix();
            }
            GL.glPopMatrix();
        }
        public void Dispose()
        {
            if(hand!=null)hand.Dispose();
            foreach(var role in roles)if(role!=null&&role.parts!=null)foreach(var s in role.parts)if(s!=null)s.Dispose();
            foreach(uint id in textures.Values){uint t=id;GL.glDeleteTextures(1,ref t);}
        }
    }
}

