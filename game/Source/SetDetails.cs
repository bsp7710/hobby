using System;
namespace Uncalled
{
    public partial class Campus
    {
        static void Sleeve(Builder b)
        {
            float[] d={0,.011f,.034f,.115f,.19f},radius={.035f,.039f,.044f,.054f,.063f};
            for(int row=0;row<4;row++)for(int k=0;k<18;k++){
                var v=new Pt[4];
                for(int j=0;j<4;j++){
                    int ri=row+(j>=2?1:0);float a=(k+(j==1||j==2?1:0))*(float)Math.PI/9;
                    float c=(float)Math.Cos(a)*radius[ri],sn=(float)Math.Sin(a)*radius[ri],t=d[ri];
                    v[j]=P(.098f+.68f*t+.70f*c,-.060f-.29f*t+sn,-.019f-.70f*t+.68f*c-.18f*sn);
                }
                b.Q(v[0],v[1],v[2],v[3],row==0?13:111,.86f);
            }
        }
        static void CallBox(Builder b,float x,float z,bool stand)
        {
            if(stand){
                b.Box(x,.80f,z+.10f,.68f,1.60f,.28f,126,.84f);
                b.Box(x,1.61f,z+.10f,.76f,.052f,.35f,105,1.07f);
                b.Box(x,.083f,z+.10f,.77f,.165f,.36f,14,.84f);
            }
            b.Box(x,1.53f,z,.58f,.87f,.15f,124,.85f);
            b.Box(x,1.55f,z-.088f,.535f,.815f,.046f,124,1.08f);
            b.Box(x+.055f,1.82f,z-.118f,.32f,.149f,.024f,115,1.18f);
            b.Box(x+.058f,1.657f,z-.118f,.257f,.095f,.024f,127);
            for(int j=0;j<4;j++)for(int i=0;i<3;i++)b.Box(x+.001f+i*.059f,1.535f-j*.047f,z-.125f,.040f,.028f,.028f,125,.99f);
            b.Box(x-.169f,1.64f,z-.147f,.063f,.302f,.066f,127,1.13f);
            for(int side=-1;side<=1;side+=2)b.Box(x-.166f,1.64f+side*.152f,z-.15f,.104f,.076f,.098f,14,1.07f);
            for(int k=0;k<30;k++){
                float t=k/29f,xx=x-.168f+.113f*t,yy=1.479f-.25f*(float)Math.Sin(t*Math.PI)-.10f*t;
                b.Box(xx+.010f*(float)Math.Cos(k),yy,z-.13f+.01f*(float)Math.Sin(k),.014f,.016f,.014f,127);
            }
            b.Box(x+.194f,1.70f,z-.119f,.032f,.032f,.018f,38,1.25f);
            b.Box(x,1.232f,z-.118f,.30f,.083f,.024f,114,.99f);
        }
        static void TitleStation(Builder b)
        {
            float x=-.48f,z=3.82f;
            b.Box(x,1.63f,z+.20f,.94f,3.26f,.32f,2,.90f);
            b.Box(x,2.62f,z+.032f,.79f,.89f,.027f,114,1.23f);
            CallBox(b,x,z,false);
            b.Box(x-.36f,.67f,z+.005f,.035f,1.23f,.039f,108,1.06f);
            b.Box(x,2.069f,z+.008f,.931f,.028f,.041f,105,.96f);
            b.Box(x,.09f,z+.19f,.97f,.18f,.36f,14,.78f);
            for(int j=0;j<3;j++){
                float zz=10.5f+j*8.0f;
                b.Box(.51f,3.07f,zz,.033f,.43f,.038f,105);
                b.Box(.51f,2.79f,zz,.88f,.34f,.077f,j==0?119:118,1.49f);
            }
        }
    }
}

