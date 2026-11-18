using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Uncalled
{
    public static class QA
    {
        static int checks;
        static void Assert(bool yes, string msg)
        {
            checks++;
            if (!yes)
                throw new Exception(msg);
        }

        static void CheckPencil(Play game,V goal)
        {
            var line = game.w.GuideRoute(game.player, goal);
            Assert(line.Length > 0, "empty map route in sector " + game.s.stage);
            V last = game.player;
            foreach(V turn in line)
            {
                int samples = Math.Max(1,(int)Math.Ceiling(last.Dist(turn)/.08f));
                for(int k=1;k<=samples;k++)
                {
                    float t=k/(float)samples;
                    V point = new V(last.X+(turn.X-last.X)*t,last.Z+(turn.Z-last.Z)*t);
                    if(!game.w.Walk(point))
                        throw new Exception("map route crosses furniture in " + game.s.stage + " at " + point.X + "," + point.Z);
                }
                last=turn;
            }
            Assert(last.Dist(goal)<=1.73f && game.w.Sight(last,goal),"map route beyond reach");
        }

        static void CheckNotebook()
        {
            var g=new Play(true); g.Fresh(7921);
            V target; string name;
            for(int i=0;i<40;i++)
            {
                g.s.stage=i;g.s.side=-1;g.s.notes=0;g.s.solved=false;g.s.fieldDone=0;g.s.witnessed=false;g.Build();
                Assert(g.MapDestination(out target,out name) && name.Length>0,"first map goal");
                CheckPencil(g,target);
                g.s.notes=7;
                Assert(g.MapDestination(out target,out name),"board or switch");
                CheckPencil(g,target);
                g.s.solved=true;g.s.fieldDone=2;
                Assert(g.MapDestination(out target,out name),"exit on map");
                CheckPencil(g,target);
            }
            g.s.proof=255;g.s.dates=511;g.s.trustA=g.s.trustB=g.s.trustC=9;
            for(int i=0;i<5;i++){
                g.guideExit=i;
                Assert(g.MapDestination(out target,out name) && name==Play.EndDoor[i],"separate ending waypoint");
                CheckPencil(g,target);
            }
            for(int i=0;i<9;i++){
                g.s.stage=9+10*(i%3);g.s.side=i;g.s.notes=0;g.s.solved=false;g.s.fieldDone=0;g.Build();
                Assert(g.MapDestination(out target,out name),"keepsake location");CheckPencil(g,target);
                g.s.notes=1;
                Assert(g.MapDestination(out target,out name)&&target.Dist(g.w.actors[g.Witness].Pos)<.001f,"private meeting character");
                CheckPencil(g,target);
            }
            g.s.stage=3;g.s.side=-1;g.s.witnessed=false;g.s.solved=false;g.s.fieldDone=0;g.Build();g.s.notes=1<<g.Case.evidence;
            Assert(g.WitnessChoice && g.MapDestination(out target,out name) && target.Dist(g.w.actors[g.Witness].Pos)<.001f,"testimony waypoint");
            g.guideByRecords=true;
            Assert(g.MapDestination(out target,out name) && g.w.things.Any(o=>o.kind==Kind.Note && (g.s.notes&(1<<o.tag))==0 && o.p.Dist(target)<.001f),"documentary alternative");
            g.s.notes=7;
            Assert(g.MapDestination(out target,out name) && name==g.w.page.panel,"all documentary evidence returns to desk");
        }

        public static void Stats(string outfile)
        {
            var lines = new List<string>();
            Book.Init();
            CheckNotebook();
            Assert(Book.Main.Length == 40 && Book.Side.Length == 9, "script count");
            double all = 0, maxStage = 0;
            double[] metres = new double[40];
            int seeds = 8, reachable = 0;
            int[] variants =
            {
                -1,
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8
            };
            for (int sd = 0; sd < seeds; sd++)
            {
                for (int st = 0; st < 40; st++)
                {
                    var w = new World(70719 + sd, st, -1,true);
                    var objectives = w.things.Take(5).ToArray();
                    var s = w.Tile(w.start);
                    var ds = w.Distances(s);
                    Assert(w.things.All(o => w.Open(w.Tile(o.p)) && ds[w.Tile(o.p)] >= 0), "disconnected " + st);
                    var nav=new SchoolNav(w);var survey=nav.Survey(w.start);
                    Assert(w.things.All(o=>nav.CanApproach(survey,o.p)),"furniture-level access "+st+" seed "+sd);
                    reachable++;
                    double best = 1e9;
                    int[][] perms =
                    {
                        new[]
                        {
                            0,
                            1,
                            2
                        },
                        new[]
                        {
                            0,
                            2,
                            1
                        },
                        new[]
                        {
                            1,
                            0,
                            2
                        },
                        new[]
                        {
                            1,
                            2,
                            0
                        },
                        new[]
                        {
                            2,
                            0,
                            1
                        },
                        new[]
                        {
                            2,
                            1,
                            0
                        }
                    };
                    var points = objectives.Select(o => w.Tile(o.p)).ToArray();
                    int[][] tab = points.Select(v => w.Distances(v)).ToArray();
                    var stations = w.things.Where(o => o.kind == Kind.Station).Select(o => w.Tile(o.p)).ToArray();
                    int legs = tab[3][stations[0]] + w.Distances(stations[0])[stations[1]] + w.Distances(stations[1])[points[4]];
                    foreach (var p in perms)
                    {
                        int sum = ds[points[p[0]]] + tab[p[0]][points[p[1]]] + tab[p[1]][points[p[2]]] + tab[p[2]][points[3]] + legs;
                        best = Math.Min(best, sum * World.Unit);
                    }

                    metres[st] += best;
                    all += best;
                    maxStage = Math.Max(maxStage, best);
                }
            }

            lines.Add("Seeded map connectivity checked for objectives and exits.");
            foreach (int sub in variants.Skip(1))
            {
                var w = new World(1234, 9 + sub % 3 * 10, sub,true);
                int[] d = w.Distances(w.Tile(w.start));
                Assert(w.things.All(o => d[w.Tile(o.p)] >= 0), "side map");
            }

            foreach (int end in Enumerable.Range(0, 5))
            {
                var g = new Play(true);
                g.Fresh(19730 + end);
                g.To(Screen.Play);
                int n = 0;
                var acts = new HashSet<int>();
                var sides = new List<int>();
                while (g.screen != Screen.End && n++ < 50)
                {
                    g.To(Screen.Play);
                    int wanted = end == 0 ? 0 : end == 4 ? 2 : end - 1;
                    g.Talk(wanted);
                    g.ChooseTalk(0);
                    g.ChooseTalk(1);
                    g.To(Screen.Play);
                    foreach (var clue in g.w.things.Where(t => t.kind == Kind.Note))
                    {
                        g.Interact(clue);
                        g.To(Screen.Play);
                    }

                    foreach (var ar in g.w.things.Where(t => t.kind == Kind.Archive))
                        g.Interact(ar);
                    g.To(Screen.Play);
                    if(g.Assignment==5){
                        g.Talk(g.Witness);g.HearWitness();Assert(g.s.witnessed&&g.Ready,"keepsake");g.To(Screen.Play);
                    }else if(g.Assignment==2){
                        g.Interact(g.w.things.First(o=>o.kind==Kind.Board));
                        Assert(g.screen==Screen.Board,"analog board");
                        Solve(g.puzzle);g.Solve();
                    }else if(g.Assignment!=1){
                        if(g.Assignment==3 && end%2==0){g.Talk(g.Witness);g.HearWitness();g.To(Screen.Play);}
                        g.Interact(g.w.things.First(o=>o.kind==Kind.Board));
                        Assert(g.screen==Screen.Inference,"case opened");
                        int right=Enumerable.Range(0,3).First(i=>g.SlideOption(i)==0);
                        g.Interpret((right+1)%3);Assert(!g.s.solved&&g.mistaken>=0,"bad account blocked");
                        g.Interpret(right);g.Interpret((g.Case.evidence+1)%3);
                        Assert(g.reasoning==1&&!g.s.solved,"unrelated exhibit blocked");
                        g.Interpret(g.Case.evidence);Assert(g.reasoning==2&&g.Ready,"evidence accepted");g.To(Screen.Play);
                    }
                    if(g.Assignment==1||g.Assignment==2){
                        int total=g.Assignment==1?2:1;
                        for(int i=0;i<total;i++){
                            g.Interact(g.w.things.First(o=>o.kind==Kind.Station&&o.tag==i));
                            Assert(g.screen==Screen.Field,"relay reached"); WalkCheck.TrialOK(g);
                            Assert(g.screen==Screen.Play,"relay solved");
                        }
                    }
                    Assert(g.Ready,"exit enabled");
                    if (g.s.side >= 0)
                        sides.Add(g.s.side);
                    else
                        acts.Add(g.s.stage);
                    bool final = g.s.stage == 39;
                    var door = final && end > 0 ? g.w.things.First(z => z.kind == Kind.Final && z.tag == end) : g.w.things.First(z => z.kind == Kind.Door);
                    g.Interact(door);
                    if(g.screen==Screen.Echo){
                        int bit=1<<(g.s.stage/5);g.AnswerEcho(end%2);
                        Assert((g.s.wrote&bit)!=0,"chapter decision persists");
                    }
                    if (g.screen == Screen.Date)
                    {
                        g.DateNext();
                        g.DateNext();
                        g.DateNext();
                    }
                }

                Assert(g.screen == Screen.End && g.endingView == end && acts.Count == 40 && sides.Count == 3, "ending route " + end);
                lines.Add("route " + end + ": 40 main, " + string.Join(",", sides) + ", ending " + Book.EndingTitle[end]);
            }

            var legacy=new Play(true);legacy.Resume(new Saved{seed=70191,stage=6,notes=7,solved=true,fieldDone=1});
            Assert(!legacy.Revised&&legacy.w.W==new World(70191,6,-1).W,"legacy layout persisted");
            legacy.Interact(legacy.w.things.First(o=>o.kind==Kind.Station&&o.tag==1));
            Assert(legacy.screen==Screen.Field,"old save can finish its current relay");WalkCheck.TrialOK(legacy);
            legacy.Interact(legacy.w.things.First(o=>o.kind==Kind.Door));
            Assert(legacy.s.stage==7&&legacy.Revised,"old save migrates at next door");

            var sim = new Play(true);
            sim.Fresh(9);
            sim.To(Screen.Play);
            var initial = sim.player;
            for (int f = 0; f < 100; f++)
                sim.Tick(.016f, 1, 0, false, false);
            Assert(initial.Dist(sim.player) > 2, "W movement");
            var student = sim.w.actors[1];
            float travelled=0;bool crossed=false;
            for (int f=0;f<1800;f++){
                V beforeMove=student.Pos;sim.Tick(.016f,0,0,false,false);travelled+=beforeMove.Dist(student.Pos);
                if(!sim.w.Walk(student.Pos))crossed=true;
            }
            Assert(travelled>3 && !crossed,"student can patrol, take shelter and return without crossing desks");
            var obj = sim.w.things.First(o => o.kind == Kind.Note);
            sim.player = new V(obj.p.X, obj.p.Z - 1.35f);
            sim.yaw = 0;
            sim.E();
            Assert(sim.screen == Screen.Read, "player E collects clue");
            sim.To(Screen.Play);
            var hide = sim.w.things.First(o => o.kind == Kind.Hide);
            sim.Interact(hide);
            Assert(sim.hidden, "hide");
            sim.E();
            Assert(!sim.hidden, "unhide");
            int before = sim.s.lures;
            sim.Toss();
            Assert(sim.s.lures == before - 1 && sim.decoyTime > 0, "lure");
            var enemy = sim.w.actors.Last();
            sim.safeTime = 0;
            enemy.grace = 0;
            enemy.mode = 2;
            enemy.alert = 1;
            enemy.x = sim.player.X;
            enemy.z = sim.player.Z + .5f;
            enemy.yaw = (float)Math.PI;
            sim.Tick(.016f, 0, 0, false, false);
            Assert(sim.screen == Screen.Caught, "catch");
            sim.Recover();
            Assert(sim.screen == Screen.Play && sim.s.notes != 0, "checkpoint");
            for (int k = 0; k < 8; k++)
            {
                var ww = new World(955 + k, 12, -1);
                V basePos = ww.start;
                bool hit = false;
                for (int dir = 0; dir < 4 && !hit; dir++)
                {
                    V q = basePos;
                    float dx = dir == 0 ? .17f : dir == 2 ? -.17f : 0, dz = dir == 1 ? .17f : dir == 3 ? -.17f : 0;
                    for (int z = 0; z < 220; z++)
                    {
                        V t = ww.Slide(q, dx, dz);
                        if (q.Dist(t) < .001)
                        {
                            hit = true;
                            break;
                        }

                        q = t;
                    }
                }

                Assert(hit, "wall collision");
            }

            var echoTest=new Footprint();
            for(int step=0;step<16;step++)echoTest.Add(new V(step+.5f,5),.7f);
            Assert(echoTest.Ring()&&echoTest.total==12,"footprints snapshot a finite reversed route");
            Assert(echoTest.echo[0].spot.X>echoTest.echo[11].spot.X,"footsteps lead away from the player");
            var score=new Score();
            for(int scene=0;scene<8;scene++){
                double energy=0;float max=0;
                for(int j=0;j<27000;j++){
                    if(j%768==0)score.Set(scene);
                    float l=0,r=0;score.Sample(ref l,ref r);energy+=l*l+r*r;max=Math.Max(max,Math.Abs(l)+Math.Abs(r));
                }Assert(energy>0.01&&!double.IsNaN(energy)&&max<1.6,"score cue "+scene);
            }
            var witnessTest=new Play(true);witnessTest.Fresh(17);witnessTest.s.stage=3;witnessTest.Build();witnessTest.To(Screen.Play);
            var desk=witnessTest.w.things.First(o=>o.kind==Kind.Board);witnessTest.s.notes=1<<witnessTest.Case.evidence;
            witnessTest.Interact(desk);Assert(witnessTest.screen==Screen.Play,"uncorroborated witness route blocked");
            witnessTest.Talk(witnessTest.Witness);witnessTest.HearWitness();witnessTest.To(Screen.Play);witnessTest.Interact(desk);
            Assert(witnessTest.screen==Screen.Inference,"spoken witness substitutes for two more finds");
            string original = Disk.Dir;
            Disk.Dir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outfile)), "save-fixture-" + Guid.NewGuid().ToString("N"));
            try
            {
                var keeper = new Play(false);
                keeper.Fresh(70719);
                keeper.s.trustB = 4;
                keeper.s.pledge = 1;
                keeper.s.answers = 8;
                keeper.s.fieldDone = 1;
                keeper.s.notes = 7;
                keeper.s.proof = 17;keeper.s.wrote=3;keeper.s.privateRecord=2;keeper.s.witnessed=true;
                keeper.s.solved = true;
                keeper.Save();
                var loaded = Disk.Load();
                Assert(loaded != null && loaded.trustB == 4 && loaded.solved && loaded.fieldDone == 1 && loaded.pledge == 1 && loaded.answers == 8, "save roundtrip");
                Assert(loaded.edition==2&&loaded.witnessed&&loaded.privateRecord==2&&loaded.wrote==3,"testimony persistence");
                var newer = new Play(false);
                newer.Resume(loaded);
                Assert(newer.s.seed == 70719 && newer.w.seed == keeper.w.seed && newer.w.seen.SequenceEqual(keeper.w.seen), "deterministic resume plus exploration");
                newer.Save();
                File.WriteAllText(Path.Combine(Disk.Dir, "night.sav"), "{");
                Assert(Disk.Load().proof == 17, "corruption fallback");
                newer.opts.preferences = 4;
                newer.opts.memories = 8;
                newer.SaveOpts();
                Assert(Disk.Read<Settings>("options.json").preferences == 4, "gallery retention");
            }
            finally
            {
                Disk.Dir = original;
            }

            lines.Add("PASS " + checks + " assertions; " + reachable + " seeded main maps and 9 side maps.");
            File.WriteAllLines(outfile, lines.ToArray(), System.Text.Encoding.UTF8);
        }

        public static void Solve(Puzzle p)
        {
            if (p.game == 1)
            {
                for (int b = 0; b < 8; b++)
                {
                    p.Clear();
                    for (int c = 0; c < 3; c++)
                        if ((b & (1 << c)) != 0)
                            p.Hit(c, 1);
                    if (p.Correct())
                        return;
                }
            }
            else if (p.Sequence)
            {
                for (int c = 0; c < 3; c++)
                    p.Hit(p.a[c], 1);
            }
            else
            {
                for (int c = 0; c < 3; c++)
                    for (int i = 0; i < p.a[c]; i++)
                        p.Hit(c, 1);
            }
        }
    }
}