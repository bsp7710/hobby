using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Uncalled
{
    public class WalkCheck
    {
        const int Sub = 5;
        float unit = World.Unit / Sub;
        Play g;
        int width, height;
        bool[] open;
        int[] queue, from, mark;
        int frame;
        double distance;
        int contacts, legs;
        WalkCheck(Play game)
        {
            g = game;
        }

        V Position(int n)
        {
            return new V((n % width + .5f) * unit, (n / width + .5f) * unit);
        }

        int Index(V v)
        {
            return (int)(v.Z / unit) * width + (int)(v.X / unit);
        }

        void Map()
        {
            width = g.w.W * Sub;
            height = g.w.H * Sub;
            int size = width * height;
            open = new bool[size];
            queue = new int[size];
            from = new int[size];
            mark = new int[size];
            frame = 0;
            for (int n = 0; n < size; n++)
                open[n] = g.w.Walk(Position(n));
        }

        bool CanUse(V p, Thing o)
        {
            float d = p.Dist(o.p);
            if (d < .77f || d > 1.78f || !g.w.Sight(p, o.p))
                return false;
            V prev = g.player;
            float yaw = g.yaw;
            g.player = p;
            g.yaw = (float)Math.Atan2(o.p.X - p.X, o.p.Z - p.Z);
            g.Focus();
            bool ok = g.target == o;
            g.player = prev;
            g.yaw = yaw;
            return ok;
        }

        void Travel(Thing o)
        {
            legs++;
            frame++;
            int a = Index(g.player);
            if (!open[a])
                throw new Exception("blocked start " + g.s.stage);
            int h = 0, e = 0;
            queue[e++] = a;
            mark[a] = frame;
            int end = -1;
            int[] dirs =
            {
                1,
                -1,
                width,
                -width
            };
            while (h < e)
            {
                int n = queue[h++];
                V pos = Position(n);
                if (CanUse(pos, o))
                {
                    end = n;
                    break;
                }

                foreach (int dir in dirs)
                {
                    int b = n + dir;
                    if (b < 0 || b >= open.Length || !open[b] || mark[b] == frame)
                        continue;
                    mark[b] = frame;
                    from[b] = n;
                    queue[e++] = b;
                }
            }

            if (end < 0)
                throw new Exception("no authentic interaction pose " + g.s.stage + " / " + g.s.side + " " + o.kind + " " + o.tag);
            var path = new List<int>();
            while (end != a)
            {
                path.Add(end);
                end = from[end];
            }

            path.Reverse();
            foreach (int node in path)
            {
                V dest = Position(node);
                float delta = g.player.Dist(dest);
                if (delta < .01f)
                    continue;
                float dx = (dest.X - g.player.X), dz = (dest.Z - g.player.Z);
                int ticks = (int)Math.Ceiling(delta / .14);
                for (int f = 0; f < ticks; f++)
                {
                    V last = g.player;
                    g.player = g.w.Slide(g.player, dx / ticks, dz / ticks);
                    distance += g.player.Dist(last);
                }

                if (g.player.Dist(dest) > .10f)
                    throw new Exception("collision along path at " + g.s.stage + " " + node);
            }

            g.yaw = (float)Math.Atan2(o.p.X - g.player.X, o.p.Z - g.player.Z);
            g.E();
            contacts++;
        }

        public static void TrialOK(Play g)
        {
            var p = g.trial;
            switch (p.mode)
            {
                case 0:
                case 5:
                    for (int i = 0; i < p.desired; i++)
                        p.Hit(1);
                    break;
                case 1:
                    p.Hit(0);
                    p.Tick((.35f + p.desired * .024f) / .21f);
                    p.Hit(0);
                    break;
                case 2:
                    foreach (int k in p.keys)
                        p.Hit(k);
                    break;
                case 3:
                    for (int i = 0; i < 3; i++)
                        p.Hit(i);
                    break;
                case 4:
                    for (int i = 0; i < p.orientation; i++)
                        p.Hit(1);
                    break;
            }

            g.FieldConfirm();
        }

        public static void Run(string filename)
        {
            var g = new Play(true);
            g.Fresh(20021019);
            var sim = new WalkCheck(g);
            int count = 0;
            for (int i = 0; i < 40; i++)
            {
                if (g.s.stage != i)
                    throw new Exception("wrong active sector");
                g.To(Screen.Play);
                sim.Map();
                foreach (var n in g.w.things.Where(o => o.kind == Kind.Note).OrderBy(o => o.tag))
                {
                    sim.Travel(n);
                    if (g.screen != Screen.Read)
                        throw new Exception("missed note");
                    g.To(Screen.Play);
                }

                if(g.Assignment!=1){
                    sim.Travel(g.w.things.First(o => o.kind == Kind.Board));
                    if(g.Assignment==2){if(g.screen!=Screen.Board)throw new Exception("analog board");QA.Solve(g.puzzle);g.Solve();}
                    else{if(g.screen!=Screen.Inference)throw new Exception("case desk");g.Interpret(Enumerable.Range(0,3).First(j=>g.SlideOption(j)==0));g.Interpret(g.Case.evidence);g.To(Screen.Play);}
                }
                for(int j=0;j<(g.Assignment==1?2:g.Assignment==2?1:0);j++){
                    sim.Travel(g.w.things.First(o=>o.kind==Kind.Station&&o.tag==j));
                    if(g.screen!=Screen.Field)throw new Exception("missed field");TrialOK(g);
                }
                sim.Travel(g.w.things.First(o => o.kind == Kind.Door));
                if(g.screen==Screen.Echo)g.AnswerEcho(i%2);
                count++;
            }

            if (g.screen != Screen.End || g.endingView != 0)
                throw new Exception("reference failed to escape");
            File.WriteAllText(filename,string.Format("PASS. Seed 20021019. {0} sectors. {1} interactable placements reached using the player collision and E targeting. Travelled {2:0} m. This test checks accessibility only.",count,sim.contacts,sim.distance),System.Text.Encoding.UTF8);
        }
    }
}