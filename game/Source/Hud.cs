using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace Uncalled
{
    public class Hit
    {
        public RectangleF r;
        public int id;
        public bool active;
    }

    public sealed partial class Hud : IDisposable
    {
        public const int W = 1120, H = 630;
        public Bitmap img = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        public List<Hit> buttons = new List<Hit>();
        Dictionary<string, Font> fonts = new Dictionary<string, Font>();
        Graphics d;
        Play g;
        public int hover = -1, selected = -1;
        public bool HasSave;
        public Saved saved;
        public static Color White = Color.FromArgb(238, 236, 219), Cream = Color.FromArgb(221, 211, 170), Mute = Color.FromArgb(166, 184, 168), Gold = Color.FromArgb(219, 174, 115), Green = Color.FromArgb(101, 157, 136), Red = Color.FromArgb(210, 130, 111), Ink = Color.FromArgb(20, 31, 31);
        public static Color[] Cast =
        {
            Color.FromArgb(148, 174, 206),
            Color.FromArgb(211, 148, 145),
            Color.FromArgb(153, 186, 145)
        };
        Font F(int n, int style)
        {
            string k = n + "-" + style;
            Font f;
            if (!fonts.TryGetValue(k, out f))
            {
                f = new Font(style == 2 ? "바탕" : style == 3 ? "Consolas" : "맑은 고딕", n, style == 1 ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
                fonts[k] = f;
            }

            return f;
        }

        public void R(float x, float y, float w, float h, Color c)
        {
            using (var b = new SolidBrush(c))
                d.FillRectangle(b, x, y, w, h);
        }

        void L(float x, float y, float x2, float y2, Color c, float width = 1)
        {
            using (var p = new Pen(c, width))
                d.DrawLine(p, x, y, x2, y2);
        }

        void Dot(float x, float y, float rad, Color c)
        {
            using (var b = new SolidBrush(c))
                d.FillEllipse(b, x - rad, y - rad, rad * 2, rad * 2);
        }

        void Word(string s, float x, float y, int n, Color c, int sty = 0, float width = 1010, float height = 120)
        {
            using (var b = new SolidBrush(c))
            using (var sf = (StringFormat)StringFormat.GenericTypographic.Clone())
            {
                sf.FormatFlags |= StringFormatFlags.LineLimit;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                d.DrawString(s, F(n, sty), b, new RectangleF(x, y, width, height), sf);
            }
        }

        void Right(string s, int right, int y, int size, Color c)
        {
            var f = F(size, 0);
            float width = d.MeasureString(s, f).Width;
            Word(s, right - width, y, size, c);
        }

        void Dark(int alpha = 242)
        {
            R(0, 0, W, H, Color.FromArgb(alpha, 12, 22, 23));
        }

        void Grad(float x, float y, float w, float h, Color c1, Color c2, float angle = 0)
        {
            var r = new RectangleF(x, y, w, h);
            using (var b = new LinearGradientBrush(r, c1, c2, angle))
                d.FillRectangle(b, r);
        }

        void ColHeader(string small, string big, string right = "")
        {
            Word(small, 65, 35, 14, Green, 1);
            Word(big, 63, 61, 35, White, 2);
            if (right.Length > 0)
                Right(right, 1057, 71, 14, Mute);
            L(64, 119, 1056, 119, Color.FromArgb(119, 125, 149, 128));
        }

        void KeyFoot(string s)
        {
            Word(s, 65, 584, 14, Mute);
        }

        void Btn(int id, string txt, int x, int y, int w, int h, bool active = true, bool accent = false)
        {
            var r = new RectangleF(x, y, w, h);
            buttons.Add(new Hit { r = r, id = id, active = active });
            bool hi = active && (id == hover || id == selected);
            Color fill = hi ? Color.FromArgb(247, 62, 86, 75) : accent ? Color.FromArgb(232, 51, 74, 65) : Color.FromArgb(219, 28, 43, 42);
            R(x, y, w, h, fill);
            L(x, y + h - 1, x + w, y + h - 1, hi ? Cream : Color.FromArgb(95, 112, 136, 119));
            if (accent || hi)
                R(x, y, 3, h, hi ? Gold : Green);
            Word(txt, x + 14, y + (h > 42 ? 10 : 7), h > 42 ? 18 : 15, active ? (hi ? White : accent ? Cream : White) : Color.FromArgb(110, 132, 137, 126), active && hi ? 1 : 0, w - 25, h - 6);
        }

        public int HitAt(float x, float y)
        {
            for (int i = buttons.Count - 1; i >= 0; i--)
                if (buttons[i].active && buttons[i].r.Contains(x, y))
                    return buttons[i].id;
            return -1;
        }

        public void Paint(Play game)
        {
            g = game;
            buttons.Clear();
            using (d = Graphics.FromImage(img))
            {
                d.Clear(Color.Transparent);
                d.SmoothingMode = SmoothingMode.AntiAlias;
                d.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                switch (g.screen)
                {
                    case Screen.Field:
                        Field();
                        break;
                    case Screen.Title:
                        Title();
                        break;
                    case Screen.Play:
                        Ingame();
                        break;
                    case Screen.Intro:
                        Intro();
                        break;
                    case Screen.Book:
                        Journal();
                        break;
                    case Screen.Read:
                        Read();
                        break;
                    case Screen.Echo:
                        Echo();break;
                    case Screen.Inference:
                        Inference();
                        break;
                    case Screen.Board:
                        Board();
                        break;
                    case Screen.Talk:
                        Talk();
                        break;
                    case Screen.Date:
                        Scene(false);
                        break;
                    case Screen.End:
                        Scene(true);
                        break;
                    case Screen.Gallery:
                        Gallery();
                        break;
                    case Screen.Settings:
                        Settings();
                        break;
                    case Screen.Caught:
                        Caught();
                        break;
                    case Screen.Pause:
                        Pause();
                        break;
                    case Screen.Confirm:
                        Confirm();
                        break;
                }
            }
        }

        void Title()
        {
            Grad(0, 0, 800, H, Color.FromArgb(247, 11, 23, 23), Color.FromArgb(0, 10, 22, 23));
            Grad(0, 474, W, 156, Color.Transparent, Color.FromArgb(188, 9, 17, 18), 90);
            R(65, 63, 40, 2, Gold);
            Word("은람고등학교  /  야간부", 65, 80, 16, Mute);
            Word("결 번", 55, 120, 99, White, 2, 420, 131);
            Word("마지막 예비종", 64, 246, 31, Cream, 2);
            Word("THE  UNCALLED", 66, 295, 14, Mute, 3);
            int yy = 344;
            if (HasSave)
            {
                Btn(1, "이어 하기", 65, yy, 279, 41, true, true);
                yy += 48;
            }

            Btn(2, "새로 시작", 65, yy, 279, 41, true, !HasSave);
            yy += 48;
            Btn(3, "기억 보관함", 65, yy, 279, 39);
            yy += 45;
            Btn(4, "환경 설정", 65, yy, 279, 39);
            yy += 45;
            Btn(5, "게임 종료", 65, yy, 279, 39);
            Right("2002년 10월 19일   ·   폐교 전야", 1058, 569, 14, Cream);
        }

        void Ingame()
        {
            Grad(0, 0, 540, 126, Color.FromArgb(150, 10, 20, 20), Color.Transparent);
            Word(Chapter(), 31, 22, 13, Cream, 1);
            Word(g.w.page.title, 30, 43, 22, White, 2);
            int tile = g.w.Tile(g.player);
            int loc = g.w.Open(tile) ? g.w.roomAt[tile] : -1;
            Word((loc >= 0 ? g.w.rooms[loc].Name : Book.Themes[g.w.theme] + " 복도") + "     ·     " + (g.Revised?g.Objective():(!g.s.solved ? "기록 " + Play.Count(g.s.notes) + "/3" : g.s.fieldDone < 2 ? "현장 인계 " + g.s.fieldDone + "/2" : "출구 개방")), 31, 77, 13, Mute);
            string ls = g.hidden ? "은신 중" : g.crouch ? "웅크림" : g.run ? "달리는 중" : g.s.lit ? "손전등 켜짐" : "손전등 꺼짐";
            Grad(0, 507, W, 123, Color.Transparent, Color.FromArgb(168, 7, 14, 16), 90);
            Word(ls + "     분필 " + g.s.lures, 31, 572, 14, Mute);
            R(31, 600, 126, 3, Color.FromArgb(95, 100, 125, 117));
            R(31, 600, 126 * g.s.stamina, 3, Cream);
            Right("Tab  수첩과 약도", 1088, 574, 14, Cream);
            if (!g.hidden)
            {
                Dot(560, 315, 2, Color.FromArgb(160, 230, 225, 199));
                if (g.prompt.Length > 0)
                {
                    float wid = Math.Min(580, Math.Max(245, d.MeasureString(g.prompt, F(17, 1)).Width + 36));
                    int px = (int)(560 - wid / 2);
                    R(px, 385, wid, 38, Color.FromArgb(218, 20, 32, 30));
                    Word(g.prompt, px + 17, 392, 17, White, 1, wid - 25, 29);
                }
            }
            else
            {
                R(0, 0, W, 228, Color.FromArgb(245, 9, 16, 16));
                R(0, 395, W, 235, Color.FromArgb(245, 9, 16, 16));
                for (int j = 0; j < 5; j++)
                    R(0, 245 + j * 30, W, 9, Color.FromArgb(220, 18, 26, 24));
                Word("E   물품함에서 나오기", 451, 474, 17, Cream);
            }

            if (g.heat > .20f)
            {
                string warn = g.heat > .62f ? "추적 중  ·  모퉁이에서 시선을 끊으세요" : "순찰이 이쪽을 살핍니다";
                Right(warn, 1088, 28, 16, Red);
                R(873, 57, 214 * g.heat, 3, Red);
            }

            if (g.listen > 0)
            {
                int li = 0;
                foreach (Actor a in g.w.actors.Where(v => v.who < 0))
                {
                    float dis = a.Pos.Dist(g.player);
                    if (dis < 46)
                    {
                        Word((a.who == -1 ? "검인 도장" : "열쇠 고리") + " 소리  ·  " + g.Direction(a.Pos) + "  /  " + (dis < 10 ? "가까움" : dis < 23 ? "복도 건너" : "멀리"), 34, 115 + li++ * 25, 16, Cream, 1);
                    }
                }

                if (li == 0)
                    Word("귀를 기울입니다. 가까운 발소리가 없습니다.", 34, 115, 15, Mute);
            }

            if (g.toastTime > 0)
            {
                R(288, 531, 544, 34, Color.FromArgb(202, 17, 31, 29));
                Word(g.toast, 301, 538, 15, Cream, 0, 520, 24);
            }
            else if (g.s.stage == 0 && g.s.notes == 0 && g.s.seconds < 95)
            {
                Word("WASD 이동    마우스 시점    E 조사    Space 소리 듣기", 311, 537, 15, Cream, 0, 680, 28);
            }
        }

        string Chapter()
        {
            return g.s.side >= 0 ? "개인 약속   /   " + Book.Names[g.s.side / 3] : "제 " + (g.s.stage / 5 + 1) + " 장   /   " + Book.Acts[g.s.stage / 5];
        }

        void Intro()
        {
            Grad(0, 0, W, H, Color.FromArgb(244, 16, 30, 29), Color.FromArgb(155, 10, 21, 23));
            Word(Chapter(), 81, 76, 17, Green, 1);
            Word(g.w.page.title, 78, 119, 48, White, 2);
            Word(Book.Themes[g.w.theme] + "     " + Book.Times[g.s.stage / 5] + "     " + (g.s.side >= 0 ? "개인 기록" : "야간 순회 " + (g.s.stage % 5 + 1) + " / 5"), 82, 198, 17, Cream);
            L(82, 242, 1009, 242, Color.FromArgb(95, 125, 153, 132));
            Word(g.s.side < 0 && g.s.stage % 5 == 0 ? Choices.Intro[g.s.stage / 5] + "\n" + g.w.page.intro : g.w.page.intro, 82, 266, g.s.side < 0 && g.s.stage % 5 == 0 ? 21 : 25, White, 2, 925, 139);
            Word(g.Revised?g.Objective():(g.s.stage == 0 ? "신발장 너머 세 곳에서 현장 기록을 찾으세요. Tab을 누르면 교내 약도와 목적지가 나옵니다." : "조사할 물건과 약속 장소는 수첩에 적혀 있습니다."), 82, 415, 18, Mute, 0, 900, 64);
            Btn(10, "학교 안으로     ↵", 82, 503, 278, 51, true, true);
        }

        void Read()
        {
            if(g.reader>=0&&g.reader<3){
                if(g.inspectZoom)
                {
                    Grad(0,0,W,84,Color.FromArgb(208,13,24,26),Color.Transparent,90);
                    Word(g.w.page.items[g.reader],46,27,20,Cream,2,580,43);
                    Grad(0,540,W,90,Color.Transparent,Color.FromArgb(241,13,24,26),90);
                    Word("← →   또는 드래그로 물건 돌리기",45,575,16,Mute);
                    Btn(563,"축소   Z / Esc",877,560,178,45,true,true);
                    return;
                }
                Grad(550,0,570,H,Color.FromArgb(208,15,26,29),Color.FromArgb(252,12,22,24));
                Grad(0,0,553,151,Color.FromArgb(160,15,24,27),Color.Transparent,90);
                Word(g.w.rooms[g.w.reqRooms[g.reader]].Name,48,52,17,Mute);
                Word("드래그 또는 ← →   물건 돌리기     Z   확대",43,559,15,Mute);
                Word("은람고 야간부    /    습득 기록",608,84,15,Gold,1);
                Word(g.w.page.items[g.reader],604,130,38,White,2,460,68);
                L(608,210,1042,210,Color.FromArgb(108,130,129,109));
                Word(g.w.page.notes[g.reader],607,247,24,White,2,432,155);
                if(!g.Revised||g.Assignment==2)Word(g.puzzle.Hint(g.reader),609,427,17,Gold,1,420,55);
                Btn(10,g.reviewing?"현장 기록으로     ↵":"수첩에 넣기     ↵",609,506,255,49,true,true);
                Btn(563,g.inspectZoom?"축소    Z":"확대    Z",878,506,168,49);return;
            }
            Dark(231);
            string name = g.reader < 3 ? g.w.page.items[g.reader] : Book.Archive[g.reader - 3];
            ColHeader(g.reader < 3 ? "현장 수첩   /   습득 기록" : "교적 원본   /   증언 보관", name);
            R(69, 149, 115, 267, Color.FromArgb(60, 70, 98, 79));
            Word(g.reader < 3 ? "기록" : "원본", 92, 177, 21, Cream, 2);
            Word((g.reader < 3 ? g.reader + 1 : g.reader - 2).ToString("00"), 87, 221, 44, White, 3);
            Word(g.reader < 3 ? g.w.page.notes[g.reader] : new[] { "번호표에 남은 네 사람의 손길이 서로 다르다.", "야간부가 쓰던 문은 도면 뒷장에 따로 그렸다.", "통합 이전의 원부에는 연우의 자리가 있다.", "목소리를 돌려주기 위한 정정문이 남았다.", "연우는 그날 자신의 이름으로 귀가했다.", "폐과를 결정한 것은 학생의 결석이 아니었다.", "연우는 오늘 이 학교로 오겠다고 썼다.", "뜯지 않은 졸업장 봉투의 수신인은 문연우다." }[g.reader - 3], 221, 170, 25, White, 2, 764, 132);
            L(223, 331, 954, 331, Color.FromArgb(103, 125, 145, 122));
            Word(g.reader < 3 ? g.puzzle.Hint(g.reader) : "원본 " + Play.Count(g.s.proof) + " / 8   ·   마지막 장의 증언 보관문에 사용합니다.", 222, 354, 22, Gold, 1, 765, 60);
            Btn(10, "수첩에 넣기     ↵", 220, 496, 249, 49, true, true);
        }

        void Journal()
        {
            Dark();
            ColHeader(Chapter(), g.w.page.title, "학교 수첩    /    " + Book.Times[g.s.stage / 5]);
            int[] xx =
            {
                65,
                286,
                507
            };
            string[] labels =
            {
                "1  교내 약도",
                "2  현장 기록",
                "3  인물과 약속"
            };
            for (int i = 0; i < 3; i++)
                Btn(20 + i, labels[i], xx[i], 132, 211, 36, true, i == g.bookTab);
            Btn(10, "닫기   Tab", 937, 132, 118, 36);
            if (g.bookTab == 0)
                Map();
            else if (g.bookTab == 1)
            {
                for (int i = 0; i < 3; i++)
                {
                    bool got = (g.s.notes & (1 << i)) != 0;
                    int y = 193 + i * 105;
                    Word((i + 1).ToString("00"), 68, y, 19, got ? Gold : Mute, 3);
                    Word(g.w.page.items[i] + "   ·   " + g.w.rooms[g.w.reqRooms[i]].Name, 116, y, 19, got ? White : Mute, 1);
                    Word(got ? g.w.page.notes[i] : "이 장소에서 기록을 찾아보세요.", 117, y + 29, 16, Mute, 0, 729, 46);
                    if (got) Btn(560 + i, "다시 살펴보기", 876, y + 29, 169, 37);
                    if (got && (!g.Revised || g.Assignment==2))
                        Word(g.puzzle.Hint(i), 117, y + 63, 15, Gold);
                    L(65, y + 94, 1053, y + 94, Color.FromArgb(50, 127, 150, 129));
                }

                Word("장치  " + g.w.page.panel + "     /     " + g.w.rooms[g.w.reqRooms[3]].Name, 69, 525, 18, Cream);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    var a = g.w.actors[i];
                    int y = 196 + i * 99;
                    Word(Book.Names[i], 69, y, 24, Cast[i], 2);
                    Word(Book.Roles[i], 69, y + 33, 14, Mute);
                    Word("오가는 곳   " + g.w.rooms[a.home].Name + "   ↔   " + g.w.rooms[a.other].Name, 295, y + 1, 17, White);
                    Word("마음을 나눈 횟수  " + g.s.Trust(i) + "    ·    개인 약속  " + Play.Count((g.s.dates >> (i * 3)) & 7) + " / 3" + (g.s.pledge == i ? "    ·    만나기로 함" : ""), 295, y + 33, 15, Cream);
                    L(65, y + 80, 1053, y + 80, Color.FromArgb(70, 127, 150, 129));
                }

                Word("2·4·6장 끝에 약속 장소로 들어갑니다. 대화에서 마음을 나누고 만날 사람에게 약속을 남기세요.", 66, 512, 16, Mute, 0, 989, 50);
            }

            KeyFoot("Tab / Esc  닫기       1–3  수첩 페이지");
        }

        void Map()
        {
            World w = g.w;
            float bx = 75, by = 184, bw = 557, bh = 365;
            R(bx, by, bw, bh, Color.FromArgb(255, 19, 35, 34));
            float scale = Math.Min((bw - 20) / w.W, (bh - 14) / w.H);
            float ox = bx + (bw - w.W * scale) / 2, oy = by + (bh - w.H * scale) / 2;
            for (int z = 0; z < w.H; z++)
                for (int x = 0; x < w.W; x++)
                {
                    int n = z * w.W + x;
                    if (!w.Open(n))
                        continue;
                    bool known = w.seen[n];
                    if (!known && !g.opts.hints)
                        continue;
                    R(ox + x * scale, oy + z * scale, scale + .15f, scale + .15f, known ? (w.cells[n] == 3 ? Color.FromArgb(93, 119, 100) : Color.FromArgb(110, 129, 108)) : Color.FromArgb(41, 62, 54));
                }

            DrawRoute(ox, oy, scale);
            for (int i = 0; i < w.things.Count; i++)
            {
                var o = w.things[i];
                bool field = o.kind == Kind.Station && (g.s.solved || g.Assignment==1) && o.tag == g.s.fieldDone;
                bool wanted = field || o.kind == Kind.Note || o.kind == Kind.Board || o.kind == Kind.Door || o.kind == Kind.Archive || o.kind == Kind.Final;
                if (!wanted || !g.opts.hints && !w.seen[w.Tile(o.p)] || o.kind == Kind.Note && (g.s.notes & (1 << o.tag)) != 0)
                    continue;
                Color c = field ? Color.Aquamarine : o.kind == Kind.Note ? Gold : o.kind == Kind.Archive ? Red : o.kind == Kind.Board ? Color.SkyBlue : Cream;
                float x = ox + o.p.X / World.Unit * scale, y = oy + o.p.Z / World.Unit * scale;
                Dot(x, y, 4.8f, c);
                if (o.kind == Kind.Note)
                    Word((o.tag + 1).ToString(), x + 5, y - 8, 12, Cream, 1, 27, 18);
            }

            foreach (var a in w.actors.Where(a => a.who >= 0))
                if (g.opts.hints || w.seen[w.Tile(a.Pos)])
                {
                    float xx = ox + a.x / World.Unit * scale, yy = oy + a.z / World.Unit * scale;
                    R(xx - 2, yy - 2, 5, 5, Cast[a.who]);
                }

            float px = ox + g.player.X / World.Unit * scale, py = oy + g.player.Z / World.Unit * scale;
            Dot(px, py, 5, White);
            L(px, py, px + (float)Math.Sin(g.yaw) * 10, py + (float)Math.Cos(g.yaw) * 10, White, 2);
            if (g.LastHall) { ExitDirectory(); return; }
            Word(g.Revised?g.Objective():"현재 목표", 655, 178, 14, Cream, 1, 421, 36);
            for (int i = 0; i < 3; i++)
            {
                bool ok = (g.s.notes & (1 << i)) != 0;
                Word((ok ? "✓  " : (i + 1) + ".  ") + w.page.items[i], 655, 212 + i * 47, 17, ok ? Mute : White, 1, 396, 28);
                Word(w.rooms[w.reqRooms[i]].Name, 680, 235 + i * 47, 13, Mute, 0, 360, 24);
            }

            Word("장치   " + w.page.panel, 655, 362, 16, Color.LightBlue, 1, 390, 25);
            Word(w.rooms[w.reqRooms[3]].Name, 680, 385, 13, Mute);
            Word("출구   " + w.page.door, 655, 411, 16, Cream, 1, 390, 25);
            Word(w.rooms[w.reqRooms[4]].Name, 680, 434, 13, Mute);
            if (g.WitnessChoice && g.opts.hints)
                Btn(575, g.guideByRecords ? "증언할 사람 위치 표시" : "증언 대신 다른 기록 위치 표시", 655, 470, 393, 44);
            else if ((g.s.solved||g.Assignment==1) && g.s.fieldDone < 2)
            {
                int leg = g.s.fieldDone;
                Word("현장 인계   " + (leg + 1) + " / 2   " + FieldText.Station(w, leg), 655, 467, 17, Color.Aquamarine, 1, 392, 29);
                Word(w.rooms[w.reqRooms[5 + leg]].Name + "   ·   약도의 민트색 점", 655, 500, 14, Mute, 0, 390, 42);
            }
            else
                Word("금색  기록   ·   파랑  장치   ·   상아  출구\n작은 사각형  학생   ·   흰 화살표  내 위치", 655, 473, 14, Mute, 0, 390, 64);
        }

        void Field()
        {
            Dark(242);
            Trial p = g.trial;
            ColHeader("현장 인계  " + (g.s.fieldDone + 1) + " / 2", p.title, g.w.rooms[g.w.reqRooms[5 + g.s.fieldDone]].Name);
            Word(g.s.side < 0 ? FieldText.Instructions[g.s.stage] : g.w.page.intro, 66, 139, 16, Mute, 0, 985, 48);
            Word(p.goal, 66, 189, 20, White, 0, 985, 55);
            R(68, 257, 975, 113, Color.FromArgb(255, 24, 42, 38));
            if (p.mode == 0)
            {
                for (int lane = 0; lane < 2; lane++)
                {
                    int v = lane == 0 ? p.desired : p.knob;
                    float y = 288 + lane * 46;
                    Word(lane == 0 ? "기준" : "수신", 84, y - 12, 16, lane == 0 ? Gold : Color.Aquamarine);
                    for (int i = 0; i < 392; i++)
                    {
                        float a = (float)Math.Sin((v + 2) * Math.PI * 2 * i / 392.0), b = (float)Math.Sin((v + 2) * Math.PI * 2 * (i + 1) / 392.0);
                        L(157 + i * 2, y + a * 12, 159 + i * 2, y + b * 12, lane == 0 ? Gold : Color.Aquamarine, 1.5f);
                    }
                }
            }
            else if (p.mode == 1)
            {
                float q = .35f + p.desired * .024f;
                R(124, 303, 827, 11, Color.FromArgb(100, 126, 110));
                R(124 + (q - .09f) * 827, 294, 149, 29, Gold);
                R(124 + p.exposure * 827, 278, 4, 61, White);
                Word(p.pressed ? "노광 중" : "셔터 닫힘", 80, 337, 14, Mute);
            }
            else if (p.mode == 2)
            {
                Word(string.Join("     ·     ", p.keys.Select(k => Trial.Solfege[k])), 101, 274, 26, Gold, 2);
                Word("연주   " + new string ('●', p.step) + new string ('○', 4 - p.step), 103, 326, 18, White);
            }
            else if (p.mode == 3)
            {
                Word("작업표", 94, 278, 17, Mute);
                Word(string.Join("      ", Enumerable.Range(0, 3).Select(i => ((p.required & (1 << i)) != 0 ? "● 점등" : "○ 소등"))), 223, 294, 26, Gold, 1);
            }
            else if (p.mode == 4)
            {
                for (int i = 0; i < 2; i++)
                {
                    int cx = 345 + i * 410, cy = 315;
                    using (var pen = new Pen(i == 0 ? Gold : Color.Aquamarine, 2))
                        d.DrawEllipse(pen, cx - 43, cy - 43, 86, 86);
                    double a = (i == 0 ? p.orientation : p.cursor) * Math.PI * .5;
                    Dot(cx + (float)Math.Sin(a) * 30, cy - (float)Math.Cos(a) * 30, 6, i == 0 ? Gold : Color.Aquamarine);
                    Dot(cx + (float)Math.Cos(a) * 16, cy + (float)Math.Sin(a) * 16, 3, White);
                    Word(i == 0 ? "원본" : "사본", cx - 135, 304, 16, Mute);
                }
            }
            else
            {
                Word("정비표   " + p.Clock(p.desired), 107, 292, 27, Gold, 3);
                Word("시계   " + p.Clock(p.knob), 595, 292, 27, Cream, 3);
            }

            int bw = (976 - 12 * (p.N - 1)) / p.N;
            for (int i = 0; i < p.N; i++)
                Btn(400 + i, (i + 1) + "    " + p.Button(i), 68 + i * (bw + 12), 395, bw, 47, true, i == 0);
            if (p.wrong)
                Word("표식이 어긋났습니다. 장치를 다시 맞춰 주세요.", 68, 458, 15, Red);
            Btn(410, "인계 확인     ↵", 68, 509, 228, 49, true, true);
            Btn(10, "현장으로", 857, 509, 185, 49);
            KeyFoot("1–4   입력      Enter   확인      Esc   나가기");
        }

        void Board()
        {
            Dark(243);
            var p = g.puzzle;
            ColHeader("은람고   /   설비 조작", g.w.page.panel);
            Word(p.Rules, 66, 151, 20, White, 0, 993, 59);
            for (int i = 0; i < 3; i++)
            {
                int x = 66 + i * 330;
                Word(g.w.page.items[i], x, 230, 15, Mute);
                Word(p.Hint(i), x, 258, 19, Gold, 1, 321, 40);
            }

            int bw = (977 - (p.Buttons - 1) * 10) / p.Buttons;
            for (int i = 0; i < p.Buttons; i++)
                Btn(100 + i, (i + 1) + "   " + p.Button(i), 66 + i * (bw + 10), 342, bw, 50, true, i == p.sel);
            if (p.Sequence)
            {
                string v = "입력     ";
                for (int i = 0; i < 3; i++)
                    v += i < p.count ? p.Value(0, p.input[i]) + "     " : "—     ";
                Word(v, 67, 421, 19, Cream);
            }
            else
                Word(p.game == 1 ? "스위치를 누르면 연결된 램프가 바뀝니다." : "각 칸을 누를 때마다 값이 하나씩 바뀝니다.", 67, 421, 16, Mute);
            if (p.error)
                Word("표식이 맞지 않습니다. 기록과 입력을 다시 대조하세요.", 66, 462, 16, Red);
            Btn(110, "작동 확인     ↵", 66, 507, 231, 49, true, true);
            Btn(111, "입력 초기화", 309, 507, 174, 49);
            Btn(10, "장치에서 손 떼기", 848, 507, 196, 49);
            KeyFoot("1–6  입력     ← →  선택     ↑ ↓  값 변경     Enter  확인     Esc  나가기");
        }

        void Talk()
        {
            int n = g.talk;
            Grad(0, 278, W, 352, Color.FromArgb(0, 12, 22, 22), Color.FromArgb(250, 10, 21, 22), 90);
            R(58, 368, 4, 178, Cast[n]);
            Word(Book.Roles[n], 78, 367, 14, Cast[n], 1);
            Word(Book.Names[n], 76, 390, 28, White, 2);
            string line = g.part == 0 ? Book.Wishes[n][g.s.stage / 5] : g.part == 1 ? new[]
            {
                "…고마워. 누군가 듣고 있다고 생각하니까 말을 끝까지 할 수 있네.",
                "웃었지? 좋아. 오늘 사진을 꺼내면 그 표정부터 떠올릴게.",
                "오랫동안 누구에게도 들려주지 않은 얘기야. 들어 줘서 고마워."
            }[n] : "약속이야. " + (g.s.stage < 10 ? "두 번째" : g.s.stage < 20 ? "네 번째" : "여섯 번째") + " 장 마지막 문에서 만나. " + new[]
            {
                "왼쪽 이어폰은 네 거야.",
                "필름 한 장은 남겨 둘게.",
                "의자 반은 비워 둘게."
            }[n];
            if(g.part==1 && g.RememberedLine(n)!=null)line=g.RememberedLine(n);
            if(g.part==3)line=g.Assignment==5?new[]{"이걸 찾아 왔네. 방송실 문 열어 둘게. 둘이서만 들을 게 있거든.","네가 고른 물건을 가운데 두고 찍자. 나중에 오늘을 알아볼 수 있게.","이제 한 곡만 더 치면 돼. 네가 먼저 와 줄래?"}[n]:g.Testimony;
            Word(line, 78, 439, 21, White, 0, 970, 73);
            if (g.part == 0)
            {
                if(g.Revised && g.talk==g.Witness && (g.Assignment==3&&!g.s.witnessed || g.Assignment==5&&!g.Ready&&g.s.notes!=0))Btn(48,g.Assignment==5?"4   약속의 물건을 건넨다":"4   그날 본 것을 물어본다",77,485,394,42,true,true);
                Btn(40, new[] { "1   방송이 끝날 때까지 들어 줄게요", "1   찍고 싶은 걸 이야기해 줘요", "1   쉬는 박자까지 같이 들을게요" }[n], 77, 538, 391, 49, true, true);
                if (g.s.side < 0 && g.s.stage < 30)
                    Btn(41, "2   둘이 만날 약속을 남긴다", 479, 538, 294, 49);
                Btn(42, "대화를 마친다", 832, 538, 205, 49);
            }
            else
            {
                Btn(42, "다시 복도로     ↵", 78, 538, 237, 49, true, true);
                if (g.part == 1 && g.s.side < 0 && g.s.stage < 30)
                    Btn(47, "이 사람과 만날 약속을 남긴다", 332, 538, 338, 49);
            }
        }

        void Scene(bool ending)
        {
            int id = g.archiveView >= 0 ? g.archiveView : g.s.side;
            int k = Math.Min(2, Math.Max(0, g.part));
            string title = ending ? Book.EndingTitle[g.endingView] : Book.Invite[id / 3][id % 3];
            string line = ending ? Book.Ends[g.endingView][k] : Book.Dates[id / 3][id % 3][k];
            if (!ending && k == 2)
            {
                int ans = (g.archiveView >= 0 ? g.opts.preferences : g.s.answers) >> id & 1;
                line = Choices.Reply[id / 3][id % 3 * 2 + ans];
            }

            if(ending && k==2){
                int recorded=g.archiveView>=0?(g.opts.exitMemory!=null&&g.opts.exitMemory.Length==5?g.opts.exitMemory[g.endingView]:0):((g.s.wrote&128)!=0?((g.s.privateRecord&128)!=0?2:1):0);
                if(recorded>0 && recorded<=2)line=AfterBell.Coda[recorded-1]+" "+line.Split('|')[line.Split('|').Length-1];
            }
            string[] parts = line.Split('|');
            Grad(0, 290, W, 340, Color.Transparent, Color.FromArgb(253, 9, 19, 20), 90);
            Word(ending ? "졸업 기록   /   ENDING " + (g.endingView + 1).ToString("00") : "둘만의 기억   /   " + Book.Names[id / 3], 68, 354, 16, Gold, 1);
            Word(title, 65, 380, 31, White, 2);
            Word(parts[parts.Length - 1], 68, 432, 22, White, 0, 975, 86);
            if (!ending && k == 1 && g.archiveView < 0)
            {
                Btn(45, "1   " + Choices.Options[id / 3][id % 3 * 2], 69, 540, 420, 48, true, true);
                Btn(46, "2   " + Choices.Options[id / 3][id % 3 * 2 + 1], 505, 540, 420, 48);
            }
            else
                Btn(43, k < 2 ? "다음     ↵" : ending ? "기록을 덮는다     ↵" : "기억을 간직한다     ↵", 69, 545, 244, 46, true, true);
            Right((k + 1) + "  /  3", 1047, 565, 14, Mute);
        }

        void Gallery()
        {
            Dark();
            ColHeader("은람고   /   야간부", "기억 보관함");
            Word("둘만의 장면", 65, 145, 19, Cream, 1);
            for (int n = 0; n < 3; n++)
            {
                Word(Book.Names[n], 67 + n * 331, 184, 15, Cast[n]);
                for (int k = 0; k < 3; k++)
                {
                    int id = n * 3 + k;
                    bool ok = (g.opts.memories & (1 << id)) != 0;
                    Btn(200 + id, ok ? Book.Invite[n][k] : "잠긴 약속  " + (k + 1), 67 + n * 331, 209 + k * 44, 309, 37, ok);
                }
            }

            Word("탈출 기록", 65, 366, 19, Cream, 1);
            for (int j = 0; j < 5; j++)
            {
                bool ok = (g.opts.endings & (1 << j)) != 0;
                Btn(220 + j, ok ? Book.EndingTitle[j] : "미발견  " + (j + 1), 66 + j * 198, 412, 188, 41, ok);
            }

            Btn(10, "타이틀로", 66, 523, 166, 45, true, true);
            KeyFoot("개인 구역과 각 출구의 결말을 마치면 다시 감상할 수 있습니다.");
        }

        void Settings()
        {
            Dark();
            ColHeader("환경 설정", "화면과 소리");
            string[] title =
            {
                "화면 밝기",
                "소리 크기",
                "마우스 감도",
                "표시 거리",
                "약도 안내"
            };
            string[] value =
            {
                (g.opts.light + 1) + " / 5",
                g.opts.volume == 0 ? "음소거" : (g.opts.volume * 25) + "%",
                (g.opts.sens + 1) + " / 5",
                g.opts.quality == 0 ? "인접 구역" : "넓게",
                g.opts.hints ? "구조·목표·경로 표시" : "탐색한 곳만"
            };
            for (int j = 0; j < 5; j++)
            {
                int y = 151 + j * 63;
                Word(title[j], 67, y + 6, 20, White);
                Word(value[j], 488, y + 6, 19, Cream);
                Btn(300 + j * 2, "−", 789, y, 63, 40);
                Btn(301 + j * 2, "+", 860, y, 63, 40);
                L(66, y + 50, 1045, y + 50, Color.FromArgb(49, 140, 160, 143));
            }

            Btn(10, "돌아가기", 67, 504, 168, 47, true, true);
            KeyFoot("F11  전체 화면      변경 내용은 자동으로 저장됩니다.");
        }

        void Pause()
        {
            Dark(228);
            ColHeader(Chapter(), "일시 정지");
            Btn(10, "복도로 돌아가기     Esc", 65, 153, 324, 47, true, true);
            Btn(60, "저장하기", 65, 213, 324, 47);
            Btn(4, "환경 설정", 65, 273, 324, 47);
            Btn(61, "저장하고 타이틀로", 65, 333, 324, 47);
            Word("조작", 485, 155, 23, Cream, 2);
            Word("WASD     이동\n마우스     둘러보기\nShift     달리기      Ctrl / C     웅크리기\nE     조사·대화·숨기      F     손전등\nQ     분필 던지기      Space     소리 듣기\nTab     수첩·약도      F5     저장      F11     전체 화면", 487, 204, 18, White, 0, 548, 270);
            if (g.toastTime > 0)
                Word(g.toast, 65, 457, 16, Gold);
            var duration = TimeSpan.FromSeconds(g.s.seconds);
            Word("플레이 시간   " + (int)duration.TotalHours + "시간 " + duration.Minutes + "분", 66, 516, 16, Cream);
            KeyFoot("현장 기록, 약속, 탐색 위치는 플레이 중 자동으로 저장됩니다.");
        }

        void Confirm()
        {
            Dark();
            ColHeader("새로 시작", "새 밤을 시작할까요?");
            Word("현재 이어 하기 기록을 새 게임으로 교체합니다. 보관함에 모은 장면과 엔딩은 계속 감상할 수 있습니다.", 67, 198, 22, White, 0, 943, 99);
            Btn(70, "새 게임 시작", 67, 390, 244, 51, true, true);
            Btn(10, "돌아가기", 323, 390, 186, 51);
        }

        void Caught()
        {
            Dark(243);
            ColHeader("은람고   /   당직 기록", "출석부가 덮였다");
            Word("눈을 감기 직전, 도장을 찍는 소리가 들렸습니다. 확보한 기록과 약속을 가지고 안전한 지점에서 다시 시작합니다.", 67, 192, 24, White, 2, 915, 122);
            Word("달리면 발소리가 멀리 퍼집니다. 모퉁이를 돌아 손전등을 끄거나 철제 물품함에 숨으세요.", 68, 356, 18, Mute, 0, 905, 74);
            Btn(10, "다시 일어나기     ↵", 68, 489, 250, 51, true, true);
        }

        public void Dispose()
        {
            img.Dispose();
            foreach (var f in fonts.Values)
                f.Dispose();
        }
    }
}