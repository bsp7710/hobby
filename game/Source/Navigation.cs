using System;
using System.Collections.Generic;
namespace Uncalled
{
    public sealed class SchoolNav
    {
        const int Sub=5;const float Cell=World.Unit/Sub;
        readonly World world;readonly int width,height;
        readonly byte[] walkable;
        readonly int[] visited,parent,queue;
        int search;
        public SchoolNav(World w)
        {
            world=w;width=w.W*Sub;height=w.H*Sub;
            walkable=new byte[width*height];visited=new int[walkable.Length];queue=new int[walkable.Length];parent=new int[walkable.Length];
            for(int z=0;z<height;z++)for(int x=0;x<width;x++)if(w.Open((z/Sub)*w.W+x/Sub))walkable[z*width+x]=(byte)(w.Walk(Point(z*width+x))?1:0);
        }
        V Point(int n){return new V((n%width+.5f)*Cell,(n/width+.5f)*Cell);}
        int Nearest(V p, bool requireClear=false)
        {
            int x=(int)(p.X/Cell),z=(int)(p.Z/Cell),best=-1;
            float dis=float.MaxValue;
            for(int k=0;k<=Sub+1;k++){
                for(int j=Math.Max(0,z-k);j<Math.Min(height,z+k+1);j++)
                    for(int i=Math.Max(0,x-k);i<Math.Min(width,x+k+1);i++){
                        if(k>0&&Math.Abs(i-x)!=k&&Math.Abs(j-z)!=k)continue;
                        int n=j*width+i;if(walkable[n]==0)continue;
                        float d=p.Dist(Point(n));if(d<dis && world.Sight(p,Point(n)) && (!requireClear || world.LineWalkable(p,Point(n)))){dis=d;best=n;}
                    }
                if(best>=0)break;
            }return best;
        }
        public bool[] Survey(V entrance)
        {
            var result=new bool[walkable.Length];int src=Nearest(entrance);
            if(src<0)return result;
            result[src]=true;int first=0,end=1;queue[0]=src;
            while(first<end){
                int n=queue[first++];
                for(int d=0;d<4;d++){int x=n%width+(d==0?1:d==1?-1:0),z=n/width+(d==2?1:d==3?-1:0);
                    if(x<0||x>=width||z<0||z>=height)continue;
                    int j=z*width+x;if(result[j]||walkable[j]==0)continue;
                    result[j]=true;queue[end++]=j;
                }
            }return result;
        }
        public bool CanApproach(bool[] reachable,V p){
            int x=(int)(p.X/Cell),z=(int)(p.Z/Cell);
            for(int a=Math.Max(0,x-3);a<Math.Min(width,x+4);a++)
                for(int b=Math.Max(0,z-3);b<Math.Min(height,z+4);b++){
                    int n=b*width+a;V spot=Point(n);
                    if(reachable[n]&&spot.Dist(p)<=1.82f && world.Sight(spot,p))return true;
                }
            return false;
        }
        bool CanInteract(V standing, V target)
        {
            float dx = standing.X - target.X, dz = standing.Z - target.Z;
            return dx*dx + dz*dz <= 1.72f*1.72f && world.Sight(standing, target);
        }
        public V[] Plan(V origin,V destination,bool interaction=false)
        {
            if(interaction && CanInteract(origin,destination))return new[]{origin};
            if(!interaction && origin.Dist(destination)<10 && world.LineWalkable(origin,destination))return new[]{destination};
            int a=Nearest(origin,interaction),b=interaction?-1:Nearest(destination);
            if(a<0 || !interaction && b<0)return new V[0];
            if(++search==int.MaxValue){Array.Clear(visited,0,visited.Length);search=1;}
            int h=0,t=1;queue[0]=a;visited[a]=search;
            while(h<t && (interaction || visited[b]!=search)){
                int cur=queue[h++],x=cur%width,z=cur/width;
                if(interaction && CanInteract(Point(cur),destination)){b=cur;break;}
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){
                    if((dx==0&&dz==0)||x+dx<0||z+dz<0||x+dx>=width||z+dz>=height)continue;
                    int n=cur+dx+dz*width;
                    if(visited[n]==search||walkable[n]==0)continue;
                    if(dx!=0&&dz!=0&&(walkable[cur+dx]==0||walkable[cur+dz*width]==0))continue;
                    if(interaction && !world.LineWalkable(Point(cur),Point(n)))continue;
                    visited[n]=search;parent[n]=cur;queue[t++]=n;
                }
            }
            if(b<0 || visited[b]!=search)return new V[0];
            if(interaction && b==a)return new[]{Point(a)};
            var raw=new List<V>();if(!interaction && world.Walk(destination)&&world.LineWalkable(Point(b),destination))raw.Add(destination);
            for(int n=b;n!=a;n=parent[n])raw.Add(Point(n));
            if(!world.LineWalkable(origin,raw.Count>0?raw[raw.Count-1]:destination))raw.Add(Point(a));
            raw.Reverse();
            var result=new List<V>();V last=origin;
            for(int i=0;i<raw.Count;){
                int far=i;
                // Short visibility pulls remove grid-shaped turns without crossing furniture.
                while(far+1<raw.Count && far<i+9 && world.LineWalkable(last,raw[far+1]))far++;
                last=raw[far];result.Add(last);i=far+1;
            }
            return result.ToArray();
        }
        public static float Turn(float a,float b,float delta)
        {
            float d=b-a;while(d>Math.PI)d-=2*(float)Math.PI;while(d< -Math.PI)d+=2*(float)Math.PI;
            return a+Math.Max(-delta,Math.Min(delta,d));
        }
        public bool Advance(Actor a,float dt,float speed)
        {
            a.walking=false;float budget=dt*speed;
            while(a.route!=null&&a.routeStep<a.route.Length && budget>0){
                V q=a.route[a.routeStep],p=a.Pos;float dx=q.X-p.X,dz=q.Z-p.Z,d=(float)Math.Sqrt(dx*dx+dz*dz);
                if(d<.014f){a.routeStep++;continue;}
                float step=Math.Min(budget,d);V r=world.Slide(p,dx/d*step,dz/d*step);
                float travelled=p.Dist(r);
                if(travelled<.0001f){a.route=null;a.routeStep=0;return true;}
                a.yaw=Turn(a.yaw,(float)Math.Atan2(dx,dz),dt*5.4f);
                a.x=r.X;a.z=r.Z;a.node=world.Tile(r);a.step+=travelled*4.5f;a.walking=true;budget-=step;
                if(travelled>=d-.014f)a.routeStep++;
            }return a.route==null||a.routeStep>=a.route.Length;
        }
    }
}

