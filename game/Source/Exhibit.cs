using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Uncalled
{
    public sealed class Exhibit : IDisposable
    {
        Mesh article, front, reverse;
        uint face, back, campus;
        public readonly int kind;
        const int Paper=13,Brass=106,Edge=105,Black=14;
        static bool Has(string s,params string[] words){foreach(string w in words)if(s.Contains(w))return true;return false;}
        static int Classify(string s)
        {
            if(Has(s,"명찰","번호패","완장","표찰","꼬리표"))return 0;
            if(Has(s,"테이프","상자","릴 케이스","음반")&&!s.Contains("분필"))return 1;
            if(s.Contains("봉투"))return 6;
            if(Has(s,"인화","사진","네거티브","필름","밀착"))return 2;
            if(Has(s,"도장","검인"))return 3;
            if(Has(s,"열쇠","쐐기","종추","주물","변환 잭","호각"))return 4;
            if(Has(s,"악보","파트보","교가","연주표","건반","박자"))return 5;
            if(Has(s,"봉투","엽서","편지","쪽지"))return 6;
            if(Has(s,"연필","분필","만년필","지휘봉"))return 7;
            return 8;
        }
        static void Disc(Builder b,float x,float y,float z,float r,int mat,float shade)
        {
            for(int k=0;k<32;k++){double a=k*Math.PI/16,c=(k+1)*Math.PI/16;
                b.Q(new Pt(x,y,z),new Pt(x+r*(float)Math.Cos(a),y+r*(float)Math.Sin(a),z),new Pt(x+r*(float)Math.Cos(c),y+r*(float)Math.Sin(c),z),new Pt(x,y,z),mat,shade);}
        }
        static Mesh Label(float x,float y,float z,float w,float h,bool mirrored)
        {
            var b=new Builder();b.Q(new Pt(x-w/2,y-h/2,z),new Pt(x+w/2,y-h/2,z),new Pt(x+w/2,y+h/2,z),new Pt(x-w/2,y+h/2,z),13,1);
            var m=b.End();for(int i=0;i<4;i++){m.verts[i].u=((i==1||i==2)!=mirrored)?1:0;m.verts[i].v=i>=2?0:1;m.verts[i].nz=mirrored?-1:1;}
            return m;
        }
        public Exhibit(uint atlas,World w,int tag,Func<int,int,Bitmap> expose)
        {
            campus=atlas;string title=w.page.items[tag],note=w.page.notes[tag];kind=Classify(title);
            var b=new Builder();float labelX=0,labelY=0,labelZ=.030f,labelW=.54f,labelH=.28f,backZ=-.030f;
            if(kind==0){
                b.Box(0,0,0,.63f,.264f,.044f,Brass,1.29f);b.Box(0,0,.024f,.596f,.225f,.012f,Black,1.04f);
                b.Box(0,0,-.042f,.41f,.019f,.040f,Brass);b.Box(.20f,-.025f,-.053f,.022f,.071f,.026f,Edge);
                labelW=.565f;labelH=.188f;labelZ=.032f;
            }else if(kind==1){
                b.Box(0,0,0,.66f,.395f,.077f,Black,.81f);b.Box(0,.02f,.041f,.61f,.31f,.014f,109);
                b.Box(0,-.14f,.054f,.43f,.08f,.024f,Black);for(int n=-1;n<=1;n+=2){Disc(b,n*.135f,-.027f,.063f,.071f,13,1.05f);Disc(b,n*.135f,-.027f,.064f,.036f,Black,.6f);
                for(int j=0;j<6;j++){double a=j*Math.PI/3;b.Box(n*.135f+.042f*(float)Math.Cos(a),-.027f+.042f*(float)Math.Sin(a),.067f,.012f,.014f,.007f,Edge);}}
                labelY=.102f;labelH=.095f;labelZ=.053f;backZ=-.041f;
            }else if(kind==2){
                for(int j=0;j<3;j++)b.Box(j*.009f,j*.008f,-j*.006f,.525f,.396f,.007f,Paper);
                labelW=.50f;labelH=.37f;labelZ=.006f;backZ=-.017f;
            }else if(kind==3){
                b.Box(0,-.13f,0,.405f,.096f,.234f,28,.92f);b.Box(0,-.068f,0,.398f,.033f,.226f,11);
                b.Lathe(0,0,new[]{-.049f,.037f,.098f,.149f,.183f},new[]{.101f,.057f,.07f,.108f,.082f},new[]{.085f,.052f,.067f,.101f,.074f},3,25,1.1f);
                labelY=-.122f;labelW=.32f;labelH=.057f;labelZ=.119f;backZ=-.119f;
            }else if(kind==4){
                b.Box(.04f,0,0,.35f,.043f,.041f,Brass,1.18f);for(int j=0;j<3;j++)b.Box(.105f+j*.044f,-.032f-j%2*.017f,0,.025f,.09f,.045f,Brass,1.13f);
                for(int j=0;j<24;j++){double a=j*Math.PI/12,c=(j+1)*Math.PI/12;
                    b.Q(new Pt(-.18f+.107f*(float)Math.Cos(a),.102f*(float)Math.Sin(a),.017f),new Pt(-.18f+.107f*(float)Math.Cos(c),.102f*(float)Math.Sin(c),.017f),new Pt(-.18f+.067f*(float)Math.Cos(c),.062f*(float)Math.Sin(c),.017f),new Pt(-.18f+.067f*(float)Math.Cos(a),.062f*(float)Math.Sin(a),.017f),Brass,1.25f);}
                b.Box(-.12f,-.153f,-.026f,.314f,.126f,.015f,Paper);labelX=-.12f;labelY=-.153f;labelW=.303f;labelH=.116f;labelZ=-.015f;backZ=-.039f;
            }else if(kind==7){
                b.Box(0,-.01f,0,.54f,.253f,.075f,3,1.05f);
                for(int j=0;j<4;j++)b.Box(-.18f+j*.119f,-.015f,.054f,.027f,.194f,.025f,j%2==0?13:38);
                b.Box(0,-.092f,.066f,.56f,.09f,.018f,109);labelY=-.09f;labelZ=.077f;labelW=.47f;labelH=.073f;backZ=-.041f;
            }else{
                float h=kind==6?.32f:.43f;
                b.Box(0,0,0,.55f,h,.028f,kind==8?3:13,1.08f);
                if(kind!=6){b.Box(0,0,.016f,.53f,h-.015f,.012f,13);b.Box(-.255f,0,.021f,.016f,h,.012f,28);}
                labelH=h-.031f;labelW=.505f;labelZ=.025f;backZ=-.017f;
            }
            article=b.End();front=Label(labelX,labelY,labelZ,labelW,labelH,false);reverse=Label(labelX,labelY,backZ,labelW,labelH,true);
            using(var im=kind==2?expose(w.stage,tag):Paint(title,note,tag,w,false))face=GL.Texture(im,true);
            using(var im=Paint(title,note,tag,w,true))back=GL.Texture(im,true);
            GL.glBindTexture(3553,campus);
        }
        Bitmap Paint(string name,string note,int tag,World w,bool rear)
        {
            var im=new Bitmap(640,360,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(im))using(var head=new Font("맑은 고딕",kind==0?67:44,FontStyle.Bold,GraphicsUnit.Pixel))
            using(var sub=new Font("바탕",24,FontStyle.Regular,GraphicsUnit.Pixel))
            using(var type=new Font("Consolas",18,GraphicsUnit.Pixel))
            using(var ink=new SolidBrush(Color.FromArgb(59,64,54)))using(var line=new Pen(Color.FromArgb(124,130,110),2))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(kind==0&&!rear?Color.FromArgb(46,60,66):Color.FromArgb(218,211,184));
                if(kind==0&&!rear){
                    g.DrawRectangle(Pens.DarkGoldenrod,7,8,625,343);g.DrawString("銀 藍 高 等 學 校",sub,Brushes.Wheat,45,50);
                    g.DrawString(w.stage==0&&tag==0?"문 연 우":name,head,Brushes.AntiqueWhite,42,133);g.DrawString("EUNRAM    /    NIGHT DIVISION",type,Brushes.Tan,48,278);
                }else{
                    g.DrawString(rear?"은람고 야간부  /  보관 메모":"은 람 고 등 학 교   /   야 간 부",sub,ink,28,27);
                    g.DrawLine(line,26,78,611,78);
                    if(!rear && kind==5){for(int j=0;j<5;j++)g.DrawLine(line,32,204+j*12,607,204+j*12);for(int j=0;j<13;j++){int x=55+j*42,y=205+((j*3)%7)*5;g.FillEllipse(ink,x,y,13,9);g.DrawLine(line,x+12,y+3,x+12,y-33);}}
                    g.DrawString(rear?note:name,rear?sub:head,ink,new RectangleF(31,100,577,rear?159:104));
                    if(!rear && kind!=5){for(int y=216;y<286;y+=24)g.DrawLine(line,30,y,606,y);}
                    g.DrawString("2002. 10. 19     /     "+(w.stage+1).ToString("00")+"-"+(tag+1),type,ink,32,309);
                }
            }return im;
        }
        public void Draw()
        {
            GL.glBindTexture(3553,campus);article.Draw();GL.CharacterMaterial(5);
            GL.glBindTexture(3553,face);front.Draw();GL.glBindTexture(3553,back);reverse.Draw();
            GL.CharacterMaterial(0);GL.glBindTexture(3553,campus);
        }
        public void Dispose()
        {
            article.Dispose();front.Dispose();reverse.Dispose();GL.glDeleteTextures(1,ref face);GL.glDeleteTextures(1,ref back);
        }
    }
}

