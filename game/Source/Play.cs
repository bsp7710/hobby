using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Uncalled
{
    public class ActorMemo
    {
        public int who, home, other, target, mode;
        public float x, z, yaw, wait, step, alert, hear, grace;
    }

    public class Saved
    {
        public ActorMemo[] cast;
        public float safeX, safeZ;
        public bool inLocker;
        public int edition;
        public bool witnessed;
        public int wrote,privateRecord;
        public int fieldDone, answers, answered;
        public int version = 1, seed, stage, side = -1, notes, proof, visit, trustA, trustB, trustC, dates, pledge = -1, lures = 3, ending = -1, caught, usedBoxes;
        public bool solved, lit = true;
        public float x, z, angle, angleY, stamina = 1;
        public string explored = "";
        public double seconds;
        public int Trust(int i)
        {
            return i == 0 ? trustA : i == 1 ? trustB : trustC;
        }

        public void Like(int i)
        {
            if (i == 0)
                trustA++;
            else if (i == 1)
                trustB++;
            else
                trustC++;
        }
    }

    public class Settings
    {
        public int light = 2, volume = 3, sens = 2, quality = 1;
        public bool hints = true;
        public int endings, memories, preferences;
        public int[] exitMemory;
    }

    public static class Disk
    {
        public static string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Eunram", "TheUncalled");
        static object gate = new object ();
        public static void Put<T>(string f, T val)
        {
            lock (gate)
            {
                Directory.CreateDirectory(Dir);
                string p = Path.Combine(Dir, f), tmp = p + ".new";
                File.WriteAllText(tmp, new JavaScriptSerializer().Serialize(val), System.Text.Encoding.UTF8);
                if (File.Exists(p))
                    File.Replace(tmp, p, p + ".previous", true);
                else
                    File.Move(tmp, p);
            }
        }

        public static T Read<T>(string f)
            where T : class
        {
            foreach (string suffix in new[]
            {
                "",
                ".previous"
            }

            )
                try
                {
                    string p = Path.Combine(Dir, f) + suffix;
                    if (File.Exists(p))
                    {
                        var v = new JavaScriptSerializer().Deserialize<T>(File.ReadAllText(p));
                        if (v != null)
                            return v;
                    }
                }
                catch
                {
                }

            return null;
        }

        public static bool Valid(Saved s)
        {
            return s != null && s.version == 1 && s.stage >= 0 && s.stage < 40 && s.side >= -1 && s.side < 9 && s.notes >= 0 && s.notes < 8 && s.ending >= -1 && s.ending <= 4;
        }

        public static Saved Load()
        {
            var v = Read<Saved>("night.sav");
            if (Valid(v))
                return v;
            v = Read<Saved>("night.sav.previous");
            return Valid(v) ? v : null;
        }
    }

    public class Puzzle
    {
        public int game, sel, bits, goal, count;
        public int[] input = new int[3], a = new int[3], rad = new int[3];
        public string[] pieces;
        public bool error;
        public static readonly string[] tones =
        {
            "도",
            "레",
            "미",
            "솔",
            "라"
        }, wind =
        {
            "위",
            "오른쪽",
            "아래",
            "왼쪽"
        }, marks =
        {
            "원",
            "별",
            "달",
            "잎",
            "종이배",
            "새"
        };
        public Puzzle(World w)
        {
            game = w.page.game;
            var rng = new Random(unchecked(w.seed * 31 + 17));
            for (int i = 0; i < 3; i++)
            {
                rad[i] = game == 3 ? 4 : game == 2 ? 10 : game == 4 ? (i == 0 ? 12 : i == 1 ? 6 : 10) : game == 6 ? 5 : 6;
                a[i] = rng.Next(rad[i]);
            }

            if (game == 5)
            {
                int[] order =
                {
                    0,
                    1,
                    2
                };
                for (int i = 2; i > 0; i--)
                {
                    int j = rng.Next(i + 1), v = order[i];
                    order[i] = order[j];
                    order[j] = v;
                }

                a = order;
                rad = new[]
                {
                    3,
                    3,
                    3
                };
            }

            pieces = w.page.items;
            goal = rng.Next(1, 8);
            bits = goal ^ 7;
        }

        public string Rules
        {
            get
            {
                return new[]
                {
                    "각 기록 여백의 인장을 첫째부터 차례로 눌러 주세요.",
                    "세 기록에 표시된 램프 상태로 회로를 맞추세요. 왼쪽·가운데 스위치는 바로 오른쪽 램프도 함께 바꿉니다.",
                    "세 자리 주파수를 맞춰 수신기를 고정하세요.",
                    "세 유리판의 화살표가 기록의 방향을 가리키게 돌리세요.",
                    "기록의 시·십 분·일 분을 맞추고 태엽을 놓으세요.",
                    "세 조각을 기록에 적힌 재생 차례대로 이어 붙이세요.",
                    "세 기록의 계이름을 차례대로 연주하세요."
                }[game];
            }
        }

        public string Hint(int i)
        {
            string ord = new[]
            {
                "첫째",
                "둘째",
                "셋째"
            }[i];
            if (game == 1)
                return ord + " 램프  ·  " + (((goal >> i) & 1) > 0 ? "점등" : "소등");
            if (game == 5)
                return "이 조각의 재생 차례  ·  " + (Array.IndexOf(a, i) + 1) + "번째";
            return ord + " 표식  ·  " + Value(i, a[i]);
        }

        public string Value(int i, int val)
        {
            if (game == 0)
                return marks[val];
            if (game == 3)
                return wind[val];
            if (game == 4)
                return i == 0 ? (val + 1) + "시" : (i == 1 ? val * 10 + "분" : val + "분");
            if (game == 5)
                return pieces[val];
            if (game == 6)
                return tones[val];
            return val.ToString();
        }

        public bool Sequence
        {
            get
            {
                return game == 0 || game == 5 || game == 6;
            }
        }

        public int Buttons
        {
            get
            {
                return Sequence ? rad[0] : 3;
            }
        }

        public string Button(int i)
        {
            if (Sequence)
                return Value(0, i);
            if (game == 1)
                return ((bits & (1 << i)) != 0 ? "●  켜짐" : "○  꺼짐");
            return Value(i, input[i]);
        }

        public void Hit(int i, int direction)
        {
            error = false;
            if (i < 0 || i >= Buttons)
                return;
            if (Sequence)
            {
                if (count == 3)
                    count = 0;
                input[count++] = i;
            }
            else if (game == 1)
            {
                bits ^= new[]
                {
                    3,
                    6,
                    4
                }[i];
            }
            else
                input[i] = (input[i] + direction + rad[i]) % rad[i];
        }

        public bool Correct()
        {
            bool ok = game == 1 ? bits == goal : (!Sequence || count == 3) && input.SequenceEqual(a);
            error = !ok;
            return ok;
        }

        public void Clear()
        {
            input = new int[3];
            count = 0;
            bits = goal ^ 7;
            error = false;
        }
    }

    public enum Screen
    {
        Title,
        Play,
        Intro,
        Read,
        Talk,
        Board,
        Book,
        Pause,
        Settings,
        Gallery,
        Confirm,
        Caught,
        Date,
        End,
        Field,
        Inference,
        Echo
    }

    public partial class Play
    {
        public Saved s;
        public Settings opts;
        public Trial trial;
        public World w;
        public Puzzle puzzle;
        public Screen screen = Screen.Title, back = Screen.Title;
        public Action<string, float, float> Tone;
        public Action OnWorld, OnSave;
        public bool test, hidden, run, crouch;
        public V player, lastSafe;
        public float yaw, pitch, noise, listen, listenCd, heat, foot, bob, t, nearDist, safeTime, toastTime, saveTimer, glow, linger, timeSinceStep;
        public string toast = "", prompt = "", speaker = "", text = "", dialogTitle = "";
        public float inspectAngle=-12,inspectTilt=5;
        public int focus = -1, talk = -1, reader = -1, part, bookTab, archiveView = -1, endingView, settingRow, confirm, menuChoice, titleFocus;
        public readonly Footprint tracks=new Footprint();
        public V decoy;
        public float decoyTime, nightBeat, nightFade;
        public Thing target;
        public Actor person;
        public int revision;
        public Play(bool testMode)
        {
            test = testMode;
            opts = Disk.Read<Settings>("options.json") ?? new Settings();
        }

        public void Sound(string name, float vol = 1, float pan = 0)
        {
            if (Tone != null)
                Tone(name, vol, pan);
        }

        public void Msg(string msg)
        {
            toast = msg;
            toastTime = 3.6f;
            revision++;
        }

        public void To(Screen next)
        {
            screen = next;
            menuChoice = 0;
            revision++;
        }

        public void Settle()
        {
            Build();
            To(Screen.Title);
        }

        public void Fresh(int seed)
        {
            s = new Saved
            {
                seed = seed, edition=2
            };
            Build();
            Save();
            To(Screen.Intro);
            Sound("bell");
        }

        public void Resume(Saved value)
        {
            s = value;
            Build();
            if (s.ending >= 0)
            {
                s.ending = -1;
                archiveView = -1;
                To(Screen.Play);
                Msg("졸업 복도로 돌아왔습니다. 열려 있는 다른 출구를 선택할 수 있습니다.");
            }
            else if (s.side >= 0 && (s.dates & (1 << s.side)) != 0)
            {
                part = 0;
                talk = s.side / 3;
                archiveView = -1;
                To(Screen.Date);
            }
            else
            {
                archiveView = -1;
                To(Screen.Play);
                Msg("저장한 지점에서 이어갑니다.");
            }
        }

        public void Build()
        {
            if (s == null)
                s = new Saved
                {
                    seed = 20021019, edition=2
                };
            w = new World(s.seed, s.stage, s.side, Revised);
            puzzle = new Puzzle(w);
            hidden = false;
            tracks.Clear();
            inspectZoom = false;
            reviewing = false;
            guideByRecords = false;
            guideExit = 0;
            nightBeat = 28 + s.stage % 6;
            heat = noise = decoyTime = listen = glow = foot = bob = nightFade = 0;
            safeTime = 6;
            target = null;
            person = null;
            nearDist = 100;
            prompt = "";
            talk = -1;
            saveTimer = 0;
            lastSafe = s.safeX > 0 && w.Walk(new V(s.safeX, s.safeZ)) ? new V(s.safeX, s.safeZ) : w.start;
            hidden = s.inLocker;
            if (s.cast != null && s.cast.Length == w.actors.Count)
            {
                for (int i = 0; i < w.actors.Count; i++)
                {
                    var a = w.actors[i];
                    var m = s.cast[i];
                    if (m == null || m.who != a.who || !w.Open(w.Tile(new V(m.x, m.z))))
                        continue;
                    a.x = m.x;
                    a.z = m.z;
                    a.yaw = m.yaw;
                    a.step = m.step;
                    a.last = (int)(a.step / 5.2f);
                    a.mode = m.mode;
                    a.wait = m.wait;
                    a.alert = m.alert;
                    a.hear = m.hear;
                    a.grace = Math.Max(3, m.grace);
                    if (w.Open(m.target))
                        w.Go(a, m.target);
                }
            }

            player = s.x > 0 && w.Walk(new V(s.x, s.z)) ? new V(s.x, s.z) : w.start;
            yaw = s.x > 0 ? s.angle : w.yaw;
            pitch = s.x > 0 ? s.angleY : 0;
            s.stamina = Math.Max(.35f, Math.Min(1, s.stamina));
            try
            {
                byte[] a = Convert.FromBase64String(s.explored ?? "");
                for (int i = 0; i < w.seen.Length && i / 8 < a.Length; i++)
                    w.seen[i] = (a[i / 8] & (1 << (i & 7))) != 0;
            }
            catch
            {
            }

            w.Explore(player);
            if (OnWorld != null)
                OnWorld();
            revision++;
        }

        public void Save()
        {
            if (test || s == null)
                return;
            s.safeX = lastSafe.X;
            s.safeZ = lastSafe.Z;
            s.inLocker = hidden;
            s.cast = w.actors.Select(a => new ActorMemo { who = a.who, home = a.home, other = a.other, x = a.x, z = a.z, yaw = a.yaw, wait = a.wait, step = a.step, target = a.target, mode = a.mode, alert = a.alert, hear = a.hear, grace = a.grace }).ToArray();
            s.x = player.X;
            s.z = player.Z;
            s.angle = yaw;
            s.angleY = pitch;
            byte[] bits = new byte[(w.seen.Length + 7) / 8];
            for (int i = 0; i < w.seen.Length; i++)
                if (w.seen[i])
                    bits[i / 8] |= (byte)(1 << (i & 7));
            s.explored = Convert.ToBase64String(bits);
            try
            {
                Disk.Put("night.sav", s);
                if (OnSave != null)
                    OnSave();
            }
            catch (IOException)
            {
                Msg("저장 공간을 확인해 주세요. 이번 저장을 마치지 못했습니다.");
            }
            catch (UnauthorizedAccessException)
            {
                Msg("저장 폴더에 쓰기 권한이 필요합니다.");
            }
        }

        public void SaveOpts()
        {
            if (!test)
                try
                {
                    Disk.Put("options.json", opts);
                }
                catch (IOException)
                {
                    Msg("환경 설정을 저장할 수 없습니다.");
                }
        }

        public void Next()
        {
            s.cast = null;
            s.edition=2; s.witnessed=false;
            s.safeX = s.safeZ = 0;
            s.inLocker = false;
            s.x = s.z = 0;
            s.explored = "";
            s.notes = s.visit = s.usedBoxes = s.fieldDone = 0;
            s.solved = false;
            s.lures = Math.Max(s.lures, 2);
            s.lit = true;
            s.stamina = 1;
            Build();
            if(s.side<0 && s.stage>0 && s.stage%5==0 && (s.wrote&(1<<(s.stage/5-1)))!=0){
                int previous=1<<(s.stage/5-1);
                if((s.privateRecord&previous)==0){
                    foreach(var patrol in w.actors)if(patrol.who<0){w.Go(patrol,w.hubs[w.reqRooms[4]]);patrol.wait=0;patrol.mode=1;}
                }else{
                    foreach(var patrol in w.actors)if(patrol.who<0)patrol.grace=Math.Max(22,patrol.grace);
                }
            }
            Save();
            To(Screen.Intro);
            Sound("bell");
        }

        public void Tick(float dt, float forward, float strafe, bool sprint, bool quiet)
        {
            t += dt;
            if (s != null && archiveView < 0 && (screen == Screen.Echo || screen == Screen.Inference || screen == Screen.Intro || screen == Screen.Read || screen == Screen.Board || screen == Screen.Field || screen == Screen.Book || screen == Screen.Talk || screen == Screen.Date))
                s.seconds += dt;
            toastTime = Math.Max(0, toastTime - dt);
            if (screen == Screen.Field && trial != null)
            {
                trial.Tick(dt);
                return;
            }

            if (screen != Screen.Play)
                return;
            s.seconds += dt;
            safeTime -= dt;
            listen = Math.Max(0, listen - dt);
            listenCd = Math.Max(0, listenCd - dt);
            decoyTime -= dt;
            saveTimer += dt;
            glow = Math.Max(0, glow - dt);
            linger = Math.Max(0, linger - dt);
            tracks.Tick(this,dt);
            nightBeat -= dt;
            nightFade = Math.Max(0, nightFade - dt);
            if (nightBeat <= 0)
            {
                nightBeat = 48 + (s.stage * 7) % 19;
                int ch = s.stage / 5;
                nightFade = 5;
                if(Revised && (ch==2||ch==4||ch==6) && tracks.Ring())Msg("지나온 길에서 내 발소리가 거꾸로 울립니다.");
                if (ch == 0 || ch == 7)
                    Sound("bell", .26f);
                else if (ch == 3 || ch == 6)
                    Sound("note" + ((s.stage + 2) % 5), .32f, (float)Math.Sin(t));
                else
                    Sound(ch == 4 ? "keys" : "stamp", .32f, (float)Math.Cos(t));
                if (s.solved || s.notes != 0)
                {
                    int n = w.Tile(player);
                    foreach (var a in w.actors)
                        if (a.who < 0 && a.mode != 2 && a.Pos.Dist(player) < 51)
                        {
                            Room r = w.rooms[(Math.Max(0, w.roomAt[n]) + s.stage % 3) % w.rooms.Count];
                            w.Go(a, w.hubs[r.n]);
                            a.wait = 0;
                        }
                }
            }

            crouch = quiet && !hidden;
            bool moving = !hidden && (forward != 0 || strafe != 0);
            if (s.stamina < .045f)
                timeSinceStep = 1.4f;
            timeSinceStep = Math.Max(0, timeSinceStep - dt);
            run = moving && sprint && !crouch && timeSinceStep <= 0;
            float speed = run ? 4.15f : crouch ? 1.45f : 2.55f;
            float len = (float)Math.Sqrt(forward * forward + strafe * strafe);
            if (len > 1)
            {
                forward /= len;
                strafe /= len;
            }

            if (moving)
            {
                float si = (float)Math.Sin(yaw), co = (float)Math.Cos(yaw);
                var last = player;
                player = w.Slide(player, (si * forward + co * strafe) * speed * dt, (co * forward - si * strafe) * speed * dt);
                float dist = last.Dist(player);
                foot += dist;
                bob += dist * (run ? 3.1f : 3.6f);
                if (foot >= (crouch ? 1.35f : run ? 1.65f : 1.48f))
                {
                    foot = 0;
                    if(Revised)tracks.Add(player,(float)Math.Atan2(player.X-last.X,player.Z-last.Z));
                    int floorTile=w.Tile(player);bool hall=w.Open(floorTile)&&w.roomAt[floorTile]<0;
                    Sound(crouch?"cloth":hall?"hallstep":w.theme==5||w.theme==7||w.theme==11?"woodstep":"step",run?.67f:crouch?.21f:.37f);
                    noise = run ? 1 : crouch ? .1f : .43f;
                }

                w.Explore(player);
            }

            s.stamina = Math.Max(0, Math.Min(1, s.stamina + (run ? -.106f : .067f) * dt));
            noise = Math.Max(0, noise - dt * .68f);
            heat = 0;
            nearDist = 999;
            foreach (var a in w.actors)
            {
                if (a.who >= 0)
                    Student(a, dt);
                else
                    Enemy(a, dt);
                if (screen != Screen.Play)
                    return;
            }

            Focus();
            if (saveTimer > 24)
            {
                saveTimer = 0;
                Save();
            }
        }

        void Student(Actor a, float dt)
        {
            if(Revised)
            {
                a.cool-=dt;
                if(a.cool<=0 && a.mode!=3)
                {
                    a.cool=.65f+a.who*.09f;
                    foreach(var patrol in w.actors)
                    {
                        if(patrol.who>=0 || a.Pos.Dist(patrol.Pos)>9 || !w.Sight(a.Pos,patrol.Pos))continue;
                        int shelter=a.Pos.Dist(w.Center(w.Waypoint(a.home)))<a.Pos.Dist(w.Center(w.Waypoint(a.other)))?a.home:a.other;
                        if(w.Center(w.Waypoint(shelter)).Dist(patrol.Pos)<7)shelter=shelter==a.home?a.other:a.home;
                        w.Go(a,w.Waypoint(shelter));a.mode=3;a.hear=5.4f;a.wait=0;break;
                    }
                }
                if(a.mode==3){
                    a.hear-=dt;
                    bool arrived=w.Move(a,dt,2.0f);
                    if(arrived || a.hear<=0){a.mode=0;a.wait=1.2f;a.cool=1.9f;}
                    else return;
                }
            }
            if (a.Pos.Dist(player) < 2.9f && !hidden && w.Sight(a.Pos, player))
            {
                a.walking = false;
                a.yaw = (float)Math.Atan2(player.X - a.x, player.Z - a.z);
                return;
            }

            if (a.wait > 0)
            {
                a.wait -= dt;
                a.walking = false;
                return;
            }

            if (w.Move(a, dt, .96f))
            {
                int r = a.mode == 0 ? a.other : a.home;
                a.mode = 1 - a.mode;
                w.Go(a, w.Waypoint(r));
                a.wait = 5 + (a.who + 1) * 2;
            }
        }

        void Enemy(Actor a, float dt)
        {
            float dist = a.Pos.Dist(player);
            nearDist = Math.Min(nearDist, dist);
            a.cool -= dt;
            a.hear -= dt;
            a.grace -= dt;
            bool sight = false, heard = false;
            if (!hidden && safeTime <= 0 && a.grace <= 0 && dist < (a.who == -2 ? 17 : 22))
            {
                float dot = ((player.X - a.x) * (float)Math.Sin(a.yaw) + (player.Z - a.z) * (float)Math.Cos(a.yaw)) / Math.Max(.01f, dist);
                sight = dist < (crouch ? 2.2f : 3.2f) || ((dot > (a.mode == 2 ? -.2f : .38f)) && dist < (s.lit ? (crouch ? 9 : 17) : (crouch ? 3.3f : 7.4f)));
                if(Revised && a.who==-2){sight=dist<2.1f || (dot>.55f && s.lit && dist<(crouch?3.3f:7.8f));}
                sight = sight && w.Sight(a.Pos, player);
                heard = noise > .12f && dist < (noise * 15 + 1) * (a.who == -2 ? 1.38f : 1) && (w.Sight(a.Pos, player) || dist < noise * (a.who == -2 ? 11 : 7));
            }

            if (sight || heard)
            {
                a.alert = Math.Min(1.2f, a.alert + dt * (sight ? (dist < 5 ? 1.4f : .63f) : .43f));
                a.hear = 4.5f;
                if (a.alert > .62f && a.mode != 2)
                {
                    a.mode = 2;
                    Sound("alarm", .65f);
                }

                if (a.cool <= 0)
                {
                    w.Go(a,player);
                    a.cool = .55f;
                }
            }
            else
            {
                a.alert = Math.Max(0, a.alert - dt * (hidden ? .32f : .13f));
                if (a.hear <= 0 && a.alert < .16f)
                    a.mode = 0;
            }

            heat = Math.Max(heat, Math.Min(1, a.alert));
            if (hidden && a.mode == 2)
                a.hear = Math.Min(a.hear, 1.1f);
            if (decoyTime > 0 && a.mode != 2 && a.Pos.Dist(decoy) < 25 && a.cool <= 0)
            {
                w.Go(a,decoy);
                a.mode = 1;
                a.wait = 0;
                a.cool = 2.4f;
            }

            if (a.mode == 2 && safeTime <= 0 && !hidden && dist < .79f && sight)
            {
                s.caught++;
                Sound("catch", .85f);
                To(Screen.Caught);
                return;
            }

            if (a.wait > 0 && a.mode != 2)
            {
                a.wait -= dt;
                a.walking = false;
                return;
            }

            float speed = a.mode == 2 ? (a.who == -2 ? 3.45f : 3.18f) : a.mode == 1 ? 1.95f : (a.who == -2 ? 1.63f : 1.22f);
            if (w.Move(a, dt, speed))
            {
                if (a.mode == 2)
                {
                    if (a.hear <= 0)
                    {
                        a.mode = 0;
                        a.alert = .12f;
                    }
                }
                else
                {
                    int room = w.RandRoom();
                    w.Go(a, w.hubs[room]);
                    a.wait = 1.4f;
                }
            }

            float ph = a.step / 5.2f;
            if ((int)ph > a.last && a.walking)
            {
                a.last = (int)ph;
                if (dist < 31)
                {
                    float pan = ((a.x - player.X) * (float)Math.Cos(yaw) - (a.z - player.Z) * (float)Math.Sin(yaw)) / Math.Max(1, dist);
                    bool clear=w.Sight(player,a.Pos);Sound(a.who==-2?(clear?"keys":"coveredKeys"):(clear?"stamp":"coveredStamp"),(1-dist/32)*(clear?.80f:.52f),pan);
                }
            }
        }

        public void Focus()
        {
            target = null;
            person = null;
            float best = 99;
            float si = (float)Math.Sin(yaw), co = (float)Math.Cos(yaw);
            if (hidden)
            {
                prompt = "E   숨은 곳에서 나오기";
                return;
            }

            foreach (Thing o in w.things)
            {
                if (o.kind == Kind.Note && (s.notes & (1 << o.tag)) != 0)
                    continue;
                if (o.kind == Kind.Archive && (s.proof & (1 << o.tag)) != 0)
                    continue;
                float d = player.Dist(o.p);
                if (d > 2.8f)
                    continue;
                float dot = (si * (o.p.X - player.X) + co * (o.p.Z - player.Z)) / (d + .001f);
                if ((d > .77f && dot < .35f) || !w.Sight(player, o.p))
                    continue;
                float score = d + (1 - dot) * 1.3f;
                if (score < best)
                {
                    best = score;
                    target = o;
                }
            }

            foreach (Actor a in w.actors)
            {
                if (a.who < 0)
                    continue;
                float d = player.Dist(a.Pos);
                if (d > 3.1f)
                    continue;
                float dot = (si * (a.x - player.X) + co * (a.z - player.Z)) / (d + .001f);
                if ((d > .7f && dot < .48f) || !w.Sight(player, a.Pos))
                    continue;
                float score = d + (1 - dot) * 1.2f;
                if (score < best)
                {
                    best = score;
                    target = null;
                    person = a;
                }
            }

            prompt = person != null ? "E   " + Book.Names[person.who] + "와 이야기" : target != null ? "E   " + ThingName(target) : "";
        }

        public string ThingName(Thing o)
        {
            switch (o.kind)
            {
                case Kind.Note:
                    return w.page.items[o.tag] + " 조사";
                case Kind.Board:
                    return w.page.panel + " 조작";
                case Kind.Door:
                    return w.page.door + " 열기";
                case Kind.Station:
                    return FieldText.Station(w, o.tag) + " 조사";
                case Kind.Hide:
                    return "철제 물품함에 숨기";
                case Kind.Supply:
                    return "분필통 챙기기";
                case Kind.Archive:
                    return Book.Archive[o.tag] + " 보관";
                default:
                    return EndDoor[o.tag] + " 열기";
            }
        }

        public static string[] EndDoor =
        {
            "정문",
            "옥상 송신탑",
            "암실 뒷문",
            "무대 계단",
            "증언 보관문"
        };
        public void E()
        {
            if (screen != Screen.Play)
                return;
            if (hidden)
            {
                hidden = false;
                safeTime = .6f;
                Sound("locker", .4f);
                revision++;
                return;
            }

            Focus();
            if (person != null)
            {
                Talk(person.who);
                return;
            }

            if (target != null)
                Interact(target);
        }

        public void Interact(Thing o)
        {
            if(TakeCase(o))return;
            switch (o.kind)
            {
                case Kind.Station:
                    if (!s.solved)
                    {
                        Msg("중앙 장치가 대기 중입니다. 먼저 " + w.page.panel + "을 복구하세요.");
                        break;
                    }

                    if (o.tag != s.fieldDone)
                    {
                        Msg(o.tag < s.fieldDone ? "이미 인계를 마쳤습니다." : "먼저 첫 번째 현장을 복구하세요.");
                        break;
                    }

                    trial = new Trial(w, o.tag);
                    To(Screen.Field);
                    break;
                case Kind.Note:
                    s.notes |= 1 << o.tag;
                    reviewing = false;
                    reader = o.tag;inspectAngle=-12;inspectTilt=5;inspectZoom=false;
                    Sound("paper");
                    To(Screen.Read);
                    Save();
                    break;
                case Kind.Archive:
                    s.proof |= 1 << o.tag;
                    reviewing = false;
                    reader = 3 + o.tag;
                    Sound("paper");
                    To(Screen.Read);
                    Save();
                    break;
                case Kind.Supply:
                    int bit = 1 << (o.room % 30);
                    if ((s.usedBoxes & bit) != 0)
                    {
                        Msg("비어 있는 분필통입니다.");
                        break;
                    }

                    if (s.lures >= 5)
                    {
                        Msg("분필을 이미 다섯 개 가지고 있습니다.");
                        break;
                    }

                    s.usedBoxes |= bit;
                    s.lures = Math.Min(5, s.lures + 2);
                    Sound("paper");
                    Msg("분필을 챙겼습니다.  Q로 던져 소리를 내세요.");
                    Save();
                    break;
                case Kind.Hide:
                    hidden = true;
                    lastSafe = player;
                    Sound("locker", .55f);
                    Msg("물품함 안입니다. 발소리가 멀어질 때까지 기다리세요.");
                    break;
                case Kind.Board:
                    if (s.solved)
                    {
                        Msg(s.fieldDone < 2 ? "장치가 작동 중입니다. 수첩에서 현장 인계 위치를 확인하세요." : "출구 잠금이 풀렸습니다.");
                        break;
                    }

                    if (s.notes != 7)
                    {
                        Msg("장치의 표식이 부족합니다. 현장 기록 " + Count(s.notes) + " / 3");
                        break;
                    }

                    To(Screen.Board);
                    break;
                case Kind.Door:
                case Kind.Final:
                    if (!s.solved || s.fieldDone < 2)
                    {
                        Msg(!s.solved ? "잠겨 있습니다. " + w.page.panel + "부터 복구하세요." : "현장 인계 " + s.fieldDone + " / 2  ·  수첩의 약도를 확인하세요.");
                        break;
                    }

                    Exit(o.kind == Kind.Final ? o.tag : 0);
                    break;
            }
        }

        public static int Count(int v)
        {
            int c = 0;
            while (v > 0)
            {
                c += v & 1;
                v >>= 1;
            }

            return c;
        }

        public void Solve()
        {
            if (!puzzle.Correct())
            {
                Sound("wrong");
                revision++;
                return;
            }

            s.solved = true;
            Sound("unlock");
            To(Screen.Play);
            Msg("장치 복구  ·  수첩에 표시된 첫 번째 현장으로 가세요.");
            Save();
        }

        public void FieldConfirm()
        {
            if (!trial.Check())
            {
                Sound("wrong");
                revision++;
                return;
            }

            s.fieldDone++;
            if(Assignment==2||Assignment==1&&s.fieldDone==2){s.solved=true;s.fieldDone=2;}
            if(Assignment==1&&s.fieldDone==1)RingAt(player);
            Sound("unlock");
            To(Screen.Play);
            Msg(s.fieldDone == 2 ? "출구 잠금이 풀렸습니다.  " + w.page.door : "첫 현장 인계 완료  ·  수첩에서 다음 현장을 확인하세요.");
            Save();
        }

        public bool EndOK(int e)
        {
            if (e == 0)
                return true;
            if (e == 4)
                return s.proof == 255;
            int i = e - 1;
            return ((s.dates >> (i * 3)) & 4) != 0 && s.Trust(i) >= 3;
        }

        public string EndNeed(int e)
        {
            return e == 4 ? "각 장 마지막 구역의 원본 8장을 보관해야 합니다." : Book.Names[e - 1] + "의 세 번째 약속을 마치고 세 번 이상 마음을 나누어야 합니다.";
        }

        public void Exit(int e)
        {
            if (s.side >= 0)
            {
                int id = s.side;
                s.dates |= 1 << id;
                opts.memories |= 1 << id;
                SaveOpts();
                part = 0;
                talk = id / 3;
                archiveView = -1;
                To(Screen.Date);
                Save();
                return;
            }

            if (s.stage == 39)
            {
                if (!EndOK(e))
                {
                    Msg(EndNeed(e));
                    return;
                }

                if(Revised && (s.wrote&128)!=0){
                    if(opts.exitMemory==null || opts.exitMemory.Length!=5)opts.exitMemory=new int[5];
                    opts.exitMemory[e]=(s.privateRecord&128)!=0?2:1;
                }
                s.ending = e;
                opts.endings |= 1 << e;
                SaveOpts();
                endingView = e;
                part = 0;
                archiveView = -1;
                To(Screen.End);
                Save();
                Sound("end");
                return;
            }

            if (s.stage == 9 || s.stage == 19 || s.stage == 29)
            {
                int k = s.stage / 10;
                if (s.pledge >= 0 && s.Trust(s.pledge) >= k + 1)
                {
                    s.side = s.pledge * 3 + k;
                    s.pledge = -1;
                    Next();
                    return;
                }

                s.pledge = -1;
            }

            s.stage++;
            Next();
        }

        public void DateAnswer(int k)
        {
            int id = archiveView >= 0 ? archiveView : s.side;
            if (archiveView < 0)
            {
                s.answered |= 1 << id;
                if (k == 1)
                    s.answers |= 1 << id;
                else
                    s.answers &= ~(1 << id);
                if (k == 1)
                    opts.preferences |= 1 << id;
                else
                    opts.preferences &= ~(1 << id);
                SaveOpts();
                Save();
            }

            Sound(id/3==0?"broadcast":id/3==1?"shutter":"note2",.44f);
            sceneStart=t;
            DateNext();
        }

        public void DateNext()
        {
            part++;
            revision++;
            if (part < 3)
                return;
            if (archiveView >= 0)
            {
                To(Screen.Gallery);
                return;
            }

            s.side = -1;
            s.stage++;
            Next();
        }

        public void Talk(int id)
        {
            talk = id;
            part = 0;
            speaker = Book.Names[id];
            To(Screen.Talk);
            Sound("cloth", .3f);
        }

        public void ChooseTalk(int n)
        {
            if (n == 0)
            {
                if ((s.visit & (1 << talk)) == 0)
                {
                    s.visit |= 1 << talk;
                    s.Like(talk);
                }

                part = 1;
                Sound("chime", .5f);
                Save();
                revision++;
                return;
            }

            if (n == 1 && s.side < 0 && s.stage < 30)
            {
                s.pledge = talk;
                part = 2;
                Sound("chime", .6f);
                Save();
                revision++;
                return;
            }

            To(Screen.Play);
        }

        public void Recover()
        {
            player = lastSafe;
            hidden = false;
            heat = 0;
            safeTime = 10;
            foreach (var a in w.actors.Where(a => a.who < 0))
            {
                var v = w.Center(w.hubs[a.home]);
                a.x = v.X;
                a.z = v.Z;
                a.path = null;
                a.mode = 0;
                a.alert = 0;
                a.hear = 0;
                a.grace = 10;
            }

            s.stamina = 1;
            s.lures = Math.Max(s.lures, 1);
            To(Screen.Play);
            Save();
            Msg("정신을 차렸습니다. 모은 기록은 수첩에 있습니다.");
        }

        public void Toss()
        {
            if (hidden)
                return;
            if (s.lures <= 0)
            {
                Msg("분필이 없습니다. 보급 상자를 찾아보세요.");
                return;
            }

            s.lures--;
            V d = player;
            for (int k = 1; k <= 28; k++)
            {
                V q = new V(player.X + (float)Math.Sin(yaw) * k * .43f, player.Z + (float)Math.Cos(yaw) * k * .43f);
                if (!w.Open(w.Tile(q)))
                    break;
                d = q;
            }

            decoy = d;
            decoyTime = 6;
            Sound("chalk");
            Msg("분필이 부서졌습니다.");
        }

        public void Listen()
        {
            if (listenCd > 0)
                return;
            listen = 4;
            listenCd = 6;
            Sound("cloth", .35f);
            revision++;
        }

        public string Direction(V dest)
        {
            float a = (float)Math.Atan2(dest.X - player.X, dest.Z - player.Z) - yaw;
            while (a > Math.PI)
                a -= (float)(2 * Math.PI);
            while (a < -Math.PI)
                a += (float)(2 * Math.PI);
            return Math.Abs(a) < .65f ? "앞쪽" : Math.Abs(a) > 2.42f ? "뒤쪽" : a > 0 ? "오른쪽" : "왼쪽";
        }
    }
}