using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Uncalled
{
    public sealed class Audio : IDisposable
    {
        const int Rate = 22050, Frames = 768;
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        struct Format
        {
            public ushort tag, channels;
            public uint rate, bytes;
            public ushort align, bits, extra;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Header
        {
            public IntPtr data;
            public uint length, recorded;
            public UIntPtr user;
            public uint flags, loops;
            public IntPtr next;
            public UIntPtr reserved;
        }

        [DllImport("winmm.dll")]
        static extern int waveOutOpen(out IntPtr device, uint which, ref Format fmt, IntPtr callback, IntPtr data, uint flags);
        [DllImport("winmm.dll")]
        static extern int waveOutPrepareHeader(IntPtr d, IntPtr h, int size);
        [DllImport("winmm.dll")]
        static extern int waveOutWrite(IntPtr d, IntPtr h, int size);
        [DllImport("winmm.dll")]
        static extern int waveOutUnprepareHeader(IntPtr d, IntPtr h, int size);
        [DllImport("winmm.dll")]
        static extern int waveOutReset(IntPtr d);
        [DllImport("winmm.dll")]
        static extern int waveOutClose(IntPtr d);
        class Voice
        {
            public float[] data;
            public int p;
            public float left, right;
        }

        Dictionary<string, float[]> bank = new Dictionary<string, float[]>();
        List<Voice> live = new List<Voice>();
        Queue<Voice> waiting = new Queue<Voice>();
        object gate = new object ();
        IntPtr device;
        Thread worker;
        volatile bool stop;
        public volatile float volume = .6f, pressure = 0;
        public volatile int mood;
        public volatile float enclosure, weather;
        public volatile int cue;
        readonly Score score=new Score();
        float[] delayL=new float[3169],delayR=new float[3967];
        int echoL,echoR;float air,drip;
        public bool Available
        {
            get
            {
                return device != IntPtr.Zero;
            }
        }

        long clock;
        uint white = 912347;
        float filtered;
        public Audio(bool silent = false)
        {
            if (silent)
                volume = 0;
            string[] sounds =
            {
                "step",
                "cloth",
                "stamp",
                "keys",
                "paper",
                "bell",
                "chalk",
                "chime",
                "wrong",
                "unlock",
                "locker",
                "alarm",
                "catch",
                "end",
                "note0",
                "note1",
                "note2",
                "note3",
                "note4","shutter","broadcast","hallstep","woodstep","coveredStamp","coveredKeys"
            };
            for (int k = 0; k < sounds.Length; k++)
                bank[sounds[k]] = Generate(sounds[k], k);
            var f = new Format
            {
                tag = 1,
                channels = 2,
                rate = Rate,
                bytes = Rate * 4,
                align = 4,
                bits = 16
            };
            try
            {
                if (waveOutOpen(out device, 0xffffffff, ref f, IntPtr.Zero, IntPtr.Zero, 0) == 0)
                {
                    worker = new Thread(Loop);
                    worker.IsBackground = true;
                    worker.Name = "Eunram · stereo";
                    worker.Start();
                }
            }
            catch
            {
                device = IntPtr.Zero;
            }
        }

        static float[] Generate(string name, int k)
        {
            bool muffled=name=="coveredStamp"||name=="coveredKeys";
            if(muffled)name=name=="coveredStamp"?"stamp":"keys";
            bool metal = name == "keys" || name == "bell" || name == "chime" || name == "end" || name.StartsWith("note");
            double dur = metal ? (name == "bell" || name == "end" ? 2.4 : .82) : name == "catch" ? 1.1 : name == "alarm" ? .75 : name == "chalk" ? .71 : .36;
            float[] a = new float[(int)(dur * Rate)];
            Random r = new Random(314 + k);
            double old = 0, low=0;
            for (int i = 0; i < a.Length; i++)
            {
                double t = i / (double)Rate, v = 0, white = r.NextDouble() * 2 - 1;
                old = .79 * old + .21 * white;
                double pi = Math.PI * 2;
                if(name=="shutter"){
                    v=(white*.45+Math.Sin(t*pi*980)*.30)*Math.Exp(-t*145);
                    if(t>.082)v+=(old*.93+Math.Sin((t-.082)*pi*350)*.30)*Math.Exp(-(t-.082)*61);
                }else if(name=="broadcast"){
                    v=(white-old)*.49*Math.Exp(-t*24)+Math.Sin(t*pi*491)*.09*Math.Exp(-t*8);
                }else if(name=="woodstep"||name=="hallstep"){
                    double f=name=="woodstep"?99:155;double heel=Math.Sin(t*pi*(f+51*Math.Exp(-t*22)))*Math.Exp(-t*21)*.59;
                    double sole=t>.069?(old*1.12+Math.Sin((t-.069)*pi*f*.69)*.17)*Math.Exp(-(t-.069)*32):0;
                    v=heel+sole+(white-old)*Math.Exp(-t*87)*.15;
                }else if (metal)
                {
                    double f = name.StartsWith("note") ? new[]
                    {
                        261.63,
                        293.66,
                        329.63,
                        392.0,
                        440.0
                    }[int.Parse(name.Substring(4))] : name == "bell" ? 349.23 : name == "keys" ? 1667 : name == "end" ? 392 : 660;
                    v = (Math.Sin(t * pi * f) * .53 + Math.Sin(t * pi * f * 2.011) * .21 + Math.Sin(t * pi * f * 3.94) * .14) * Math.Exp(-t * (name == "keys" ? 11 : 2.6));
                    if (name == "bell" && t > .46)
                        v += .42 * Math.Sin((t - .46) * pi * 261.63) * Math.Exp(-(t - .46) * 2.7);
                }
                else if (name == "stamp" || name == "step" || name == "locker")
                {
                    double f = name == "stamp" ? 61 : name == "locker" ? 90 : 112;
                    v = (Math.Sin(pi * (f * t + 2.9 * (1 - Math.Exp(-t * 22)))) * .52 + old * 1.4) * Math.Exp(-t * (name == "stamp" ? 13 : 22));
                    if (t > .10 && name == "stamp")
                        v += .25 * Math.Sin((t - .10) * pi * 156) * Math.Exp(-(t - .10) * 25);
                }
                else if (name == "chalk")
                {
                    double mod = t < .16 ? t : t < .34 ? t - .16 : t - .34;
                    v = (white * .55 + Math.Sin(pi * 1520 * mod) * .26) * Math.Exp(-mod * 49) * Math.Exp(-t * 3);
                }
                else if (name == "paper" || name == "cloth")
                {
                    v = white * Math.Exp(-t * 13) * (.1 + .45 * Math.Pow(Math.Sin(t * 38), 2));
                }
                else if (name == "wrong" || name == "unlock")
                {
                    v = (.4 * Math.Sin(pi * (name == "wrong" ? 158 : 417) * t) + .24 * Math.Sin(pi * (name == "wrong" ? 166 : 623) * t)) * Math.Exp(-t * 8);
                }
                else
                    v = (.31 * Math.Sin(pi * (59 * t + 8 * t * t)) + .17 * Math.Sin(pi * 69 * t) + old * .8) * Math.Sin(Math.PI * t / dur);
                if(muffled){low+=.089*(v-low);v=.94*low;}
                a[i] = (float)(v * Math.Min(1, t * 140) * Math.Min(1, (dur - t) * 30) * .64);
            }

            return a;
        }

        public void Play(string name, float amplitude, float pan)
        {
            if (!Available || volume < .001f)
                return;
            float[] data;
            if (!bank.TryGetValue(name, out data))
                return;
            pan = Math.Max(-1, Math.Min(1, pan));
            lock (gate)
            {
                if (waiting.Count < 30)
                    waiting.Enqueue(new Voice { data = data, left = amplitude * (float)Math.Sqrt((1 - pan) * .5), right = amplitude * (float)Math.Sqrt((1 + pan) * .5) });
            }
        }

        void Fill(short[] buff)
        {
            lock (gate)
            {
                while (waiting.Count > 0)
                {
                    if (live.Count < 24)
                        live.Add(waiting.Dequeue());
                    else
                        waiting.Dequeue();
                }
            }

            float vol = volume, tension = pressure, reflections=enclosure, rainy=weather;
            int color = mood;
            score.Set(cue);
            for (int i = 0; i < Frames; i++)
            {
                double t = clock++ / (double)Rate;
                white = unchecked(1664525 * white + 1013904223);
                float n = ((white >> 10) / (float)(1 << 22) - .5f);
                filtered = filtered * .976f + n * .024f;
                double hum = (Math.Sin(t * 314.1593) + .34 * Math.Sin(t * 628.3185)) * .0065;
                double room = filtered * (color == 3 ? .22 : .42) + hum + (Math.Sin(t * 367.6) * Math.Sin(t * .27) * .0024);
                air+=.087f*(n-air);
                if((white&0x1fff)==17)drip=.027f;
                drip*=.9974f;
                room+=rainy*(air*.082f+drip*Math.Sin(t*8734));
                if (tension > .08f)
                {
                    double pulse = t % (1.16 - tension * .35);
                    room += Math.Sin(pulse * 281) * Math.Exp(-pulse * 19) * .13 * tension;
                }

                float l = (float)room, r = l;
                for (int j = live.Count - 1; j >= 0; j--)
                {
                    var v = live[j];
                    float sample = v.data[v.p++];
                    l += sample * v.left;
                    r += sample * v.right;
                    if (v.p >= v.data.Length)
                        live.RemoveAt(j);
                }

                score.Sample(ref l,ref r);
                float wetL=delayL[echoL],wetR=delayR[echoR];
                delayL[echoL]=(float)(l*.15)+wetR*.31f;delayR[echoR]=(float)(r*.15)+wetL*.28f;
                if(++echoL==delayL.Length)echoL=0;if(++echoR==delayR.Length)echoR=0;
                l+=wetL*reflections;r+=wetR*reflections;
                buff[i * 2] = (short)(Math.Max(-.98, Math.Min(.98, l * vol)) * 32767);
                buff[i * 2 + 1] = (short)(Math.Max(-.98, Math.Min(.98, r * vol)) * 32767);
            }
        }

        void Loop()
        {
            int sz = Marshal.SizeOf(typeof(Header));
            IntPtr[] headers = new IntPtr[4], blocks = new IntPtr[4];
            bool[] active = new bool[4];
            short[] samples = new short[Frames * 2];
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    blocks[i] = Marshal.AllocHGlobal(samples.Length * 2);
                    headers[i] = Marshal.AllocHGlobal(sz);
                    Marshal.StructureToPtr(new Header { data = blocks[i], length = (uint)(samples.Length * 2) }, headers[i], false);
                    waveOutPrepareHeader(device, headers[i], sz);
                }

                while (!stop)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (active[i] && ((((Header)Marshal.PtrToStructure(headers[i], typeof(Header))).flags & 1) == 0))
                            continue;
                        Fill(samples);
                        Marshal.Copy(samples, 0, blocks[i], samples.Length);
                        waveOutWrite(device, headers[i], sz);
                        active[i] = true;
                    }

                    Thread.Sleep(7);
                }
            }
            finally
            {
                waveOutReset(device);
                for (int i = 0; i < 4; i++)
                {
                    if (headers[i] != IntPtr.Zero)
                    {
                        waveOutUnprepareHeader(device, headers[i], sz);
                        Marshal.FreeHGlobal(headers[i]);
                    }

                    if (blocks[i] != IntPtr.Zero)
                        Marshal.FreeHGlobal(blocks[i]);
                }

                waveOutClose(device);
                device = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            stop = true;
            if (worker != null)
                worker.Join(1200);
        }
    }
}