using System;
using System.CodeDom;

namespace RTS {
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class Util {
        public const float PI = (float) Math.PI;


        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys vKey);
        private static Random _random =new Random();
        public static bool IsKeyDown(Keys key) {
            return (GetAsyncKeyState(key) & 0x8000) != 0;
        }
        public static void ReleaseCom<T>(ref T x) where T : class, IDisposable {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static int Rand(int max) { return _random.Next(max); }

        public static float RandF() { return (float)_random.NextDouble(); }

        public static T Clamp<T>(T val, T min, T max) where T:IComparable {
            if (val.CompareTo(min) < 0 ) return min;
            if (val.CompareTo(max) > 0) return max;
            return val;
        }

        public static float Cos(float angle) { return (float) Math.Cos(angle); }
        public static float Sin(float angle) { return (float)Math.Sin(angle); }
    }
}