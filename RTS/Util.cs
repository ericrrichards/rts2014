using System;

namespace RTS {
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class Util {
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
    }
}