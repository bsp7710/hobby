using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Uncalled
{
    public class GameForm : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        struct Msg
        {
            public IntPtr hwnd;
            public uint msg;
            public UIntPtr w;
            public IntPtr l;
            public uint time;
            public Point point;
            public uint priv;
        }

        [DllImport("user32.dll")]
        static extern bool PeekMessage(out Msg m, IntPtr h, uint min, uint max, uint remove);
        public Play game;
        Hud hud;
        Campus campus;
        Audio audio;
        IntPtr dc, rc;
        uint ui;
        Stopwatch clock = Stopwatch.StartNew();
        double prev, lastPaint, lastFrame;
        bool alive, locked, focused, crouchToggle, closing, fullscreen, qa;
        bool dirty = true;
        int paintVersion = -1;
        Screen lastScreen;
        Rectangle windowed;
        string checkPath;
        HashSet<Keys> keys = new HashSet<Keys>();
        Saved current;
        float yawScale;
        bool inspecting; Point inspectPoint;
        protected override CreateParams CreateParams
        {
            get
            {
                var p = base.CreateParams;
                p.ClassStyle |= 0x20;
                if (qa)
                    p.ExStyle |= 0x08000080;
                return p;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get
            {
                return qa;
            }
        }

        public GameForm(string inspect)
        {
            checkPath = inspect;
            qa = inspect != null;
            game = new Play(qa);
            Text = "결번 · 마지막 예비종";
            BackColor = Color.FromArgb(15, 26, 25);
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.None;
            StartPosition = qa ? FormStartPosition.Manual : FormStartPosition.CenterScreen;
            if (qa)
            {
                Location = new Point(-5500, -5500);
                ShowInTaskbar = false;
                FormBorderStyle = FormBorderStyle.None;
            }

            var sz = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            ClientSize = new Size(Math.Min(1280, Math.Max(800, sz.Width - 90)), Math.Min(720, Math.Max(500, sz.Height - 100)));
            if (qa)
                ClientSize = new Size(1120, 630);
            MinimumSize = new Size(720, 435);
            SetStyle(ControlStyles.Opaque | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (alive && !closing && !qa)
                Frame();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                rc = GL.Open(Handle, out dc);
                hud = new Hud();
                campus = new Campus();
                game.OnWorld = delegate
                {
                    campus.Build(game.w);
                    dirty = true;
                };
                game.Settle();
                current = Disk.Load();
                hud.HasSave = current != null;
                hud.saved = current;
                ui = GL.Texture(hud.img, false);
                if (!qa)
                {
                    audio = new Audio();
                    game.Tone = audio.Play;
                }

                alive = true;
                focused = ContainsFocus || Focused;
                prev = clock.Elapsed.TotalSeconds;
                lastFrame = prev;
                if (qa)
                {
                    BeginInvoke((MethodInvoker)CheckRender);
                }
                else
                {
                    Application.Idle += Idle;
                    Frame();
                }
            }
            catch (Exception ex)
            {
                Failed(ex);
            }
        }

        void Failed(Exception e)
        {
            if (qa)
            {
                Directory.CreateDirectory(checkPath);
                File.WriteAllText(Path.Combine(checkPath, "render-error.txt"), e.ToString());
                Environment.ExitCode = 1;
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(Disk.Dir);
                    File.WriteAllText(Path.Combine(Disk.Dir, "last-error.txt"), e.ToString());
                }
                catch
                {
                }

                MessageBox.Show("실행 중 오류가 발생했습니다.\n" + e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Close();
        }

        void Idle(object sender, EventArgs e)
        {
            Msg msg;
            while (alive && !closing && !PeekMessage(out msg, IntPtr.Zero, 0, 0, 0))
            {
                double now = clock.Elapsed.TotalSeconds;
                double step = WindowState == FormWindowState.Minimized || !focused ? .12 : 1.0 / 60;
                if (now - lastFrame < step)
                {
                    Thread.Sleep(1);
                    continue;
                }

                float dt = (float)Math.Min(.07, now - prev);
                prev = now;
                lastFrame = now;
                if (game.screen == Screen.Play && focused && !qa)
                {
                    CaptureLook();
                    float f = (Down(Keys.W) ? 1 : 0) - (Down(Keys.S) ? 1 : 0), r = (Down(Keys.D) ? 1 : 0) - (Down(Keys.A) ? 1 : 0);
                    game.Tick(dt, f, r, Down(Keys.ShiftKey) || Down(Keys.LShiftKey), crouchToggle || Down(Keys.ControlKey) || Down(Keys.LControlKey));
                }
                else
                    game.Tick(dt, 0, 0, false, false);
                if (audio != null)
                {
                    audio.volume = game.opts.volume * .25f;
                    audio.pressure = game.screen == Screen.Play ? game.heat : 0;
                    int tile=game.w.Tile(game.player);
                    bool hall=game.w.Open(tile)&&game.w.roomAt[tile]<0;
                    audio.enclosure=game.hidden?.24f:hall?1.0f:.47f;audio.weather=game.hidden?.18f:hall?.74f:.38f;
                    audio.cue=game.screen==Screen.Title?0:game.screen==Screen.Talk?3+game.talk:game.screen==Screen.Date?3+Math.Max(0,game.archiveView>=0?game.archiveView:game.s.side)/3:game.screen==Screen.Echo?6:game.screen==Screen.End?7:game.screen==Screen.Play&&game.heat>.57f?2:1;
                    audio.mood = game.screen == Screen.Title ? 12 : game.w.theme;
                }

                Frame();
            }
        }

        bool Down(Keys k)
        {
            return keys.Contains(k);
        }

        Point Center()
        {
            return PointToScreen(new Point(ClientSize.Width / 2, ClientSize.Height / 2));
        }

        void CaptureLook()
        {
            if (!locked)
                return;
            Point p = Cursor.Position, c = Center();
            int dx = p.X - c.X, dy = p.Y - c.Y;
            if (dx != 0 || dy != 0)
            {
                yawScale = .00145f + game.opts.sens * .00053f;
                game.yaw += dx * yawScale;
                game.pitch = Math.Max(-1.12f, Math.Min(1.12f, game.pitch - dy * yawScale));
                Cursor.Position = c;
                if (Math.Abs(game.yaw) > 7)
                    game.yaw = (float)(game.yaw % (2 * Math.PI));
            }
        }

        void Lock(bool yes)
        {
            if (qa || locked == yes)
                return;
            locked = yes;
            if (yes)
            {
                Cursor.Clip = RectangleToScreen(ClientRectangle);
                Cursor.Position = Center();
                Cursor.Hide();
            }
            else
            {
                Cursor.Clip = Rectangle.Empty;
                Cursor.Show();
            }

            keys.Clear();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            focused = true;
            dirty = true;
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            focused = false;
            keys.Clear();
            if (!qa && alive && game.screen == Screen.Play)
            {
                game.To(Screen.Pause);
                game.Save();
            }

            Lock(false);
            dirty = true;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            dirty = true;
            if (locked && !qa)
            {
                Cursor.Clip = RectangleToScreen(ClientRectangle);
                Cursor.Position = Center();
            }
        }

        void Frame()
        {
            if (!alive || ClientSize.Width <= 0 || ClientSize.Height <= 0 || WindowState == FormWindowState.Minimized)
                return;
            bool title = game.screen == Screen.Title;
            if (lastScreen != game.screen)
            {
                dirty = true;
                hud.hover = -1;
                hud.selected = game.screen==Screen.Book && lastScreen==Screen.Read && game.bookTab==1 ? 560+game.reader : -1;
                if (title && !qa)
                {
                    current = Disk.Load();
                    hud.HasSave = current != null;
                    hud.saved = current;
                }

                lastScreen = game.screen;
            }

            Lock(game.screen == Screen.Play && focused);
            campus.Render(game, ClientSize.Width, ClientSize.Height);
            double now = clock.Elapsed.TotalSeconds;
            bool timed = (game.screen == Screen.Play || game.screen == Screen.Field) && now - lastPaint > (game.screen == Screen.Field ? .047 : .15);
            if (dirty || timed || paintVersion != game.revision)
            {
                hud.Paint(game);
                GL.Upload(ui, hud.img);
                paintVersion = game.revision;
                lastPaint = now;
                dirty = false;
            }

            GL.glDisable(2929);
            GL.glEnable(3042);
            GL.glBlendFunc(770, 771);
            GL.glBindTexture(3553, ui);
            GL.glMatrixMode(5889);
            GL.glLoadIdentity();
            GL.glOrtho(0, 1, 1, 0, -1, 1);
            GL.glMatrixMode(5888);
            GL.glLoadIdentity();
            GL.glColor4f(1, 1, 1, 1);
            GL.glBegin(7);
            GL.glTexCoord2f(0, 0);
            GL.glVertex2f(0, 0);
            GL.glTexCoord2f(1, 0);
            GL.glVertex2f(1, 0);
            GL.glTexCoord2f(1, 1);
            GL.glVertex2f(1, 1);
            GL.glTexCoord2f(0, 1);
            GL.glVertex2f(0, 1);
            GL.glEnd();
            GL.glDisable(3042);
            if (!qa)
                GL.SwapBuffers(dc);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return true;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            keys.Remove(e.KeyCode);
            base.OnKeyUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!alive)
                return;
            bool held = !keys.Add(e.KeyCode);
            var k = e.KeyCode;
            if (held)
                return;
            if (k == Keys.F11)
            {
                ToggleFull();
                return;
            }

            var sc = game.screen;
            if (sc == Screen.Play)
            {
                if (k == Keys.E)
                    game.E();
                else if (k == Keys.F)
                {
                    game.s.lit = !game.s.lit;
                    game.Sound("locker", .18f);
                }
                else if (k == Keys.Q)
                    game.Toss();
                else if (k == Keys.Space)
                    game.Listen();
                else if (k == Keys.C)
                    crouchToggle = !crouchToggle;
                else if (k == Keys.Tab || k == Keys.J || k == Keys.M)
                {
                    game.bookTab = k == Keys.J ? 1 : 0;
                    game.To(Screen.Book);
                }
                else if (k == Keys.Escape)
                {
                    game.To(Screen.Pause);
                    game.Save();
                }
                else if (k == Keys.F5)
                {
                    game.Save();
                    game.Msg("현재 위치를 저장했습니다.");
                }

                dirty = true;
                return;
            }

            int num = (k >= Keys.D1 && k <= Keys.D9) ? (int)k - (int)Keys.D1 : (k >= Keys.NumPad1 && k <= Keys.NumPad9) ? (int)k - (int)Keys.NumPad1 : -1;
            if (k == Keys.Escape || k == Keys.Tab && sc == Screen.Book)
            {
                Back();
                dirty = true;
                return;
            }

            if(sc==Screen.Echo && num>=0&&num<2){Act(540+num);return;}
            if(sc==Screen.Read && game.reader<3 && k==Keys.Z){Act(563);return;}
            if(sc==Screen.Read && game.reader<3 && (k==Keys.Left || k==Keys.Right)){game.inspectAngle+=k==Keys.Right?35:-35;dirty=true;return;}
            if(sc==Screen.Inference && num>=0 && num<3 && game.reasoning<2){Act(500+num);return;}
            if(sc==Screen.Talk && num==3 && game.part==0 && hud.buttons.Exists(h=>h.id==48)){Act(48);return;}
            if (sc == Screen.Field)
            {
                if (num >= 0 && num < game.trial.N)
                    Act(400 + num);
                else if (k == Keys.Enter)
                    Act(410);
                else if (k == Keys.Left)
                    Act(400);
                else if (k == Keys.Right)
                    Act(400 + Math.Min(1, game.trial.N - 1));
                dirty = true;
                return;
            }

            if (sc == Screen.Board)
            {
                if (num >= 0 && num < game.puzzle.Buttons)
                    Act(100 + num);
                else if (k == Keys.Enter)
                    Act(110);
                else if (k == Keys.Back || k == Keys.Delete)
                    Act(111);
                else if (k == Keys.Left || k == Keys.Right)
                    game.puzzle.sel = (game.puzzle.sel + (k == Keys.Left ? -1 : 1) + game.puzzle.Buttons) % game.puzzle.Buttons;
                else if (k == Keys.Up || k == Keys.Down || k == Keys.E || k == Keys.Space)
                {
                    game.puzzle.Hit(game.puzzle.sel, k == Keys.Down ? -1 : 1);
                    game.Sound("chime", .25f);
                }

                dirty = true;
                return;
            }

            if (sc == Screen.Date && game.part == 1 && game.archiveView < 0 && num >= 0 && num < 2)
            {
                Act(45 + num);
                return;
            }

            if (sc == Screen.Book && num >= 0 && num < 3)
            {
                Act(20 + num);
                return;
            }

            if (sc == Screen.Talk && game.part == 0 && num < 3 && num >= 0)
            {
                Act(40 + num);
                return;
            }

            if (k == Keys.Up || k == Keys.Down || k == Keys.Left || k == Keys.Right)
            {
                var list = hud.buttons.Where(h => h.active).Select(h => h.id).ToList();
                if (list.Count > 0)
                {
                    int i = list.IndexOf(hud.selected);
                    int d = k == Keys.Up || k == Keys.Left ? -1 : 1;
                    hud.selected = list[(i < 0 ? (d > 0 ? 0 : list.Count - 1) : (i + d + list.Count) % list.Count)];
                    dirty = true;
                }

                return;
            }

            if (k == Keys.Enter || k == Keys.E || k == Keys.Space)
            {
                if (hud.selected >= 0 && hud.buttons.Any(h => h.id == hud.selected && h.active))
                {
                    Act(hud.selected);
                    return;
                }

                if (sc == Screen.Title)
                    Act(hud.HasSave ? 1 : 2);
                else if (sc == Screen.Date || sc == Screen.End)
                    Act(sc == Screen.Date && game.part == 1 && game.archiveView < 0 ? 45 : 43);
                else if(sc==Screen.Echo || sc==Screen.Inference && game.reasoning<2)return;
                else if (sc == Screen.Talk)
                    Act(game.part == 0 ? 40 : 42);
                else if (sc == Screen.Confirm)
                    Act(10);
                else
                    Act(10);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if(inspecting && game.screen==Screen.Read && (e.Button&MouseButtons.Left)!=0){
                game.inspectAngle+=(e.X-inspectPoint.X)*.53f;game.inspectTilt=Math.Max(-65,Math.Min(65,game.inspectTilt+(e.Y-inspectPoint.Y)*.4f));
                inspectPoint=e.Location;return;
            }
            if (!alive || game.screen == Screen.Play)
                return;
            int i = hud.HitAt(e.X * (float)Hud.W / Math.Max(1, ClientSize.Width), e.Y * (float)Hud.H / Math.Max(1, ClientSize.Height));
            if (hud.hover != i)
            {
                hud.hover = i;
                dirty = true;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e){inspecting=false;base.OnMouseUp(e);}
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!alive)
                return;
            if(game.screen==Screen.Read&&game.reader<3&&e.Button==MouseButtons.Left&&(game.inspectZoom ? e.Y*630/Math.Max(1,ClientSize.Height)<550 : e.X*1120/Math.Max(1,ClientSize.Width)<576)){inspecting=true;inspectPoint=e.Location;return;}
            if (game.screen == Screen.Play)
            {
                if (e.Button == MouseButtons.Left)
                    game.E();
                return;
            }

            if (dirty || paintVersion != game.revision)
            {
                hud.Paint(game);
                dirty = true;
            }

            int id = hud.HitAt(e.X * (float)Hud.W / ClientSize.Width, e.Y * (float)Hud.H / ClientSize.Height);
            if (id >= 0)
                Act(id);
        }

        void Back()
        {
            switch (game.screen)
            {
                case Screen.Confirm:
                case Screen.Gallery:
                    game.To(Screen.Title);
                    break;
                case Screen.Settings:
                    game.To(game.back);
                    break;
                case Screen.End:
                case Screen.Date:
                    if(game.screen==Screen.Date && game.part==1 && game.archiveView<0)break;
                    Act(43);
                    break;
                case Screen.Read:
                    if(game.inspectZoom){game.inspectZoom=false;game.revision++;break;}
                    if(game.reviewing)
                    {
                        game.reviewing=false;
                        game.bookTab=1;
                        game.To(Screen.Book);
                    }
                    else game.To(Screen.Play);
                    break;
                case Screen.Caught:
                    game.Recover();
                    break;
                case Screen.Title:
                    break;
                default:
                    game.To(Screen.Play);
                    break;
            }
        }

        void Act(int id)
        {
            hud.selected = -1;
            dirty = true;
            game.Sound("cloth", .23f);
            if (id == 1 && current != null)
            {
                game.Resume(current);
                crouchToggle = false;
            }
            else if (id == 2)
            {
                if (hud.HasSave)
                    game.To(Screen.Confirm);
                else
                    New();
            }
            else if (id == 3)
            {
                game.To(Screen.Gallery);
            }
            else if (id == 4)
            {
                game.back = game.screen;
                game.To(Screen.Settings);
            }
            else if (id == 5)
                Close();
            else if (id == 10)
                Back();
            else if (id >= 20 && id <= 22)
            {
                game.bookTab = id - 20;
                game.revision++;
            }
            else if (id >= 40 && id <= 42)
            {
                if (id == 42 || game.part != 0)
                    game.To(Screen.Play);
                else
                    game.ChooseTalk(id - 40);
            }
            else if(id==563 && game.screen==Screen.Read && game.reader>=0 && game.reader<3)
            {
                game.inspectZoom=!game.inspectZoom;
                game.revision++;
            }
            else if(id>=560 && id<=562)game.ReadAgain(id-560);
            else if(id>=570 && id<=574 && game.screen==Screen.Book && game.bookTab==0 && game.LastHall && game.opts.hints && game.EndOK(id-570))
            {
                game.guideExit=id-570;
                game.revision++;
            }
            else if(id==575 && game.screen==Screen.Book && game.bookTab==0 && game.WitnessChoice && game.opts.hints)
            {
                game.guideByRecords=!game.guideByRecords;
                game.revision++;
            }
            else if(id==540||id==541)game.AnswerEcho(id-540);
            else if(id>=500&&id<503)game.Interpret(id-500);
            else if(id==48)game.HearWitness();
            else if (id == 45 || id == 46)
                game.DateAnswer(id - 45);
            else if (id == 47)
                game.ChooseTalk(1);
            else if (id == 43)
            {
                if (game.screen == Screen.Date)
                    game.DateNext();
                else
                {
                    game.part++;
                    if (game.part >= 3)
                        game.To(game.archiveView >= 0 ? Screen.Gallery : Screen.Title);
                }
            }
            else if (id == 60)
            {
                game.Save();
                game.Msg("현재 위치를 저장했습니다.");
            }
            else if (id == 61)
            {
                game.Save();
                game.To(Screen.Title);
            }
            else if (id == 70)
                New();
            else if (id >= 100 && id < 106)
            {
                game.puzzle.sel = id - 100;
                game.puzzle.Hit(id - 100, 1);
                game.Sound(game.puzzle.game == 6 ? "note" + (id - 100) : "chime", .4f);
            }
            else if (id == 110)
                game.Solve();
            else if (id == 111)
                game.puzzle.Clear();
            else if (id >= 200 && id < 209)
            {
                game.archiveView = id - 200;
                game.part = 0;
                game.To(Screen.Date);
            }
            else if (id >= 220 && id < 225)
            {
                game.archiveView = id - 220;
                game.endingView = id - 220;
                game.part = 0;
                game.To(Screen.End);
            }
            else if (id >= 400 && id < 404)
            {
                game.trial.Hit(id - 400);
                game.Sound(game.trial.mode == 2 ? "note" + (id - 400) : "chime", .4f);
            }
            else if (id == 410)
                game.FieldConfirm();
            else if (id >= 300 && id < 310)
            {
                int row = (id - 300) / 2, delta = id % 2 == 0 ? -1 : 1;
                var o = game.opts;
                if (row == 0)
                    o.light = Math.Max(0, Math.Min(4, o.light + delta));
                if (row == 1)
                    o.volume = Math.Max(0, Math.Min(4, o.volume + delta));
                if (row == 2)
                    o.sens = Math.Max(0, Math.Min(4, o.sens + delta));
                if (row == 3)
                    o.quality = 1 - o.quality;
                if (row == 4)
                    o.hints = !o.hints;
                game.SaveOpts();
            }
        }

        void New()
        {
            crouchToggle = false;
            game.archiveView = -1;
            game.Fresh(hud.HasSave ? unchecked(Environment.TickCount ^ Guid.NewGuid().GetHashCode()) : 20021019);
        }

        void ToggleFull()
        {
            if (!fullscreen)
            {
                windowed = Bounds;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = System.Windows.Forms.Screen.FromControl(this).Bounds;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                Bounds = windowed;
            }

            fullscreen = !fullscreen;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (alive && !qa && game.screen != Screen.Title && game.screen != Screen.Confirm && game.screen != Screen.Gallery && (game.screen != Screen.Settings || game.back != Screen.Title) && game.archiveView < 0)
                game.Save();
            closing = true;
            Lock(false);
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            alive = false;
            Application.Idle -= Idle;
            if (audio != null)
                audio.Dispose();
            if (campus != null)
                campus.Dispose();
            if (hud != null)
                hud.Dispose();
            if (rc != IntPtr.Zero)
            {
                GL.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
                GL.wglDeleteContext(rc);
                GL.ReleaseDC(Handle, dc);
            }

            base.OnFormClosed(e);
        }

        void Shot(string name)
        {
            dirty = true;
            Frame();
            int width = ClientSize.Width, height = ClientSize.Height;
            using (var b = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                var data = b.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                GL.glReadPixels(0, 0, width, height, 0x80E1, 5121, data.Scan0);
                b.UnlockBits(data);
                b.RotateFlip(RotateFlipType.RotateNoneFlipY);
                b.Save(Path.Combine(checkPath, name + ".png"), ImageFormat.Png);
            }
        }

        void Key(Keys k)
        {
            OnKeyDown(new KeyEventArgs(k));
            OnKeyUp(new KeyEventArgs(k));
        }

        string TimeScene(string label,int count,bool advance)
        {
            var ms=new double[count];
            for(int i=-12;i<count;i++){
                var timer=Stopwatch.StartNew();
                if(advance){game.Tick(1f/60,0,0,false,false);game.yaw+=.0013f;}
                else game.t+=1f/60;
                Frame();GL.glFinish();timer.Stop();if(i>=0)ms[i]=timer.Elapsed.TotalMilliseconds;
            }
            Array.Sort(ms);
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}, {1}x{2}, {3} frames, median {4:F2} ms / p95 {5:F2} ms / slowest {6:F2} ms",label,ClientSize.Width,ClientSize.Height,count,ms[count/2],ms[(int)Math.Ceiling(count*.95)-1],ms[count-1]);
        }

        void CheckRender()
        {
            try
            {
                Directory.CreateDirectory(checkPath);
                hud.HasSave = false;
                Shot("title");
                Key(Keys.Enter);
                Shot("intro");
                Key(Keys.Enter);
                game.Focus();
                Shot("hall");
                Key(Keys.Tab);
                Shot("map");
                Key(Keys.Escape);
                Room room = game.w.rooms[0];
                V rp = game.w.Center(room.z * game.w.W + room.x);
                game.player = new V(rp.X - .3f, rp.Z - World.Unit * .89f);
                game.yaw = .12f;
                game.pitch = -.025f;
                var student = game.w.actors[0];
                student.x = rp.X - .85f;
                student.z = rp.Z + .35f;
                student.yaw = (float)Math.PI;
                game.Focus();
                Shot("classroom");
                game.player = new V(student.x, student.z - 1.38f);
                game.yaw = 0;
                Key(Keys.E);
                if (game.screen != Screen.Talk)
                    throw new Exception("normal input E could not start conversation");
                Shot("conversation");
                Act(40);
                Act(42);
                game.player = new V(game.w.things[0].p.X, game.w.things[0].p.Z - 1.38f);
                game.yaw = 0;
                Key(Keys.E);
                if (game.screen != Screen.Read)
                    throw new Exception("normal E did not pick up a record");
                Shot("record");float before=game.inspectAngle;Key(Keys.Right);
                if(game.inspectAngle==before)throw new Exception("exhibit did not rotate");
                Key(Keys.Enter);
                Key(Keys.J);Shot("kept-records");
                if(game.screen!=Screen.Book || !hud.buttons.Any(h=>h.id==560&&h.active))throw new Exception("missing reread action");
                int savedNotes=game.s.notes;
                var box=hud.buttons.First(h=>h.id==560).r;
                OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,(int)((box.X+box.Width/2)*ClientSize.Width/Hud.W),(int)((box.Y+box.Height/2)*ClientSize.Height/Hud.H),0));
                if(game.screen!=Screen.Read||!game.reviewing)throw new Exception("journal mouse pick");
                before=game.inspectAngle;Key(Keys.Left);Key(Keys.Enter);
                if(game.screen!=Screen.Book||game.bookTab!=1||game.s.notes!=savedNotes||game.inspectAngle==before)throw new Exception("journal inspection state");
                Key(Keys.Escape);
                Key(Keys.F);
                Key(Keys.Q);
                Key(Keys.Space);
                Key(Keys.Tab);
                Key(Keys.D3);
                Shot("relationships");
                Key(Keys.Escape);
                game.s.side = 5;
                game.Build();
                game.archiveView = -1;
                game.part = 0;
                game.To(Screen.Date);
                Shot("date");
                Key(Keys.Enter);
                Key(Keys.D2);
                if (game.part != 2 || (game.s.answers & (1 << 5)) == 0)
                    throw new Exception("private choice");
                game.s.side = -1;
                game.s.stage = 24;game.Build();game.To(Screen.Play);game.s.notes=7;
                game.Interact(game.w.things.First(o=>o.kind==Kind.Board));
                Shot("case");int right=Enumerable.Range(0,3).First(i=>game.SlideOption(i)==0);
                Key((Keys)((int)Keys.D1+right));Key((Keys)((int)Keys.D1+game.Case.evidence));
                if(!game.Ready || game.reasoning!=2)throw new Exception("case evidence hotkeys");
                Key(Keys.Enter);game.s.stage=22;game.s.solved=false;game.s.fieldDone=0;game.Build();game.To(Screen.Play);
                game.Interact(game.w.things.First(o=>o.kind==Kind.Board));Shot("board");QA.Solve(game.puzzle);Act(110);
                game.Interact(game.w.things.First(o=>o.kind==Kind.Station));Shot("field");Key(Keys.Escape);
                for(int i=0;i<3;i++){game.Talk(i);Shot("role"+i);game.To(Screen.Play);}
                game.s.stage=4;game.s.side=-1;game.s.solved=true;game.s.fieldDone=2;game.Build();game.To(Screen.Play);
                game.Interact(game.w.things.First(o=>o.kind==Kind.Door));
                if(game.screen!=Screen.Echo)throw new Exception("chapter memory did not open");
                game.t+=1.67f;Shot("witness");Key(Keys.D2);
                if(game.s.stage!=5 || (game.s.privateRecord&1)==0)throw new Exception("chapter decision hotkey");
                game.s.stage=3;game.s.side=-1;game.s.notes=0;game.s.solved=false;game.s.fieldDone=0;game.Build();
                game.Interact(game.w.things.First(o=>o.kind==Kind.Note&&o.tag==2));Key(Keys.Z);
                if(!game.inspectZoom)throw new Exception("film loupe key");
                Shot("film-contact");Key(Keys.Enter);
                game.s.stage=33;game.Build();game.Interact(game.w.things.First(o=>o.kind==Kind.Note&&o.tag==2));
                Key(Keys.Z);Shot("film-gate");Key(Keys.Enter);
                game.s.stage=20;game.Build();game.To(Screen.Play);
                var walker=game.w.actors[3];walker.x=game.player.X+2.9f;walker.z=game.player.Z;walker.yaw=-(float)Math.PI/2;game.yaw=(float)Math.PI/2;
                Shot("patrol");var second=game.w.actors[4];second.x=walker.x;second.z=walker.z;second.yaw=walker.yaw;walker.x+=22;Shot("keys");
                bool audioOK;
                using (var snd = new Audio(true))
                {
                    audioOK = snd.Available;
                    Thread.Sleep(90);
                }

                game.s.stage=0;game.s.side=-1;game.s.solved=false;game.s.notes=0;game.s.fieldDone=0;game.Build();game.To(Screen.Play);
                var times=new double[180];
                for(int i=-16;i<times.Length;i++){
                    var timer=Stopwatch.StartNew();game.Tick(1f/60,i<55?.27f:0,0,false,false);
                    game.yaw+=.008f;Frame();GL.glFinish();timer.Stop();if(i>=0)times[i]=timer.Elapsed.TotalMilliseconds;
                }
                Array.Sort(times);
                File.WriteAllText(Path.Combine(checkPath,"render-pass.txt"),string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "GPU {0} | GLSL {1} | sound device {2} | {3}x{4} | synchronized 180 first-sector frames, no vertical sync, median {5:F2} ms, p95 {6:F2} ms, worst {7:F2} ms | school triangles {8}. Real form input for conversations, pickups, journal reinspection, rotation, case evidence, choices, fields.",campus.renderer,GL.Shader,audioOK,ClientSize.Width,ClientSize.Height,times[89],times[170],times[179],campus.triangles),System.Text.Encoding.UTF8);
                ClientSize=new Size(1920,1080);
                game.s.stage=34;game.s.notes=0;game.s.solved=false;game.s.fieldDone=0;game.Build();game.opts.quality=1;
                var roomView=game.w.rooms.FirstOrDefault(o=>o.tag==0)??game.w.rooms[0];
                var center=game.w.Center(roomView.z*game.w.W+roomView.x);
                game.player=new V(center.X,center.Z-3f);game.yaw=0;game.pitch=0;
                for(int a=0;a<game.w.actors.Count;a++){var actor=game.w.actors[a];actor.x=center.X+(a-2)*.79f;actor.z=center.Z+1.1f+Math.Abs(a-2)*.5f;actor.yaw=(float)Math.PI;actor.walking=true;actor.step=a*.82f;}
                game.To(Screen.Play);game.Focus();
                string crowd=TimeScene("occupied hall, 3 students and 2 patrols in view",180,false);
                game.Talk(1);string close=TimeScene("dialogue with facial animation",120,false);
                game.To(Screen.Play);game.opts.quality=0;
                string low=TimeScene("occupied hall, reduced draw distance",120,false);
                game.To(Screen.Echo);game.sceneStart=game.t;
                string memory=TimeScene("chapter scene, two actors",90,false);
                File.AppendAllLines(Path.Combine(checkPath,"render-pass.txt"),new[]{Environment.NewLine+crowd,close,low,memory},System.Text.Encoding.UTF8);
                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                Environment.ExitCode = 1;
                File.WriteAllText(Path.Combine(checkPath, "render-error.txt"), e.ToString());
            }

            Close();
        }
    }

    static class Entry
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 2 && args[0] == "--verify")
                {
                    QA.Stats(args[1]);
                    return 0;
                }

                if (args.Length == 2 && args[0] == "--walk-check")
                {
                    Book.Init();
                    WalkCheck.Run(args[1]);
                    return 0;
                }

                Book.Init();
                try
                {
                    SetProcessDPIAware();
                }
                catch
                {
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new GameForm(args.Length == 2 && args[0] == "--render-check" ? args[1] : null));
                return Environment.ExitCode;
            }
            catch (Exception e)
            {
                if (args.Length == 2 && (args[0] == "--verify" || args[0] == "--walk-check"))
                    File.WriteAllText(args[1], e.ToString());
                else
                    MessageBox.Show(e.ToString(), "결번");
                return 1;
            }
        }
    }
}