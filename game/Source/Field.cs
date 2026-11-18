using System;
using System.Linq;

namespace Uncalled
{
    public class Trial
    {
        public int mode, knob, cursor, sequence, bits, step, orientation;
        public int desired, required, mask;
        public int[] keys = new int[4];
        public float time, exposure;
        public bool pressed, wrong;
        public string title, goal;
        public string[] words;
        public static string[] Dial =
        {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15"
        };
        public static string[] Solfege =
        {
            "도",
            "레",
            "미",
            "솔"
        };
        public static string[] Arrow =
        {
            "↑",
            "→",
            "↓",
            "←"
        };
        public static int Mode(World w, int leg)
        {
            if (w.side >= 0)
                return (new[]
                {
                    0,
                    1,
                    2
                }[w.side / 3] + leg * 3) % 6;
            return Modes[w.stage, leg];
        }

        static int[, ] Modes =
        {
            {
                3,
                0
            },
            {
                4,
                3
            },
            {
                0,
                2
            },
            {
                1,
                4
            },
            {
                5,
                3
            },
            {
                0,
                4
            },
            {
                2,
                5
            },
            {
                1,
                3
            },
            {
                4,
                5
            },
            {
                3,
                0
            },
            {
                4,
                5
            },
            {
                1,
                4
            },
            {
                4,
                3
            },
            {
                0,
                1
            },
            {
                3,
                4
            },
            {
                0,
                2
            },
            {
                1,
                4
            },
            {
                2,
                5
            },
            {
                4,
                1
            },
            {
                3,
                0
            },
            {
                5,
                3
            },
            {
                1,
                5
            },
            {
                0,
                4
            },
            {
                4,
                1
            },
            {
                2,
                0
            },
            {
                0,
                3
            },
            {
                1,
                5
            },
            {
                2,
                4
            },
            {
                4,
                3
            },
            {
                3,
                5
            },
            {
                0,
                5
            },
            {
                4,
                1
            },
            {
                0,
                2
            },
            {
                1,
                4
            },
            {
                2,
                3
            },
            {
                5,
                0
            },
            {
                3,
                1
            },
            {
                4,
                5
            },
            {
                0,
                2
            },
            {
                3,
                2
            }
        };
        public Trial(World w, int leg)
        {
            mode = Mode(w, leg);
            var rng = new Random(unchecked(w.seed * 7 + leg * 919));
            desired = 2 + rng.Next(12);
            required = rng.Next(1, 8);
            mask = required ^ 7;
            bits = mask;
            for (int i = 0; i < 4; i++)
                keys[i] = rng.Next(4);
            orientation = rng.Next(3) + 1;
            words = w.page.items;
            title = Names[mode];
            goal = new[]
            {
                "기준 파형과 수신 파형의 봉우리 수를 맞춘 뒤 신호를 고정하세요.",
                "셔터를 열고 노광계의 바늘이 금색 영역에 들면 닫으세요.",
                "악보의 네 음을 왼쪽에서 오른쪽 순서로 한 번씩 연주하세요.",
                "상단 작업표의 점등 순서에 맞춰 세 접점의 전원을 복구하세요.",
                "원판을 돌려 원본과 사본의 두 구멍이 각각 겹치게 하세요.",
                "흰 시침을 옮겨 정비표에 표시된 귀가 시각에 맞추세요."
            }[mode];
        }

        public static string[] Names =
        {
            "수신 파형 맞추기",
            "인화지 노광",
            "네 음의 응답",
            "접점 복구",
            "원본 겹쳐 보기",
            "시계 정렬"
        };
        public string Button(int i)
        {
            if (mode == 0 || mode == 5)
                return i == 0 ? "한 칸 뒤로" : "한 칸 앞으로";
            if (mode == 1)
                return pressed ? "셔터 닫기" : "셔터 열기";
            if (mode == 2)
                return Solfege[i];
            if (mode == 3)
                return ((bits & (1 << i)) > 0 ? "●   켜짐" : "○   꺼짐");
            return i == 0 ? "왼쪽 회전" : "오른쪽 회전";
        }

        public int N
        {
            get
            {
                return mode == 1 ? 1 : mode == 2 ? 4 : mode == 3 ? 3 : 2;
            }
        }

        public void Tick(float dt)
        {
            time += dt;
            if (pressed && mode == 1)
                exposure = Math.Min(1, exposure + dt * .21f);
        }

        public bool InBand
        {
            get
            {
                return Math.Abs(exposure - (.35f + desired * .024f)) <= .09f;
            }
        }

        public void Hit(int i)
        {
            if (i < 0 || i >= N)
                return;
            wrong = false;
            if (mode == 0 || mode == 5)
                knob = (knob + (i == 0 ? -1 : 1) + 16) % 16;
            else if (mode == 1)
            {
                if (pressed)
                {
                    pressed = false;
                }
                else
                {
                    pressed = true;
                    exposure = 0;
                }
            }
            else if (mode == 2)
            {
                if (step >= 4)
                    step = 0;
                if (keys[step] == i)
                    step++;
                else
                {
                    wrong = true;
                    step = 0;
                }
            }
            else if (mode == 3)
                bits ^= 1 << i;
            else
                cursor = (cursor + (i == 0 ? -1 : 1) + 4) % 4;
        }

        public bool Check()
        {
            bool yes = mode == 0 || mode == 5 ? knob == desired : mode == 1 ? !pressed && InBand : mode == 2 ? step == 4 : mode == 3 ? bits == required : cursor == orientation;
            wrong = !yes;
            return yes;
        }

        public string Clock(int k)
        {
            int min = 17 + k * 3;
            return (min / 60 + 10).ToString("00") + ":" + (min % 60).ToString("00");
        }
    }

    public static class FieldText
    {
        public static string[] Instructions =
        {
            "인계종을 돌려 현관 호출의 발신지를 찾아냅니다.",
            "칠판에서 굴러온 분필이 멈춘 자리를 두 기록에 대조합니다.",
            "녹음의 뒤쪽 채널을 잠깐 열어 숨소리를 분리합니다.",
            "두 장을 따로 인화하면 계단의 같은 흠집이 보입니다.",
            "시계 바늘과 교대표가 가리킨 순간을 다시 맞춥니다.",
            "녹음실 양쪽 끝의 채널을 살려 밤 수업만 꺼냅니다.",
            "한 사람이 쉬었을 때 다른 사람이 친 네 음을 되짚습니다.",
            "밀착 사진 두 장을 같은 노출로 맞춥니다.",
            "보건 일지의 대여 시각과 약품장의 기록을 확인합니다.",
            "뒷문을 닫던 자물쇠의 순서를 현장에서 되살립니다.",
            "두 주소가 가리키던 정류장의 귀가 시간을 대조합니다.",
            "감광판을 비추고 반사된 숫자를 바로 세웁니다.",
            "먹지에 남은 서명과 원부의 도장 자리를 포갭니다.",
            "녹음을 되감아 물이 흐른 원래 방향을 찾습니다.",
            "두 결재자의 손에서 넘어간 서류를 차례로 인계합니다.",
            "서윤이 읽은 문장 뒤의 숨을 원래 트랙으로 보냅니다.",
            "접힌 사진을 펴서 가린 사람의 그림자를 복원합니다.",
            "건반 양 끝에서 두 사람의 악절을 번갈아 잇습니다.",
            "식탁에 남은 네 번째 자리를 장부에 옮겨 놓습니다.",
            "전원을 살려 끝까지 가지 않았던 정정 방송을 송출합니다.",
            "당직이 걷는 순서를 시계와 순시판에 돌려놓습니다.",
            "각 창에 비친 마지막 주자의 그림자를 맞춥니다.",
            "보건실 전화가 걸려 왔던 수신 대역을 복원합니다.",
            "종이 위 먹물이 번지기 전 이름을 두 장에 비춰 봅니다.",
            "합창 뒤에 더해진 네 음을 강당 양쪽에 돌려줍니다.",
            "서윤이 정한 채널로 청취권을 본인에게 넘깁니다.",
            "노광 시간과 셔터가 닫힌 순간을 맞춥니다.",
            "두 의자에 남은 서로 다른 리듬을 살려냅니다.",
            "통계표에서 본인의 서명이 살아 있는 줄을 복원합니다.",
            "실습실의 두 개 회로를 열어 양쪽 문받침을 작동시킵니다.",
            "버스 안내와 교내 안내가 섞인 대역을 분리합니다.",
            "네 해의 우편물을 소인과 봉투 원본에 대조합니다.",
            "목소리를 원래 순서로 이어 대답할 자리를 만듭니다.",
            "우산 든 사람의 손이 가리킨 방향으로 인화를 맞춥니다.",
            "이름을 부르지 않아도 끝날 수 있는 예행 신호를 보냅니다.",
            "종탑 안팎에서 하교 시간이 다시 만나게 합니다.",
            "원래 먹물 색이 드러나도록 물과 빛의 순서를 맞춥니다.",
            "졸업장 위에 하나뿐인 본인 이름을 맞춥니다.",
            "세 곳의 목소리를 마지막 응답 채널에 모읍니다.",
            "마지막 종을 수동으로 인계한 뒤 열고 싶은 출구를 직접 고릅니다."
        };
        public static string Station(World w, int leg)
        {
            int m = Trial.Mode(w, leg);
            return new[]
            {
                "모니터 수신대",
                "인화 노광대",
                "응답 건반",
                "현장 접점함",
                "원본 투사대",
                "정비 시계"
            }[m];
        }
    }
}