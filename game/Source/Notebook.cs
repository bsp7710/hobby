using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Uncalled
{
    public partial class Play
    {
        public bool reviewing, guideByRecords, inspectZoom;
        public int guideExit;
        public bool LastHall { get { return s.stage == 39 && s.side < 0 && Ready; } }
        public bool WitnessChoice
        {
            get { return Assignment == 3 && !Ready && !s.witnessed && (s.notes & (1 << Case.evidence)) != 0 && Count(s.notes) < 3; }
        }

        public void ReadAgain(int index)
        {
            if (screen != Screen.Book || bookTab != 1 || index < 0 || index > 2 || (s.notes & (1 << index)) == 0)
                return;
            reader = index;
            reviewing = true;
            inspectZoom = false;
            inspectAngle = -12;
            inspectTilt = 5;
            Sound("paper", .48f);
            To(Screen.Read);
        }

        public bool MapDestination(out V position, out string label)
        {
            bool visitWitness = Assignment == 5 && s.notes != 0 || WitnessChoice && !guideByRecords;
            if (!Ready && visitWitness)
            {
                Actor witness = w.actors.First(a => a.who == Witness);
                position = witness.Pos;
                label = Book.Names[Witness] + (Assignment == 5 ? "에게 물건 건네기" : " 만나기");
                return true;
            }

            Kind kind;
            int tag = -1;
            if (Ready)
            {
                int exit = LastHall && EndOK(guideExit) ? guideExit : 0;
                kind = exit == 0 ? Kind.Door : Kind.Final;
                tag = exit;
            }
            else if (Assignment == 1 || s.solved)
            {
                kind = Kind.Station;
                tag = s.fieldDone;
            }
            else if (Assignment == 5 || Assignment == 2 || !Revised)
                kind = s.notes == 7 ? Kind.Board : Kind.Note;
            else
            {
                int required = Case.evidence;
                if ((s.notes & (1 << required)) == 0) { kind = Kind.Note; tag = required; }
                else if (WitnessChoice || Assignment != 3 && Count(s.notes) < 2) kind = Kind.Note;
                else kind = Kind.Board;
            }

            var choices = w.things.Where(o => o.kind == kind && (tag < 0 || tag == o.tag));
            if (kind == Kind.Note) choices = choices.Where(o => (s.notes & (1 << o.tag)) == 0);
            var target = choices.OrderBy(o => player.Dist(o.p)).FirstOrDefault();
            if (target == null) { position = player; label = ""; return false; }
            position = target.p;
            switch (kind)
            {
                case Kind.Note: label = w.page.items[target.tag]; break;
                case Kind.Station: label = FieldText.Station(w, target.tag); break;
                case Kind.Board: label = w.page.panel; break;
                default: label = LastHall ? EndDoor[target.tag] : w.page.door; break;
            }
            return true;
        }
    }

    public sealed partial class Hud
    {
        World guideWorld;
        V guideOrigin, guideTarget;
        V[] guideLine;

        void DrawRoute(float ox, float oy, float scale)
        {
            V target;
            string label;
            if (!g.opts.hints || !g.MapDestination(out target, out label)) return;
            if (guideWorld != g.w || guideOrigin.Dist(g.player) > .01f || guideTarget.Dist(target) > .01f)
            {
                guideWorld = g.w;
                guideOrigin = g.player;
                guideTarget = target;
                guideLine = g.w.GuideRoute(guideOrigin, target);
            }

            Color mint = Color.FromArgb(222, 160, 224, 194);
            float unit = scale / World.Unit;
            using (var pen = new Pen(mint, 2.1f))
            {
                pen.DashStyle = DashStyle.Dot;
                pen.DashCap = DashCap.Round;
                if (guideLine.Length > 0)
                {
                    var points = new PointF[guideLine.Length + 1];
                    points[0] = new PointF(ox + guideOrigin.X * unit, oy + guideOrigin.Z * unit);
                    for (int i = 0; i < guideLine.Length; i++)
                        points[i + 1] = new PointF(ox + guideLine[i].X * unit, oy + guideLine[i].Z * unit);
                    d.DrawLines(pen, points);
                }
                pen.DashStyle = DashStyle.Solid;
                d.DrawEllipse(pen, ox + target.X * unit - 6.8f, oy + target.Z * unit - 6.8f, 13.6f, 13.6f);
            }
            Word("연필 표시   " + label, 76, 551, 14, mint, 1, 549, 26);
        }

        void ExitDirectory()
        {
            Word("졸업 복도에서 열 수 있는 문", 655, 185, 19, White, 2, 410, 36);
            for (int i = 0; i < Play.EndDoor.Length; i++)
            {
                bool open = g.EndOK(i);
                string info = open ? (g.opts.hints && g.guideExit == i ? "약도에 표시됨" : "열림") :
                    i == 4 ? "교적 원본 " + Play.Count(g.s.proof) + " / 8" : Book.Names[i - 1] + "의 세 번째 약속과 대화 3회";
                Btn(570 + i, Play.EndDoor[i] + "   ·   " + info, 655, 232 + i * 55, 400, 45, open && g.opts.hints, open && g.opts.hints && g.guideExit == i);
            }
            Word(g.opts.hints ? "열린 문을 선택해 위치를 표시합니다. 실제로 문을 열면 결말이 결정됩니다." : "문 앞에서 직접 열면 결말이 결정됩니다. 설정에서 약도 안내를 켤 수 있습니다.", 657, 515, 14, Mute, 0, 396, 62);
        }
    }
}
