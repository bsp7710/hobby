using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Uncalled
{
    public sealed class Deposition
    {
        public int evidence;
        public string question, finding;
        public string[] interpretations;
        static Dictionary<int,Deposition> file;
        public static Deposition For(int stage)
        {
            if(file==null)
            {
                var index=new Dictionary<int,Deposition>();
                using(var reader=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("cases.txt"),Encoding.UTF8))
                {
                    string line;
                    while((line=reader.ReadLine())!=null)
                    {
                        if(line.Length==0||line[0]=='#')continue;
                        string[] p=line.Split('|');
                        var d=new Deposition{evidence=int.Parse(p[1]),question=p[2],interpretations=new[]{p[3],p[4],p[5]},finding=p[6]};
                        index.Add(int.Parse(p[0]),d);
                    }
                }
                file=index;
            }
            Deposition value;return file.TryGetValue(stage,out value)?value:null;
        }
    }

    public partial class Play
    {
        public bool Revised {get{return s!=null && s.edition>=2;}}
        public int Assignment {get{return !Revised?-1:s.side>=0?5:s.stage%5;}}
        public int Witness {get{return s.side>=0?s.side/3:new[]{1,0,0,2,1,0,1,0}[s.stage/5];}}
        public bool Ready {get{return s.solved && s.fieldDone>=2;}}
        public Deposition Case {get{return Deposition.For(s.stage);}}
        public int reasoning, mistaken=-1;
        static readonly string[] accounts={
            "내가 손을 흔들었다고 사진 밑에 썼어. 다시 보니까 손바닥이 아니라 손등이 렌즈를 향하고 있더라.",
            "솜을 빌린 건 나야. 녹음하는 동안 문고리가 달그락거렸거든. 그날 보건실에 아픈 사람은 없었어.",
            "우리가 걸레를 짠 건 금요일이었어. 토요일이라고 들리는 방송, 그건 하루 전 녹음을 튼 거야.",
            "연우가 먼저 간 날에도 난 물을 네 잔 따랐어. 그날은 나 대신 가게 문을 닫아 줬거든.",
            "책등을 다시 붙이다가 이름까지 뜯어낸 적이 있어. 종이가 떨어지기 전에 찍어 두면 아래 글씨도 남아.",
            "돈이 일찍 끊긴 게 아니야. 부록이 빠져서 우리 반이 예산에 안 잡혔어. 본문만 들고 갔던 건 나였고.",
            "우산은 내가 들고 있었던 게 아니야. 찍는 동안 운동장 밖에 선 사람은 얼굴이 안 보였어.",
            "남았다고 해서 또 쓰려고 하진 말자. 연우가 이 테이프는 딱 한 번만 틀어 달랬어."
        };
        public string Testimony {get{return s.side>=0?Book.Dates[s.side/3][s.side%3][0].Split('|')[0]:accounts[s.stage/5];}}
        public int SlideOption(int displayed){return (displayed+(int)((uint)w.seed%3))%3;}
        public string Objective()
        {
            if(LastHall)return "졸업 복도   " + EndDoor[guideExit] + " · 다른 열린 문은 약도에서 확인하세요.";
            if(Ready)return "출구   "+w.page.door+" · "+w.rooms[w.reqRooms[4]].Name;
            if(Assignment==1)return "현장 복구   "+w.rooms[w.reqRooms[5+s.fieldDone]].Name+" · "+(s.fieldDone+1)+"/2";
            if(Assignment==5)return s.notes==0?"약속의 물건을 한 점 찾아보세요.":Book.Names[Witness]+"에게 물건을 건네세요.";
            if(Assignment==2 && s.solved)return w.rooms[w.reqRooms[5]].Name+"에서 마지막 수신 상태를 확인하세요.";
            if(Assignment==2)return s.notes==7?w.page.panel+"을 조작하세요.":"회로를 복구할 기록 "+Count(s.notes)+"/3";
            if(Assignment>=0 && Case!=null){
                if((s.notes&(1<<Case.evidence))==0)return w.page.items[Case.evidence]+" · "+w.rooms[w.reqRooms[Case.evidence]].Name;
                if(Assignment==3&&!s.witnessed&&Count(s.notes)<3)return Book.Names[Witness]+"의 증언을 듣거나 나머지 두 기록을 찾으세요.";
                if(Assignment!=3 && Count(s.notes)<2)return "다른 기록 한 점을 더 찾아 대조하세요.";
                return w.page.panel+"에서 두 기록을 대조하세요.";
            }
            return !s.solved?"현장 기록 "+Count(s.notes)+" / 3":"현장 인계 "+s.fieldDone+" / 2";
        }
        void OpenWay()
        {
            s.solved=true;s.fieldDone=2;Sound("unlock");Save();revision++;
        }
        public bool TakeCase(Thing o)
        {
            if(!Revised)return false;
            if(o.kind==Kind.Final||o.kind==Kind.Door){
                if(!Ready){Msg(Objective());return true;}
                int e=o.kind==Kind.Final?o.tag:0;
                if(Assignment==4 && (s.wrote&(1<<(s.stage/5)))==0 && (s.stage<39||EndOK(e))){RememberAt(e);return true;}
                return false;
            }
            if(o.kind==Kind.Board){
                if(Ready||Assignment==1||Assignment==5){Msg(Objective());return true;}
                if(Assignment==2)return false;
                if((s.notes&(1<<Case.evidence))==0||(Assignment==3?(!s.witnessed&&Count(s.notes)<3):Count(s.notes)<2)){Msg(Objective());return true;}
                reasoning=0;mistaken=-1;To(Screen.Inference);return true;
            }
            if(o.kind==Kind.Station){
                if(Ready||Assignment!=1&&Assignment!=2){Msg(Objective());return true;}
                if(Assignment==2&&!s.solved||o.tag!=s.fieldDone){Msg(Objective());return true;}
                trial=new Trial(w,o.tag);To(Screen.Field);return true;
            }
            return false;
        }
        public void Interpret(int option)
        {
            if(screen!=Screen.Inference||option<0||option>2)return;
            if(reasoning==0){
                if(SlideOption(option)!=0){mistaken=option;Sound("wrong",.35f);revision++;return;}
                reasoning=1;mistaken=-1;Sound("paper",.55f);revision++;return;
            }
            if(reasoning!=1 || (s.notes&(1<<option))==0)return;
            if(option!=Case.evidence){mistaken=option;Sound("wrong",.35f);revision++;return;}
            reasoning=2;mistaken=-1;OpenWay();
        }
        public void HearWitness()
        {
            if(screen!=Screen.Talk||talk!=Witness||!Revised)return;
            if(Assignment==5 && s.notes==0){Msg("약속의 물건을 한 점 찾은 뒤에 건네세요.");return;}
            if(Assignment!=3&&Assignment!=5)return;
            s.witnessed=true;part=3;
            if(Assignment==5)OpenWay();
            else {Sound("paper");Save();}
            revision++;
        }
        void RingAt(V source)
        {
            // The building rings at the relay, giving both player and patrol a world-space source.
            Sound("bell",.67f);nightFade=5;decoyTime=5;decoy=source;
            foreach(var a in w.actors)if(a.who<0&&a.mode!=2){w.Go(a,w.Tile(source));a.wait=0;a.mode=1;a.cool=3;}
        }
    }
}

