using System;
using System.Drawing;
namespace Uncalled
{
    public static class AfterBell
    {
        public static readonly string[] Place={"숙직실 인계대","밤 운동장 뒷문","행정실 철제 서랍","빈 강당 무대","합주실 마지막 줄","야간부 실습동","정류장 옆 방송실","졸업식이 끝난 교실"};
        public static readonly string[] Witness={"한이솔","윤해진","강서윤","강서윤","한이솔","윤해진","강서윤","문연우의 편지"};
        public static readonly int[] Cast={2,1,0,0,2,1,0,2};
        public static readonly int[] Room={2,3,0,3,2,1,0,3};
        public static readonly string[] Line={
            "인계대에 내가 방금 쓴 사선이 두 개야. 누군가 다른 쪽에서 내 손을 따라 긋고 있어. 네가 보고 있는 쪽 종이는 어때?",
            "문을 닫은 사람만 찍혔어. 손잡이를 밀어 본 사람은 프레임 밖이야. 내가 사진 뒤에 쓴 설명부터 고칠게.",
            "서랍 안에서 카본지가 두 장 나왔어. 교무실에 걸린 한 장만 보면 연우가 스스로 결석한 것처럼 보여.",
            "내가 한 일을 대신 사과해 달라고 너를 여기 데려온 게 아니야. 대본을 읽는 동안, 내가 또 남의 이름을 읽으면 멈춰 줘.",
            "마지막 음을 칠 때마다 저 빈 의자가 뒤로 밀려. 사람이 일어나서 갈 수 있게, 이번에는 내가 먼저 비켜 볼게.",
            "한쪽 문에만 끼우면 다른 쪽에서 오는 사람이 또 갇혀. 난 이쪽으로 갈게. 반대편에서 네가 문 여는 소리를 들을게.",
            "스피커가 연우 목소리로 내 사과를 읽었어. 전원을 내리면 조용해질 텐데, 그 전에 내가 네 이름을 제대로 불러 보고 싶어.",
            "내 이름이 남아 있는 곳에서 나를 꺼내려고 애쓰지 않아도 돼. 나는 벌써 여기까지 걸어왔어. 네가 들고 나갈 건 무엇이야?"
        };
        public static readonly string[,] Answer={
            {"두 사선을 모두 읽어 들려준다","틀린 사선 위에 투명지를 덮는다"},
            {"뒷문을 열어 사진 속 간판을 확인한다","사진 뒷면의 설명을 해진에게 돌려준다"},
            {"먹지를 붙여 두 장의 이름을 함께 읽는다","연우 몫의 사본을 봉투에 넣는다"},
            {"빈 강당의 스피커까지 연결한다","부스 안에서 서윤의 말을 듣는다"},
            {"네 번째 박자를 운동장까지 보낸다","빈 의자 곁에서 울림이 잦아들길 기다린다"},
            {"반대편 문을 두드려 서로의 위치를 알린다","양쪽 문에 약속한 쐐기를 조용히 끼운다"},
            {"세 사람에게 돌아가며 마이크를 건넨다","각자 녹음한 말을 한 사람씩 돌려준다"},
            {"다시 읽을 수 있게 교무실에 정정본을 남긴다","직접 건넬 수 있게 봉투를 들고 나간다"}
        };
        public static readonly string[] Coda={
            "다음 날 교무실에는 두 줄이 나란히 남았습니다. 찾아온 사람은 지워진 자리까지 읽을 수 있었습니다.",
            "봉투를 받아 든 연우는 겉에 자기 글씨로 이름을 썼습니다. 이번에는 아무도 대신 열지 않았습니다."
        };
    }
    public partial class Play
    {
        public string RememberedLine(int who){
            int mask=(s.answered>>(who*3))&7;if(mask==0)return null;
            int level=(mask&4)!=0?2:(mask&2)!=0?1:0;
            bool second=(s.answers&(1<<(who*3+level)))!=0;
            string[][] callbacks={
                new[]{"그날 남기자던 빗소리, 테이프 틀 때마다 네가 먼저 생각나.","녹음한 우리 목소리, 오늘 혼자 있을 때 한 번 더 들었어.","네 옆에서 대본을 읽으니까 글자가 덜 떨리더라.","부스 밖에서 들어 주던 얼굴이 아직도 유리에 비치는 것 같아.","라벨에 날짜 하나 더 써도 될까? 우리 다음 만날 날.","‘반쪽 이어폰’이라고 적은 테이프, 아직 내 주머니에 있어."},
                new[]{"내가 고른 흔들린 사진, 걸을 때마다 가방에서 모서리가 닿아.","너랑 같이 골랐던 사진 있지. 그날 내 손이 참 편안해 보였어.","갑자기 셔터 눌렀을 때 내 표정, 누구한테도 안 보여 줬어.","내가 렌즈 밖을 볼 때까지 기다려 준 사람은 네가 처음이야.","우리 이름 둘 다 적은 사진, 안 구겨지게 잘 넣어 뒀지?","연우한테 줄 사진 봉투, 아까 비 안 맞는 칸에 옮겼어."},
                new[]{"틀린 음에서 한 마디 더 이어 간 거 기억해? 그게 후렴이 됐어.","내 박자 따라오던 네 손 기억나. 오늘은 내가 먼저 맞출게.","첫 마디 다시 치던 자리, 네 몫으로 반쯤 비워 놨어.","그날 어깨 빌려 줘서 고마워. 건반보다 그 온기가 오래 가더라.","다음 합주 날짜 빼 놨어. 네가 앉던 의자도 그대로야.","같이 쓰기로 한 다음 곡, 제목 칸부터 비워 뒀어."}
            };return callbacks[who][level*2+(second?1:0)];
        }
        public int echoDoor;
        public float sceneStart;
        public void RememberAt(int exit){
            echoDoor=exit;sceneStart=t;To(Screen.Echo);Sound("paper",.47f);
        }
        public void AnswerEcho(int answer){
            if(screen!=Screen.Echo||answer<0||answer>1)return;
            int act=s.stage/5,bit=1<<act;s.wrote|=bit;
            if(answer==1)s.privateRecord|=bit;else s.privateRecord&=~bit;
            Sound(answer==0?"broadcast":"paper",.5f);
            if(s.stage==39){
                if(opts.exitMemory==null || opts.exitMemory.Length!=5)opts.exitMemory=new int[5];
                opts.exitMemory[echoDoor]=answer+1;
            }
            Save();Exit(echoDoor);
        }
    }
    public sealed partial class Hud
    {
        void Echo(){
            int act=g.s.stage/5;
            Grad(0,290,W,340,Color.Transparent,Color.FromArgb(249,12,20,22),90);
            Word(AfterBell.Place[act],63,345,15,Gold,1);
            Word(AfterBell.Witness[act],60,372,30,White,2);
            Word(AfterBell.Line[act],63,424,22,White,0,999,94);
            Btn(540,"1   "+AfterBell.Answer[act,0],63,543,488,54,true,true);
            Btn(541,"2   "+AfterBell.Answer[act,1],565,543,493,54);
        }
    }
}

