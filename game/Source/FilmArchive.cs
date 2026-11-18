using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Uncalled
{
    public partial class Campus
    {
        Mesh stairNegative, schoolGate, classPortrait;

        void MakeArchiveSets()
        {
            var b = new Builder { indoor=true, palette=0, baseLight=1.16f };
            b.Box(0,-.10f,3.9f,7,.20f,9,0);
            b.Box(0,2.1f,8.0f,7,4.2f,.20f,1);
            b.Box(-3.31f,2.1f,3.9f,.16f,4.2f,8.2f,2);
            b.Box(3.31f,2.1f,3.9f,.16f,4.2f,8.2f,2);
            for(int i=0;i<10;i++)
            {
                float z=4.42f+i*.33f, h=.16f*(i+1);
                b.Box(1.38f,h/2,z,2.70f,h,.34f,19);
                b.Box(1.38f,h-.015f,z-.154f,2.69f,.022f,.027f,105,1.15f);
                if(i%2==0)b.Box(.07f,h+.52f,z,.034f,1.04f,.035f,105);
            }
            for(int i=0;i<32;i++)b.Box(.07f,1.12f+i*.047f,4.34f+i*.101f,.072f,.083f,.14f,3,1.1f);
            b.Box(-1.66f,2.09f,7.87f,2.40f,1.88f,.056f,98,1.38f);
            b.Box(-1.66f,2.09f,7.81f,.056f,1.89f,.066f,105);
            b.Box(-1.66f,2.04f,7.81f,2.40f,.056f,.066f,105);
            b.Box(1.68f,2.88f,7.82f,.68f,.37f,.05f,118,1.10f);
            stairNegative=b.End();

            b=new Builder { indoor=true, palette=0, baseLight=1.14f };
            b.Box(0,-.125f,6.4f,18,.25f,15.8f,19,.94f);
            b.Box(0,2.44f,12f,16,4.9f,.1f,96,1.34f);
            for(int side=-1;side<=1;side+=2)
            {
                b.Box(side*1.61f,1.28f,6.80f,.44f,2.56f,.42f,126,1.18f);
                b.Box(side*1.61f,2.56f,6.8f,.53f,.12f,.51f,105);
                b.Box(side*3.78f,.43f,6.80f,4.02f,.86f,.33f,2,1.19f);
                b.Box(side*3.78f,1.51f,6.8f,4.02f,.06f,.061f,105);
                for(int j=0;j<14;j++)b.Box(side*(1.9f+j*.28f),1.35f,6.8f,.037f,.99f,.037f,105);
            }
            b.Box(-1.60f,1.67f,6.54f,.33f,.71f,.042f,119,1.21f);
            schoolGate=b.End();

            b=new Builder { indoor=true, palette=0, baseLight=1.12f };
            b.Box(0,-.09f,5.1f,10,.18f,10,0);
            b.Box(0,1.66f,7.95f,10,3.33f,.14f,1);
            b.Box(0,1.87f,7.82f,4.62f,1.21f,.086f,7);
            b.Box(0,1.23f,7.76f,4.72f,.061f,.146f,3);
            for(int i=0;i<4;i++)Chair(b,(i-1.5f)*.74f,5.55f+i%2*.08f);
            classPortrait=b.End();
        }

        Bitmap ExposeFrame(int stage, int take)
        {
            const int width=480, height=270;
            GL.glClearColor(.10f,.13f,.14f,1);
            GL.glDepthMask(1);GL.glClear(16640);GL.glEnable(2929);GL.glDepthFunc(515);
            GL.glEnable(3553);GL.glDisable(3042);GL.glBindTexture(3553,atlas);
            if(stage==3)
            {
                if(take==0)Camera(-1.57f,1.38f,1.18f,.46f,.008f,width,height,43);
                else Camera(-.34f,1.52f,2.94f,.16f,.016f,width,height,37);
                GL.Shade(.63f,1.39f,.021f,0);GL.SceneLight(0);
                stairNegative.Draw();
                if(take==0)people.Draw(0,.58f,4.04f,2.32f,1.46f,1.37f,true,9);
                else people.Draw(0,-.14f,3.74f,3.27f,0,.81f,false,8);
            }
            else if(stage==33)
            {
                Camera(take==0?-.62f:.78f,1.54f,take==0?.95f:2.39f,take==0?.12f:-.09f,-.005f,width,height,41);
                GL.Shade(.62f,1.39f,.020f,0);GL.SceneLight(0);schoolGate.Draw();
                people.Draw(0,.38f,8.04f,3.15f,0,.95f,false,8);
                At(umbrella,.50f,1.99f,8.01f,-15);
            }
            else if(stage==39)
            {
                Camera(take==0?.06f:-.49f,1.38f,1.02f,take==0?0:.13f,-.096f,width,height,41);
                GL.Shade(.55f,1.36f,.019f,0);GL.SceneLight(0);classPortrait.Draw();
            }
            else
            {
                Camera(take==0?-.87f:.60f,1.55f,1.23f,take==0?.21f:-.17f,-.11f,width,height,43);
                GL.Shade(.48f,1.38f,.024f,0);GL.SceneLight(0);roomsFilm[1].Draw();
                if(stage!=26)people.Draw(1,-.16f,3.38f,stage==16?.36f:2.49f,0,2.22f,false,9);
            }
            var frame=new Bitmap(width,height,PixelFormat.Format32bppArgb);
            var rect=new Rectangle(0,0,width,height);
            var locked=frame.LockBits(rect,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{GL.glReadPixels(0,0,width,height,0x80E1,5121,locked.Scan0);}
            finally{frame.UnlockBits(locked);}
            GL.Ui();frame.RotateFlip(RotateFlipType.RotateNoneFlipY);
            SilverPrint(frame,stage*23+take);
            return frame;
        }

        static void SilverPrint(Bitmap frame,int seed)
        {
            int width=frame.Width, height=frame.Height;
            var rect=new Rectangle(0,0,width,height);
            var locked=frame.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
            try
            {
                var data=new byte[locked.Stride*height];
                Marshal.Copy(locked.Scan0,data,0,data.Length);
                int[] bins=new int[256];
                for(int i=0;i<data.Length;i+=4)bins[(data[i]*18+data[i+1]*75+data[i+2]*35)/128]++;
                int lo=0,hi=255,total=width*height;
                for(int count=0;lo<125 && count+bins[lo]<total*.016;lo++)count+=bins[lo];
                for(int count=0;hi>lo+36 && count+bins[hi]<total*.014;hi--)count+=bins[hi];
                var noise=new Random(131+seed);
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                {
                    int k=y*locked.Stride+x*4;
                    float light=(data[k]*18+data[k+1]*75+data[k+2]*35)/128f;
                    float dx=2f*x/width-1,dz=2f*y/height-1;
                    int value=(int)(18+(light-lo)*217/(hi-lo)-11*(dx*dx+dz*dz)+noise.Next(-5,6));
                    value=Math.Max(9,Math.Min(238,value));
                    data[k]=(byte)(value-4);data[k+1]=(byte)value;data[k+2]=(byte)(value+5);data[k+3]=255;
                }
                Marshal.Copy(data,0,locked.Scan0,data.Length);
            }
            finally{frame.UnlockBits(locked);}
        }

        public Bitmap ContactPrint(int stage,int tag)
        {
            bool pair=stage==3 && tag==2;
            var paper=new Bitmap(640,360,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(paper))
            using(var mono=new Font("Consolas",16,FontStyle.Regular,GraphicsUnit.Pixel))
            using(var dark=new SolidBrush(Color.FromArgb(46,45,38)))
            using(var caption=new SolidBrush(Color.FromArgb(233,198,133)))
            {
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                g.Clear(Color.FromArgb(220,212,188));
                g.FillRectangle(dark,13,14,614,289);
                int n=pair?2:1;
                for(int take=0;take<n;take++)
                {
                    int x=pair?21+take*305:22, width=pair?293:595;
                    using(var frame=ExposeFrame(stage,take))
                    {
                        var crop=pair?new Rectangle(55,0,370,270):new Rectangle(0,15,480,233);
                        g.DrawImage(frame,new Rectangle(x,42,width,229),crop,GraphicsUnit.Pixel);
                    }
                    string edge=pair?("17"+(take==0?"A":"B")+"     19:08"):((stage+1).ToString("00")+"A     EUNRAM");
                    g.DrawString(edge,mono,caption,x+7,273);
                }
                for(int i=0;i<28;i++)g.FillRectangle(Brushes.Wheat,22+i*22,22,10,8);
                g.DrawString(pair?"ER 35-2  /  2002.10.19        17A + 17B":stage==33?"GATE   /   2002.10.19":stage==39?"GRADUATION   /   2002.10.19":"NIGHT DIVISION   /   CONTACT PROOF",mono,dark,24,319);
            }
            return paper;
        }
    }
}
