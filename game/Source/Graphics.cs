using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using System.Text;

namespace Uncalled
{
    public static class GL
    {
        const string G = "opengl32.dll";
        [StructLayout(LayoutKind.Sequential)]
        public struct Pfd
        {
            public ushort size, version;
            public uint flags;
            public byte type, color, red, redShift, green, greenShift, blue, blueShift, alpha, alphaShift, accum, accR, accG, accB, accA, depth, stencil, aux, layer, reserved;
            public uint mask, visible, damage;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr h, IntPtr d);
        [DllImport("gdi32.dll")]
        static extern int ChoosePixelFormat(IntPtr d, ref Pfd p);
        [DllImport("gdi32.dll")]
        static extern bool SetPixelFormat(IntPtr d, int n, ref Pfd p);
        [DllImport("gdi32.dll")]
        public static extern bool SwapBuffers(IntPtr d);
        [DllImport(G)]
        static extern IntPtr wglCreateContext(IntPtr d);
        [DllImport(G)]
        public static extern bool wglMakeCurrent(IntPtr d, IntPtr r);
        [DllImport(G)]
        public static extern bool wglDeleteContext(IntPtr r);
        [DllImport(G)]
        static extern IntPtr wglGetProcAddress(string s);
        [DllImport(G)]
        public static extern void glViewport(int x, int y, int w, int h);
        [DllImport(G)]
        public static extern void glMatrixMode(int m);
        [DllImport(G)]
        public static extern void glLoadIdentity();
        [DllImport(G)]
        public static extern void glFrustum(double l, double r, double b, double t, double n, double f);
        [DllImport(G)]
        public static extern void glOrtho(double l, double r, double b, double t, double n, double f);
        [DllImport(G)]
        public static extern void glTranslatef(float x, float y, float z);
        [DllImport(G)]
        public static extern void glScalef(float x, float y, float z);
        [DllImport(G)]
        public static extern void glRotatef(float a, float x, float y, float z);
        [DllImport(G)]
        public static extern void glPushMatrix();
        [DllImport(G)]
        public static extern void glPopMatrix();
        [DllImport(G)]
        public static extern void glClearColor(float r, float g, float b, float a);
        [DllImport(G)]
        public static extern void glClear(int f);
        [DllImport(G)]
        public static extern void glEnable(int x);
        [DllImport(G)]
        public static extern void glDisable(int x);
        [DllImport(G)]
        public static extern void glDepthMask(byte enabled);
        [DllImport(G)]
        public static extern void glDepthFunc(int x);
        [DllImport(G)]
        public static extern void glBlendFunc(int x, int y);
        [DllImport(G)]
        public static extern void glGenTextures(int n, out uint id);
        [DllImport(G)]
        public static extern void glBindTexture(int target, uint id);
        [DllImport(G)]
        public static extern void glTexParameteri(int target, int p, int v);
        [DllImport(G)]
        public static extern void glTexImage2D(int t, int lev, int i, int w, int h, int b, int f, int type, IntPtr data);
        [DllImport(G)]
        public static extern void glTexSubImage2D(int t, int l, int x, int y, int w, int h, int f, int ty, IntPtr p);
        [DllImport(G)]
        public static extern void glEnableClientState(int v);
        [DllImport(G)]
        public static extern void glNormalPointer(int type, int stride, IntPtr ptr);
        [DllImport(G)]
        public static extern void glFinish();
        [DllImport(G)]
        public static extern void glInterleavedArrays(int fmt, int stride, IntPtr p);
        [DllImport(G)]
        public static extern void glDrawArrays(int m, int f, int c);
        [DllImport(G)]
        public static extern void glColor4f(float r, float g, float b, float a);
        [DllImport(G)]
        public static extern void glBegin(int m);
        [DllImport(G)]
        public static extern void glEnd();
        [DllImport(G)]
        public static extern void glTexCoord2f(float u, float v);
        [DllImport(G)]
        public static extern void glVertex2f(float x, float y);
        [DllImport(G)]
        public static extern void glFogi(int p, int v);
        [DllImport(G)]
        public static extern void glFogf(int p, float v);
        [DllImport(G)]
        public static extern void glFogfv(int p, float[] v);
        [DllImport(G)]
        public static extern void glReadPixels(int x, int y, int w, int h, int fmt, int type, IntPtr p);
        [DllImport(G)]
        public static extern IntPtr glGetString(int name);
        public static IntPtr Open(IntPtr hwnd, out IntPtr dc)
        {
            dc = GetDC(hwnd);
            var p = new Pfd();
            p.size = (ushort)Marshal.SizeOf(p);
            p.version = 1;
            p.flags = 0x25;
            p.color = 32;
            p.depth = 24;
            p.stencil = 8;
            int n = ChoosePixelFormat(dc, ref p);
            if (n == 0 || !SetPixelFormat(dc, n, ref p))
                throw new Exception("화면 형식을 만들 수 없습니다.");
            IntPtr rc = wglCreateContext(dc);
            if (rc == IntPtr.Zero || !wglMakeCurrent(dc, rc))
                throw new Exception("3D 화면을 시작할 수 없습니다.");
            return rc;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate uint Create(uint t);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate void Source(uint s, int n, IntPtr list, IntPtr lengths);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate void Single(uint s);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate void Pair(uint a, uint b);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate uint Make();
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate void Status(uint s, uint what, out int res);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate int Loc(uint p, [MarshalAs(UnmanagedType.LPStr)] string n);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate void Val(int loc, float f);
        static D Fn<D>(string n)
            where D : class
        {
            IntPtr f = wglGetProcAddress(n);
            if (f.ToInt64() < 5 && f.ToInt64() >= -1)
                return null;
            return Marshal.GetDelegateForFunctionPointer(f, typeof(D)) as D;
        }

        static Single use;
        static Val uniform;
        static uint program;
        static int lamp, brightness, depth, tone, expression, character, lighting,invW,invH,clockLight;
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]delegate void Active(int unit);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]delegate void SetI(int loc,int value);
        static Active active;
        public static bool LightSupport{get{return active!=null;}}
        public static void SceneLight(int mode,float time=0,uint map=0,int width=1,int height=1){
            if(!Shader)return;
            uniform(lighting,mode);uniform(clockLight,time);
            if(mode==1 && active!=null){
                active(0x84C1);glBindTexture(3553,map);active(0x84C0);
                uniform(invW,1f/(width*World.Unit));uniform(invH,1f/(height*World.Unit));
            }
        }
        public static void CharacterMaterial(float value){if(Shader)uniform(character,value);}
        [DllImport(G)] public static extern void glDrawElements(int mode,int count,int type,IntPtr indices);
        [DllImport(G)] public static extern void glAlphaFunc(int func,float value);
        [DllImport(G)] public static extern void glDeleteTextures(int count,ref uint name);

        public static void Face(float mode)
        {
            if (Shader)
                uniform(expression, mode);
        }

        public static bool Shader
        {
            get
            {
                return program != 0;
            }
        }

        public static void Programs()
        {
            try
            {
                var build = Fn<Create>("glCreateShader");
                if (build == null)
                    return;
                var source = Fn<Source>("glShaderSource");
                var compile = Fn<Single>("glCompileShader");
                var check = Fn<Status>("glGetShaderiv");
                var mk = Fn<Make>("glCreateProgram");
                var att = Fn<Pair>("glAttachShader");
                var link = Fn<Single>("glLinkProgram");
                var chk = Fn<Status>("glGetProgramiv");
                use = Fn<Single>("glUseProgram");
                uniform = Fn<Val>("glUniform1f");
                var loc = Fn<Loc>("glGetUniformLocation");
                uint pr = mk();
                string[] src =
                {
                    @"#version 120
 varying vec3 local;varying vec3 eye;varying vec3 smoothN;varying vec4 paint;varying vec2 uv;
 void main(){local=gl_Vertex.xyz;smoothN=gl_NormalMatrix*gl_Normal;vec4 p=gl_ModelViewMatrix*gl_Vertex;eye=p.xyz;paint=gl_Color;uv=gl_MultiTexCoord0.xy;gl_Position=gl_ProjectionMatrix*p;}",
                    @"#version 120
 uniform sampler2D tex;uniform float lamp;uniform float exposure;uniform float mist;uniform float warm;uniform float expression;uniform float character;uniform sampler2D lux;uniform float lighting,invW,invH,clockLight;
 varying vec3 local;varying vec3 eye;varying vec3 smoothN;varying vec4 paint;varying vec2 uv;
 void main(){float d=length(eye);float cone=dot(normalize(-eye+vec3(.0001)),normalize(vec3(.025,.022,1.)));float beam=smoothstep(.795,.972,cone)*(.57+.43*smoothstep(.958,.996,cone));float att=1./(1.+.043*d+.007*d*d);if(character>.5){
    vec4 albedo=texture2D(tex,uv); if(albedo.a<.055)discard;
    vec3 n=normalize(smoothN+.00000001);vec3 v=normalize(-eye);
    if(dot(n,v)<0.)n=-n;
    vec3 l=normalize(vec3(-.54,.72,.74));vec3 h=normalize(l+v);
    float diffuse=dot(n,l);float wrap=smoothstep(-.57,.90,diffuse);
    float rim=pow(1.-max(0.,dot(n,v)),3.);
    vec3 key=vec3(1.0,.90,.84);vec3 fill=vec3(.53,.58,.66);
    vec3 light=mix(fill,key,wrap)+vec3(.12,.16,.20)*rim+vec3(.14,.13,.12)*lamp*beam*att;
    if(character<1.5)light+=vec3(.045,.015,.010)*max(0.,1.-abs(diffuse));
    vec3 c=albedo.rgb*paint.rgb*light;
    if(character>1.5&&character<2.5)c+=vec3(.65,.66,.61)*pow(max(0.,dot(n,h)),85.);
    if(character>2.5&&character<3.5)c+=vec3(.067,.079,.095)*pow(max(0.,dot(n,h)),15.)+vec3(.031,.041,.05)*rim;
    c*=exposure; gl_FragColor=vec4(mix(c,vec3(.042,.059,.071),1.-exp(-mist*mist*d*d)),albedo.a);return;
    }float id=floor(uv.x*8.)+8.*floor(uv.y*16.);if(id==117.){
        vec2 s=(fract(uv*vec2(8.,16.))-.5)*2.;
        vec2 toe=(s-vec2(-.06,-.34))/vec2(.57,.57),heel=(s-vec2(.08,.51))/vec2(.44,.31);
        float a=(1.-smoothstep(.67,1.,min(dot(toe,toe),dot(heel,heel))))*(.67+.33*sin(s.y*38.));
        gl_FragColor=vec4(.51,.67,.64,a*paint.r*.65);return;
    }if(id==92.){vec2 st=(fract(uv*vec2(8.,16.))-.5)*2.;float a=clamp(1.-dot(st,st),0.,1.);gl_FragColor=vec4(.012,.023,.024,.50*a*a);return;}vec2 sampleUV=uv;if(id>=24.&&id<=26.&&expression>.5)sampleUV.y+=(expression<1.5?7.:8.)/16.;vec3 base=texture2D(tex,sampleUV).rgb;vec3 normal=normalize(cross(dFdx(eye),dFdy(eye))+.000001);if(dot(smoothN,smoothN)>.015)normal=normalize(smoothN);if(dot(normal,-eye)<0.)normal=-normal;float key=max(0.,dot(normal,normalize(vec3(-.62,.60,.53))));bool human=(id>=24.&&id<=33.)||id==36.||id==37.;vec3 c;if(human){float rim=pow(1.-abs(dot(normal,normalize(-eye))),3.);c=base*(paint.rgb*(.67+.41*key)+vec3(.055,.072,.09)*rim+lamp*beam*att*.055)*exposure;}else{c=base*(paint.rgb+lamp*beam*att*vec3(.56,.54,.43))*exposure;float floorMask=(id==0.||id==3.)?1.:0.;float shine=pow(max(0.,dot(reflect(-normalize(vec3(-.22,.58,.79)),normal),normalize(-eye))),16.);c+=vec3(.077,.11,.115)*shine*floorMask;}
    if(id==111.||id==113.){
        vec3 cloth=id==111.?vec3(.112,.132,.142):vec3(.162,.139,.121);
        float nap=sin(local.y*331.+local.x*14.)*.008;
        float edge=pow(1.-abs(dot(normal,normalize(-eye))),3.);
        c=(cloth+nap)*(paint.rgb*(.41+.56*key)+(.35*lamp*beam*att))+
            vec3(.026,.043,.047)*edge;
    }
    if(id==110.||id==112.){
        vec2 inner=(floor(uv*vec2(8.,16.))+.18+.64*fract(uv*vec2(8.,16.)))/vec2(8.,16.);
        vec3 printed=texture2D(tex,inner,-1.1).rgb;
        c=mix(vec3(.41,.382,.313),min(printed,vec3(.63,.61,.53)),.70)*(.43+.6*key+.21*lamp*beam*att)*paint.rgb;
    }
    if(lighting>.5 && !human){
        vec3 lit=lighting<1.5?texture2D(lux,local.xz*vec2(invW,invH)).rgb:vec3(.22+.65*pow(max(0.,cos((local.z-.25)*1.0472)),4.)/(1.+.19*local.x*local.x),.04,.66/(1.+.18*pow(local.x+4.3,2.)));
        float lampPool=lit.r;float occlusion=lit.g;float window=lit.b;
        float trim=clamp(.88+.10*local.y,.82,1.04);
        vec3 indirect=vec3(.18,.239,.31)+lampPool*vec3(.72,.65,.49)+window*vec3(.21,.37,.55);
        vec3 plaster=base*(mix(paint.rgb,vec3(.83),.61)*indirect*trim*(1.-occlusion));
        vec3 flash=base*lamp*beam*att*vec3(.53,.48,.36);
        bool glass=id>=96.&&id<=99.;
        if(glass){
            vec2 st=fract(uv*vec2(8.,16.));
            float rain=pow(max(0.,sin(st.x*743.+6.*sin(st.x*93.))),31.)*pow(max(0.,cos(st.y*13.+clockLight*(1.5+fract(st.x*7.)))),9.);
            c=(base*vec3(.86,.99,1.13)+vec3(.05,.073,.095)*rain+lamp*beam*att*.044)*exposure;
        }else if(id==39.) c=base*(1.17+.24*lampPool)*vec3(1.,.95,.83);
        else{
            if(id==0.||id==3.){
                float strip=smoothstep(.10,.16,abs(fract((local.z+local.x*.59)*.285)-.5));
                plaster+=vec3(.041,.068,.095)*window*strip;
                float highlight=pow(max(0.,dot(reflect(-normalize(vec3(-.25,.57,.77)),normal),normalize(-eye))),19.);
                plaster+=vec3(.111,.13,.135)*highlight*(window+lampPool*.55);
            }
            c=(plaster+flash)*exposure;
        }
    }
    float fog=1.-exp(-mist*mist*d*d);c=mix(c,vec3(.051+.026*warm,.073,.081-.021*warm),fog);gl_FragColor=vec4(c,1.);}"
                };
                for (int i = 0; i < 2; i++)
                {
                    uint sh = build((uint)(i == 0 ? 0x8B31 : 0x8B30));
                    IntPtr ss = Marshal.StringToHGlobalAnsi(src[i]), ls = Marshal.AllocHGlobal(IntPtr.Size);
                    Marshal.WriteIntPtr(ls, ss);
                    source(sh, 1, ls, IntPtr.Zero);
                    Marshal.FreeHGlobal(ls);
                    Marshal.FreeHGlobal(ss);
                    compile(sh);
                    int ok;
                    check(sh, 0x8B81, out ok);
                    if (ok == 0)
                        return;
                    att(pr, sh);
                }

                link(pr);
                int good;
                chk(pr, 0x8B82, out good);
                if (good != 1)
                    return;
                program = pr;
                lamp = loc(pr, "lamp");
                brightness = loc(pr, "exposure");
                depth = loc(pr, "mist");
                tone = loc(pr, "warm");
                expression = loc(pr, "expression");
                character = loc(pr, "character");
                lighting=loc(pr,"lighting");invW=loc(pr,"invW");invH=loc(pr,"invH");clockLight=loc(pr,"clockLight");
                active=Fn<Active>("glActiveTexture");
                if(active!=null){use(pr);Fn<SetI>("glUniform1i")(loc(pr,"lux"),1);use(0);}
            }
            catch
            {
                program = 0;
            }
        }

        public static void Shade(float light, float bright, float fog, float warm)
        {
            if (Shader)
            {
                use(program);
                uniform(lamp, light);
                uniform(brightness, bright);
                uniform(depth, fog);
                uniform(tone, warm);
            }
            else
            {
                glEnable(0x0B60);
                glFogi(0x0B65, 0x0801);
                glFogf(0x0B62, fog);
                glFogfv(0x0B66, new[] { .055f, .075f, .08f, 1f });
            }
        }

        public static void Ui()
        {
            if (Shader)
                use(0);
            glDisable(0x0B60);
        }

        public static uint Texture(Bitmap source, bool mip)
        {
            uint id;
            glGenTextures(1, out id);
            glBindTexture(3553, id);
            glTexParameteri(3553, 10241, mip ? 9987 : 9729);
            glTexParameteri(3553, 10240, 9729);
            glTexParameteri(3553, 10242, 33071);
            glTexParameteri(3553, 10243, 33071);
            int w = source.Width, h = source.Height, lev = 0;
            while (true)
            {
                using (var b = new Bitmap(w, h, PixelFormat.Format32bppArgb))
                {
                    using (Graphics gg = Graphics.FromImage(b))
                    {
                        gg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                        gg.DrawImage(source, new Rectangle(0, 0, w, h));
                    }

                    var d = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    glTexImage2D(3553, lev, 0x1908, w, h, 0, 0x80E1, 5121, d.Scan0);
                    b.UnlockBits(d);
                }

                if (!mip || (w == 1 && h == 1))
                    break;
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);
                lev++;
            }

            return id;
        }

        public static void Upload(uint id, Bitmap b)
        {
            glBindTexture(3553, id);
            var d = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            glTexSubImage2D(3553, 0, 0, 0, b.Width, b.Height, 0x80E1, 5121, d.Scan0);
            b.UnlockBits(d);
        }
    }

    public struct Pt
    {
        public float x, y, z;
        public Pt(float a, float b, float c)
        {
            x = a;
            y = b;
            z = c;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex
    {
        public float u, v;
        public uint color;
        public float x, y, z, nx, ny, nz;
    }

    public class Mesh : IDisposable
    {
        uint[] baked;
        public Vertex[] verts;
        GCHandle handle;
        public int Count
        {
            get
            {
                return verts == null ? 0 : verts.Length;
            }
        }

        public Mesh(List<Vertex> v)
        {
            verts = v.ToArray();
            if (verts.Length > 0)
                handle = GCHandle.Alloc(verts, GCHandleType.Pinned);
        }

        public void Draw()
        {
            if (Count == 0)
                return;
            IntPtr ptr = handle.AddrOfPinnedObject();
            GL.glInterleavedArrays(0x2A29, 36, ptr);
            GL.glEnableClientState(0x8075);
            GL.glNormalPointer(5126, 36, IntPtr.Add(ptr, 24));
            GL.glDrawArrays(7, 0, verts.Length);
        }

        public void DrawLit(float px, float py, float pz, float yaw, float pitch, bool torch, float exposure)
        {
            if (GL.Shader)
            {
                Draw();
                return;
            }

            if (baked == null)
            {
                baked = new uint[Count];
                for (int i = 0; i < Count; i++)
                    baked[i] = verts[i].color;
            }

            float sx = (float)(Math.Sin(yaw) * Math.Cos(pitch)), sy = (float)Math.Sin(pitch), sz = (float)(Math.Cos(yaw) * Math.Cos(pitch));
            for (int i = 0; i < Count; i++)
            {
                float dx = verts[i].x - px, dy = verts[i].y - py, dz = verts[i].z - pz;
                float sq = dx * dx + dy * dy + dz * dz, beam = 0;
                if (torch && sq < 625)
                {
                    float dis = (float)Math.Sqrt(sq + .001f);
                    float dot = (dx * sx + dy * sy + dz * sz) / dis;
                    beam = .6f * Math.Max(0, Math.Min(1, (dot - .67f) / .28f)) / (1 + dis * .06f + sq * .008f);
                }

                uint c = baked[i];
                verts[i].color = Builder.Col(exposure * ((c & 255) / 255f + beam), exposure * (((c >> 8) & 255) / 255f + beam * .93f), exposure * (((c >> 16) & 255) / 255f + beam * .77f));
            }

            Draw();
        }

        public void Dispose()
        {
            if (handle.IsAllocated)
                handle.Free();
            verts = null;
        }
    }

    public class Builder
    {
        public List<Vertex> list = new List<Vertex>();
        public World terrain;
        public Pt normal;
        public bool indoor = false;
        public int palette = 0;
        public float baseLight = 1;
        public static uint Col(float r, float g, float b)
        {
            return (uint)(Math.Max(0, Math.Min(255, (int)(r * 255))) | (Math.Max(0, Math.Min(255, (int)(g * 255))) << 8) | (Math.Max(0, Math.Min(255, (int)(b * 255))) << 16)) | 0xff000000u;
        }

        public static Pt P(float x, float y, float z)
        {
            return new Pt(x, y, z);
        }

        public void Vert(Pt p, int mat, float u, float v, float shade)
        {
            if (mat >= 27 && mat <= 39)
            {
                u = .26f + .48f * u;
                v = .26f + .48f * v;
            }

            float r = 1, g = 1, b = 1, br = shade * baseLight;
            if (indoor)
            {
                float wave = (float)(.5 + .5 * Math.Cos(p.z * .57 + p.x * .33)), lamp = (float)(.5 + .5 * Math.Cos(p.x * .82 - p.z * .25));
                br *= .27f + .39f * wave + .17f * lamp + (p.y > 2.8f ? .035f : 0);
                r = .88f;
                g = .98f;
                b = .98f;
                if (palette == 3)
                {
                    r = 1.17f;
                    g = .83f;
                    b = .79f;
                }
                else if (palette == 5 || palette == 11)
                {
                    r = 1.05f;
                    g = .93f;
                    b = .87f;
                }
                else if (palette == 8)
                {
                    r = .94f;
                    g = .90f;
                    b = 1.02f;
                }
            }

            list.Add(new Vertex { u = ((mat % 8) + .006f + u * .988f) / 8, v = ((mat / 8) + .006f + v * .988f) / 16, color = Col(br * r, br * g, br * b), x = p.x, y = p.y + (terrain == null ? 0 : terrain.Elevation(p.z)), z = p.z, nx = normal.x, ny = normal.y, nz = normal.z });
        }

        public void Q(Pt a, Pt b, Pt c, Pt d, int m, float l)
        {
            Vert(a, m, 0, 1, l);
            Vert(b, m, 1, 1, l);
            Vert(c, m, 1, 0, l);
            Vert(d, m, 0, 0, l);
        }

        void BQ(Pt a, Pt b, Pt c, Pt d, int m, float l)
        {
            Q(b, a, d, c, m, l);
        }

        public void Box(float x, float y, float z, float sx, float sy, float sz, int m, float light = 1)
        {
            float l = x - sx / 2, r = x + sx / 2, b = y - sy / 2, t = y + sy / 2, n = z - sz / 2, f = z + sz / 2;
            BQ(P(l, b, f), P(r, b, f), P(r, t, f), P(l, t, f), m, light);
            BQ(P(r, b, n), P(l, b, n), P(l, t, n), P(r, t, n), m, light * .91f);
            BQ(P(l, b, n), P(l, b, f), P(l, t, f), P(l, t, n), m, light * .83f);
            BQ(P(r, b, f), P(r, b, n), P(r, t, n), P(r, t, f), m, light * .92f);
            BQ(P(l, t, f), P(r, t, f), P(r, t, n), P(l, t, n), m, light * 1.05f);
            BQ(P(l, b, n), P(r, b, n), P(r, b, f), P(l, b, f), m, light * .68f);
        }

        public void Lathe(float cx, float cz, float[] yy, float[] xx, float[] zz, int mat, int sides = 12, float brightness = 1, bool pleat = false)
        {
            for (int j = 0; j < yy.Length - 1; j++)
                for (int k = 0; k < sides; k++)
                {
                    float a = (float)(2 * Math.PI * k / sides), b = (float)(2 * Math.PI * (k + 1) / sides);
                    float sa = (float)Math.Sin(a), ca = (float)Math.Cos(a), sb = (float)Math.Sin(b), cb = (float)Math.Cos(b);
                    float p = pleat && k % 2 == 1 ? .91f : 1, q = pleat && (k + 1) % 2 == 1 ? .91f : 1;
                    float l = (.86f + .14f * (float)Math.Cos(a - .6)) * brightness;
                    Q(P(cx + sa * xx[j] * p, yy[j], cz + ca * zz[j] * p), P(cx + sb * xx[j] * q, yy[j], cz + cb * zz[j] * q), P(cx + sb * xx[j + 1] * q, yy[j + 1], cz + cb * zz[j + 1] * q), P(cx + sa * xx[j + 1] * p, yy[j + 1], cz + ca * zz[j + 1] * p), mat, l);
                }
        }

        public void Oval(float x, float y, float z, float rx, float ry, float rz, int mat, float light = 1, int sides = 12)
        {
            int rings = 8;
            float[] yy = new float[rings + 1], xx = new float[rings + 1], zz = new float[rings + 1];
            for (int j = 0; j <= rings; j++)
            {
                double a = (j / (double)rings - .5) * Math.PI;
                yy[j] = y + ry * (float)Math.Sin(a);
                xx[j] = rx * (float)Math.Cos(a);
                zz[j] = rz * (float)Math.Cos(a);
            }

            Lathe(x, z, yy, xx, zz, mat, sides, light);
        }

        public Mesh End()
        {
            return new Mesh(list);
        }
    }
}