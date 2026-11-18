using System;
using System.Collections.Generic;
using System.Linq;

namespace Uncalled
{
    public struct V
    {
        public float X, Z;
        public V(float x, float z)
        {
            X = x;
            Z = z;
        }

        public float Dist(V b)
        {
            float a = X - b.X, c = Z - b.Z;
            return (float)Math.Sqrt(a * a + c * c);
        }

        public static V operator +(V a, V b)
        {
            return new V(a.X + b.X, a.Z + b.Z);
        }

        public static V operator *(V a, float t)
        {
            return new V(a.X * t, a.Z * t);
        }
    }

    public class Room
    {
        public int x, z, n, tag;
        public string Name
        {
            get
            {
                return Book.RoomNames[tag] + " " + (101 + n);
            }
        }
    }

    public enum Kind
    {
        Note,
        Board,
        Door,
        Hide,
        Supply,
        Archive,
        Final,
        Station
    }

    public class Thing
    {
        public Kind kind;
        public int tag, room;
        public V p;
        public float rot;
        public Thing(Kind k, int t, int r, V v)
        {
            kind = k;
            tag = t;
            room = r;
            p = v;
        }
    }

    public class Actor
    {
        public int who, home, other, node, target = -1, last = -1, mode;
        public float x, z, yaw, wait, step, alert, cool, hear, anim, grace;
        public bool walking;
        public V[] route; public int routeStep;
        public int[] path;
        public int pathIndex;
        public V Pos
        {
            get
            {
                return new V(x, z);
            }
        }

        public Actor(int w, int n, V p)
        {
            who = w;
            node = n;
            x = p.X;
            z = p.Z;
        }
    }

    public class World
    {
        public const float Unit = 3.45f;
        public float Rise
        {
            get
            {
                return stage == 0 ? 0 : theme == 7 || theme == 11 ? 2.4f : 1.68f;
            }
        }

        public float Elevation(float z)
        {
            float band = (z / Unit - 2f) / 7f;
            int row = (int)Math.Floor(band);
            if (row < 0)
                return 0;
            float off = (band - row) * 7f;
            return (row + Math.Max(0, Math.Min(1, (off - 5f) / 2f))) * Rise;
        }

        public int W, H, stage, side, theme, seed;
        public bool revisedLayout;
        public byte[] cells;
        public int[] roomAt;
        public List<Room> rooms = new List<Room>();
        public List<Thing> things = new List<Thing>();
        public List<Actor> actors = new List<Actor>();
        public Dictionary<int, List<V>> solids = new Dictionary<int, List<V>>();
        public Dictionary<int, V> furniture = new Dictionary<int, V>();
        public bool[] seen;
        public V start;
        public float yaw;
        public Page page;
        public int[] reqRooms = new int[7];
        public int[] Dir;
        public List<int> hubs = new List<int>();
        Random rand;
        int[] visited, previous, queue;
        int visit;
        public World(int campaign, int s, int sub,bool revised=false)
        {
            revisedLayout=revised;
            stage = s;
            side = sub;
            seed = unchecked(campaign ^ ((s + 1) * 1597334677) ^ ((sub + 2) * 35771));
            rand = new Random(seed);
            page = side < 0 ? Book.Main[s] : Book.Side[sub];
            theme = page.theme;
            int nx = sub >= 0 ? 4 : (s == 0 ? 3 : (s < 4 ? 4 : (s % 5 == 4 ? 6 : 5)));
            int nz = sub >= 0 ? 3 : (s == 0 ? 2 : (s < 4 ? 3 : (s % 5 == 4 ? 5 : 4)));
            if(revised){
                if(sub>=0){nx=3;nz=2;}
                else if(s!=0){
                    nx=(theme==7||theme==11)?5:(theme==8||theme==9)?3:4;
                    nz=(s%5==4||theme==5)?4:3;
                }
            }
            W = nx * 8 + 2;
            H = nz * 7 + 2;
            cells = new byte[W * H];
            roomAt = Enumerable.Repeat(-1, W * H).ToArray();
            seen = new bool[W * H];
            Dir = new[]
            {
                1,
                W,
                -1,
                -W
            };
            visited = new int[W * H];
            previous = new int[W * H];
            queue = new int[W * H];
            var links = new HashSet<int>();
            var done = new bool[nx * nz];
            CarveGraph(0, nx, nz, done, links);
            for (int i = 0; i < nx * nz; i++)
            {
                if (i % nx < nx - 1 && rand.NextDouble() < .29)
                    Link(i, i + 1, links);
                if (i / nx < nz - 1 && rand.NextDouble() < .27)
                    Link(i, i + nx, links);
            }

            if(revised && (theme==0 || theme==7 || theme==11 || theme==5)){
                for(int k=0;k<nx-1;k++){Link(k,k+1,links);Link((nz-1)*nx+k,(nz-1)*nx+k+1,links);}
                for(int k=0;k<nz-1;k++){Link(k*nx,(k+1)*nx,links);Link((k+1)*nx-1,(k+2)*nx-1,links);}
            }
            int[] rt = Book.Rooms[theme];
            for (int i = 0; i < nx * nz; i++)
            {
                int ax = 2 + (i % nx) * 8, az = 2 + (i / nx) * 7;
                int typical = (i % 3 == 0 ? 0 : (i + s) % rt.Length);
                var r = new Room
                {
                    x = ax + 4,
                    z = az + 3,
                    n = i,
                    tag = rt[typical]
                };
                rooms.Add(r);
                hubs.Add(az * W + ax);
                cell(ax, az, 1, -1);
                if (i % nx < nx - 1 && links.Contains(Key(i, i + 1)))
                    for (int k = 0; k <= 8; k++)
                        cell(ax + k, az, 1, -1);
                if (i / nx < nz - 1 && links.Contains(Key(i, i + nx)))
                    for (int k = 0; k <= 7; k++)
                        cell(ax, az + k, 1, -1);
                for (int k = 0; k <= 3; k++)
                    cell(ax + k, az, 1, -1);
                cell(ax + 3, az + 1, 2, i);
                for (int xx = ax + 2; xx <= ax + 6; xx++)
                    for (int zz = az + 2; zz <= az + 4; zz++)
                        cell(xx, zz, 3, i);
                if (i % nx < nx - 1 && i % 4 == 2)
                {
                    cell(ax + 7, az + 3, 2, i);
                    cell(ax + 8, az + 3, 1, -1);
                    for (int k = 0; k < 3; k++)
                        cell(ax + 8, az + k, 1, -1);
                }
            }

            start = Center(hubs[0]);
            yaw = (float)Math.PI / 2;
            var used = new HashSet<int>();
            used.Add(0);
            int cur = hubs[0];
            for (int k = 0; k < 5; k++)
            {
                var d = Distances(cur);
                var ordered = rooms.Where(r => !used.Contains(r.n)).OrderByDescending(r => d[r.z * W + r.x]).ToArray();
                var selected = ordered.Length > 0 ? ordered[rand.Next(Math.Max(1, (ordered.Length + 3) / 5))] : rooms[k % rooms.Count];
                if(revised){
                    var available=rooms.Where(r=>!used.Contains(r.n)).OrderBy(r=>d[wRoomTile(r.n)]).ThenBy(r=>r.n).ToArray();
                    if(k==0&&s==0&&sub<0)selected=rooms[0];
                    else if(available.Length>0){
                        float fraction=k==0?.16f:k==3?.32f:k==4?.55f:.38f;
                        selected=available[Math.Min(available.Length-1,(int)(fraction*(available.Length-1)) +rand.Next(Math.Min(2,available.Length)))];
                    }
                }
                int ri = selected.n;
                reqRooms[k] = ri;
                used.Add(ri);
                cur = selected.z * W + selected.x;
                var th = new Thing(k < 3 ? Kind.Note : (k == 3 ? Kind.Board : Kind.Door), k < 3 ? k : 0, ri, Center(cur));
                if (k == 4)
                    th.p = new V(th.p.X, th.p.Z + 1.5f * Unit - .19f);
                things.Add(th);
            }

            int last = wRoomTile(reqRooms[3]);
            var exitDist = Distances(wRoomTile(reqRooms[4]));
            for (int j = 0; j < 2; j++)
            {
                var from = Distances(last);
                var candidates = rooms.Where(r => !used.Contains(r.n)).ToList();
                if (candidates.Count == 0)
                    candidates = rooms.Where(r => r.n != reqRooms[3] && r.n != reqRooms[4] && (j == 0 || r.n != reqRooms[5])).ToList();
                var r0 = candidates.OrderByDescending(r => from[wRoomTile(r.n)] + (j == 1 ? exitDist[wRoomTile(r.n)] / 2 : 0)).First();
                if(revised)r0=candidates.OrderBy(r=>Math.Abs(from[wRoomTile(r.n)]-(j==0?16:23)) + .14f*exitDist[wRoomTile(r.n)]).First();
                last = wRoomTile(r0.n);
                used.Add(r0.n);
                reqRooms[5 + j] = r0.n;
                V p = Center(last);
                things.Add(new Thing(Kind.Station, j, r0.n, new V(p.X - 1.45f, p.Z + 1.45f)));
            }

            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                int cellId = r.z * W + r.x;
                V h = Center((r.z - 1) * W + r.x + 2);
                things.Add(new Thing(Kind.Hide, 0, i, new V(h.X + .65f, h.Z - .5f)));
                if (i % 4 == 0)
                    things.Add(new Thing(Kind.Supply, 0, i, Center((r.z + 1) * W + r.x - 1)));
                for (int dx = -2; dx <= 2; dx += 4)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 2 && dz == -1)
                            continue;
                        int tile = cellId + dx + dz * W;
                        furniture[tile] = Center(tile);
                    }
            }

            if (side < 0 && stage % 5 == 4)
            {
                int ri = rooms.Count / 2;
                things.Add(new Thing(Kind.Archive, stage / 5, ri, Center((rooms[ri].z + 1) * W + rooms[ri].x + 1)));
            }

            if (side < 0 && s == 39)
            {
                for (int k = 1; k <= 4; k++)
                {
                    int ri = (k * 5 + 2) % rooms.Count;
                    V end = Center((rooms[ri].z + 1) * W + rooms[ri].x + 1);
                    things.Add(new Thing(Kind.Final, k, ri, new V(end.X, end.Z + Unit * .5f - .18f)));
                }
            }

            foreach (var o in things)
            {
                int t = Tile(o.p);
                List<V> list;
                if (!solids.TryGetValue(t, out list))
                {
                    list = new List<V>();
                    solids[t] = list;
                }

                list.Add(o.p);
            }

            if(revisedLayout)foreach(var room in rooms){
                if(room.tag!=0&&room.tag!=12)continue;
                for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++){
                    int tile=(room.z+z)*W+room.x+x;
                    if(!solids.ContainsKey(tile))furniture[tile]=Center(tile);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                int a = (side >= 0 && i == side / 3) ? reqRooms[4] : (i == 0 && s == 0 ? 0 : (i * 7 + 2 + s) % rooms.Count);
                int b = (a + Math.Max(2, rooms.Count / 3) + i) % rooms.Count;
                var r = rooms[a];
                int origin = stage == 0 && side < 0 && i == 0 ? hubs[0] + 2 : r.z * W + r.x - 1;
                var actor = new Actor(i, origin, Center(origin));
                actor.home = a;
                actor.other = b;
                actor.wait = 5 + i * 3;
                actor.yaw = -(float)Math.PI / 2;
                actors.Add(actor);
            }

            int count = (side >= 0 ? 1 : (s >= 20 ? 2 : 1));
            for (int i = 0; i < count; i++)
            {
                int ri = reqRooms[i == 0 ? 2 : 3];
                int n = hubs[ri];
                var a = new Actor(-1 - i, n, Center(n));
                a.home = ri;
                a.wait = 4;
                a.grace = (s == 0 ? 35 : 12);
                actors.Add(a);
            }
        }

        static int Key(int a, int b)
        {
            return Math.Min(a, b) * 100 + Math.Max(a, b);
        }

        void Link(int a, int b, HashSet<int> v)
        {
            v.Add(Key(a, b));
        }

        void CarveGraph(int a, int nx, int nz, bool[] got, HashSet<int> links)
        {
            got[a] = true;
            int[] ds = new[]
            {
                0,
                1,
                2,
                3
            }.OrderBy(t => rand.Next()).ToArray();
            foreach (int dr in ds)
            {
                int xx = a % nx + (dr == 0 ? 1 : dr == 2 ? -1 : 0), zz = a / nx + (dr == 1 ? 1 : dr == 3 ? -1 : 0);
                if (xx < 0 || zz < 0 || xx >= nx || zz >= nz)
                    continue;
                int b = zz * nx + xx;
                if (got[b])
                    continue;
                Link(a, b, links);
                CarveGraph(b, nx, nz, got, links);
            }
        }

        void cell(int x, int z, byte val, int ri)
        {
            int n = z * W + x;
            cells[n] = val;
            roomAt[n] = ri;
        }

        public V Center(int n)
        {
            return new V((n % W + .5f) * Unit, (n / W + .5f) * Unit);
        }

        public int Tile(V p)
        {
            int x = (int)Math.Floor(p.X / Unit), z = (int)Math.Floor(p.Z / Unit);
            return x < 0 || z < 0 || x >= W || z >= H ? -1 : z * W + x;
        }

        public bool Open(int n)
        {
            return n >= 0 && n < cells.Length && cells[n] > 0;
        }

        public bool Walk(V p)
        {
            float rad = .22f;
            for (int k = 0; k < 4; k++)
            {
                int n = Tile(new V(p.X + ((k & 1) == 0 ? -rad : rad), p.Z + ((k & 2) == 0 ? -rad : rad)));
                if (!Open(n))
                    return false;
            }

            int tile=Tile(p);
            int x=tile%W, z=tile/W;
            return !ContentsBlock(tile,p,p,x*Unit,z*Unit,(x+1)*Unit,(z+1)*Unit);
        }

        bool ContentsBlock(int tile,V a,V b,float left,float top,float right,float bottom)
        {
            V fp;
            if(furniture.TryGetValue(tile,out fp))
            {
                int room=roomAt[tile], tag=room>=0?rooms[room].tag:-1;
                if(revisedLayout && (tag==0 || tag==12))
                {
                    for(int side=-1;side<=1;side+=2)
                    {
                        float cx=fp.X+side*.86f;
                        if(Crosses(a,b,Math.Max(left,cx-.68f),Math.Max(top,fp.Z-.96f),Math.Min(right,cx+.68f),Math.Min(bottom,fp.Z+.51f)))return true;
                    }
                }
                else if(Crosses(a,b,Math.Max(left,fp.X-.82f),Math.Max(top,fp.Z-.61f),Math.Min(right,fp.X+.82f),Math.Min(bottom,fp.Z+.61f)))return true;
            }
            List<V> contents;
            if(solids.TryGetValue(tile,out contents))
                foreach(V v in contents)
                    if(Crosses(a,b,Math.Max(left,v.X-.64f),Math.Max(top,v.Z-.43f),Math.Min(right,v.X+.64f),Math.Min(bottom,v.Z+.43f)))return true;
            return false;
        }

        static bool ClipAxis(float origin, float delta, float lo, float hi, ref float near, ref float far)
        {
            if (Math.Abs(delta) < .000001f) return origin >= lo && origin <= hi;
            float start = (lo - origin) / delta, finish = (hi - origin) / delta;
            if (start > finish) { float swap = start; start = finish; finish = swap; }
            near = Math.Max(near, start);
            far = Math.Min(far, finish);
            return near <= far;
        }

        static bool Crosses(V a, V b, float left, float top, float right, float bottom)
        {
            if(a.X==b.X && a.Z==b.Z)
                return a.X>left && a.X<right && a.Z>top && a.Z<bottom;
            float near=0, far=1;
            return left<=right && top<=bottom &&
                ClipAxis(a.X,b.X-a.X,left,right,ref near,ref far) &&
                ClipAxis(a.Z,b.Z-a.Z,top,bottom,ref near,ref far);
        }

        public bool LineWalkable(V a, V b)
        {
            const float radius=.22f;
            int x0=(int)Math.Floor((Math.Min(a.X,b.X)-radius)/Unit), x1=(int)Math.Floor((Math.Max(a.X,b.X)+radius)/Unit);
            int z0=(int)Math.Floor((Math.Min(a.Z,b.Z)-radius)/Unit), z1=(int)Math.Floor((Math.Max(a.Z,b.Z)+radius)/Unit);
            for(int z=z0;z<=z1;z++)
                for(int x=x0;x<=x1;x++)
                {
                    int tile=z*W+x;
                    float left=x*Unit, top=z*Unit, right=(x+1)*Unit, bottom=(z+1)*Unit;
                    if(x<0 || x>=W || z<0 || z>=H || !Open(tile))
                    {
                        if(Crosses(a,b,left-radius,top-radius,right+radius,bottom+radius))return false;
                        continue;
                    }
                    if(ContentsBlock(tile,a,b,left,top,right,bottom))return false;
                }
            return true;
        }

        public V Slide(V p, float dx, float dz)
        {
            V q = new V(p.X + dx, p.Z);
            if (Walk(q))
                p = q;
            q = new V(p.X, p.Z + dz);
            if (Walk(q))
                p = q;
            return p;
        }

        public bool Sight(V a, V b)
        {
            float dis = a.Dist(b);
            int n = (int)(dis / .62f) + 1;
            for (int k = 1; k < n; k++)
            {
                float t = k / (float)n;
                if (!Open(Tile(new V(a.X + (b.X - a.X) * t, a.Z + (b.Z - a.Z) * t))))
                    return false;
            }

            return true;
        }

        bool CenterOccupiedByDesk(int n){
            if(!furniture.ContainsKey(n))return false;
            int r=roomAt[n];
            // Paired classroom desks have an open aisle through the centre of the tile.
            return !revisedLayout || r<0 || (rooms[r].tag!=0 && rooms[r].tag!=12);
        }
        public int[] Distances(int startN)
        {
            int[] d = Enumerable.Repeat(-1, cells.Length).ToArray();
            int head = 0, end = 0;
            queue[end++] = startN;
            d[startN] = 0;
            while (head < end)
            {
                int n = queue[head++];
                foreach (int u in Dir)
                {
                    int t = n + u;
                    if (!Open(t) || d[t] >= 0 || CenterOccupiedByDesk(t))
                        continue;
                    d[t] = d[n] + 1;
                    queue[end++] = t;
                }
            }

            return d;
        }

        bool BlockedCenter(int node)
        {
            if (CenterOccupiedByDesk(node))
                return true;
            List<V> v;
            if (!solids.TryGetValue(node, out v))
                return false;
            V p = Center(node);
            foreach (V o in v)
                if (Math.Abs(o.X - p.X) < .64f && Math.Abs(o.Z - p.Z) < .43f)
                    return true;
            return false;
        }

        public int[] Path(int a, int b)
        {
            if (!Open(a) || !Open(b) || a == b)
                return new int[0];
            visit++;
            int head = 0, end = 0;
            queue[end++] = a;
            visited[a] = visit;
            while (head < end)
            {
                int n = queue[head++];
                if (n == b)
                {
                    var res = new List<int>();
                    while (n != a)
                    {
                        res.Add(n);
                        n = previous[n];
                    }

                    res.Reverse();
                    return res.ToArray();
                }

                foreach (int d in Dir)
                {
                    int t = n + d;
                    if (!Open(t) || visited[t] == visit || (t != b && BlockedCenter(t)))
                        continue;
                    visited[t] = visit;
                    previous[t] = n;
                    queue[end++] = t;
                }
            }

            return new int[0];
        }

        int wRoomTile(int index)
        {
            Room r = rooms[index];
            return r.z * W + r.x;
        }

        public int Waypoint(int room)
        {
            Room r = rooms[room];
            return r.z * W + r.x - 1;
        }

        SchoolNav navigation;
        public V[] GuideRoute(V from, V destination)
        {
            if(navigation==null)navigation=new SchoolNav(this);
            return navigation.Plan(from,destination,true);
        }
        public void Go(Actor a,V goal)
        {
            int target=Tile(goal);
            if(!revisedLayout){Go(a,target);return;}
            if(navigation==null)navigation=new SchoolNav(this);
            a.target=target;a.route=navigation.Plan(a.Pos,goal);a.routeStep=0;
        }
        public void Go(Actor a, int target)
        {
            if(revisedLayout){Go(a,Center(target));return;}
            a.path = Path(Tile(a.Pos), target);
            a.pathIndex = 0;
            a.target = target;
        }

        public bool Move(Actor a, float dt, float speed)
        {
            if(revisedLayout){if(navigation==null)navigation=new SchoolNav(this);return navigation.Advance(a,dt,speed);}
            a.walking = false;
            if (a.path == null || a.pathIndex >= a.path.Length)
                return true;
            int n = a.path[a.pathIndex];
            V p = Center(n), v = a.Pos;
            float d = p.Dist(v), w = speed * dt;
            a.yaw = (float)Math.Atan2(p.X - v.X, p.Z - v.Z);
            if (d <= w)
            {
                a.x = p.X;
                a.z = p.Z;
                a.node = n;
                a.pathIndex++;
            }
            else
            {
                a.x += (p.X - a.x) / d * w;
                a.z += (p.Z - a.z) / d * w;
            }

            a.step += w * 3.8f;
            a.walking = true;
            return false;
        }

        public int RandRoom()
        {
            return rand.Next(rooms.Count);
        }

        public void Explore(V p)
        {
            int n = Tile(p);
            if (!Open(n))
                return;
            seen[n] = true;
            for (int k = 0; k < 4; k++)
            {
                int cur = n;
                for (int j = 0; j < 5; j++)
                {
                    cur += Dir[k];
                    if (!Open(cur))
                        break;
                    seen[cur] = true;
                }
            }

            int r = roomAt[n];
            if (r >= 0)
            {
                Room rm = rooms[r];
                for (int dx = -2; dx <= 2; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                        seen[(rm.z + dz) * W + rm.x + dx] = true;
            }
        }
    }
}