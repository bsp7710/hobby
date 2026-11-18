using System;
namespace Uncalled
{
    // An original chamber score in D minor. Notes are triggered on the audio device clock.
    // Quiet field sections deliberately leave room for nearby footsteps and the attendance stamp.
    public sealed class Score
    {
        struct Note { public int patch;public float step,amp,pan;public double phase; }
        const int Rate=22050, Length=Rate*4;
        readonly float[][] instruments=new float[4][];
        readonly Note[] ringing=new Note[18];
        int head,scene=-1,beat,remaining,pendingScene=-1,dwell;
        float gain;
        static readonly int[][] melody={
            new[]{74,0,69,0,0,65,67,0,69,0,0,65,0,64,62,0},
            new[]{62,0,0,0,0,0,65,0,0,0,0,0,61,0,0,0},
            new[]{62,0,63,0,62,0,0,57,62,63,0,0,58,0,57,0},
            new[]{69,0,72,74,0,0,72,0,65,0,67,0,69,0,0,0},
            new[]{69,72,0,0,70,0,65,0,67,69,0,0,65,0,0,0},
            new[]{65,0,69,0,74,0,0,72,69,0,67,0,64,65,0,0},
            new[]{62,0,0,69,0,0,65,0,0,0,64,0,0,0,0,0},
            new[]{74,0,72,69,0,65,0,0,67,0,69,0,74,0,0,0}
        };
        public Score(){
            for(int patch=0;patch<4;patch++){
                var samples=new float[Length];instruments[patch]=samples;
                double f=261.6256;var random=new Random(184+patch);double soft=0;
                for(int i=0;i<Length;i++){
                    double t=i/(double)Rate, a=2*Math.PI*f*t,v=0;
                    if(patch==0){
                        for(int k=1;k<=6;k++)v+=Math.Sin(a*k*Math.Sqrt(1+k*k*.000035))*Math.Exp(-t*(.93+k*.40))*(k==1?.82:.64/(k*k));
                        v+=.18*Math.Sin(a*1.0021)*Math.Exp(-t*1.24);
                        v*=1-Math.Exp(-t*125);
                    }else if(patch==1){
                        for(int k=1;k<=6;k++)v+=Math.Sin(a*k+.0012*f*Math.Sin(t*19))*Math.Exp(-t*(.83+k*.41))/(k*(k%2==0?3:1));
                        v*=(1-Math.Exp(-t*8.2))*.82;
                    }else if(patch==2){
                        v=(Math.Sin(a)+.28*Math.Sin(a*2)+.17*Math.Sin(a*2.985)+.071*Math.Sin(a*5.43))*(1-Math.Exp(-t*92))*Math.Exp(-t*1.61);
                    }else{
                        soft=.76*soft+.24*(random.NextDouble()*2-1);
                        v=(.55*Math.Sin(a+1.7*Math.Sin(a*.505)*Math.Exp(-t*2.9))+.26*Math.Sin(a*1.496)+.1*soft)*Math.Exp(-t*2.7)*(1-Math.Exp(-t*26));
                    }
                    samples[i]=(float)(v*Math.Min(1,(Length-i)/3800.0));
                }
            }
        }
        void Strike(int pitch,int patch,float amp,float pan){
            if(pitch==0)return;
            ringing[head]=new Note{patch=patch,phase=0,step=(float)Math.Pow(2,(pitch-60)/12.0),amp=amp,pan=pan};
            head=(head+1)%ringing.Length;
        }
        public void Set(int cue){
            if(cue!=pendingScene){pendingScene=cue;dwell=0;}
            if(cue!=scene && (dwell>Rate/3||scene<0)){scene=cue;beat=0;remaining=0;gain=.2f;}
        }
        void Advance(){
            int section=(beat/16)%4,cell=beat%16;int[] shifts={0,-2,-5,0};
            int shift=shifts[section];
            int pitch=melody[scene][cell];
            if(pitch>0 && !(scene==1 && section==1))Strike(pitch+shift,scene==3?1:scene==4?2:scene==2?3:0,scene==1?.037f:scene==2?.060f:.101f,-.15f);
            if(cell==0||cell==9&&scene!=1){
                int root=(section==2?46:50)+shift;
                Strike(root,1,scene==1?.034f:.065f,.24f);
                if(scene!=1&&scene!=2)Strike(root+7,2,.047f,-.36f);
            }
            if((scene==3||scene==4||scene==5||scene==7)&&cell%4==2)Strike(62+shift+(cell==6?7:0),2,.038f,.36f);
            remaining=(int)(Rate*(scene==2?.32:scene==1?.94:scene==5?.56:.67));
            beat=(beat+1)%64;
        }
        public void Sample(ref float left,ref float right){
            if(scene<0)return;
            if(dwell<Rate)dwell++;
            if(--remaining<=0)Advance();
            gain+=.00014f*(1-gain);
            for(int i=0;i<ringing.Length;i++){
                if(ringing[i].amp==0)continue;
                int j=(int)ringing[i].phase;
                if(j>=Length-1){ringing[i].amp=0;continue;}
                float[] bank=instruments[ringing[i].patch];double f=ringing[i].phase-j;
                double value=(bank[j]+(bank[j+1]-bank[j])*f)*ringing[i].amp*gain;
                left+=(float)(value*(1-ringing[i].pan));right+=(float)(value*(1+ringing[i].pan));ringing[i].phase+=ringing[i].step;
            }
        }
    }
}

