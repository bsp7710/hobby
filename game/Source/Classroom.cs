using System;

namespace Uncalled
{
    public partial class Campus
    {
        static void NotebookOnDesk(Builder b,float x,float z,int variant)
        {
            float y=.838f;
            int cloth=variant%2==0?69:27;
            if(variant%3==0)
            {
                b.Box(x,y+.017f,z,.224f,.032f,.172f,cloth);
                b.Box(x+.004f,y+.031f,z,.211f,.022f,.166f,13);
                b.Box(x,y+.046f,z,.223f,.008f,.173f,cloth);
                b.Box(x+.046f,y+.051f,z-.027f,.075f,.003f,.057f,13);
                return;
            }
            for(int side=-1;side<=1;side+=2)
            {
                float edge=x+side*.164f;
                b.Q(P(x,y+.012f,z-.104f),P(edge,y+.026f,z-.101f),P(edge,y+.025f,z+.104f),P(x,y+.012f,z+.104f),13,side<0?.97f:1.08f);
                for(int l=0;l<4;l++)
                {
                    float zz=z-.057f+l*.029f;
                    b.Q(P(x+side*.022f,y+.017f,zz),P(x+side*.137f,y+.026f,zz),P(x+side*.137f,y+.026f,zz+.0018f),P(x+side*.022f,y+.017f,zz+.0018f),105,.89f);
                }
            }
            b.Box(x+.22f,y+.006f,z,.008f,.012f,.139f,106,1.03f);
        }

        static void Satchel(Builder b,float x,float z,int color)
        {
            // The shoulder straps loop over a chair back. The bag stays inside the desk's footprint.
            b.Box(x,.482f,z,.308f,.30f,.126f,color,.95f);
            b.Box(x,.633f,z-.006f,.302f,.049f,.122f,color,1.10f);
            b.Box(x,.456f,z-.075f,.226f,.107f,.034f,color,1.17f);
            b.Box(x,.500f,z-.096f,.209f,.011f,.009f,105);
            for(int s=-1;s<=1;s+=2)
            {
                b.Box(x+s*.087f,.685f,z+.015f,.018f,.122f,.019f,109);
                b.Box(x+s*.087f,.743f,z+.043f,.018f,.016f,.065f,109);
            }
        }

        static void DeskThings(Builder b,float x,float z,int variant)
        {
            if(variant%5==0)NotebookOnDesk(b,x-.88f,z-.035f,variant);
            else if(variant%5==2)
            {
                b.Box(x+.98f,.864f,z+.125f,.205f,.054f,.086f,69);
                b.Box(x+.98f,.893f,z+.125f,.193f,.006f,.011f,105);
                b.Box(x+1.056f,.897f,z+.103f,.009f,.006f,.039f,106);
            }
            if(variant%6==1)Satchel(b,x-.86f,z-.883f,variant%2==0?69:27);
        }

        static void ClassroomBoard(Builder b,float x,float z,int tile,Room r)
        {
            int side=tile-r.x;
            const int dx=0,dz=1;
            if(side==0)
            {
                WallBox(b,x,z,dx,dz,0,1.76f,.031f,3.34f,1.40f,.059f,3,.96f);
                Wall(b,x,z,dx,dz,0,1.12f,2.39f,3.21f,.084f,7,1.08f);
                Wall(b,x,z,dx,dz,-1.05f,1.38f,2.25f,.80f,.093f,123,1.12f);
                WallBox(b,x,z,dx,dz,0,1.088f,.124f,3.32f,.064f,.171f,105,1.05f);
                WallBox(b,x,z,dx,dz,.71f,1.157f,.17f,.165f,.068f,.082f,109);
                for(int i=0;i<3;i++)WallBox(b,x,z,dx,dz,-.36f+i*.13f,1.128f,.17f,.081f,.015f,.016f,13);
                Wall(b,x,z,dx,dz,0,2.51f,3.00f,.49f,.075f,116,1.10f);
            }
            else if(side==-1)
            {
                WallBox(b,x,z,dx,dz,-.26f,1.89f,.051f,1.22f,.97f,.08f,3,1.08f);
                Wall(b,x,z,dx,dz,-.25f,1.48f,2.29f,1.11f,.102f,102,1.13f);
                WallBox(b,x,z,dx,dz,.75f,2.73f,.11f,.37f,.32f,.19f,108);
                for(int i=0;i<4;i++)WallBox(b,x,z,dx,dz,.75f,2.63f+i*.052f,.216f,.25f,.014f,.016f,14,.72f);
            }
            else if(side==1)
            {
                WallBox(b,x,z,dx,dz,.15f,2.05f,.052f,1.47f,1.14f,.081f,3);
                Wall(b,x,z,dx,dz,-.13f,1.61f,2.44f,.62f,.105f,100,1.15f);
                Wall(b,x,z,dx,dz,.52f,1.77f,2.40f,.48f,.109f,103,1.09f);
                WallBox(b,x,z,dx,dz,.19f,2.62f,.079f,1.52f,.063f,.1f,105);
            }
        }

        static void Contact(Builder b,float x,float z,float rx,float rz)
        {
            b.Q(P(x-rx,.014f,z-rz),P(x+rx,.014f,z-rz),P(x+rx,.014f,z+rz),P(x-rx,.014f,z+rz),92,1);
        }
    }
}
