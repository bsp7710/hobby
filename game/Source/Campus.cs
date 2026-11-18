using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;

namespace Uncalled
{
    public partial class Campus : IDisposable
    {
        class Chunk
        {
            public int x, z;
            public Mesh mesh, contacts;
        }

        List<Chunk> chunks = new List<Chunk>();
        public People people;
        public uint atlas;
        Mesh[] props = new Mesh[14];
        Mesh studio;
        Exhibit[] articles=new Exhibit[3];
        Mesh[] roomsFilm = new Mesh[4];
        Mesh umbrella, flash, shadow, footprint;
        public int triangles;
        public string renderer;
        static float U = World.Unit;
        public World w;
        public float cameraX, cameraZ;
        public Campus()
        {
            using (var st = Assembly.GetExecutingAssembly().GetManifestResourceStream("campus.jpg"))
            using (var b = new Bitmap(st))
            {
                atlas = GL.Texture(b, true);
            }

            GL.Programs();
            renderer = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(GL.glGetString(0x1f01));
            people = new People(atlas);
            MakeProps();
            Studio();
            MakeFilmRooms();
            MakeArchiveSets();
        }

        static Pt P(float x, float y, float z)
        {
            return new Pt(x, y, z);
        }

        void MakeProps()
        {
            for (int n = 0; n < 14; n++)
            {
                var b = new Builder();
                if (n == 0)
                {
                    Desk(b, 0, 0, 0, true);
                    b.Box(0, .846f, 0, .56f, .022f, .36f, 109, .87f);
                }

                if (n == 1)
                {
                    b.Box(0, .49f, 0, 1.12f, .98f, .59f, 14, .84f);
                    b.Box(0, 1.14f, .065f, 1.17f, .64f, .52f, 12, 1.08f);
                    b.Box(0, 1.52f, .12f, .75f, .044f, .20f, 39, 1.09f);
                }

                if (n == 2 || n == 7)
                {
                    b.Box(0, 1.20f, 0, 1.48f, 2.4f, .17f, n == 2 ? 6 : 19);
                    b.Box(-.82f, 1.22f, 0, .13f, 2.44f, .29f, 3);
                    b.Box(.82f, 1.22f, 0, .13f, 2.44f, .29f, 3);
                    b.Box(0, 2.46f, 0, 1.77f, .12f, .29f, 3);
                    b.Box(0, 2.65f, 0, .88f, .29f, .08f, 20, 1.32f);
                }

                if (n == 3)
                {
                    b.Box(0, .99f, 0, 1.17f, 1.98f, .76f, 8, 1.01f);
                    b.Box(0, .09f, 0, 1.24f, .18f, .83f, 14, .7f);
                }

                if (n == 4)
                {
                    b.Box(0, .35f, 0, .74f, .70f, .48f, 11);
                    b.Box(0, .74f, 0, .38f, .078f, .22f, 13, 1.15f);
                    for (int k = 0; k < 4; k++)
                        b.Box(k * .049f - .09f, .796f, 0, .029f, .03f, .139f, 39);
                }

                if (n == 5)
                {
                    Desk(b, 0, 0, 0, true);
                    b.Box(0, .853f, 0, .42f, .059f, .31f, 28, 1.1f);
                    b.Box(0, .89f, 0, .145f, .013f, .12f, 13, 1.20f);
                }

                if (n == 6)
                    b.Oval(0, 0, 0, .043f, .043f, .043f, 39, 1.9f, 8);
                if(n==8){CallBox(b,0,0,true);}
                if (n == 8)
                {
                    b.Box(.0f, .0f, .0f, .42f, .095f, .64f, 3, .55f);
                }

                if (n >= 9)
                {
                    Desk(b, 0, 0, 0, true);
                    int[] mats =
                    {
                        65,
                        66,
                        22,
                        12,
                        68
                    };
                    int tm = mats[n - 9];
                    b.Box(0, .887f, 0, .84f, .105f, .43f, tm, 1.13f);
                    if (n == 9)
                    {
                        b.Box(.32f, 1.13f, .18f, .25f, .40f, .21f, 17);
                    }

                    if (n == 10)
                        b.Box(0, 1.35f, .17f, .26f, .15f, .25f, 23);
                    if (n == 12)
                        b.Box(0, 1.18f, .18f, .73f, .55f, .13f, 12);
                    if (n == 13)
                        b.Box(-.29f, .96f, .02f, .19f, .09f, .16f, 38);
                }

                props[n] = b.End();
            }

            var f = new Builder();
            f.Lathe(0, 0, new[] { -.15f, -.14f, -.027f, .046f, .071f, .085f }, new[] { .032f, .036f, .036f, .062f, .063f, .061f }, new[] { .032f, .036f, .036f, .062f, .063f, .061f }, 14, 20, .79f);
            for (int k = 0; k < 6; k++)
                f.Lathe(0, 0, new[] { -.124f + k * .014f, -.12f + k * .014f }, new[] { .038f, .038f }, new[] { .038f, .038f }, 36, 20, .83f);
            f.Oval(0, -.146f, 0, .034f, .01f, .033f, 36, .84f);
            f.Box(0, .005f, .043f, .017f, .024f, .008f, 38);
            Sleeve(f);
            flash = f.End();
            var sh = new Builder();
            sh.Q(P(-.43f, .017f, -.32f), P(.43f, .017f, -.32f), P(.43f, .017f, .32f), P(-.43f, .017f, .32f), 92, 1);
            shadow = sh.End();var ft=new Builder();
            ft.Q(P(-.076f,.018f,-.153f),P(.076f,.018f,-.153f),P(.076f,.018f,.153f),P(-.076f,.018f,.153f),117,1);footprint=ft.End();
        }

        static void Desk(Builder b, float x, float y, float z, bool wide)
        {
            float width = wide ? 1.37f : 1.05f;
            b.Box(x, y + .793f, z, width, .085f, .66f, 11, .99f);
            b.Box(x, y + .641f, z+.018f, width-.14f, .032f, .46f, 105, .82f);
            b.Box(x, y + .704f, z+.230f, width-.14f, .13f, .026f, 105, .78f);
            for(int side=-1;side<=1;side+=2)
            {
                b.Box(x+side*(width*.5f-.066f),y+.704f,z+.017f,.025f,.131f,.456f,105,.86f);
                b.Box(x+side*(width*.5f-.10f),y+.18f,z,.036f,.034f,.436f,105,.82f);
            }
            for (int a = -1; a <= 1; a += 2)
                for (int c = -1; c <= 1; c += 2)
                    b.Box(x + a * (width * .5f - .10f), y + .328f, z + c * .222f, .042f, .656f, .045f, 14, .82f);
        }

        static void Chair(Builder b, float x, float z,bool forward=false)
        {
            float side=forward?-1:1;
            b.Box(x, .45f, z, .42f, .055f, .4f, 11);
            b.Box(x, .70f, z + side*.18f, .43f, .19f, .048f, 3);
            for (int a = -1; a <= 1; a += 2)
                for (int c = -1; c <= 1; c += 2)
                    b.Box(x + a * .154f, c == 1 ? .38f : .22f, z + side*c*.14f, .03f, c == 1 ? .76f : .44f, .03f, 14, .8f);
        }

        static void Furniture(Builder b, Room r, float x, float z, int v)
        {
            int m = r.tag;
            if(b.terrain!=null && b.terrain.revisedLayout && (m==0 || m==12)){
                Desk(b,x-.86f,0,z,false);Chair(b,x-.86f,z-.62f,true);
                Desk(b,x+.86f,0,z,false);Chair(b,x+.86f,z-.62f,true);
                DeskThings(b,x,z,(r.n*17+v*7+(int)(x/U)*3)%37);
                return;
            }
            if(m==20 && b.terrain!=null && b.terrain.revisedLayout){
                b.Box(x,.62f,z,1.32f,1.24f,.57f,3,.89f);
                for(int i=-1;i<=1;i++)for(int j=0;j<3;j++){
                    b.Box(x+i*.419f,.256f+j*.374f,z-.30f,.386f,.335f,.035f,104,1.08f);
                    b.Box(x+i*.419f+.133f,.27f+j*.374f,z-.331f,.026f,.07f,.03f,106);}
                b.Box(x,1.253f,z,1.38f,.057f,.62f,11);return;
            }
            if(m==13 && b.terrain!=null && b.terrain.revisedLayout && (v&1)==0){
                b.Box(x,.34f,z,1.42f,.13f,.68f,3);b.Box(x,.47f,z,1.41f,.14f,.66f,69);
                b.Box(x-.46f,.58f,z,.36f,.09f,.51f,13);for(int i=-1;i<=1;i+=2)b.Box(x+i*.59f,.19f,z,.099f,.38f,.52f,14);
                return;
            }
            if (m == 3 || m == 19 || m == 23)
            {
                b.Box(x, .66f, z, .99f, 1.30f, .56f, 30);
                b.Box(x, .80f, z - .32f, 1.14f, .083f, .3f, 22, 1.08f);
                b.Box(x, 1.28f, z, 1.09f, .10f, .60f, 3);
                return;
            }

            if (m == 8 || m == 5 || m == 21)
            {
                b.Box(x, .86f, z, 1.18f, 1.72f, .55f, 16);
                b.Box(x, 1.77f, z, 1.24f, .09f, .61f, 3);
                return;
            }

            if (m == 6)
            {
                b.Box(x, .41f, z, 1.39f, .19f, .73f, 34, 1.09f);
                b.Box(x - .48f, .54f, z, .30f, .08f, .59f, 33);
                for (int j = -1; j <= 1; j += 2)
                    b.Box(x + j * .57f, .2f, z, .055f, .4f, .64f, 14);
                return;
            }

            if (m == 7 || m == 14)
            {
                b.Box(x, .43f, z, 1.5f, .11f, .40f, 11);
                b.Box(x - .53f, .20f, z, .10f, .4f, .34f, 14);
                b.Box(x + .53f, .20f, z, .10f, .4f, .34f, 14);
                return;
            }

            Desk(b, x, 0, z, false);
            if (m == 1 || m == 9 || m == 12 || m == 17)
            {
                b.Box(x, 1.075f, z, .63f, .49f, .47f, 17);
                b.Box(x, .858f, z - .23f, .54f, .045f, .15f, 14);
            }
            else if (m == 4 || m == 11 || m == 22)
            {
                for (int k = 0; k < 3; k++)
                {
                    int t = (k + v) % 3;
                    b.Lathe(x - .29f + k * .23f, z, new[] { .84f, 1.01f, 1.12f, 1.18f }, new[] { .065f, .072f, .027f, .025f }, new[] { .065f, .072f, .027f, .025f }, new[] { 35, 14, 38 }[t], 8);
                }
            }
            else if (m == 2 || m == 10)
            {
                b.Box(x, .868f, z, .55f, .048f, .35f, 14);
                b.Box(x + .13f, 1.09f, z + .14f, .03f, .44f, .034f, 14);
                b.Box(x + .07f, 1.32f, z, .21f, .12f, .22f, 30);
            }
            else if (m == 20 || m == 13)
            {
                b.Box(x, 1.02f, z, .62f, .33f, .38f, 8);
            }
            else
                Chair(b, x, z + .71f);
        }

        static void Wall(Builder b, float x, float z, int dx, int dz, float along, float y0, float y1, float width, float inset, int mat, float light = 1)
        {
            float nx = dx, nz = dz, tx = nz, tz = -nx, mid = U * .5f - inset;
            float cx = x + nx * mid + tx * along, cz = z + nz * mid + tz * along;
            float off = width / 2;
            b.Q(P(cx - tx * off, y0, cz - tz * off), P(cx + tx * off, y0, cz + tz * off), P(cx + tx * off, y1, cz + tz * off), P(cx - tx * off, y1, cz - tz * off), mat, light);
        }

        public void Build(World map)
        {
            foreach (var c in chunks)
            {
                c.mesh.Dispose();
                if(c.contacts!=null)c.contacts.Dispose();
            }
            chunks.Clear();
            w = map;
            BakeLight(map);
            for(int i=0;i<3;i++){if(articles[i]!=null)articles[i].Dispose();articles[i]=new Exhibit(atlas,map,i,ContactPrint);}
            triangles = 0;
            int group = 5;
            for (int z0 = 0; z0 < w.H; z0 += group)
                for (int x0 = 0; x0 < w.W; x0 += group)
                {
                    var b = new Builder
                    {
                        indoor = true,
                        palette = w.theme,
                        terrain = w
                    };
                    var contacts = new Builder { terrain=w };
                    for (int iz = z0; iz < z0 + group && iz < w.H; iz++)
                        for (int ix = x0; ix < x0 + group && ix < w.W; ix++)
                        {
                            int n = iz * w.W + ix;
                            if (!w.Open(n))
                                continue;
                            float x = (ix + .5f) * U, z = (iz + .5f) * U, hl = U * .5f, ce = 3.22f;
                            int room = w.roomAt[n], type = w.cells[n];
                            Room R = room >= 0 ? w.rooms[room] : null;
                            int floor = type == 3 && R != null && (R.tag == 3 || R.tag == 23 || R.tag == 7 || R.tag == 19) ? 3 : 0;
                            float bright = type == 1 ? .85f : .97f;
                            if (Math.Abs(w.Elevation(z + hl) - w.Elevation(z - hl)) > .09f)
                            {
                                for (int step = 0; step < 7; step++)
                                {
                                    float za = z - hl + U * step / 7, zb = za + U / 7, delta = w.Elevation(za) - w.Elevation(zb);
                                    b.Q(P(x - hl, delta, zb), P(x + hl, delta, zb), P(x + hl, 0, za), P(x - hl, 0, za), 19, bright * .98f);
                                    b.Q(P(x - hl, delta, zb), P(x + hl, delta, zb), P(x + hl, 0, zb), P(x - hl, 0, zb), 14, .81f);
                                }

                                b.Box(x - hl + .07f, .90f, z, .055f, .065f, U, 11);
                                b.Box(x + hl - .07f, .90f, z, .055f, .065f, U, 11);
                            }
                            else
                                b.Q(P(x - hl, 0, z + hl), P(x + hl, 0, z + hl), P(x + hl, 0, z - hl), P(x - hl, 0, z - hl), floor, bright);
                            b.Q(P(x - hl, ce, z - hl), P(x + hl, ce, z - hl), P(x + hl, ce, z + hl), P(x - hl, ce, z + hl), 4, .83f);
                            bool lamp = type == 1 ? (ix + iz) % 4 == 0 : type == 3 && R != null && iz == R.z && ((ix + R.x) % 3 == 0);
                            if (lamp)
                            {
                                b.Box(x, 3.13f, z, 1.28f, .09f, .30f, 14, 1.0f);
                                b.Box(x, 3.065f, z, 1.10f, .041f, .056f, 39, 2.4f);
                                b.Box(x, 3.065f, z + .105f, 1.10f, .041f, .047f, 39, 2.1f);
                            }

                            if (type == 2)
                            {
                                b.Box(x - .91f, 1.39f, z, .12f, 2.78f, .17f, 3);
                                b.Box(x + .91f, 1.39f, z, .12f, 2.78f, .17f, 3);
                                b.Box(x, 2.78f, z, 1.94f, .13f, .19f, 3);
                                b.Box(x - .98f, 1.27f, z + .57f, .085f, 2.55f, 1.18f, 6);
                                if (R != null)
                                {
                                    b.Box(x + .99f, 2.28f, z - .10f, .53f, .29f, .04f, 40 + R.tag, 1.33f);
                                }
                            }

                            for (int d = 0; d < 4; d++)
                            {
                                if (w.Open(n + w.Dir[d]))
                                    continue;
                                int dx = d == 0 ? 1 : d == 2 ? -1 : 0, dz = d == 1 ? 1 : d == 3 ? -1 : 0;
                                Wall(b, x, z, dx, dz, 0, 0, 1.10f, U, 0, 2, 1);
                                Wall(b, x, z, dx, dz, 0, 1.10f, ce, U, 0, 1, .96f);
                                Wall(b, x, z, dx, dz, 0, .02f, .125f, U, .019f, 14, .72f);
                                int h = Math.Abs(ix * 13 + iz * 39 + d * 7);
                                if(type==1 && (d==0||d==1))CorridorDressing(b,x,z,dx,dz,h,w.theme);
                                if ((d == 2 || d == 3) && h % 4 != 0)
                                {
                                    Window(b,x,z,dx,dz,d==2?iz:ix);
                                }
                                else if (w.revisedLayout && R!=null && type==3 && d==1 && (R.tag==0||R.tag==12) && Math.Abs(ix-R.x)<=1)
                                    ClassroomBoard(b,x,z,ix,R);
                                else if (R != null && type == 3 && d == 1 && ix == R.x)
                                {
                                    Wall(b, x, z, dx, dz, 0, 1.12f, 2.39f, 2.90f, .035f, R.tag == 3 || R.tag == 19 ? 9 : 7, 1.05f);
                                    Wall(b, x, z, dx, dz, 0, 1.08f, 1.14f, 2.98f, .085f, 3);
                                    WallBox(b,x,z,dx,dz,0,1.083f,.133f,3.01f,.058f,.165f,105);
                                    Wall(b,x,z,dx,dz,-1.03f,1.42f,2.24f,.71f,.049f,R.tag==0?123:120+R.tag%3,1.13f);
                                    Wall(b,x,z,dx,dz,.96f,2.47f,2.94f,.48f,.067f,116,1.06f);
                                }
                                else if (type == 1)
                                {
                                    int mat = h % 6 == 0 ? 9 : h % 11 == 0 ? 23 : h % 8 == 0 ? 18 : h % 7 == 0 ? 21 : h % 5 == 0 ? 10 : -1;
                                    if (mat >= 0)
                                        Wall(b, x, z, dx, dz, 0, mat == 10 ? 1.88f : 1.25f, mat == 10 ? 2.57f : 2.13f, mat == 9 ? 1.6f : mat == 18 ? .51f : .73f, .048f, mat, 1.11f);
                                }
                                else if (R != null && type == 3 && h % 5 == 0)
                                {
                                    Wall(b, x, z, dx, dz, -.47f, 1.40f, 2.07f, .49f, .035f, 13, .96f);
                                }
                            }

                            if(ix==2&&iz==2)Entrance(b,x-1.53f,z-.9f);
                            V pos;
                            if (R != null && w.furniture.TryGetValue(n, out pos))
                            {
                                Furniture(b, R, x, z, ix + iz);
                                if(w.revisedLayout && (R.tag==0 || R.tag==12))
                                {
                                    Contact(contacts,x-.86f,z-.11f,.85f,1.11f);
                                    Contact(contacts,x+.86f,z-.11f,.85f,1.11f);
                                }
                                else Contact(contacts,x,z,.91f,.59f);
                            }
                        }

                    if (b.list.Count > 0)
                    {
                        var m = b.End();
                        chunks.Add(new Chunk { x = x0, z = z0, mesh = m, contacts=contacts.list.Count==0?null:contacts.End() });
                        triangles += m.Count / 2;
                    }
                }
        }

        void Studio()
        {
            var b = new Builder
            {
                indoor = true,
                palette = 0
            };
            float unit = 3.0f;
            for (int z = 0; z < 13; z++)
                for (int x = -1; x <= 1; x++)
                {
                    float xx = x * unit, zz = z * unit;
                    b.Q(P(xx - 1.5f, 0, zz + 1.5f), P(xx + 1.5f, 0, zz + 1.5f), P(xx + 1.5f, 0, zz - 1.5f), P(xx - 1.5f, 0, zz - 1.5f), 0, .90f);
                    b.Q(P(xx - 1.5f, 3.3f, zz - 1.5f), P(xx + 1.5f, 3.3f, zz - 1.5f), P(xx + 1.5f, 3.3f, zz + 1.5f), P(xx - 1.5f, 3.3f, zz + 1.5f), 4, .9f);
                    if (x == -1 || x == 1)
                    {
                        float wall = xx + Math.Sign(x) * 1.5f;
                        float d = x == 1 ? -1 : 1;
                        b.Q(P(wall, 0, zz - 1.5f), P(wall, 0, zz + 1.5f), P(wall, 1.10f, zz + 1.5f), P(wall, 1.10f, zz - 1.5f), 2, 1.15f);
                        b.Q(P(wall, 1.1f, zz - 1.5f), P(wall, 1.1f, zz + 1.5f), P(wall, 3.3f, zz + 1.5f), P(wall, 3.3f, zz - 1.5f), 1, 1);
                        if (x == -1){
                            Window(b,wall+U*.5f,zz,-1,0,z);
                            if(z==1)Entrance(b,wall+.26f,zz-.85f);
                        }
                        else if (z % 3 == 0)
                        {
                            b.Box(wall - .055f, 1.24f, zz, .12f, 2.48f, 1.50f, 6, 1.22f);
                            b.Box(wall - .15f, 2.55f, zz, .10f, .25f, .70f, 40 + z % 18, 1.4f);
                        }
                        else if (z % 3 == 2)
                            b.Box(wall - .04f, 1.7f, zz, .08f, 1.0f, 1.66f, 9, 1);
                    }

                    if (x == 0 && z % 2 == 0)
                    {
                        b.Box(.45f, 3.2f, zz, 1.21f, .10f, .31f, 14);
                        b.Box(.45f, 3.12f, zz, 1.1f, .038f, .15f, 39, 2.5f);
                    }
                }

            b.Box(0, 1.6f, 37.5f, 9, 3.2f, .15f, 2);
            b.Box(.2f, 1.29f, 37.38f, 1.45f, 2.58f, .11f, 6);
            b.Box(.2f, 2.77f, 37.33f, .82f, .23f, .04f, 20, 1.63f);
            TitleStation(b);
            studio = b.End();
        }

        void MakeFilmRooms()
        {
            var ub = new Builder();
            for (int i = 0; i < 48; i++)
            {
                float a=i*(float)Math.PI/24,b=(i+1)*(float)Math.PI/24;
                int cloth=i/6%2==0?69:27;
                for(int row=0;row<6;row++){
                    var p=new Pt[4];for(int j=0;j<4;j++){
                        float r=(row+(j>=2?1:0))/6f;
                        float ang=j==0||j==3?a:b;
                        float bow=(float)Math.Cos(ang*8);
                        float h=.155f-.13f*r-.25f*r*r-.020f*bow*r*r*r;
                        p[j]=P((float)Math.Sin(ang)*r*.89f,h,(float)Math.Cos(ang)*r*.89f);
                    }ub.Q(p[0],p[1],p[2],p[3],cloth,1.03f);
                    if(i%6==0)ub.Q(p[0],P(p[0].x+.007f,p[0].y-.003f,p[0].z),P(p[3].x+.007f,p[3].y-.003f,p[3].z),p[3],105,.91f);
                }
            }

            ub.Box(0, -.34f, 0, .012f, .99f, .012f, 38, 1.2f);
            umbrella = ub.End();
            for (int k = 0; k < 4; k++)
            {
                var b = new Builder
                {
                    indoor = true,
                    palette = new[]
                    {
                        2,
                        3,
                        5,
                        0
                    }[k],
                    baseLight = 1.06f
                };
                int curtain = new[]
                {
                    69,
                    70,
                    71,
                    69
                }[k];
                for (int x = -2; x <= 2; x++)
                    for (int z = 0; z < 4; z++)
                    {
                        float X = x * 2, Z = z * 2;
                        b.Q(P(X - 1, 0, Z + 1), P(X + 1, 0, Z + 1), P(X + 1, 0, Z - 1), P(X - 1, 0, Z - 1), k == 2 ? 74 : 0, .95f);
                        b.Q(P(X - 1, 3.2f, Z - 1), P(X + 1, 3.2f, Z - 1), P(X + 1, 3.2f, Z + 1), P(X - 1, 3.2f, Z + 1), 4, .82f);
                    }

                b.Box(0, 1.58f, 7.09f, 10, 3.16f, .12f, k == 3 ? 1 : 2, 1.02f);
                b.Box(-4.90f, 1.59f, 3, .16f, 3.2f, 8, 1);
                b.Box(4.90f, 1.59f, 3, .16f, 3.2f, 8, 1);
                b.Box(-.14f, 2.05f, 6.97f, 5.65f, 1.69f, .06f, k == 3 ? 75 : 5, 1.32f);
                b.Box(-3, 1.93f, 6.80f, .96f, 2.53f, .13f, curtain, .93f);
                b.Box(2.8f, 1.93f, 6.80f, .92f, 2.53f, .13f, curtain, .94f);
                b.Box(0, 3.08f, 6.8f, 7.1f, .045f, .056f, 38);
                b.Box(3.94f, 1.28f, 6.91f, 1.28f, 2.56f, .12f, 6);
                b.Box(-3.92f, 1.82f, 6.89f, .98f, .85f, .09f, 72);
                if(k==0)CallBox(b,2.12f,6.76f,false);
                if (k == 0)
                {
                    b.Box(-1.44f, .58f, 4.94f, 2.51f, 1.16f, .89f, 3);
                    b.Box(-1.45f, 1.21f, 4.94f, 2.39f, .12f, .79f, 65);
                    b.Box(-2.39f, 1.55f, 5.22f, .54f, .62f, .43f, 17);
                    b.Box(-.66f, 1.47f, 5.28f, .43f, .45f, .29f, 18);
                    b.Box(.88f, .53f, 5.84f, .62f, 1.06f, .46f, 73);
                    b.Box(1.46f, 1.91f, 6.79f, .88f, .41f, .05f, 12);
                }

                if (k == 1)
                {
                    b.Box(0, 2.22f, 4.95f, 6.11f, .021f, .019f, 14);
                    for (int j = 0; j < 7; j++)
                    {
                        float xx = -2.44f + j * .8f;
                        b.Box(xx, 1.98f, 4.95f, .47f, .54f, .014f, 66, 1.15f);
                        b.Box(xx, 2.22f, 4.94f, .044f, .12f, .029f, 38);
                    }

                    Desk(b, -1.14f, 0, 4.57f, true);
                    b.Box(-1.03f, .87f, 4.52f, .72f, .082f, .47f, 14);
                    b.Box(1.1f, 2.50f, 5.9f, .39f, .28f, .25f, 23, 1.42f);
                }

                if (k == 2)
                {
                    b.Box(-.95f, .70f, 4.69f, 2.14f, 1.4f, .74f, 30, .92f);
                    b.Box(-.95f, .87f, 4.21f, 2.17f, .10f, .32f, 22, 1.10f);
                    b.Box(-.95f, 1.43f, 4.69f, 2.23f, .1f, .82f, 74);
                    b.Box(-.80f, 1.23f, 4.25f, .56f, .46f, .028f, 67, 1.05f);
                    b.Box(-.9f, .45f, 3.82f, 1.16f, .13f, .40f, 74);
                    for (int j = -1; j <= 1; j += 2)
                        b.Box(-.9f + j * .45f, .21f, 3.82f, .07f, .42f, .27f, 14);
                    b.Box(2.3f, .97f, 5, .94f, 1.94f, .42f, 16);
                }

                if (k == 3)
                {
                    b.Box(-1.70f, .49f, 4.88f, 2.84f, .14f, .56f, 11);
                    for (int j = -1; j <= 1; j += 2)
                        b.Box(-1.7f + j * .96f, .22f, 4.88f, .12f, .44f, .4f, 14);
                    b.Box(1.88f, 1.55f, 5.63f, .095f, 3.10f, .08f, 14);
                    b.Box(1.88f, 2.02f, 5.60f, .73f, .97f, .08f, 72);
                }

                roomsFilm[k] = b.End();
            }
        }

        static void At(Mesh m, float x, float y, float z, float a = 0)
        {
            GL.glPushMatrix();
            GL.glTranslatef(x, y, z);
            if (a != 0)
                GL.glRotatef(a, 0, 1, 0);
            m.Draw();
            GL.glPopMatrix();
        }

        void Camera(float x, float y, float z, float yaw, float pitch, int width, int height, float fov)
        {
            cameraX = x;
            cameraZ = z;
            GL.glViewport(0, 0, width, height);
            GL.glMatrixMode(5889);
            GL.glLoadIdentity();
            float n = .055f, hh = n * (float)Math.Tan(fov * Math.PI / 360.0), hw = hh * width / Math.Max(1, height);
            GL.glFrustum(-hw, hw, -hh, hh, n, 120);
            GL.glMatrixMode(5888);
            GL.glLoadIdentity();
            GL.glScalef(1, 1, -1);
            GL.glRotatef(pitch * 57.29578f, 1, 0, 0);
            GL.glRotatef(-yaw * 57.29578f, 0, 1, 0);
            GL.glTranslatef(-x, -y, -z);
        }

        public void Render(Play g, int width, int height)
        {
            GL.glClearColor(.054f, .071f, .075f, 1);
            GL.glClear(16640);
            GL.glEnable(2929);
            GL.glDepthFunc(515);
            GL.glDisable(3042);
            GL.glEnable(3553);
            GL.glBindTexture(3553, atlas);
            float time = g.t;
            bool title = g.screen == Screen.Title || g.screen == Screen.Confirm || g.screen == Screen.Gallery || g.screen == Screen.Settings && g.back == Screen.Title;
            bool date = g.screen == Screen.Date, conversation = g.screen == Screen.Talk;
            float intensity = .86f + .11f * g.opts.light;
            if (title)
            {
                float t = (float)Math.Sin(time * .081) * .17f;
                Camera(-2.5f, 1.49f, .24f + t, .13f + t * .025f, .016f, width, height, 60);
                GL.Shade(.32f, intensity, .025f, .2f);
                GL.SceneLight(2,time);studio.Draw();GL.SceneLight(0);
            }
            else if(g.screen==Screen.Echo){
                int act=g.s.stage/5,who=AfterBell.Cast[act],set=AfterBell.Room[act];
                float sec=Math.Max(0,time-g.sceneStart),travel=Math.Min(1,sec*.40f);
                float ease=travel*travel*(3-2*travel);
                Camera(-.45f+ease*.13f,1.53f,1.42f+ease*.42f,.18f,-.11f,width,height,45);
                GL.Shade(.15f,intensity*.91f,.025f,set==1?1:0);GL.SceneLight(0);
                roomsFilm[set].Draw();
                int partner=(who+1)%3;
                people.Draw(partner,-1.27f+.66f*ease,4.55f+.45f*ease,2.64f,sec*4.2f,time,travel<1,0);
                people.Draw(who,.95f-.83f*ease,3.35f-.31f*ease,3.86f-.48f*ease,sec*5.3f,time,travel<1,travel<1?0:1);
            }
            else if(g.screen==Screen.Read && g.reader>=0 && g.reader<3)
            {
                Camera(0,0,0,0,0,width,height,43);
                GL.glLoadIdentity();
                GL.Shade(.12f,1.06f,.016f,0);GL.SceneLight(0);
                GL.glPushMatrix();
                GL.glTranslatef(g.inspectZoom?0:-.34f,.014f,g.inspectZoom?-1.22f:-1.05f);
                GL.glRotatef(-6,0,0,1);GL.glRotatef(g.inspectAngle,0,1,0);GL.glRotatef(g.inspectTilt,1,0,0);
                float magnification=g.inspectZoom?1.87f:1.25f;
                GL.glScalef(magnification,magnification,magnification);articles[g.reader].Draw();
                GL.glPopMatrix();
            }
            else if (date || conversation || g.screen == Screen.End)
            {
                bool ending = g.screen == Screen.End;
                int who = conversation ? g.talk : date ? (g.archiveView >= 0 ? g.archiveView : g.s.side) / 3 : Math.Max(0, Math.Min(2, g.endingView - 1));
                int id = date ? (g.archiveView >= 0 ? g.archiveView : g.s.side) : 0;
                float f = date ? (g.part == 1 ? .70f : 1.17f) : .79f;
                float off = (float)Math.Sin(time * .25) * .018f;
                if (conversation)
                {
                    Actor a = w.actors[who];
                    float headYaw = a.yaw;
                    V cam = new V(a.x + (float)Math.Sin(headYaw) * f, a.z + (float)Math.Cos(headYaw) * f);
                    if (!w.Open(w.Tile(cam)) || !w.Sight(a.Pos, cam))
                    {
                        cam = g.player;
                    }

                    float ang = (float)Math.Atan2(a.x - cam.X, a.z - cam.Z);
                    Camera(cam.X, 1.54f + w.Elevation(a.z), cam.Z, ang + .092f, -.157f, width, height, 44);
                    GL.Shade(.16f, intensity * .94f, .038f, 0);
                    GL.SceneLight(1,time,lightMap,w.W,w.H);
                    foreach (var c in chunks)
                    {
                        if (Math.Abs((c.x + 2.5f) * U - cam.X) < 33 && Math.Abs((c.z + 2.5f) * U - cam.Z) < 33)
                            c.mesh.Draw();
                    }

                    GL.SceneLight(0);
                    GL.glPushMatrix();
                    GL.glTranslatef(0, w.Elevation(a.z), 0);
                    people.Draw(who, a.x, a.z, ang + (float)Math.PI, 0, time, false, 1);
                    GL.glPopMatrix();
                }
                else
                {
                    int set = ending && (g.endingView == 0 || g.endingView == 4) ? 3 : who;
                    if (date && id == 5)
                        set = 3;
                    int beat = ending ? g.endingView : id % 3;
                    float faceX = .16f;
                    float sideAngle = g.part == 1 ? -.16f : .22f;
                    float broad = (set == 3 && ending ? 1.0f : 0f);
                    Camera(-.18f + off + sideAngle, 1.55f + g.part * .019f - (date && who == 2 ? .27f : 0), 3.23f - f - broad, .14f - sideAngle * .45f, -.145f, width, height, 46);
                    GL.Shade(.20f, intensity, .025f, set == 1 ? 1 : 0);
                    roomsFilm[set].Draw();
                    if (set < 3 || date)
                    {
                        if (date && id == 5)
                            At(umbrella, .47f, 1.99f, 3.36f);
                        people.Draw(who, faceX, 3.39f, 3.27f + sideAngle, 0, time, false, date?5+g.part:2+beat);
                    }
                    else if (g.endingView == 4)
                    {
                        for (int k = 0; k < 3; k++)
                            people.Draw(k, -1.3f + k * 1.16f, 4.00f + k * .38f, 3.18f, 0, time, false, 1);
                    }
                }
            }
            else
            {
                float lag = g.crouch ? .99f : 1.63f;
                float bob = (g.screen == Screen.Play && !g.hidden) ? (float)Math.Sin(g.bob) * .027f : 0;
                Camera(g.player.X, lag + bob + w.Elevation(g.player.Z), g.player.Z, g.yaw, g.pitch, width, height, g.run ? 72 : 67);
                GL.Shade(g.s.lit && !g.hidden ? .96f : 0, intensity * (g.nightFade > 0 && g.s.stage / 5 >= 2 ? 1 - .22f * (float)Math.Sin(g.nightFade * Math.PI / 5) : 1), g.opts.quality == 0 ? .050f : .036f, w.theme == 3 ? 1 : 0);
                GL.SceneLight(1,time,lightMap,w.W,w.H);
                float range = g.opts.quality == 0 ? 34 : 53;
                foreach (var c in chunks)
                {
                    float dx = (c.x + 2.5f) * U - g.player.X, dz = (c.z + 2.5f) * U - g.player.Z;
                    if (dx * dx + dz * dz < (range + 12) * (range + 12))
                        c.mesh.DrawLit(g.player.X, lag + w.Elevation(g.player.Z), g.player.Z, g.yaw, g.pitch, g.s.lit && !g.hidden, intensity);
                }

                GL.SceneLight(0);
                foreach (var obj in w.things)
                {
                    if (g.player.Dist(obj.p) > range)
                        continue;
                    if (obj.kind == Kind.Note && (g.s.notes & (1 << obj.tag)) != 0 || obj.kind == Kind.Archive && (g.s.proof & (1 << obj.tag)) != 0)
                        continue;
                    int i = (int)obj.kind;
                    if(obj.kind==Kind.Note){
                        At(props[0],obj.p.X,w.Elevation(obj.p.Z),obj.p.Z,obj.rot);
                        GL.glPushMatrix();
                        GL.glTranslatef(obj.p.X,w.Elevation(obj.p.Z)+.897f,obj.p.Z);
                        GL.glRotatef(11+obj.tag*8,0,1,0);GL.glRotatef(-77,1,0,0);GL.glScalef(.80f,.80f,.80f);
                        articles[obj.tag].Draw();GL.glPopMatrix();continue;
                    }
                    if (obj.kind == Kind.Station)
                    {
                        int ix = new[]
                        {
                            9,
                            10,
                            11,
                            12,
                            13,
                            12
                        }[Trial.Mode(w, obj.tag)];
                        At(props[ix], obj.p.X, w.Elevation(obj.p.Z), obj.p.Z);
                        if ((g.s.solved || g.Assignment==1) && g.s.fieldDone == obj.tag)
                            At(props[6], obj.p.X, w.Elevation(obj.p.Z) + 1.49f, obj.p.Z);
                        continue;
                    }

                    At(props[obj.kind==Kind.Board && (w.theme==0||w.theme==2||w.theme==9)?8:(i == 6 ? 7 : i)], obj.p.X, w.Elevation(obj.p.Z), obj.p.Z, obj.rot);
                    if (i == 0 || i == 5 || i == 1 && !g.s.solved)
                    {
                        float hh = i == 1 ? 1.65f : .99f;
                        At(props[6], obj.p.X, w.Elevation(obj.p.Z) + hh + (float)Math.Sin(time * 2.7 + obj.tag) * .03f, obj.p.Z);
                    }
                }

                if (GL.Shader)
                {
                    GL.glEnable(3042);
                    GL.glBlendFunc(770, 771);
                    GL.glDepthMask(0);
                    foreach(var c in chunks)
                    {
                        float dx=(c.x+2.5f)*U-g.player.X, dz=(c.z+2.5f)*U-g.player.Z;
                        if(c.contacts!=null && dx*dx+dz*dz<(range+12)*(range+12))c.contacts.Draw();
                    }
                    if(g.tracks.Active){
                        for(int j=Math.Max(0,g.tracks.played-7);j<g.tracks.played;j++){
                            var print=g.tracks.echo[j];
                            float shift=print.left?-.086f:.086f;
                            At(footprint,print.spot.X+shift*(float)Math.Cos(print.yaw),w.Elevation(print.spot.Z),print.spot.Z-shift*(float)Math.Sin(print.yaw),print.yaw*57.29578f);
                        }
                    }
                    foreach (var a in w.actors)
                        if (g.player.Dist(a.Pos) < range)
                            At(shadow, a.x, w.Elevation(a.z), a.z, a.yaw * 57.29578f);
                    GL.glDepthMask(1);
                    GL.glDisable(3042);
                }

                foreach (var a in w.actors)
                    if (g.player.Dist(a.Pos) < range && (a.who<0 || w.Sight(g.player,a.Pos)))
                    {
                        GL.glPushMatrix();
                        GL.glTranslatef(0, w.Elevation(a.z), 0);
                        people.Draw(a.who >= 0 ? a.who : a.who == -1 ? 3 : 4, a.x, a.z, a.yaw, a.step, time, a.walking);
                        GL.glPopMatrix();
                    }

                if (!g.hidden && g.screen == Screen.Play)
                {
                    GL.glClear(256);
                    GL.glLoadIdentity();
                    float sway = (float)Math.Sin(g.bob) * .005f;
                    GL.glTranslatef(.255f + sway, -.234f + bob * .19f, -.51f);
                    GL.glRotatef(16, 0, 1, 0);
                    GL.glRotatef(-80, 1, 0, 0);
                    GL.Shade(.08f, intensity, .008f, 0);
                    flash.Draw();people.FirstHand();
                }
            }

            GL.Ui();
        }

        public void Dispose()
        {
            foreach (var c in chunks)
            {
                c.mesh.Dispose();
                if(c.contacts!=null)c.contacts.Dispose();
            }
            foreach (var m in props)
                m.Dispose();
            if(lightMap!=0)GL.glDeleteTextures(1,ref lightMap);
            if(atlas!=0)GL.glDeleteTextures(1,ref atlas);
            stairNegative.Dispose();schoolGate.Dispose();classPortrait.Dispose();
            studio.Dispose();
            umbrella.Dispose();
            flash.Dispose();
            shadow.Dispose();footprint.Dispose();
            foreach (var m in roomsFilm)
                m.Dispose();
            foreach(var item in articles)if(item!=null)item.Dispose();
            people.Dispose();
        }
    }
}