using System;
namespace Uncalled
{
    public struct TrackMark{public V spot;public float yaw;public bool left;}
    // The ringing hallway replays the player's recent steps back towards their point of entry.
    public sealed class Footprint
    {
        TrackMark[] history=new TrackMark[32];
        public readonly TrackMark[] echo=new TrackMark[12];
        int next,stored;
        public int played,total;
        public float clock;
        public bool Active {get{return total>0 && clock<total*.43f+1.25f;}}
        public void Clear(){stored=next=played=total=0;clock=0;}
        public void Add(V pos,float yaw){history[next]=new TrackMark{spot=pos,yaw=yaw,left=(next&1)==0};next=(next+1)%history.Length;stored=Math.Min(stored+1,history.Length);}
        public bool Ring()
        {
            if(stored<5||Active)return false;
            total=Math.Min(echo.Length,stored-2);played=0;clock=0;
            for(int k=0;k<total;k++)echo[k]=history[(next-3-k+history.Length)%history.Length];return true;
        }
        public void Tick(Play play,float dt){
            if(!Active)return;clock+=dt;
            while(played<total && clock>=.43f*played){
                var spot=echo[played++].spot;float d=spot.Dist(play.player);
                float pan=((spot.X-play.player.X)*(float)Math.Cos(play.yaw)-(spot.Z-play.player.Z)*(float)Math.Sin(play.yaw))/Math.Max(1,d);
                play.Sound("hallstep",Math.Max(0,1-d/33)*.51f,pan);
                if(played==1||played%3==0)foreach(var a in play.w.actors){
                    if(a.who<0 && a.mode!=2 && a.Pos.Dist(spot)<(a.who==-2?27:16)){
                        play.w.Go(a,spot);a.wait=0;a.mode=1;a.cool=1.20f;
                    }
                }
            }
        }
    }
}

