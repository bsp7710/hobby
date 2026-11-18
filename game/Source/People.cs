using System;
namespace Uncalled
{
    public sealed class People : IDisposable
    {
        readonly CharacterCompany company;
        readonly Mesh[] props=new Mesh[3], gifts=new Mesh[3];
        readonly PatrolAssembly[] patrol=new PatrolAssembly[2];
        public People(uint atlas)
        {
            company=new CharacterCompany(atlas);
            for(int i=0;i<3;i++){props[i]=Held(i);gifts[i]=Gift(i);}
            for(int i=0;i<2;i++)patrol[i]=new PatrolAssembly(i);
        }
        public void FirstHand(){company.Hand();}
        static Mesh Held(int n)
        {
            var b=new Builder();
            if (n == 0)
            {
                b.Box(0, 0, 0, .102f, .141f, .038f, 14, 1.17f);
                b.Box(0, .013f, .022f, .074f, .059f, .012f, 73, 1.4f);
            }

            if (n == 1)
            {
                b.Box(0, 0, 0, .145f, .084f, .056f, 14, .85f);
                b.Oval(.019f, 0, .042f, .042f, .04f, .035f, 30, 1.3f);
                b.Box(-.042f, .021f, .029f, .023f, .014f, .009f, 13, 1.2f);
            }

            if (n == 2)
            {
                b.Box(0, 0, 0, .13f, .175f, .014f, 67, 1.05f);
                b.Box(-.052f, 0, 0, .013f, .177f, .017f, 29);
            }

            return b.End();
        }
        static Mesh Gift(int n){
            var b=new Builder();
            if(n==0){
                for(int i=0;i<25;i++){
                    float q=i/24f;float x=(q-.5f)*.112f;
                    float y=-.017f-(float)Math.Sin(q*Math.PI)*.081f;
                    b.Box(x,y,.009f,.007f,.009f,.005f,14,1.13f);
                }
                for(int i=-1;i<=1;i+=2){b.Oval(i*.056f,-.013f,.009f,.009f,.011f,.009f,13,1.19f,10);b.Box(i*.055f,-.027f,.009f,.007f,.021f,.007f,13);}
            }else if(n==1){
                b.Box(0,0,0,.096f,.119f,.003f,13);
                b.Box(0,.004f,.003f,.085f,.092f,.002f,97,.94f);
            }else{
                b.Box(0,0,0,.159f,.111f,.011f,29);
                b.Box(0,.001f,.008f,.150f,.102f,.011f,67,1.15f);
            }return b.End();
        }
        public void Draw(int n,float x,float z,float yaw,float phase,float time,bool walking,int pose=0)
        {
            if(n<3)company.Draw(n,x,z,yaw,phase,time,walking,pose,pose>=8?null:pose>=5 && (n!=1||(pose-5)%3==2)?gifts[n]:props[n]);
            else patrol[n-3].Draw(x,z,yaw,phase,time,walking);
        }
        public void Dispose()
        {
            company.Dispose();foreach(var m in props)m.Dispose();foreach(var p in patrol)p.Dispose();foreach(var gift in gifts)gift.Dispose();
        }
    }
}
