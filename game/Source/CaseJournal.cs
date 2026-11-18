using System;
using System.Drawing;
namespace Uncalled
{
    public sealed partial class Hud
    {
        void Inference()
        {
            Grad(0,0,W,H,Color.FromArgb(227,16,26,27),Color.FromArgb(251,8,17,20),90);
            R(63,53,994,505,Color.FromArgb(252,211,202,173));
            R(70,61,981,489,Color.FromArgb(252,225,217,190));
            var ink=Color.FromArgb(52,53,43);var red=Color.FromArgb(130,65,49);
            L(104,130,1010,130,Color.FromArgb(112,112,94));
            Word("은람 야간부     /     교적 대조 기록",103,78,16,red,1);
            Right(Book.Times[g.s.stage/5]+"   ·   "+g.w.page.panel,1009,80,16,ink);
            var c=g.Case;
            if(g.reasoning==2){
                Word("기록이 맞물렸습니다",104,166,36,ink,2);
                Word(c.finding,105,239,24,ink,2,881,145);
                Word(g.Objective(),106,385,18,red,1,870,60);
                Btn(10,"수첩을 덮는다     ↵",105,464,278,48,true,true);return;
            }
            Word(g.reasoning==0?c.question:"이 판단을 뒷받침하는 물건은 무엇인가요?",103,154,26,ink,2,904,77);
            if(g.reasoning==1)Word(c.interpretations[0],105,227,16,red,0,899,53);
            if(g.reasoning==0 && g.Assignment==3 && g.s.witnessed)Word(Book.Names[g.Witness]+"   “"+g.Testimony+"”",105,223,15,ink,0,887,58);
            for(int k=0;k<3;k++){
                bool got=(g.s.notes&(1<<k))!=0;
                string label=g.reasoning==0?c.interpretations[g.SlideOption(k)]:g.w.page.items[k]+(got?"":"  ·  미수집");
                Btn(500+k,(k+1)+".   "+label,105,285+k*53,906,45,g.reasoning==0||got);
            }
            if(g.mistaken>=0)Word(g.reasoning==0?"기록과 어긋납니다.  "+g.w.page.notes[c.evidence]:"그 물건에는 이 판단을 뒷받침하는 흔적이 없습니다.",106,461,17,red,1,900,62);
            else Word("1–3    선택          Esc    현장으로",106,466,15,ink);
        }
    }
}

