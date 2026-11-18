using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Uncalled
{
    public partial class Campus
    {
        uint lightMap;
        static bool Tube(World w,int ix,int iz)
        {
            int t=iz*w.W+ix;
            if(!w.Open(t))return false;
            if(w.cells[t]==1)return (ix+iz)%4==0;
            int r=w.roomAt[t];return w.cells[t]==3&&r>=0&&iz==w.rooms[r].z&&(ix+w.rooms[r].x)%3==0;
        }
        static bool Pane(World w,int ix,int iz,int d)
        {
            int t=iz*w.W+ix;
            return w.Open(t)&&!w.Open(t+w.Dir[d])&&(d==2||d==3)&&Math.Abs(ix*13+iz*39+d*7)%4!=0;
        }
        void BakeLight(World map)
        {
            if(!GL.Shader||!GL.LightSupport)return;
            const int samples=5;
            int width=map.W*samples,height=map.H*samples;
            var pixels=new byte[width*height*4];
            for(int iz=0;iz<map.H;iz++)for(int ix=0;ix<map.W;ix++)
            {
                int t=iz*map.W+ix;if(!map.Open(t))continue;
                V origin=map.Center(t);
                float bulb=Tube(map,ix,iz)?1.0f:0;
                for(int py=0;py<samples;py++)for(int px=0;px<samples;px++){
                    float x=ix+(px+.5f)/samples,z=iz+(py+.5f)/samples;
                    float warm=0,blue=0,shade=0;
                    for(int j=Math.Max(0,iz-2);j<Math.Min(map.H,iz+3);j++)
                    for(int i=Math.Max(0,ix-2);i<Math.Min(map.W,ix+3);i++){
                        if(!map.Open(j*map.W+i))continue;
                        V src=map.Center(j*map.W+i);float dx=x-i-.5f,dz=z-j-.5f;
                        bool tube=Tube(map,i,j),left=Pane(map,i,j,2),near=Pane(map,i,j,3);
                        if(!tube&&!left&&!near)continue;
                        if(i!=ix||j!=iz){if(!map.Sight(origin,src))continue;}
                        if(tube)warm=Math.Max(warm,1.04f/(1+2.8f*(dx*dx+dz*dz)));
                        if(left){float normal=x-i;if(normal>-.03f)blue=Math.Max(blue,.79f/(1+1.32f*normal*normal+2.7f*dz*dz));}
                        if(near){float normal=z-j;if(normal>-.03f)blue=Math.Max(blue,.79f/(1+1.32f*normal*normal+2.7f*dx*dx));}
                    }
                    float ex=x-ix,ez=z-iz;
                    if(!map.Open(t-1))shade=Math.Max(shade,.20f*Math.Max(0,1-ex*3));
                    if(!map.Open(t+1))shade=Math.Max(shade,.20f*Math.Max(0,1-(1-ex)*3));
                    if(!map.Open(t-map.W))shade=Math.Max(shade,.16f*Math.Max(0,1-ez*3));
                    if(!map.Open(t+map.W))shade=Math.Max(shade,.16f*Math.Max(0,1-(1-ez)*3));
                    int k=4*((iz*samples+py)*width+ix*samples+px);
                    pixels[k]=(byte)Math.Min(255,blue*255);pixels[k+1]=(byte)(shade*255);pixels[k+2]=(byte)Math.Min(255,warm*255);pixels[k+3]=255;
                }
            }
            if(lightMap!=0)GL.glDeleteTextures(1,ref lightMap);
            GL.glGenTextures(1,out lightMap);GL.glBindTexture(3553,lightMap);
            GL.glTexParameteri(3553,10241,9729);GL.glTexParameteri(3553,10240,9729);
            GL.glTexParameteri(3553,10242,33071);GL.glTexParameteri(3553,10243,33071);
            var pin=GCHandle.Alloc(pixels,GCHandleType.Pinned);
            try{GL.glTexImage2D(3553,0,0x1908,width,height,0,0x80E1,5121,pin.AddrOfPinnedObject());}finally{pin.Free();}
            GL.glBindTexture(3553,atlas);
        }
        static void WallBox(Builder b,float x,float z,int dx,int dz,float along,float y,float inset,float width,float height,float deep,int mat,float lit=1)
        {
            float xx=x+dx*(U*.5f-inset)+dz*along,zz=z+dz*(U*.5f-inset)-dx*along;
            b.Box(xx,y,zz,dx!=0?deep:width,height,dx!=0?width:deep,mat,lit);
        }
        static void Window(Builder b,float x,float z,int dx,int dz,int serial)
        {
            int frame=105;
            Wall(b,x,z,dx,dz,0,1.19f,2.88f,U-.24f,.029f,96+(serial&3),1.19f);
            WallBox(b,x,z,dx,dz,0,1.18f,.116f,U-.10f,.085f,.21f,3,1.06f);
            WallBox(b,x,z,dx,dz,0,2.88f,.074f,U-.16f,.074f,.12f,frame);
            WallBox(b,x,z,dx,dz,0,2.49f,.090f,U-.20f,.046f,.094f,frame);
            for(int j=-1;j<=1;j++){
                float off=j*(U-.23f)*.5f;
                WallBox(b,x,z,dx,dz,off,2.03f,.076f,j==0?.056f:.077f,1.71f,.11f,frame);
                if(j==0)WallBox(b,x,z,dx,dz,.055f,1.81f,.128f,.016f,.17f,.05f,106,1.09f);
            }
            if(serial%3==1){
                WallBox(b,x,z,dx,dz,0,2.99f,.12f,U-.03f,.095f,.16f,109);
                for(int j=0;j<4;j++)WallBox(b,x,z,dx,dz,0,2.87f-j*.049f,.12f,U-.09f,.023f,.105f,105,.90f);
            }
        }
        static void CorridorDressing(Builder b,float x,float z,int dx,int dz,int h,int theme)
        {
            WallBox(b,x,z,dx,dz,0,1.109f,.023f,U,.048f,.061f,105,.91f);
            WallBox(b,x,z,dx,dz,0,3.05f,.048f,U,.037f,.082f,108,.79f);
            if(h%7==2){
                for(int j=-1;j<=1;j++){
                    float u=j*.53f;
                    WallBox(b,x,z,dx,dz,u,.99f,.18f,.52f,1.82f,.34f,104,.98f);
                    WallBox(b,x,z,dx,dz,u+.175f,1.06f,.367f,.025f,.154f,.046f,106,1.12f);
                }
                WallBox(b,x,z,dx,dz,0,.074f,.18f,1.68f,.148f,.39f,14,.74f);
            }else if(h%6==1){
                WallBox(b,x,z,dx,dz,0,1.94f,.057f,1.47f,1.10f,.105f,3);
                Wall(b,x,z,dx,dz,-.33f,1.52f,2.35f,.60f,.12f,100+theme%4,1.17f);
                Wall(b,x,z,dx,dz,.345f,1.61f,2.27f,.55f,.12f,100+(theme+1)%4,1.05f);
            }else if(h%8==3){
                WallBox(b,x,z,dx,dz,.59f,2.66f,.13f,.51f,.34f,.21f,108);
                for(int v=0;v<4;v++)WallBox(b,x,z,dx,dz,.59f,2.56f+v*.052f,.241f,.36f,.014f,.021f,14,.70f);
            }
        }
        static void Entrance(Builder b,float x,float z)
        {
            // A bank of shoe cubbies, the late-class notice and an umbrella stand face the entry.
            for(int j=0;j<5;j++)for(int k=0;k<3;k++)
            {
                float zz=z+.65f+j*.36f,yy=.32f+k*.36f;
                b.Box(x,yy,zz,.28f,.34f,.34f,3,.86f);
                b.Box(x+.147f,yy,zz,.034f,.285f,.30f,104,1.04f);
                b.Box(x+.18f,yy+.04f,zz-.094f,.044f,.067f,.018f,106);
            }
            b.Box(x+.04f,1.27f,z+1.37f,.38f,.055f,1.91f,11);
            b.Box(x+.14f,1.95f,z+1.47f,.065f,.83f,.72f,100,1.20f);
            for(int k=0;k<3;k++){
                float zz=z-.45f+k*.15f;
                b.Lathe(x+.18f,zz,new[]{.10f,.27f,.71f,.78f},new[]{.011f,.030f,.048f,.022f},new[]{.012f,.038f,.05f,.023f},k==1?28:27,8);
                b.Box(x+.18f,.825f,zz,.032f,.15f,.033f,3,1.1f);
            }
        }
    }
}

