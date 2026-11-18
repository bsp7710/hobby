using System;
namespace Uncalled
{
    public sealed class PatrolAssembly : IDisposable
    {
        Mesh coat,mask, sleeve, cuff,shoe,token,pendants;
        int kind;
        static Pt P(double x,double y,double z){return new Pt((float)x,(float)y,(float)z);}
        static Mesh Finish(Builder b){return b.End();}
        public PatrolAssembly(int index)
        {
            kind=index;var b=new Builder();
            int coatMat=index==0?111:113;
            float[] heights={.32f,.48f,.81f,.99f,1.20f,1.39f,1.52f,1.57f};
            float[] widths={.245f,.246f,.206f,.174f,.154f,.189f,.205f,.078f};
            float[] depths={.16f,.16f,.125f,.101f,.108f,.124f,.103f,.067f};
            for(int j=0;j<heights.Length-1;j++)for(int i=0;i<36;i++){
                var v=new Pt[4];
                for(int h=0;h<4;h++){
                    int r=j+(h>=2?1:0),t=i+(h==1||h==2?1:0);
                    double a=t*Math.PI/18,pleat=1+.047*Math.Cos(a*11),y=heights[r];
                    if(r==0)y+=(.017+.026*index)*Math.Sin(a*9+.8);
                    v[h]=P(Math.Sin(a)*widths[r]*pleat,y,Math.Cos(a)*depths[r]*pleat);
                }
                b.Q(v[0],v[1],v[2],v[3],coatMat,.80f+.12f*(float)Math.Cos(i*.21));
            }
            for(int s=-1;s<=1;s+=2){
                b.Q(P(s*.08,1.57,.066),P(s*.173,1.48,.11),P(s*.039,1.205,.12),P(0,1.34,.13),coatMat,1.25f);
                b.Q(P(s*.03,.52,.149),P(s*.047,.52,.149),P(s*.026,1.22,.128),P(s*.013,1.22,.128),109,.85f);
            }
            for(int j=0;j<5;j++)b.Oval(.026f,1.25f-j*.126f,.133f,.009f,.009f,.004f,106,.93f,6);
            b.Box(-.103f,1.31f,.116f,.085f,.041f,.015f,index==0?110:112,.99f);coat=Finish(b);b=new Builder();
            b.Lathe(0,.012f,new[]{-.14f,-.085f,.008f,.105f,.161f},new[]{.043f,.080f,.100f,.108f,.065f},new[]{.044f,.072f,.089f,.099f,.064f},coatMat,24,.79f);
            if(index==0){
                for(int fl=0;fl<6;fl++){float x=(fl-2.5f)*.0335f,tip=-.098f+.018f*(float)Math.Sin(fl*2.5);
                    b.Q(P(x-.017,.105,.100-Math.Abs(x)*.38),P(x+.019,.101,.105-Math.Abs(x)*.38),P(x+.012,tip,.112-Math.Abs(x)*.36),P(x-.014,tip-.011,.104-Math.Abs(x)*.33),110,.84f+.028f*fl);}
                b.Lathe(0,0,new[]{.10f,.12f,.16f,.185f},new[]{.14f,.15f,.103f,.084f},new[]{.132f,.143f,.104f,.09f},111,32,.89f);
                b.Box(.028f,.061f,.11f,.068f,.012f,.006f,14,.80f);
            }else{
                b.Lathe(0,0,new[]{-.067f,.07f,.151f,.178f},new[]{.100f,.14f,.132f,.067f},new[]{.10f,.125f,.113f,.07f},113,28,.92f);
                for(int u=0;u<10;u++){float a=u*(float)Math.PI/5,x=(float)Math.Sin(a)*.113f,z=(float)Math.Cos(a)*.115f;b.Box(x,-.012f,z,.014f,.149f,.012f,106,.91f);}
                b.Box(0,.045f,.119f,.069f,.032f,.01f,112,.94f);
            }
            mask=Finish(b);b=new Builder();
            b.Lathe(0,0,new[]{-.32f,-.24f,-.08f,0},new[]{.048f,.054f,.069f,.066f},new[]{.049f,.057f,.07f,.063f},coatMat,14,.91f);sleeve=Finish(b);b=new Builder();
            b.Lathe(0,0,new[]{-.29f,-.21f,-.06f,.004f},new[]{.039f,.046f,.049f,.052f},new[]{.038f,.044f,.052f,.053f},coatMat,14,.91f);
            b.Oval(0,-.318f,.013f,.035f,.058f,.026f,110,.69f,10);cuff=Finish(b);b=new Builder();
            b.Lathe(0,0,new[]{.065f,.251f,.35f},new[]{.039f,.044f,.056f},new[]{.044f,.045f,.052f},coatMat,14);
            b.Oval(0,.057f,.045f,.049f,.051f,.11f,14,.72f);shoe=Finish(b);b=new Builder();
            if(index==0){
                b.Box(0,-.134f,.05f,.123f,.051f,.101f,110,.86f);
                b.Lathe(0,.05f,new[]{-.112f,-.075f,-.036f,-.010f},new[]{.039f,.014f,.026f,.017f},new[]{.041f,.016f,.026f,.017f},3,16);
            }else{
                for(int j=0;j<7;j++){float x=(j-3)*.018f,yy=-.094f+(float)Math.Sin(j)*.026f;
                    b.Box(x,yy,.033f,.009f,.121f,.012f,106);b.Box(x+.012f,yy-.055f,.033f,.029f,.013f,.015f,106);}
                b.Oval(0,-.024f,.033f,.062f,.02f,.01f,106,1.10f);
            }
            token=Finish(b);b=new Builder();
            for(int j=0;j<9;j++){
                float x=(j-4)*.034f,dy=.07f*(float)Math.Sin(j*2);
                b.Box(x,1.16f+dy,.135f,.005f,.24f+dy,.007f,109,.90f);
                b.Q(P(x-.019,1.085+dy,.143),P(x+.019,1.085+dy,.143),P(x+.020,1.01+dy,.151),P(x-.020,1.01+dy,.151),index==0?110:112,.86f);
            }pendants=Finish(b);
        }
        public void Draw(float x,float z,float yaw,float phase,float time,bool walking)
        {
            float gait=walking?(float)Math.Sin(phase):0;
            GL.glPushMatrix();GL.glTranslatef(x,Math.Abs(gait)*.014f,z);GL.glRotatef(yaw*57.29578f,0,1,0);
            GL.glScalef(1.02f,kind==0?1.15f:1.07f,1);
            GL.glPushMatrix();GL.glRotatef(gait*1.3f,0,0,1);coat.Draw();pendants.Draw();
            GL.glPushMatrix();GL.glTranslatef(0,1.698f,.027f);GL.glRotatef((float)Math.Sin(time*.63+kind)*9,0,1,0);GL.glRotatef(kind==0?6:-7,0,0,1);mask.Draw();GL.glPopMatrix();
            for(int s=-1;s<=1;s+=2){GL.glPushMatrix();GL.glTranslatef(s*.199f,1.464f,0);GL.glRotatef(s*6,0,0,1);GL.glRotatef(s*gait*17+(s==1?-8:0),1,0,0);sleeve.Draw();
                GL.glTranslatef(0,-.299f,0);GL.glRotatef(s==1?-30:-12,1,0,0);cuff.Draw();
                if(s==1){GL.glTranslatef(0,-.331f,0);GL.glRotatef(gait*9,0,0,1);token.Draw();}
                GL.glPopMatrix();}
            GL.glPopMatrix();
            for(int s=-1;s<=1;s+=2){GL.glPushMatrix();GL.glTranslatef(s*.083f,.34f,0);GL.glRotatef(s*gait*22,1,0,0);GL.glTranslatef(0,-.34f,0);shoe.Draw();GL.glPopMatrix();}
            GL.glPopMatrix();
        }
        public void Dispose(){coat.Dispose();mask.Dispose();sleeve.Dispose();cuff.Dispose();shoe.Dispose();token.Dispose();pendants.Dispose();}
    }
}

